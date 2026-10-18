using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    /// <summary>
    /// 車両の同期。オーナーは target の Unity 座標を論理 ECEF へ写し(chunk + 1cm 細部)、
    /// 非オーナーは速度+加速度で外挿した論理位置をローカルプレイヤーセル上の Unity 座標へ解決する。
    /// V2(FDMiRelativeObjectSync) の外挿器を ECEF フレームで再利用する。
    /// 重力は sync の責務外(FDMi.dynamics が ECEF 位置から方向を決める)。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiGeodeticObjectSync : FDMiGeodeticReferencePoint
    {
        public Transform target;

        void Start()
        {
            if (!target)
                target = body ? body.transform : transform;
            if (body)
                body.useGravity = false;
            CaptureLogicalFromTarget();
            ResetSyncStuff();
            ResetSyncTime();
        }

        /// <summary>target の Unity 座標を、絶対値を float で作らずに論理 ECEF へ写す。</summary>
        void CaptureLogicalFromTarget()
        {
            setBaseChunk(syncManager.localPlayerChunk);
            setPosition(syncManager.ToEcef(target.position));
            setRotation(syncManager.ToEcefRot(target.rotation));
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                ResetSyncTime();
                whenSynced();
            }
        }

        public override void windupPositionAndRotation()
        {
            if (!Networking.IsOwner(gameObject)) ExtrapolationAndSmoothing();
            transform.position = syncManager.ToUnity(getViewPosition());
            transform.rotation = syncManager.ToUnityRot(getViewRotation());
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
            }
        }

        void FixedUpdate()
        {
            if (!isInit) return;
            if (!Networking.IsOwner(gameObject) || (body && body.isKinematic)) return;
            if (!isRoot || !body) return;
            PinRootBodyToFrame();
        }

        /// <summary>
        /// root 車体(= フレームそのもの)を Unity 原点・無回転に固定し続け、物理が動かした分
        /// (位置と回転)を論理姿勢へ累算する。世界と他の物体は毎フレーム root からの相対で
        /// 描き直されるので、root は常に動かない基準であり続ける。
        /// 姿勢も累算するため、機体のローカル軸は常にフレーム軸と一致し、重力・空力は
        /// `FrameRotationEcef` を通して body ローカルで扱える(dynamics 側の入力)。
        /// velocity を `Inverse(br)` で回すのは、フレーム(世界の見え方)が br だけ回った分を
        /// 速度の向きに反映させるため。ここを省くと旋回中に速度ベクトルが世界からずれる。
        /// </summary>
        public void PinRootBodyToFrame()
        {
            Quaternion br = body.rotation;
            setRotation((_rotation * br).normalized);
            setPosition(syncManager.ToEcef(body.position) + _position);
            body.position = Vector3.zero;
            body.rotation = Quaternion.identity;
            body.velocity = Quaternion.Inverse(br) * body.velocity;
        }

        public void Update()
        {
            if (!isInit || stopUpdate) return;
            // body が kinematic(Sacc 車両等の transform 駆動)でも target 座標は真なので capture する
            if (Networking.IsOwner(gameObject))
            {
                CaptureAndPack();
                TrySerialize();
                return;
            }
            // 非オーナーは受信値から外挿した論理位置を自分のフレーム上の座標へ解決して描く
            windupPositionAndRotation();
        }

        /// <summary>
        /// オーナー側の論理座標 capture。target の Unity 座標(world)をそのまま論理 ECEF へ写すので、
        /// transform 階層上の親には依存しない。root 車体の位置は FixedUpdate が累算するため触らない。
        /// </summary>
        void CaptureAndPack()
        {
            if (isRoot)
            {
                // 位置は FixedUpdate、姿勢と速度は body が真(世界フレームの姿勢のまま)
                if (body)
                {
                    setRotation(syncManager.ToEcefRot(body.rotation));
                    _velocity = syncManager.ToEcef(body.velocity);
                }
            }
            else
            {
                CaptureLogicalFromTarget();
                _velocity = syncManager.ToEcef(body ? body.velocity : Vector3.zero);
            }
            ownerSyncedTime = ServerTimeDiff + Time.time;
        }

        /// <summary>SeatAnchor / SaccBridge 統合面。force で interval 待ちをスキップする。</summary>
        public void CaptureAndSend(bool force)
        {
            // 所有権は意味的イベント(座席 Interact / Sacc の O イベント)で取得済みであること。
            // ここで SetOwner すると、全クライアント配送の G イベントで非オーナーが所有権を奪う。
            if (!Networking.IsOwner(gameObject)) return;
            CaptureAndPack();
            if (force)
            {
                ResetSyncTime();
                ForceSerialize();
            }
            else
            {
                TrySerialize();
            }
        }

        #region Sync payload extension
        [UdonSynced, HideInInspector] public Vector3 syncedVel;
        private Vector3 lastSentVel;

        protected override bool ExtraPayloadChanged()
        {
            return (lastSentVel - _velocity).sqrMagnitude > 1e-6f;
        }

        protected override void WriteSyncedExtra()
        {
            syncedVel = _velocity;
            lastSentVel = _velocity;
        }
        #endregion

        #region Interpolated
        [Tooltip("How quickly to lerp rotation to new extrapolated target rotation, it might help to reduce this in high-lag situations with planes that can roll quickly")]
        public float RotationSyncAgressiveness = 10f;
        [Tooltip("Maximum amount of extrapolation for high ping players, will brake formation flying for high ping players if set lower than their ping = 0.1 = 100ms, useful for dogfight worlds")]
        public float MaxPingExtrapolationInSeconds = 0.1f;
        private bool deserialized = false;
        private float SmoothingTimeDivider;
        [UdonSynced, FieldChangeCallback(nameof(O_UpdateTime)), HideInInspector] public double ownerSyncedTime;
        public double O_UpdateTime
        {
            get => ownerSyncedTime;
            set
            {
                if (ownerSyncedTime != value)
                {
                    ownerSyncedTime = value;
                    deserialized = true;
                }
            }
        }

        private void ResetSyncStuff()
        {
            PackFineToSynced();
            lastSyncedChunk = _chunk;
            lastSyncedFineM = syncedFineM;
            L_PingAdjustedPosition = syncedFineM;
            O_LastRotation = _rotation;
            syncedRot = _rotation;
            O_LastUpdateTime = L_UpdateTime = lastframetime_extrap = Networking.GetServerTimeInMilliseconds();
            O_LastUpdateTime -= updateInterval;
            SmoothingTimeDivider = 1f / updateInterval;
        }

        void PackFineToSynced()
        {
            syncedFineX = (short)Mathf.RoundToInt(_position.x / FineUnitM);
            syncedFineY = (short)Mathf.RoundToInt(_position.y / FineUnitM);
            syncedFineZ = (short)Mathf.RoundToInt(_position.z / FineUnitM);
            syncedChunk = _chunk;
        }

        double lastframetime_extrap;
        private void ExtrapolationAndSmoothing()
        {
            if (deserialized)
            {
                deserialized = false;
                whenSynced();
            }
            float deltatime = Time.deltaTime;
            double time;
            if (deltatime > .099f)
            {
                time = Networking.GetServerTimeInSeconds();
                deltatime = (float)(time - lastframetime_extrap);
                ResetSyncTime();
            }
            else { time = ServerTimeDiff + Time.time; }
            lastframetime_extrap = Networking.GetServerTimeInSeconds();
            float TimeSinceUpdate = (float)(time - L_UpdateTime);

            Vector3 PredictedPosition = L_PingAdjustedPosition + (ExtrapolationDirection * TimeSinceUpdate);
            Quaternion PredictedRotation = (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate) * syncedRot);

            if (TimeSinceUpdate < updateInterval)
            {
                float TimeSincePrevUpdate = (float)(time - lastUpdateTime);
                Vector3 OldPredictedPosition = L_LastPingAdjustedPosition + (LastExtrapolationDirection * TimeSincePrevUpdate);
                Quaternion OldPredictedRotation = (Quaternion.SlerpUnclamped(Quaternion.identity, LastCurAngMom, LastPing + TimeSincePrevUpdate) * O_LastRotation2);

                _rotation = Quaternion.Slerp(_rotation,
                  Quaternion.Slerp(OldPredictedRotation, PredictedRotation, TimeSinceUpdate * SmoothingTimeDivider), Time.smoothDeltaTime * RotationSyncAgressiveness);

                _position = Vector3.Lerp(OldPredictedPosition, PredictedPosition, (float)TimeSinceUpdate * SmoothingTimeDivider);
            }
            else
            {
                _rotation = Quaternion.Slerp(_rotation, PredictedRotation, Time.smoothDeltaTime * SmoothingTimeDivider);
                _position = PredictedPosition;
            }
        }

        private double O_LastUpdateTime, lastUpdateTime, L_UpdateTime;
        private Vector3 Acceleration, LastAcceleration;
        private float Ping, LastPing;
        private Quaternion CurAngMom, LastCurAngMom, CurAngMomAcceleration;

        private Vector3 ExtrapolationDirection, LastExtrapolationDirection;
        private Vector3 L_PingAdjustedPosition = Vector3.zero, L_LastPingAdjustedPosition = Vector3.zero;
        private Vector3 lastSyncedFineM, lastVelocity;
        private Vector3Int lastSyncedChunk;
        private Quaternion O_LastRotation, O_LastRotation2;

        /// <summary>
        /// 受信処理。送信側の chunk と細部を、そのままの 2 段構造で受け取る。
        /// _position は送信側の現在 chunk 基準へ揃え、描画時に getViewPosition が
        /// ローカルプレイヤーの chunk との差を取る。
        /// </summary>
        private void whenSynced()
        {
            LastAcceleration = Acceleration;
            LastPing = Ping;
            lastUpdateTime = L_UpdateTime;
            LastCurAngMom = CurAngMom;

            L_UpdateTime = ServerTimeDiff + Time.time;
            // 片方向遅延の推定。クランプは上下両側(負なら外挿せず、過去へ飛ばさない)
            Ping = Mathf.Clamp((float)(L_UpdateTime - O_UpdateTime), 0f, MaxPingExtrapolationInSeconds);

            Vector3Int syncedChunkDiff = syncedChunk - lastSyncedChunk;
            Vector3 syncedChunkDiffM = syncManager.MulChunk(syncedChunkDiff);
            _chunk = syncedChunk;

            //time between this update and last
            float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime);
            float speednormalizer = 1 / updatedelta;

            bool SetVelZero = false;
            if (syncedVel.sqrMagnitude == 0)
            {
                if (lastVelocity.sqrMagnitude != 0)
                { _velocity = Vector3.zero; SetVelZero = true; }
                else
                { _velocity = (syncedChunkDiffM + syncedFineM - lastSyncedFineM) * speednormalizer; }
            }
            else
            { _velocity = syncedVel; }
            Acceleration = (_velocity - lastVelocity);//acceleration is difference in velocity

            //rotate Acceleration by the difference in rotation of vehicle between last and this update to make it match the angle for the next update better
            Quaternion PlaneRotDif = syncedRot * Quaternion.Inverse(O_LastRotation);
            Acceleration = (PlaneRotDif * Acceleration) * .5f;
            Acceleration += Acceleration * (Ping / updateInterval);

            //current angular momentum as a quaternion
            CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);
            CurAngMomAcceleration = CurAngMom * Quaternion.Inverse(LastCurAngMom);

            //if direction of acceleration changed by more than 90 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            //+ if idlemode, disable acceleration because it brakes
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0 || SetVelZero || syncedVel.magnitude < IdleMovementRange)
            { Acceleration = Vector3.zero; CurAngMomAcceleration = Quaternion.identity; }

            LastExtrapolationDirection = ExtrapolationDirection;
            ExtrapolationDirection = _velocity + Acceleration;
            L_LastPingAdjustedPosition = L_PingAdjustedPosition;
            L_PingAdjustedPosition = syncedFineM + (ExtrapolationDirection * Ping);

            L_LastPingAdjustedPosition -= syncedChunkDiffM;
            _position -= syncedChunkDiffM;

            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation2 = O_LastRotation;
            O_LastRotation = syncedRot;
            lastSyncedFineM = syncedFineM;
            lastVelocity = _velocity;
            lastSyncedChunk = syncedChunk;
        }

        #endregion

        public void LateUpdate()
        {
            if (!Networking.IsOwner(gameObject)) ExtrapolationAndSmoothing();
            if (stopUpdate || isRoot) return;
            transform.rotation = syncManager.ToUnityRot(getViewRotation());
            transform.position = syncManager.ToUnity(getViewPosition());
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
            }
        }
    }
}
