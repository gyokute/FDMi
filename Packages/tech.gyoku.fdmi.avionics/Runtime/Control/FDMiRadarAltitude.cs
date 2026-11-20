using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // レーダー高度。FEF 鉛直（地面に対する鉛直）にレイを飛ばし、地面までの距離を書く。
    //
    // Why not 地面の高さを差し引く: 地形も滑走路も同じマスクで拾えるので、
    // 高さの基準を別に持つ必要がない。レイの当たった距離がそのまま対地高度になる。
    // Why not 接地判定もここで行う: レーダー高度は車輪の接地より先に 0 へ近づく。
    // 接地は FDMiWeightOnWheels がサスから作る。
    // Why not 毎フレーム maxDistance のレイを張り直す距離を可変にする: 地形の起伏で
    // 検出漏れが起きる。固定長で飛ばし、外れたら maxDistance を書く方が状態が連続する。
    // Why not Vector3.down に飛ばす: view の -Y は origin 回転分だけ FEF 鉛直からずれる。
    // root が動くと原点が回るので、ワールド -Y は地面鉛直と一致しない。
    // Why not 機体真下に飛ばす: ピッチ・ロールで斜距離になり高度を誤る。
    // 地面鉛直に飛ばした距離がそのまま鉛直高度になる。
    // Why not anchor から up を自前計算する: avionics→sync の asmdef 依存が増える。
    // FDMiGroundAttitude が書く Attitude (rel = QInv(basis)⊗absRot) を読めば、
    // core 型だけで同じ鉛直が復元できる。dir = viewRot⊗(QInv(rel)⊗down)。
    // Why not FDMiSyncMath.QInv を使う: sync 依存になる。共役化は3項の直書きで足りる。
    [DefaultExecutionOrder(-350)]
    public class FDMiRadarAltitude : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("物体。レイの原点")]
        public Rigidbody body;

        [Tooltip("地面とみなすレイヤ")]
        public LayerMask groundMask;

        [Tooltip("レイの最大距離 [m]")]
        [Min(0.1f)]
        public float maxDistance = 200f;

        [Tooltip("FDMiGroundAttitude が書く相対姿勢と共有するパス。FEF 鉛直の復元に読む")]
        public string attitudePath = "~/Physics/Attitude";

        [FDMiDataPath(nameof(attitudePath))]
        [Tooltip("地面基準の相対姿勢を読む FDMiQuaternion。同じ機体の GroundAttitude が書く")]
        public FDMiQuaternion attitude;

        [Header("出力")]
        [Tooltip("レーダー高度のパス")]
        public string radarAltitudePath = "~/Physics/RadarAltitude";

        [FDMiDataPath(nameof(radarAltitudePath))]
        [Tooltip("レーダー高度 [m] を書く FDMiFloat")]
        public FDMiFloat radarAltitude;

        float[] _radarAltitude;
        Quaternion[] _attitude;
        int _groundMask;

        void Start()
        {
            _radarAltitude = radarAltitude.data;
            _attitude = attitude.data;
            _groundMask = groundMask.value;
        }

        void FixedUpdate()
        {
            RaycastHit hit;

            // rel = QInv(basis)⊗absRot。FEF 鉛直の view 表現は viewRot⊗(QInv(rel)⊗down)。
            Quaternion rel = _attitude[0];
            Quaternion inv = new Quaternion(-rel.x, -rel.y, -rel.z, rel.w);
            Vector3 dir = body.rotation * (inv * Vector3.down);

            if (Physics.Raycast(body.position, dir, out hit, maxDistance, _groundMask))
            {
                _radarAltitude[0] = hit.distance;
            }
            else
            {
                _radarAltitude[0] = maxDistance;
            }
        }
    }
}
