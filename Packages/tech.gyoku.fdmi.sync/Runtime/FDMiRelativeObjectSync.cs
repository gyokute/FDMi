using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using FDMi.core;

namespace FDMi.sync
{
    public class FDMiRelativeObjectSync : FDMiReferencePoint
    {
        [UdonSynced] public int[] syncedPosition = new int[3];
        [UdonSynced] public Vector3 syncedVel;
        [UdonSynced] public short[] syncedRotation = new short[4];
        [UdonSynced] public short[] syncedAngularVel = new short[3];
        [UdonSynced] public short[] syncedChunkPosition = new short[3];

        // Why-not: getViewPosition との差では C 更新分が A に混入するため pin 値との差で積分する。
        protected Vector3 pinnedPos = Vector3.zero;

        public override void initReferencePoint()
        {
            base.initReferencePoint();
            body.useGravity = false;

            isRoot = false;
            setPositionAbsolute(body.position);
            setRotation(body.rotation);
            ResetSyncStuff();
            transform.SetParent(null);
            pinnedPos = body.position;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                ResetSyncTime();
                whenSynced();
            }
            if (Networking.IsOwner(gameObject)) TrySerialize();
        }
        public override void windupPositionAndRotation()
        {
            if (!Networking.IsOwner(gameObject)) whenSynced();
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
                body.transform.position = transform.position;
                body.transform.rotation = transform.rotation;
            }
            // Why-not: base を呼ばない override で pin し直さないと切換時の bp - pinnedPos が旧 C との差になり A が飛ぶ。
            pinnedPos = getViewPosition();
        }

        private Vector3 tentativeVel;
        public void SetLocalPlayerAsOwner()
        {
            tentativeVel = _velocity;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            SendCustomEventDelayedFrames(nameof(RevertTentativeVelocity), 3);
        }
        public void RevertTentativeVelocity()
        {
            _velocity = tentativeVel;
            body.velocity = Quaternion.Inverse(_rotation) * _velocity;
        }
        void FixedUpdate()
        {
            if (!isInit) return;
            if (Networking.IsOwner(gameObject) && !body.isKinematic)
            {
                if (isRoot)
                {
                    Quaternion qOld = _rotation;
                    Vector3 bp = body.position;
                    Quaternion br = body.rotation;
                    setRotation((qOld * br).normalized);
                    // Why-not: getViewPosition との差ではなく pin 値との差を旧回転で回して自 fine に足す。view は C 更新で動くため ΔC が混入する。
                    setPosition(_position + qOld * (bp - pinnedPos));
                    body.position = getViewPosition();
                    body.rotation = Quaternion.identity;
                    body.velocity = Quaternion.Inverse(br) * body.velocity;
                    pinnedPos = body.position;
                }
                else
                {
                    FDMiReferencePoint R = rootRefPoint;
                    Quaternion qr = R._rotation;
                    Vector3 C = syncManager.localCell;
                    Vector3 bp = body.position;
                    Quaternion br = body.rotation;
                    _chunkPosition = R._chunkPosition;
                    _posMm[0] = R._posMm[0];
                    _posMm[1] = R._posMm[1];
                    _posMm[2] = R._posMm[2];
                    // Why-not: setPosition は上書きのため R._position を落とすと fine が消え total が A_R + C + Q_R·bp にならない。
                    setPosition(R._position + C + qr * bp);
                    // Why-not: br は view 姿勢 (getViewRotation) なので絶対姿勢へは Q_R を掛けて戻す。
                    // Inverse(Q_R) を掛けると毎フレーム Q_R⁻² が前置され姿勢が回り続ける。
                    setRotation((qr * br).normalized);
                    body.rotation = getViewRotation();
                    body.position = getViewPosition();
                }
            }
            else
            {
                if (isRoot) return;
                body.rotation = getViewRotation();
                body.position = getViewPosition();
                body.transform.position = getViewPosition();
                body.transform.rotation = getViewRotation();
            }
        }

        public void Update()
        {
            if (!isInit || stopUpdate) return;
            if (Networking.IsOwner(gameObject))
            {
                if (Networking.IsOwner(body.gameObject) && isRoot && body.isKinematic) body.isKinematic = false;
                if (isRoot)
                {
                    _velocity = _rotation * body.velocity;
                }
                else if (parentIsRoot)
                {
                    _velocity = parentRefPoint._rotation * body.velocity;
                }
                else
                {
                    _velocity = Vector3.zero;
                }
                // Serialize
                TrySerialize();
            }
            if (!Networking.IsOwner(gameObject))
            {
                ExtrapolationAndSmoothing();
                if (Networking.IsOwner(body.gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
                if (!Networking.IsOwner(body.gameObject))
                {
                    if (!body.isKinematic) body.isKinematic = true;
                }
            }
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();

        }

        public override void OnDeserialization()
        {
            whenSynced();
        }


        #region Interpolated
        protected override void TrySerialize()
        {
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                bool changed = Vector3.Distance(prevPostedPos, _position) > 0.01f || Quaternion.Angle(prevPostedRot, _rotation) > 0.75f;
                for (int i = 0; i < 3; i++)
                {
                    int mm = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                    int ch = (int)_chunkPosition[i];
                    if (mm >= MM_PER_CHUNK) { mm -= MM_PER_CHUNK; ch += 1; }
                    else if (mm <= -MM_PER_CHUNK) { mm += MM_PER_CHUNK; ch -= 1; }
                    if (mm != syncedPosition[i] || (short)ch != syncedChunkPosition[i]) changed = true;
                    syncedPosition[i] = mm;
                    // Why-not: chunk を short に収める。±32767 chunk を超える遠方は送れない。
                    if (ch > 32767) ch = 32767;
                    else if (ch < -32767) ch = -32767;
                    syncedChunkPosition[i] = (short)ch;
                }
                if (!changed) return;
                syncedRotation = PackQuaternion(_rotation);
                syncedAngularVel = PackOmega(body.angularVelocity);
                syncedVel = _velocity;
                RequestSerialization();
                prevPostedPos = _position;
                prevPostedRot = _rotation;
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        private double lastSyncedTime;
        private float localSyncedTime, syncedInterval = 100000f;
        private Vector3 lastVel, lastPos;
        private Vector3 prevPostedPos = Vector3.zero;
        private Quaternion prevPostedRot = Quaternion.identity;
        private int[] lastSyncedMm = new int[3];
        private Vector3 lastSyncedChunk = Vector3.zero;
        private Vector3 syncTargetM = Vector3.zero;

        // rotation MEKF filter variables
        Quaternion _syncedRot;
        private Vector3 _syncedOmega, _omega, _alpha;
        [SerializeField] private float K_theta = 1.0f;

        private void ResetSyncStuff()
        {
            for (int i = 0; i < 3; i++)
            {
                int mm = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                int ch = (int)_chunkPosition[i];
                if (mm >= MM_PER_CHUNK) { mm -= MM_PER_CHUNK; ch += 1; }
                else if (mm <= -MM_PER_CHUNK) { mm += MM_PER_CHUNK; ch -= 1; }
                if (ch > 32767) ch = 32767;
                else if (ch < -32767) ch = -32767;
                syncedPosition[i] = mm;
                lastSyncedMm[i] = mm;
                syncedChunkPosition[i] = (short)ch;
            }
            lastSyncedChunk = _chunkPosition;
            syncTargetM = _position;
            _velocity = Vector3.zero;
            lastVel = Vector3.zero;
            lastPos = _position;
            syncedRotation = PackQuaternion(_rotation);
            _syncedRot = _rotation;
            _omega = Vector3.zero;
        }

        private void ExtrapolationAndSmoothing()
        {
            float interpFactor = (Time.time - localSyncedTime) / syncedInterval;
            // update position
            Vector3 predPosFromSync = Vector3.Lerp(lastPos, syncTargetM, interpFactor);
            _velocity = Vector3.Slerp(lastVel, syncedVel, interpFactor);
            Vector3 predPosFromVel = _position + _velocity * Time.deltaTime;
            _position = Vector3.Lerp(predPosFromSync, predPosFromVel, 0.85f);
            // update rotation
            _rotation = PredictAttitude(_rotation, _omega, Time.deltaTime);
            _rotation = Quaternion.Slerp(_rotation, _syncedRot, K_theta * Time.deltaTime);
            _omega += _alpha * Time.deltaTime;
        }

        private void whenSynced()
        {
            if (!isInit) return;
            // time
            double prevSyncedTime = lastSyncedTime;
            lastSyncedTime = Networking.GetServerTimeInSeconds();
            syncedInterval = (float)Networking.CalculateServerDeltaTime(lastSyncedTime, prevSyncedTime);
            syncedInterval = Mathf.Max(syncedInterval, updateInterval);
            localSyncedTime = Time.time;
            // Why-not: 粗部にスナップし残差は捨てる。Δを残差にも畳むと総位置が二重計上になる。
            lastVel = _velocity;
            if (!Networking.IsOwner(gameObject))
            {
                for (int i = 0; i < 3; i++)
                {
                    lastSyncedMm[i] = syncedPosition[i];
                    _posMm[i] = syncedPosition[i];
                    lastSyncedChunk[i] = syncedChunkPosition[i];
                    _chunkPosition[i] = syncedChunkPosition[i];
                }
                _position = Vector3.zero;
                syncTargetM = Vector3.zero;
                lastPos = Vector3.zero;
            }
            // rotation
            Vector3 pSyncedOmega = _syncedOmega;
            _syncedRot = UnpackQuaternion(syncedRotation);
            _syncedOmega = UnpackOmega(syncedAngularVel);
            // update state
            _alpha = (_syncedOmega - pSyncedOmega) / syncedInterval;
            _omega = pSyncedOmega;
        }

        #endregion

        # region Attitude Filter
        // Predict step
        static Quaternion PredictAttitude(Quaternion q, Vector3 omega, float dt)
        {
            // Exterpolate attitude
            Quaternion q_dt = Quaternion.identity;
            Vector3 angle = omega * dt;
            float angleRad = angle.magnitude;
            if (angleRad > 1e-5f)
            {
                q_dt = Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, angle.normalized);
            }
            return (q * q_dt).normalized;
        }

        #endregion

        #region Sync Utility
        public static short[] PackOmega(Vector3 omega)
        {
            // 0.0005rad/s, 0.0286deg/s per 1 input
            short[] input = new short[3];
            input[0] = (short)Mathf.Clamp(omega.x * 2000f, -32767, 32767);
            input[1] = (short)Mathf.Clamp(omega.y * 2000f, -32767, 32767);
            input[2] = (short)Mathf.Clamp(omega.z * 2000f, -32767, 32767);
            return input;
        }

        public static Vector3 UnpackOmega(short[] data)
        {
            return new Vector3(data[0] * 0.0005f, data[1] * 0.0005f, data[2] * 0.0005f);
        }
        #endregion
    }
}
