using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    /// <summary>
    /// 位置同期の基盤。論理位置 = MulChunk(chunk) + position。
    /// 論理座標は ECEF フレーム(ワールドの ECEF 原点 = Unity 原点からの m オフセット)で、
    /// transform の親子関係には依存しない(親は階層の都合だけを決める)。
    /// 同期値は 2 段: 細部 short(1cm 格子、short 全域 = 1 chunk) + 粗部 int(チャンクカウンタ)。
    /// 1 chunk = 65536 counts = 655.36 m。1cm 格子は受信側でもそのまま再現されるため、
    /// 原点から数万 km 離れても同期値の分解能は 1cm のまま落ちない
    /// (描画側の上限は Unity float32 の座標精度で決まる)。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiGeodeticReferencePoint : FDMiBehaviour
    {
        /// <summary>細部 1 count の長さ [m]。0.5cm 単位にする場合は 0.005f にする。</summary>
        public const float FineUnitM = 0.01f;
        /// <summary>細部 short の全範囲 = 1 chunk [m]。</summary>
        public const float ChunkM = FineUnitM * 65536f;

        [HideInInspector] public int index;
        public FDMiGeodeticSyncManager syncManager;
        public Rigidbody body;

        [UdonSynced, FieldChangeCallback(nameof(ParentIndex)), HideInInspector] protected int _parentIndex;
        public int ParentIndex
        {
            get => _parentIndex;
            set { handleParentIndex(value); }
        }

        // 粗部: チャンクカウンタ(int)。細部: セル内成分(short, 1cm)。
        [UdonSynced, HideInInspector] public int syncedChunkX;
        [UdonSynced, HideInInspector] public int syncedChunkY;
        [UdonSynced, HideInInspector] public int syncedChunkZ;
        [UdonSynced, HideInInspector] public short syncedFineX;
        [UdonSynced, HideInInspector] public short syncedFineY;
        [UdonSynced, HideInInspector] public short syncedFineZ;

        public Vector3Int syncedChunk
        {
            get => new Vector3Int(syncedChunkX, syncedChunkY, syncedChunkZ);
            set
            {
                syncedChunkX = value.x;
                syncedChunkY = value.y;
                syncedChunkZ = value.z;
            }
        }

        /// <summary>受信したセル内成分 [m]。</summary>
        public Vector3 syncedFineM => new Vector3(syncedFineX, syncedFineY, syncedFineZ) * FineUnitM;

        // 姿勢を short x4 (15bit 成分) で同期する。Quaternion 型同期(16 B)の半分で、
        // 量子化 step は 360/65535 度。受信側で正規化して drift を打ち消す。
        [UdonSynced, HideInInspector] public short syncedRotX;
        [UdonSynced, HideInInspector] public short syncedRotY;
        [UdonSynced, HideInInspector] public short syncedRotZ;
        [UdonSynced, HideInInspector] public short syncedRotW;

        public Quaternion syncedRot
        {
            get
            {
                Quaternion q = new Quaternion(syncedRotX / 32767f, syncedRotY / 32767f, syncedRotZ / 32767f, syncedRotW / 32767f);
                float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
                if (mag < 1e-6f)
                    return Quaternion.identity;
                q.x /= mag;
                q.y /= mag;
                q.z /= mag;
                q.w /= mag;
                return q;
            }
            set
            {
                syncedRotX = (short)Mathf.RoundToInt(value.x * 32767f);
                syncedRotY = (short)Mathf.RoundToInt(value.y * 32767f);
                syncedRotZ = (short)Mathf.RoundToInt(value.z * 32767f);
                syncedRotW = (short)Mathf.RoundToInt(value.w * 32767f);
            }
        }

        /// <summary>論理位置のセル内成分 [m]。1cm 格子に乗っている。</summary>
        public Vector3 _position;
        /// <summary>論理速度 [ECEF m/s]。</summary>
        public Vector3 _velocity;
        /// <summary>論理位置の粗部(チャンクカウンタ)。</summary>
        public Vector3Int _chunk;
        public Quaternion _rotation = Quaternion.identity;

        public bool isRoot = false;
        public FDMiGeodeticReferencePoint parentRefPoint, rootRefPoint;
        protected bool isInit = false;

        /// <summary>リスポーン位置(論理 ECEF, m)。既定はワールド ECEF 原点。</summary>
        public Vector3 respawnPoint;

        #region RelativePosition
        public bool stopUpdate = true;

        public virtual void initReferencePoint()
        {
            if (!parentRefPoint) parentRefPoint = syncManager;
            rootRefPoint = syncManager;
            if (parentRefPoint) ParentIndex = parentRefPoint.index;

            waitUpdate();
            isInit = true;
        }

        // 再錨定(チャンク通過・root 切替)中は Update 系を数フレーム止めて windup 後の姿勢を落着させる。
        public void waitUpdate()
        {
            stopUpdate = true;
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 5);
        }

        public void turnOnStopUpdate()
        {
            stopUpdate = true;
        }

        public void turnOffStopUpdate()
        {
            stopUpdate = false;
        }

        public void onChangeRootRefPoint(FDMiGeodeticReferencePoint refPoint)
        {
            rootRefPoint = refPoint;
            windupPositionAndRotation();
        }

        public void changeParentRefPoint(FDMiGeodeticReferencePoint refPoint)
        {
            if (!isRoot)
                ParentIndex = refPoint.index;
            RequestSerialization();
        }

        public virtual void handleParentIndex(int value)
        {
            turnOnStopUpdate();
            _parentIndex = value;
            SendCustomEventDelayedFrames(nameof(turnOffStopUpdate), 2);
            if (value >= 0)
            {
                parentRefPoint = syncManager.refPoints[value];
                transform.SetParent(parentRefPoint.transform);
            }
            else
            {
                transform.SetParent(null);
            }
        }

        /// <summary>
        /// 描画フレームから見た位置 [m] = 論理位置 − フレーム原点。
        /// フレーム原点は root の論理位置そのもの(非着席時はプレイヤーのセル、着席中は車体の
        /// chunk + セル内成分)。root 自身は必ず 0 になり、他はすべて root からの相対になる。
        /// 姿勢は `FDMiGeodeticSyncManager.ToUnityRot` がフレーム姿勢で回すので、ここでは回さない。
        /// </summary>
        public virtual Vector3 getViewPosition()
        {
            return _position - syncManager.FrameOriginFine + syncManager.MulChunk(_chunk - syncManager.FrameOriginChunk);
        }

        /// <summary>論理姿勢。Unity 側でフレーム姿勢を掛けて描く(root は無回転になる)。</summary>
        public virtual Quaternion getViewRotation()
        {
            return _rotation;
        }

        public virtual void windupPositionAndRotation()
        {
            transform.position = syncManager.ToUnity(getViewPosition());
            transform.rotation = syncManager.ToUnityRot(getViewRotation());
        }

        /// <summary>
        /// セル内成分(m)を 1cm 格子へ量子化し、short 域(1 chunk)を超えた分を chunk へ繰り上げる。
        /// 引数 pos は「現在の _chunk 基準の m」を渡す(|pos| は 1 chunk 強までを想定)。
        /// 戻り値は chunk の増分。
        /// </summary>
        Vector3Int foldToChunk(Vector3 pos)
        {
            int cx = Mathf.RoundToInt(pos.x / FineUnitM);
            int cy = Mathf.RoundToInt(pos.y / FineUnitM);
            int cz = Mathf.RoundToInt(pos.z / FineUnitM);
            Vector3Int k = Vector3Int.zero;
            if (cx >= 32768)
            {
                k.x = 1 + (cx - 32768) / 65536;
                cx -= k.x * 65536;
            }
            else if (cx < -32768)
            {
                k.x = -1 - (-32769 - cx) / 65536;
                cx -= k.x * 65536;
            }
            if (cy >= 32768)
            {
                k.y = 1 + (cy - 32768) / 65536;
                cy -= k.y * 65536;
            }
            else if (cy < -32768)
            {
                k.y = -1 - (-32769 - cy) / 65536;
                cy -= k.y * 65536;
            }
            if (cz >= 32768)
            {
                k.z = 1 + (cz - 32768) / 65536;
                cz -= k.z * 65536;
            }
            else if (cz < -32768)
            {
                k.z = -1 - (-32769 - cz) / 65536;
                cz -= k.z * 65536;
            }
            _position = new Vector3(cx, cy, cz) * FineUnitM;
            return k;
        }

        /// <summary>セル内成分を更新し、1 chunk を超えた分を chunk へ繰り上げる。</summary>
        public virtual bool setPosition(Vector3 pos)
        {
            Vector3Int k = foldToChunk(pos);
            if (k.x == 0 && k.y == 0 && k.z == 0) return false;
            _chunk += k;
            return true;
        }

        /// <summary>chunk を基準に置き換える(絶対値を作らずに論理位置を指定するための入口)。</summary>
        public void setBaseChunk(Vector3Int baseChunk)
        {
            _chunk = baseChunk;
            _position = Vector3.zero;
        }

        public virtual void setRotation(Quaternion rot)
        {
            _rotation = rot;
        }
        #endregion

        #region Serialize
        [SerializeField] protected float updateInterval = 0.25f;
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle")]
        public float IdleMovementRange = .35f;

        protected double nextUpdateTime, ServerTimeDiff;

        protected void ResetSyncTime()
        {
            ServerTimeDiff = Networking.GetServerTimeInSeconds() - Time.time;
            nextUpdateTime = Time.time + (double)updateInterval;
        }

        // 送信済みミラー。1cm 格子へ量子化した値で比較するため、
        // 静止機の 1cm 未満の揺れや姿勢の不変は送信を発生させない。
        int lastSentChunkX, lastSentChunkY, lastSentChunkZ;
        short lastSentFineX, lastSentFineY, lastSentFineZ;
        short lastSentRotX, lastSentRotY, lastSentRotZ, lastSentRotW;
        bool hasSerialized;

        /// <summary>派生が追加する同期値(速度など)に変化があったか。既定は変化なし。</summary>
        protected virtual bool ExtraPayloadChanged() { return false; }
        /// <summary>派生が追加する同期値を書く(RequestSerialization の直前に呼ばれる)。</summary>
        protected virtual void WriteSyncedExtra() { }

        protected void TrySerialize()
        {
            if (Time.time <= nextUpdateTime || Networking.IsClogged) return;
            nextUpdateTime = Time.time + updateInterval;

            short fx = (short)Mathf.RoundToInt(_position.x / FineUnitM);
            short fy = (short)Mathf.RoundToInt(_position.y / FineUnitM);
            short fz = (short)Mathf.RoundToInt(_position.z / FineUnitM);
            // 姿勢は現在値を pack して比較(syncedRotX は前回送信値のまま)
            short rx = (short)Mathf.RoundToInt(_rotation.x * 32767f);
            short ry = (short)Mathf.RoundToInt(_rotation.y * 32767f);
            short rz = (short)Mathf.RoundToInt(_rotation.z * 32767f);
            short rw = (short)Mathf.RoundToInt(_rotation.w * 32767f);

            if (hasSerialized
                && !ExtraPayloadChanged()
                && fx == lastSentFineX && fy == lastSentFineY && fz == lastSentFineZ
                && _chunk.x == lastSentChunkX && _chunk.y == lastSentChunkY && _chunk.z == lastSentChunkZ
                && rx == lastSentRotX && ry == lastSentRotY && rz == lastSentRotZ && rw == lastSentRotW)
                return;

            WriteSyncedExtra();
            PackSynced(fx, fy, fz);
            hasSerialized = true;
            lastSentChunkX = _chunk.x;
            lastSentChunkY = _chunk.y;
            lastSentChunkZ = _chunk.z;
            lastSentFineX = fx;
            lastSentFineY = fy;
            lastSentFineZ = fz;
            lastSentRotX = rx;
            lastSentRotY = ry;
            lastSentRotZ = rz;
            lastSentRotW = rw;
            RequestSerialization();
        }

        /// <summary>現在の論理位置・姿勢を同期フィールドへ書く。</summary>
        void PackSynced(short fx, short fy, short fz)
        {
            syncedChunk = _chunk;
            syncedFineX = fx;
            syncedFineY = fy;
            syncedFineZ = fz;
            syncedRot = _rotation;
        }

        /// <summary>SeatAnchor / SaccBridge からの強制再送。interval 待ちを飛ばし、変化が無くても送る。</summary>
        public void ForceSerialize()
        {
            WriteSyncedExtra();
            PackSynced((short)Mathf.RoundToInt(_position.x / FineUnitM),
                (short)Mathf.RoundToInt(_position.y / FineUnitM),
                (short)Mathf.RoundToInt(_position.z / FineUnitM));
            hasSerialized = true;
            lastSentChunkX = _chunk.x;
            lastSentChunkY = _chunk.y;
            lastSentChunkZ = _chunk.z;
            lastSentFineX = syncedFineX;
            lastSentFineY = syncedFineY;
            lastSentFineZ = syncedFineZ;
            lastSentRotX = syncedRotX;
            lastSentRotY = syncedRotY;
            lastSentRotZ = syncedRotZ;
            lastSentRotW = syncedRotW;
            RequestSerialization();
        }
        #endregion
    }
}
