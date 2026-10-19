using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using FDMi.core;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiReferencePoint : FDMiBehaviour
    {
        // 1 Chunk = 2^30 mm = 1073741.824 m。月 (~384400 km = ±358 Chunk) が short に収まる最小単位。
        public const float M_PER_CHUNK = 1073741.824f;
        // _posMm の正規形は |m| < MM_PER_CHUNK。carry 判定は >= を使う。
        // Why-not: > では ±2^30 を許し、同一 Chunk 内の int 差分が最大 2^31 で int32 max を 1 だけ溢れる。
        public const int MM_PER_CHUNK = 1073741824;
        // 1 / M_PER_CHUNK (decomposeTotalM の Chunk 丸め用)。
        public const float CHUNK_PER_M = 0.00000093132258f;
        // 地球半径 [m] (高度・重力の基準)。
        public const float EARTH_RADIUS_M = 6371000f;
        // 地心重力定数 μ [m^3/s^2] (float 7 桁に収まる概数で十分)。
        public const float MU_EARTH = 3.986e14f;

        [HideInInspector] public int index; // use in FDMiRelativeObjectSyncManager.Identify FDMiReferencePoint.
        public bool parentIsRoot;// use in FDMiRelativeObjectSyncManager.
        public FDMiRelativeObjectSyncManager syncManager;
        public Rigidbody body;
        [UdonSynced, FieldChangeCallback(nameof(ParentIndex))] protected int _parentIndex;
        public int ParentIndex
        {
            get => _parentIndex;
            set { handleParentIndex(value); }
        }

        public Vector3 _position, _velocity, _chunkPosition;
        public int[] _posMm = new int[3];
        public Quaternion _rotation = Quaternion.identity;

        public bool _isRoot = false;
        public bool isRoot
        {
            get => _isRoot;
            set
            {
                _isRoot = value;
            }
        }

        public FDMiReferencePoint parentRefPoint, rootRefPoint;
        protected bool isInit = false;
        public Transform respawnPoint;
        [HideInInspector] public Vector3 respawnPos = Vector3.zero;
        [HideInInspector] public Quaternion respawnRot = Quaternion.identity;
        private
        protected Vector3 prevPos;
        protected Quaternion prevRot;

        public static Vector3 MmToM(int[] mm)
        {
            return new Vector3(mm[0] * 0.001f, mm[1] * 0.001f, mm[2] * 0.001f);
        }

        public Vector3 getTotalPositionM()
        {
            return _position + MmToM(_posMm) + _chunkPosition * M_PER_CHUNK;
        }

        // totalM (親相対 [m]) を _position / _posMm / _chunkPosition に分解する。
        // Why-not: float 分解なので月距離では rem に ~32 m の ULP 誤差が出る。
        // テレポートは境界付近の小値同士で厳密、遠方は描画上無視できるため許容する。
        protected void decomposeTotalM(Vector3 totalM)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = totalM[i];
                int ci = (int)Mathf.Round(t * CHUNK_PER_M);
                float rem = t - ci * M_PER_CHUNK;
                int mm = (int)Mathf.Round(rem * 1000f);
                if (mm >= MM_PER_CHUNK) { ci += 1; mm -= MM_PER_CHUNK; }
                else if (mm <= -MM_PER_CHUNK) { ci -= 1; mm += MM_PER_CHUNK; }
                _posMm[i] = mm;
                _chunkPosition[i] = ci;
                _position[i] = rem - mm * 0.001f;
            }
        }

        public static short[] PackQuaternion(Quaternion q)
        {
            Quaternion qin = q.normalized;
            short[] q_array = new short[4];
            q_array[0] = (short)(qin.x * 32767f);
            q_array[1] = (short)(qin.y * 32767f);
            q_array[2] = (short)(qin.z * 32767f);
            q_array[3] = (short)(qin.w * 32767f);
            return q_array;
        }

        public static Quaternion UnpackQuaternion(short[] data)
        {
            return new Quaternion(data[0] / 32767f, data[1] / 32767f, data[2] / 32767f, data[3] / 32767f);
        }

        #region RelativePosition
        public bool careIsKinematic = true;
        [HideInInspector] public bool stopUpdate = true;
        bool kinematicFlag = false;
        public virtual void initReferencePoint()
        {
            if (!parentRefPoint) parentRefPoint = syncManager;
            rootRefPoint = syncManager;
            if (parentRefPoint) ParentIndex = parentRefPoint.index;
            if (Utilities.IsValid(respawnPoint))
            {
                respawnPos = transform.InverseTransformPoint(respawnPoint.position);
                respawnRot = Quaternion.Inverse(transform.rotation) * respawnPoint.rotation;
            }
            if (body) kinematicFlag = body.isKinematic;
            stopUpdate = false;
            waitUpdate();
            isInit = true;
        }
        public virtual void waitUpdate()
        {
            if (!body) return;
            turnOnStopUpdate();
        }
        public void turnOnStopUpdate()
        {
            if (stopUpdate) return;
            setKinematic();
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 5);
            stopUpdate = true;
        }

        public void turnOffStopUpdate()
        {
            unsetKinematic();
            stopUpdate = false;
        }
        public void setKinematic()
        {
            if (!body || !careIsKinematic || !isInit) return;
            body.isKinematic = true;
        }
        public void unsetKinematic()
        {
            if (!body || !careIsKinematic) return;
            body.isKinematic = body.isKinematic && (kinematicFlag || (!isRoot && !parentIsRoot));
        }

        public void onChangeRootRefPoint(FDMiReferencePoint refPoint)
        {
            rootRefPoint = refPoint;
            parentIsRoot = (parentRefPoint.index == refPoint.index);
            waitUpdate();
            windupPositionAndRotation();
        }
        public void changeParentRefPoint(FDMiReferencePoint refPoint)
        {
            if (!isRoot)
                ParentIndex = refPoint.index;
            RequestSerialization();
        }

        public virtual void handleParentIndex(int value)
        {
            turnOnStopUpdate();
            _parentIndex = value;
            if (value >= 0)
            {
                parentRefPoint = syncManager.refPoints[value];
                // transform.SetParent(parentRefPoint.transform);
            }
            // else
            // {
            // transform.SetParent(null);
            // }
        }

        public virtual Vector3 getViewPosition()
        {
            // localPlayerPosition
            Vector3 localTotalM = syncManager.localPlayerPosition ? syncManager.localPlayerPosition.getTotalPositionM() : Vector3.zero;
            if (isRoot) return -localTotalM;
            // localRootRefPoint
            Vector3 rootPos = rootRefPoint ? rootRefPoint._position : Vector3.zero;
            Vector3 rootChunkPos = rootRefPoint ? rootRefPoint._chunkPosition : Vector3.zero;
            int[] rootMm = rootRefPoint ? rootRefPoint._posMm : _posMm;
            Vector3 chunkDiff = _chunkPosition - rootChunkPos;
            Quaternion dir = Quaternion.identity;
            if (rootRefPoint)
            {
                dir = Quaternion.Inverse(rootRefPoint._rotation);
            }
            Vector3 diff = _position - rootPos;
            // Why-not: mm 差分は int 域で厳密に取ってから m 化する。
            // chunk 項と float で素朴に加算すると、被り帯域内の境界跨ぎ近接機で ULP 誤差 (~64-128 mm) が出る。
            diff += new Vector3(_posMm[0] - rootMm[0], _posMm[1] - rootMm[1], _posMm[2] - rootMm[2]) * 0.001f;
            diff += chunkDiff * M_PER_CHUNK - localTotalM;
            return dir * diff;
        }
        public virtual Vector3 getViewPositionInterpolated()
        {
            // if (Networking.IsOwner(gameObject)) return getViewPosition();
            // prevPos = Vector3.Lerp(prevPos, getViewPosition(), Time.deltaTime * 10);
            return getViewPosition();
        }
        public virtual Quaternion getViewRotation()
        {
            if (isRoot) return Quaternion.identity;
            if (!rootRefPoint) return _rotation;
            return (Quaternion.Inverse(rootRefPoint._rotation) * _rotation).normalized;
        }
        public virtual Quaternion getViewRotationInterpolated()
        {
            // if (Networking.IsOwner(gameObject)) return getViewRotation();
            // prevRot = Quaternion.Slerp(prevRot, getViewRotation(), Time.deltaTime * 10);
            return getViewRotation();
        }
        public virtual void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
            transform.position = prevPos;
            transform.rotation = prevRot;
        }

        public virtual bool setPosition(Vector3 pos)
        {
            Vector3 v = pos, k = _chunkPosition;
            int[] m = _posMm;
            bool updateChunkPos = false;
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Abs(v[i]);
                if (a >= 750f)
                {
                    updateChunkPos = true;
                    float s = Mathf.Sign(v[i]);
                    s *= Mathf.Round(a * 0.001f + 0.25000001f);
                    m[i] += (int)(s * 1000f);
                    v[i] -= s * 1000f;
                    if (m[i] >= MM_PER_CHUNK) { k[i] += 1f; m[i] -= MM_PER_CHUNK; }
                    else if (m[i] <= -MM_PER_CHUNK) { k[i] -= 1f; m[i] += MM_PER_CHUNK; }
                }
            }
            if (updateChunkPos) _chunkPosition = k;
            _position = v;
            return updateChunkPos;
        }
        public virtual void setRotation(Quaternion rot)
        {
            _rotation = rot;
        }
        #endregion
        #region Serialize
        [SerializeField] protected float updateInterval = 0.25f;
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle, may need to be increased for vehicles that want to be idle on water. If the vehicle floats away sometimes, this value is probably too big")]
        public float IdleMovementRange = .35f;

        [Tooltip("If vehicle rotates less than this many degrees since it's last update, it'll be considered to be idle")]
        public float IdleRotationRange = 5f;
        protected double nextUpdateTime, ServerTimeDiff;
        protected void ResetSyncTime()
        {
            ServerTimeDiff = Networking.GetServerTimeInSeconds() - Time.time;
            nextUpdateTime = Time.time + (double)updateInterval;
        }
        protected virtual void TrySerialize()
        {
            // Try Serialize.
            // if (Time.time > nextUpdateTime && !Networking.IsClogged)
            // {
            //     for (int i = 0; i < 3; i++) syncedPosition[i] = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
            //     for (int i = 0; i < 3; i++) syncedChunkPosition[i] = (short)_chunkPosition[i];
            //     RequestSerialization();
            //     nextUpdateTime = Time.time + updateInterval;
            // }
        }
        #endregion


    }
}
