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

        public override void initReferencePoint()
        {
            base.initReferencePoint();
            body.useGravity = false;

            isRoot = false;
            setPosition(body.position);
            setRotation(body.rotation);
            ResetSyncStuff();
            transform.SetParent(null);
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
            prevRootPos = rootRefPoint._position;
            prevRootRot = rootRefPoint._rotation;
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
                body.transform.position = transform.position;
                body.transform.rotation = transform.rotation;
            }
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
        Vector3 prevRootPos = Vector3.zero;
        Quaternion prevRootRot = Quaternion.identity;
        void FixedUpdate()
        {
            if (!isInit) return;
            if (Networking.IsOwner(gameObject) && !body.isKinematic)
            {
                Quaternion br = body.rotation;
                Vector3 bp = body.position;
                if (isRoot)
                {
                    setRotation((_rotation * br).normalized);
                    setPosition(_rotation * bp + _position);
                    body.position = Vector3.zero;
                    body.rotation = Quaternion.identity;
                    body.velocity = Quaternion.Inverse(br) * body.velocity;

                    // gravity: central (ECEF)
                    Vector3 selfTotalM = getTotalPositionM();
                    float r = selfTotalM.magnitude;
                    Vector3 g = selfTotalM * (-MU_EARTH / (r * r * r));
                    body.AddForce(Quaternion.Inverse(_rotation) * g, ForceMode.Acceleration);
                }
                else
                {
                    Quaternion smr = syncManager.localRotation;
                    Vector3 smp = syncManager.localPosition;
                    Vector3 rbdp = Vector3.zero;
                    if (rootRefPoint.body)
                    {
                        smr = rootRefPoint.body.rotation * smr;
                        rbdp = rootRefPoint.body.position;
                    }

                    setRotation((Quaternion.Inverse(smr) * br).normalized);
                    setPosition(Quaternion.Inverse(smr) * (bp - smp - rbdp));
                    body.rotation = getViewRotation();
                    body.position = getViewPosition();
                    // body.velocity = Quaternion.Inverse(rbr) * body.velocity;

                    // gravity: central (ECEF), evaluated at self position composed from root
                    Vector3 selfTotalM = rootRefPoint.getTotalPositionM() + rootRefPoint._rotation * getTotalPositionM();
                    float r = selfTotalM.magnitude;
                    Vector3 g = selfTotalM * (-MU_EARTH / (r * r * r));
                    body.AddForce(Quaternion.Inverse(rootRefPoint._rotation) * g, ForceMode.Acceleration);
                }
            }
            else
            {
                if (isRoot) return;
                body.rotation = getViewRotation();
                body.position = getViewPosition();
                body.transform.position = getViewPosition();
                body.transform.rotation = getViewRotation();
                Vector3 selfTotalM = rootRefPoint.getTotalPositionM() + rootRefPoint._rotation * getTotalPositionM();
                float r = selfTotalM.magnitude;
                Vector3 g = selfTotalM * (-MU_EARTH / (r * r * r));
                body.AddForce(Quaternion.Inverse(rootRefPoint._rotation) * g, ForceMode.Acceleration);
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
                // if (parentRefPoint.index == rootRefPoint.index)
                // {
                //     setRotation((btr * parentRefPoint._rotation).normalized);
                //     setPosition(parentRefPoint._rotation * btp + parentRefPoint._position + 1000f * (parentRefPoint.syncedKmPos - syncedKmPos));
                //     _velocity = parentRefPoint._rotation * body.velocity;
                // }
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
            // Try Serialize.
            if (Time.time > nextUpdateTime)
            {
                for (int i = 0; i < 3; i++)
                {
                    syncedPosition[i] = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                    syncedChunkPosition[i] = (short)_chunkPosition[i];
                }
                syncedRotation = PackQuaternion(_rotation);
                syncedAngularVel = PackOmega(body.angularVelocity);
                syncedVel = _velocity;
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        private double lastSyncedTime;
        private float localSyncedTime, syncedInterval = 100000f;
        private Vector3 lastVel, lastPos;
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
                syncedPosition[i] = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                lastSyncedMm[i] = syncedPosition[i];
                syncedChunkPosition[i] = (short)_chunkPosition[i];
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
            // position: syncedPosition holds total mm int.
            // Why-not: 差分だけ残差 _position に畳んで world 連続を保つ。絶対値をそのまま _position に入れると float ULP で壊れる。
            lastVel = _velocity;
            syncTargetM = _position;
            for (int i = 0; i < 3; i++)
            {
                int dMm = syncedPosition[i] - lastSyncedMm[i];
                lastSyncedMm[i] = syncedPosition[i];
                _posMm[i] = syncedPosition[i];
                float dChunkM = (syncedChunkPosition[i] - lastSyncedChunk[i]) * M_PER_CHUNK;
                lastSyncedChunk[i] = syncedChunkPosition[i];
                _chunkPosition[i] = syncedChunkPosition[i];
                syncTargetM[i] += dMm * 0.001f + dChunkM;
            }
            lastPos = _position;
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
