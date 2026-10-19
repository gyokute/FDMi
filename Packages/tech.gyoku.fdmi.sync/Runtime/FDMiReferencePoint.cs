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
        // 1 / M_PER_CHUNK (Editor の Total [chunk] 表示用)。
        public const float CHUNK_PER_M = 0.00000093132258f;
        // Why-not: 750 m だと軸 750 m・対角 1299 m で「原点から 1 km 以内」を満たさない。
        // 500 m なら対角 866 m。繰り上げは対称丸め (round half up) で行う。
        public const float CARRY_M = 500f;
        // 1 反復で畳む最大 km。int mm 加算 (<= 1.073e9) が int32 を溢れない上限。
        public const int CARRY_KM_MAX = 1073;
        // 地球半径 [m] (高度・重力の基準)。
        public const float EARTH_RADIUS_M = 6371000f;

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
        protected Vector3 prevPos;
        protected Quaternion prevRot;

        public static Vector3 MmToM(int[] mm)
        {
            return new Vector3(mm[0] * 0.001f, mm[1] * 0.001f, mm[2] * 0.001f);
        }
        // Why-not: Mathf.Round は banker's rounding のため使わない。setPosition と同じ floor(x + 0.5) で対称丸めする。
        // Why-not: D を float 化するのは player–root 間 (通常 < 数 km) の差分だけであり、月距離の絶対 total 化とは精度が違う。
        protected Vector3 coarseCellFor(FDMiPlayerPosition p, FDMiReferencePoint r)
        {
            float dchx = p._chunkPosition.x - r._chunkPosition.x;
            float dchy = p._chunkPosition.y - r._chunkPosition.y;
            float dchz = p._chunkPosition.z - r._chunkPosition.z;
            int dmmx = p._posMm[0] - r._posMm[0];
            int dmmy = p._posMm[1] - r._posMm[1];
            int dmmz = p._posMm[2] - r._posMm[2];
            float dfx = p._position.x - r._position.x;
            float dfy = p._position.y - r._position.y;
            float dfz = p._position.z - r._position.z;
            float Dx = dchx * M_PER_CHUNK + dmmx * 0.001f + dfx;
            float Dy = dchy * M_PER_CHUNK + dmmy * 0.001f + dfy;
            float Dz = dchz * M_PER_CHUNK + dmmz * 0.001f + dfz;
            int qx = (int)Mathf.Floor(Dx * 0.001f + 0.5f);
            int qy = (int)Mathf.Floor(Dy * 0.001f + 0.5f);
            int qz = (int)Mathf.Floor(Dz * 0.001f + 0.5f);
            return new Vector3(qx * 1000f, qy * 1000f, qz * 1000f);
        }

        public Vector3 getTotalPositionM()
        {
            return _position + MmToM(_posMm) + _chunkPosition * M_PER_CHUNK;
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
                // Why-not: manager が refPoints に自身を含むと親が自分になるため、self-parent だけ SetParent しない。
                if (value != index) transform.SetParent(parentRefPoint.transform);
            }
            else
            {
                transform.SetParent(null);
            }
        }

        // Why-not: root も同一式で解く。位置の isRoot 分岐は持たない (回転の高速路は残す)。
        protected Vector3 viewForRoot(FDMiReferencePoint r)
        {
            Quaternion invQ = r ? Quaternion.Inverse(r._rotation) : Quaternion.identity;
            Vector3 C = syncManager ? syncManager.localCell : Vector3.zero;
            if (!syncManager || !syncManager.localPlayerPosition) C = Vector3.zero;
            float chx = _chunkPosition.x - (r ? r._chunkPosition.x : 0f);
            float chy = _chunkPosition.y - (r ? r._chunkPosition.y : 0f);
            float chz = _chunkPosition.z - (r ? r._chunkPosition.z : 0f);
            int mmx = _posMm[0] - (r ? r._posMm[0] : 0);
            int mmy = _posMm[1] - (r ? r._posMm[1] : 0);
            int mmz = _posMm[2] - (r ? r._posMm[2] : 0);
            float fx = _position.x - (r ? r._position.x : 0f);
            float fy = _position.y - (r ? r._position.y : 0f);
            float fz = _position.z - (r ? r._position.z : 0f);
            Vector3 inner = new Vector3(chx * M_PER_CHUNK, chy * M_PER_CHUNK, chz * M_PER_CHUNK)
                + new Vector3(mmx, mmy, mmz) * 0.001f
                + new Vector3(fx, fy, fz)
                - C;
            return invQ * inner;
        }

        public virtual Vector3 getViewPosition()
        {
            return viewForRoot(rootRefPoint);
        }
        public virtual Vector3 getViewPositionInterpolated()
        {
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
                if (Mathf.Abs(v[i]) <= CARRY_M) continue;
                updateChunkPos = true;
                // Why-not: s*1000000f を int に単発キャストすると |v| >= 2147 km で Convert.ToInt32 が
                // 例外を投げ、1074 km 以上では m[i] += が int をラップする。1 反復 <= 1 chunk に割る。
                while (Mathf.Abs(v[i]) > CARRY_M)
                {
                    int s = (int)(Mathf.Abs(v[i]) * 0.001f + 0.5f);
                    if (s < 1) s = 1;
                    if (s > CARRY_KM_MAX) s = CARRY_KM_MAX;
                    int d = v[i] < 0f ? -s : s;
                    m[i] += d * 1000000;
                    v[i] -= d * 1000f;
                    if (m[i] >= MM_PER_CHUNK) { k[i] += 1f; m[i] -= MM_PER_CHUNK; }
                    else if (m[i] <= -MM_PER_CHUNK) { k[i] -= 1f; m[i] += MM_PER_CHUNK; }
                }
            }
            if (updateChunkPos) _chunkPosition = k;
            _position = v;
            return updateChunkPos;
        }
        // Why-not: setPosition は fine への上書きで、超過分だけ粗部へ繰り上げる (加算ではない)。
        // Start/Bake で「その位置そのもの」を入れるときは粗部を一度 0 に戻す。戻さないと 2 回目で粗部が二重になる。
        public void setPositionAbsolute(Vector3 pos)
        {
            _posMm[0] = 0;
            _posMm[1] = 0;
            _posMm[2] = 0;
            _chunkPosition = Vector3.zero;
            setPosition(pos);
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
        protected double nextUpdateTime;
        protected void ResetSyncTime()
        {
            nextUpdateTime = Time.time + (double)updateInterval;
        }
        protected virtual void TrySerialize()
        {
        }
        #endregion


    }
}
