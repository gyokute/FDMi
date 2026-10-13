using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 1枚翼パネルの空気力。揚力・抗力 + 他翼からの吹き下ろし (Vni) を controlPoint に加える。
    // 座標はすべて機体固定 (body ローカル)。+Z = 前方。
    // 旧 aerodynamics からの移植。下記を直した:
    // - 揚力の向きを spanNormal x DNormal (旧版は逆で下面向きだった)。
    // - 揚力は cos 補正なし (Cl が垂直成分を定義するため。二重補正だった)。
    // - 空力中心の回転ずれは Editor 焼き付けの bakedChordNormal で受ける (実行時の毎刻 Inverse を削減)。
    // - 循環 Gamma は焼き付け rhoRef・1ステップ遅れ参照で解く (旧版は self-Gamma の Lerp で発散余地)。
    //
    // 干渉モデル: Vni = Σ Qij * Gamma_j。Qij は Editor が馬蹄渦 (Biot-Savart) で焼き付ける。
    // 他翼が未計算の刻は前刻の Gamma を使う (Gauss-Seidel 1ステップ遅れ。陽解法の連立回避)。
    public class FDMiWing : FDMiBehaviour
    {
        [Header("Wiring")]
        public Rigidbody body;
        public FDMiVector3 AirSpeed; // 機体座標の対気速度 [m/s]
        public FDMiFloat Rho; // [kg/m3]
        public FDMiFloat SonicSpeed; // [m/s]

        [Header("Aero maps")]
        public AnimationCurve Cl_Alpha = AnimationCurve.Linear(-15f, -1f, 15f, 1f);
        public AnimationCurve Cd_Alpha = AnimationCurve.Linear(-15f, 0.05f, 15f, 0.05f);
        public AnimationCurve Cl_Mach = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public AnimationCurve Cd_Mach = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Interference")]
        public FDMiWing[] affectWing; // 干渉を与える翼 (通常は自分以外すべて)
        public FDMiWingSection SectionL; // 左端断面
        public FDMiWingSection SectionR; // 右端断面

        [Header("Bake results (Editor が設定。触らない)")]
        public Vector3 spanNormal = Vector3.right; // 翼幅方向 (右+)
        public Vector3 chordNormal; // コード方向 (後方+。焼き付け)
        public Vector3 controlPoint; // 空力中心 (body ローカル) [m]
        public float cpChordLength = 1f; // 代表コード [m]
        public float cpSpanLength = 1f; // パネル幅 [m]
        public float cpArea = 1f; // パネル面積 [m2]
        public Vector3[] Qij = new Vector3[0]; // affectWing ごとの干渉係数 [1/m]
        public float rhoRef = 1.225f; // 焼き付け時密度 [kg/m3]

        [Header("Telemetry")]
        public float Lift; // [N]
        public float Drag; // [N]
        public float Gamma; // 循環 [m2/s]
        public Vector3 Force; // 機体座標の合力 [N]

        Vector3[] _air;
        float[] _rho, _sonic;

        void Start()
        {
            _air = AirSpeed.data;
            _rho = Rho.data;
            _sonic = SonicSpeed.data;
        }

        void FixedUpdate()
        {
            if (body == null)
                return;

            Vector3 omega = Quaternion.Inverse(body.rotation) * body.angularVelocity;
            Vector3 cpAir = -_air[0] - Vector3.Cross(omega, controlPoint);

            Vector3 vni = Vector3.zero;
            for (int j = 0; j < affectWing.Length; j++)
            {
                if (affectWing[j] != null)
                    vni += Qij[j] * affectWing[j].Gamma;
            }
            cpAir += vni;
            cpAir = Vector3.ProjectOnPlane(cpAir, spanNormal);

            float v = cpAir.magnitude;
            float mach = v / Mathf.Max(_sonic[0], 0.1f);
            float alpha = Vector3.SignedAngle(cpAir, chordNormal, spanNormal);

            float cl = Cl_Alpha.Evaluate(alpha) * Cl_Mach.Evaluate(mach);
            float cd = Cd_Alpha.Evaluate(alpha) * Cd_Mach.Evaluate(mach);

            float qS = 0.5f * _rho[0] * v * cpArea;
            Lift = qS * cl;
            Drag = qS * cd;

            Vector3 dN = v > 1e-4f ? cpAir / v : chordNormal;
            Vector3 lN = Vector3.Cross(spanNormal, dN);
            Force = Lift * lN + Drag * dN + _rho[0] * Gamma * cpSpanLength * vni;

            float halfV = 0.5f * v;
            Gamma = halfV * cl * cpChordLength * (_rho[0] / Mathf.Max(rhoRef, 1e-4f));

            body.AddForceAtPosition(body.rotation * Force, body.position + body.rotation * controlPoint);
        }
    }
}
