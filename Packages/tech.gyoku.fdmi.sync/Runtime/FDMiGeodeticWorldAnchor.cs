using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    /// <summary>
    /// ワールドの静的構造物(地面・滑走路・建物など)をフレームに参加させる参照点。
    /// 論理位置はシーン記述(= ECEF 原点基準の Unity 座標)のまま固定し、描画だけを
    /// フレーム(非着席時はプレイヤーのセル、着席中は root 車両のセル)に追従させる。
    /// 子オブジェクトはこの transform ごと動くので、地形をこの下にまとめて置けば
    /// コライダーも一緒に動き、root 車体との衝突判定も正しいままになる。
    /// これが無いと、root 切替(車体の原点固定)で車体だけが固定され、ワールドが置き去りになる。
    /// 基底が `[UdonSynced]` フィールドを持つため、ここで NoVariableSync を宣言すると
    /// 基底の同期フィールドが「sync mode None」として弾かれる(Manual を継承させる)。
    /// </summary>
    public class FDMiGeodeticWorldAnchor : FDMiGeodeticReferencePoint
    {
        Vector3 appliedPos;
        Quaternion appliedRot = Quaternion.identity;
        bool applied;

        public override void initReferencePoint()
        {
            // シーン記述の座標をそのまま論理位置として取り込む(以後、論理位置は動かさない)
            setBaseChunk(Vector3Int.zero);
            setPosition(syncManager.ToEcef(transform.position));
            _rotation = syncManager.ToEcefRot(transform.rotation);
            base.initReferencePoint();
        }

        void Update()
        {
            if (!isInit) return;
            Vector3 pos = syncManager.ToUnity(getViewPosition());
            Quaternion rot = syncManager.ToUnityRot(getViewRotation());
            // フレームのセルが動いた時だけ書く(毎フレーム書くと静的バッチが崩れる)
            if (applied && pos == appliedPos && rot == appliedRot) return;
            appliedPos = pos;
            appliedRot = rot;
            applied = true;
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
