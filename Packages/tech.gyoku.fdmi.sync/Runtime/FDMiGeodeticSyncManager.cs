using FDMi.core;
using UnityEngine;

namespace FDMi.sync
{
    /// <summary>
    /// 毎フレーム、ローカルプレイヤーのチャンクを原点として座標系を解決する。
    /// transform = ENU(ローカルプレイヤーセル) * (論理ECEF - プレイヤーセル)。
    /// チャンクが変わった時だけ ENU 基底を再計算する(FDMiEcefOrigin.ENUAt)。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class FDMiGeodeticSyncManager : FDMiGeodeticReferencePoint
    {
        public FDMiGeodeticReferencePoint[] refPoints;
        public FDMiGeodeticReferencePoint localRootRefPoint;
        public FDMiGeodeticPlayerPosition localPlayerPosition;
        public FDMiEcefOrigin origin;

        Quaternion enuQ = Quaternion.identity;
        bool enuDirty = true;
        Vector3Int lastLocalChunk;

        /// <summary>
        /// 描画フレームのセル。着席中は root 車両のセル、非着席時はローカルプレイヤーのセル。
        /// 車両を root にした瞬間から、世界は「車両セルを ENU 原点とする」見え方へ切り替わる。
        /// </summary>
        public Vector3Int localPlayerChunk
        {
            get
            {
                if (localRootRefPoint && localRootRefPoint != this) return localRootRefPoint._chunk;
                return localPlayerPosition ? localPlayerPosition._chunk : Vector3Int.zero;
            }
        }

        void Start()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            _rotation = Quaternion.identity;
            localRootRefPoint = this;
            ParentIndex = -1;
            // refPoints の initReferencePoint が ToEcef/ToUnity を使うため、先に ENU 基底を確定させる
            UpdateENU();
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].index = i;
            for (int i = 0; i < refPoints.Length; i++)
                refPoints[i].initReferencePoint();
        }

        public void LateUpdate()
        {
            if (!isInit) return;
            UpdateENU();
            transform.rotation = ToUnityRot(getViewRotation());
            transform.position = ToUnity(getViewPosition());
        }

        public void onChangeLocalPlayerKMPosition()
        {
            for (int i = 0; i < refPoints.Length; i++)
            {
                refPoints[i].waitUpdate();
                refPoints[i].windupPositionAndRotation();
            }
        }

        /// <summary>
        /// 座標系の主役(root)を切り替える。着席時に車両、降車時に自分自身(ワールド)へ戻す。
        /// 論理位置は transform 階層に依存しないので、親の付け替えはしない
        /// (rigidbody の SetParent は物理を乱すだけで、位置計算には寄与しない)。
        /// 変わるのは「どのセルを ENU 原点とするか」だけで、切替時は
        /// 各参照点の描画位置を合わせ直す(onChangeRootRefPoint)。
        /// </summary>
        public void changeRootRefPoint(FDMiGeodeticReferencePoint target)
        {
            if (!target || target == localRootRefPoint) return;
            FDMiGeodeticReferencePoint prevRoot = localRootRefPoint;
            // 切替前のフレーム(原点 2 段 + 姿勢)とプレイヤーの論理位置を控える
            Vector3Int oldOriginChunk = FrameOriginChunk;
            Vector3 oldOriginFine = FrameOriginFine;
            Quaternion oldFrameRot = FrameRotationEcef;
            Vector3Int ppChunk = Vector3Int.zero;
            Vector3 ppFine = Vector3.zero;
            if (localPlayerPosition)
            {
                ppChunk = localPlayerPosition._chunk;
                ppFine = localPlayerPosition._position;
            }

            if (prevRoot)
            {
                prevRoot.isRoot = false;
                prevRoot.waitUpdate();
            }
            target.isRoot = true;
            target.waitUpdate();
            localRootRefPoint = target;   // ここから root がフレームになる
            // アバターは VRChat 管理なので動かさず、論理位置を切替前のフレームで読み直し、
            // 見えているもの全部がずれる分だけ手で動かす(着席中は VRChat が座席へ置き直す)。
            if (localPlayerPosition)
                localPlayerPosition.rebaseFrame(oldOriginChunk, oldOriginFine, oldFrameRot,
                    FrameShift(ppChunk, ppFine, oldOriginChunk, oldOriginFine, oldFrameRot));
            for (int i = 0; i < refPoints.Length; i++)
            {
                if (refPoints[i] != localPlayerPosition)
                    refPoints[i].onChangeRootRefPoint(target);
            }
            target.windupPositionAndRotation();
        }

        /// <summary>
        /// 描画フレームの ECEF 姿勢。フレームの軸が ECEF でどちらを向いているか。
        /// 非着席時はフレームのセルの ENU 基底、着席中(root = 車両)は車体の論理姿勢。
        /// これが「root 対象を固定し、他のすべてをそれに対して毎フレーム動かす」の正体。
        /// 物理はこのフレームの軸で回るので、重力・空力の「下向き」は
        /// `Inverse(FrameRotationEcef) * gravityEcef` として取り出す(dynamics 側の責務)。
        /// </summary>
        public Quaternion FrameRotationEcef
        {
            get
            {
                if (localRootRefPoint && localRootRefPoint != this) return localRootRefPoint._rotation;
                return Quaternion.Inverse(enuQ);
            }
        }

        /// <summary>チャンクカウンタ → m。double 経由でチャンク差の精度を落とさない。</summary>
        public Vector3 MulChunk(Vector3Int chunk)
        {
            return new Vector3((float)(chunk.x * (double)ChunkM), (float)(chunk.y * (double)ChunkM), (float)(chunk.z * (double)ChunkM));
        }

        /// <summary>フレーム座標のベクトル → Unity 座標(フレームの姿勢を掛けるだけ)。</summary>
        public Vector3 ToUnity(Vector3 frameVec)
        {
            return Quaternion.Inverse(FrameRotationEcef) * frameVec;
        }

        public Quaternion ToUnityRot(Quaternion frameRot)
        {
            return Quaternion.Inverse(FrameRotationEcef) * frameRot;
        }

        /// <summary>Unity 姿勢 → フレーム座標。オーナー側の capture 用。</summary>
        public Quaternion ToEcefRot(Quaternion unityRot)
        {
            return FrameRotationEcef * unityRot;
        }

        /// <summary>Unity ベクトル(速度・位置成分) → フレーム座標。オーナー側の capture 用。</summary>
        public Vector3 ToEcef(Vector3 unityVec)
        {
            return FrameRotationEcef * unityVec;
        }

        /// <summary>フレーム原点(= root)のセル。着席中は root 車両のセル、非着席時はプレイヤーのセル。</summary>
        public Vector3Int FrameOriginChunk
        {
            get { return (localRootRefPoint && localRootRefPoint != this) ? localRootRefPoint._chunk : localPlayerChunk; }
        }

        /// <summary>フレーム原点のセル内成分。着席中(root = 車両)だけ非 0 になり、車体が厳密に原点へ来る。</summary>
        public Vector3 FrameOriginFine
        {
            get { return (localRootRefPoint && localRootRefPoint != this) ? localRootRefPoint._position : Vector3.zero; }
        }

        /// <summary>
        /// フレーム切替の瞬間に、ある論理点が Unity 座標でどれだけ動くか。
        /// 座標系の基準が変われば同じ論理点でも描かれる場所が変わるので、
        /// VRChat 管理のアバターだけは同じだけ手で動かす必要がある。
        /// 呼び出し側は切替前の原点・姿勢を渡す(切替後に呼ぶ)。
        /// </summary>
        public Vector3 FrameShift(Vector3Int pointChunk, Vector3 pointFine, Vector3Int oldOriginChunk, Vector3 oldOriginFine, Quaternion oldFrameRot)
        {
            Vector3 before = oldFrameRot * (pointFine - oldOriginFine + MulChunk(pointChunk - oldOriginChunk));
            Vector3 after = Quaternion.Inverse(FrameRotationEcef) * (pointFine - FrameOriginFine + MulChunk(pointChunk - FrameOriginChunk));
            return after - before;
        }

        void UpdateENU()
        {
            Vector3Int localChunk = localPlayerChunk;
            if (!enuDirty && localChunk == lastLocalChunk) return;
            enuQ = origin ? origin.ENUAt(MulChunk(localChunk)) : Quaternion.identity;
            lastLocalChunk = localChunk;
            enuDirty = false;
        }
    }
}
