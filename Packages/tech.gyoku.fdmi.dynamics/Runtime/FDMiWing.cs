using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 複数区間の一点空力モデル。V1 (tag 1.0.0 の FDMi_wing) の計算をそのまま持ち込んだ。
    // 区間ごとの対気速度から Cl/Cd/Cm カーブを引き、合力と合モーメントを機体 Rigidbody に渡す。
    //
    // 座標はすべて機体固定 (body ローカル、+Z = 前方、+X = 右)。本コンポーネントの transform は使わない。
    // 区間は coordPos / spanNormal / chordNormal / sectionChord / sectionArea の配列で与える。
    // V1 の Editor が wingTip/wingToe から焼き付けていた値であり、いまは直接編集する。
    // 配列は翼面1枚の全域 (左右の翼端まで) を覆う。面積の合計がその翼の面積になる。
    //
    // V1 からの変更は入力の受け方だけ。SaccFlight の airData/sharedBool を FDMiFloat/FDMiVector3 に置き換え、
    // パス文字列 (FDMiDataPath) で名前解決するようにした。~ は機体 (名前空間ルート) の起点。
    // 力の適用は V1 と同じ AddRelativeForce / AddRelativeTorque。区間ごとの作用点は与えず、
    // 空力中心まわりのモーメントを Cm 項と重心オフセットの外積で明示的に組む。
    //
    // カーブは2次元 (翼型) の値として与える。スパン方向の分布はカーブ側で表し、3次元の循環は
    // 起動時に一度だけ求めた倍率で表す。これで区間ごとに掛け算と足し算が1回ずつ増えるだけになる。
    //   a3D = 2π AR / (2 + √(4 + (AR/κ)²)),  κ = 翼型の2次元揚力傾斜 / 2π
    // 翼単独の揚力傾斜 (DATCOM、M=0・後退角0)。κ=1 でヘルムボルトの式に一致する。
    //   k = 1 / (π AR e),  e = 1.78 (1 - 0.045 AR^0.68) - 0.64
    // オズワルド係数はレイマーの近似。AR 3〜8 向けの式なので 0.6〜0.95 で切る。
    // 誘導抗力は2次元抗力の外側で足す。マッハ補正カーブの意味 (2次元抗力への倍率) を変えないため。
    //
    // 操舵面と高揚力装置は FDMiControlSurface が区間ごとの増分として出す。装置1つが1コンポーネントで、
    // 区間配列の並びは本コンポーネントの区間に合わせる。増分は装置側で舵角から求める。
    //   動翼   : 等価迎え角 Δα = τ(c_e/c) δ。揚力の増分は区間の曲線の傾斜が決める
    //   フラップ: 加算揚力 ΔClmax (δ/δmax) と、舵角の2乗で増える抗力
    //   スラット: 失速角の延長 ΔClmax / 2π と、減ったぶんを戻す加算揚力
    //   スポイラ: 揚力倍率 1 - 2.5 (c_sp/c) sinδ と、平板の抗力 2 (c_sp/c) sin²δ
    // 装置のピッチングモーメントは区間の曲線に持たせず、装置が作った揚力増分の 1/4 弦ぶんとして
    // ここで組む。負で機首下げ (cmAlpha と同じ向きの規約)。フラップは機首下げ、スポイラは機首上げになる。
    // 装置の等価迎え角シフトは計測値 alpha に入れない。AoA は機体の向きの計測値だから。
    //
    // 胴体の持上げ (翼-胴干渉) は倍率1つ。DATCOM の図表法 (4.3.1.2 の K_B, K_W(B), K_B(W)) は
    // 閉じた式にできないため、前実装の経験式をそのまま使う。胴体自身の揚力は FDMiBodyAero が
    // 別に持っているので、この倍率が担うのは翼側の増分だけ。d/b=0.1 で3%。
    //   Kwb = 1 + 0.3 (d / b)
    //
    // 上流翼の後流 (翼-翼干渉) は下流側の翼だけが持つ。吹き下ろし角は迎え角の減算1回になる。
    //   ε = Gd CL_src,  Gd = 2 / (π AR_src) × 1 / (1 + 4 (Δy / b_src)²)
    // 遠方渦の近似 (NACA TR-648/651)。高さの減衰項は2本の trailing vortex の厳密解。
    // 前後距離を式に入れないのは、この渦モデルでは縦距離に依らず、TR-651 はむしろ翼の後方で
    // ε が増えるとするため (前実装の 1/√ 減衰は採らない)。上流翼の公開値は前ステップのものを
    // 読み、1段遅れは許容する。翼自身の吹き下ろしは独立項にしない。循環が自分に作る下向き速度は
    // 3次元揚力傾斜の減少と誘導抗力として既に表れているので、別項にすると二重計上になる。
    // 胴体も吹き下ろしを受けない。胴体の垂直力はクロスフロー (循環由来ではなく速度成分そのもの)
    // で、後流の下向き成分は速度ベクトルに既に含まれているため、足すと二重に効く。
    //
    // 地面効果は電波高度計の対地高さから出す。φ はレイマーの地面効果係数で、誘導抗力の残存率。
    // 効果の強さ ge = 1 - φ を持つ (φ を効果量として直接引くと、高度と共に効果が強くなる)。
    //   φ = (16 h / b)² / (1 + (16 h / b)²)
    // 誘導抗力は φ 倍。揚力傾斜は2次元値 (倍率1) へ ge の割合で寄せる。アスペクト比を 1/(1-ge)
    // 倍する鏡像渦の扱いと両端で一致し、途中の差は1%未満。2次元傾斜を超えない上限も自然に保証される。
    // 胴体は地面効果を持たない (鏡像渦が効くのは循環だけで、クロスフローに同じ機構がない)。
    // 地面効果で吹き下ろしが変わる分は持たない。
    // 地面効果も吹き下ろしも水平翼を前提にする。翼面が鉛直な尾翼では高さの解釈が変わり、
    // 地面効果は効き過ぎる。その用途では heightInput と sourceWing を配線しない。
    //
    // 扱わない: スパン方向の積分、後退角とマッハの3次元補正 (後退角は上式に tan²Λ を1項足す
    // だけなので、必要になった時に1フィールド追加する)、後流欠損 (動圧比。制動板の後流が下流翼の
    // 動圧を落とす分は持たない)、失速の切り方、超音速、三角翼の渦揚力。
    public class FDMiWing : FDMiBehaviour
    {
        [Header("配線")]
        // 力を加える機体の Rigidbody。Editor が機体 (名前空間ルート) から解決する。
        public Rigidbody body;
        public string WindPath = "~/Physics/Wind";

        // 風速ベクトル。世界座標。未接続なら無風。単位は m/s。
        [FDMiDataPath(nameof(WindPath))]
        public FDMiVector3 windInput;
        public string DensityPath = "~/Physics/Rho";

        // 空気密度。未接続なら rho0 を使う。単位は kg/m^3。
        [FDMiDataPath(nameof(DensityPath))]
        public FDMiFloat densityInput;
        public string SonicSpeedPath = "~/Physics/SonicSpeed";

        // 音速。未接続なら sonicSpeed0 を使う。単位は m/s。
        [FDMiDataPath(nameof(SonicSpeedPath))]
        public FDMiFloat sonicSpeedInput;
        public string HeightPath = "~/Physics/RadarAltitude";

        // 対地高さ。電波高度計模擬の代表値。未接続なら地面効果なし。単位は m。
        [FDMiDataPath(nameof(HeightPath))]
        public FDMiFloat heightInput;
        // 操舵面と高揚力装置。区間ごとの等価迎え角・加算揚力・加算抗力・揚力倍率をここから読む。
        // Editor がこの翼の子から解決する。装置は翼の子に置く。
        public FDMiControlSurface[] controlSurface;
        // 上流翼。吹き下ろしを受ける側だけが指定する。未接続なら吹き下ろしなし。
        public FDMiWing sourceWing;
        // 胴体。持上げ倍率に直径を読む。未接続なら翼単独として扱う。Editor が機体から解決する。
        public FDMiBodyAero bodyAero;

        [Header("区間 (body ローカル)")]
        // 空力中心。力の作用点であり、対気速度の測定点でもある。単位は m。
        public Vector3[] coordPos;
        // 翼幅方向の単位ベクトル。区間の気流はこの方向の成分を落とす。
        // 迎え角と揚力の符号はこの向きで決まる。水平翼では右翼端向き (+X) にそろえる。
        // 左翼端向きにすると迎え角の符号が反転し、キャンバー付きのカーブでは揚力が逆になる。
        public Vector3[] spanNormal;
        // 弦線の向き。V1 と同じく後ろ向き (-Z) を基準にする。迎え角はここから気流までの符号付き角度。
        public Vector3[] chordNormal;
        // 区間の平均コード長。Cm 項の基準長。単位は m。
        public float[] sectionChord;
        // 区間の面積。単位は m^2。
        public float[] sectionArea;

        [Header("カーブ (区間ごと)")]
        // 2次元揚力係数。迎え角 [deg] に対する値。
        public AnimationCurve[] clAlpha;
        // 2次元抗力係数。
        public AnimationCurve[] cdAlpha;
        // 揚力係数のマッハ補正倍率。
        public AnimationCurve[] clMach;
        // 抗力係数のマッハ補正倍率。
        public AnimationCurve[] cdMach;
        // ピッチングモーメント係数。
        public AnimationCurve[] cmAlpha;

        [Header("平面形 (設計値)")]
        // 翼幅。アスペクト比、地面効果の高さ、上流翼からの上下差の基準。単位は m。
        public float span = 12f;
        // 翼型の2次元揚力傾斜。clAlpha の直線部の傾きに合わせる。薄翼の 2π を初期値とする。単位は 1/rad。
        public float airfoilLiftSlope = 6.283185f;

        [Header("設計値")]
        // 面積の全体倍率。V1 の翼面積補正。
        public float wingAreaCoef = 1f;
        // 海面標準大気の密度。densityInput 未接続時の値。単位は kg/m^3。
        public float rho0 = 1.225f;
        // 海面の音速。sonicSpeedInput 未接続時の値。単位は m/s。
        public float sonicSpeed0 = 340.3f;

        [Header("係数 (Start で求める)")]
        // アスペクト比。起動時に区間の面積から求める。
        public float outAR;
        // 3次元揚力傾斜 ÷ 2次元揚力傾斜 × 胴体持上げ。clAlpha に掛ける倍率。
        public float outLiftGain;
        // 誘導抗力係数。cl の2乗に掛ける。
        public float outInducedK;
        // 吹き下ろし角の揚力係数微分。上流翼の CL に掛ける。単位は deg。
        public float outDownwashGain;

        [Header("計測値")]
        // 区間の迎え角。吹き下ろしの加算込み。装置の等価迎え角シフトは含めない。coordPos と同じ長さにする。単位は度。
        public float[] alpha;
        // 面積加重平均の3次元揚力係数。下流翼が読む。
        public float outCL;
        // いま効いている吹き下ろし角。単位は度。
        public float outEpsilonDeg;
        // 地面効果の強さ。0 が効果なし、1 が地面。地面効果の計測値でもある。
        public float outGroundFactor;
        // 直前のステップで加えた合力。機体座標。単位は N。
        public Vector3 totalForce;
        // 直前のステップで加えた合モーメント。機体座標。単位は N m。
        public Vector3 totalMoment;

        // 翼面積。区間配列から一度だけ求める。アスペクト比と outCL の分母に使う。
        float _wingArea;

        // 装置の揚力増分が作るピッチングモーメントの係数。1/4 弦まわりの値で、負が機首下げ。
        // DATCOM のフラップの Cm チャートは揚力増分のおよそ 1/4。装置ごとに持たせず1定数にまとめる。
        // スポイラは 0.7 弦あたりで揚力を落とすので実際はもう少し大きいが、符号と桁は合う。
        const float _cmPerCl = -0.25f;

        void Start()
        {
            // 翼面積。区間配列は翼面1枚の全域を覆う前提なので、単純に足せば平面形の面積になる。
            _wingArea = 0f;
            for (int i = 0; i < sectionArea.Length; i++)
                _wingArea += sectionArea[i];
            _wingArea *= wingAreaCoef;

            // 3次元揚力傾斜。κ は翼型の揚力傾斜効率。κ=1 でヘルムボルトの式になる。
            float ar = span * span / _wingArea;
            float kappa = airfoilLiftSlope / (2f * Mathf.PI);
            float arK = ar / kappa;
            float a3D = 2f * Mathf.PI * ar / (2f + Mathf.Sqrt(4f + arK * arK));

            // 胴体の持上げ。未接続なら翼単独。
            float kwb = 1f;
            if (bodyAero != null)
                kwb = 1f + 0.3f * bodyAero.diameter / span;

            outAR = ar;
            outLiftGain = a3D / airfoilLiftSlope * kwb;

            // 誘導抗力係数。オズワルド係数はレイマーの近似。
            float e = 1.78f * (1f - 0.045f * Mathf.Pow(ar, 0.68f)) - 0.64f;
            outInducedK = 1f / (Mathf.PI * ar * Mathf.Clamp(e, 0.6f, 0.95f));

            // 上流翼の吹き下ろし。上流翼の面積も自分の Start で数える (Start の実行順に依らない)。
            outDownwashGain = 0f;
            if (sourceWing != null)
            {
                float srcArea = 0f;
                for (int i = 0; i < sourceWing.sectionArea.Length; i++)
                    srcArea += sourceWing.sectionArea[i];
                srcArea *= sourceWing.wingAreaCoef;
                float srcAR = sourceWing.span * sourceWing.span / srcArea;
                float hb = (MeanY(coordPos) - MeanY(sourceWing.coordPos)) / sourceWing.span;
                outDownwashGain = 2f / (Mathf.PI * srcAR) * (1f / (1f + 4f * hb * hb)) * Mathf.Rad2Deg;
            }
        }

        // 区間配列の高さの平均。翼-翼の上下差を配列から導出するために使う。
        float MeanY(Vector3[] points)
        {
            float sum = 0f;
            for (int i = 0; i < points.Length; i++)
                sum += points[i].y;
            return sum / points.Length;
        }

        void FixedUpdate()
        {
            // 入力。未接続の端子は無風・海面標準・地面効果なしとして扱う。
            Vector3 wind = Vector3.zero;
            if (windInput != null)
                wind = windInput.Data;
            float rho = rho0;
            if (densityInput != null)
                rho = densityInput.Data;
            float sonicSpeed = sonicSpeed0;
            if (sonicSpeedInput != null)
                sonicSpeed = sonicSpeedInput.Data;

            // 地面効果。φ が誘導抗力の残存率、ge = 1 - φ が効果の強さ。区間ごとではなく翼ごとに1回。
            float ge = 0f;
            if (heightInput != null)
            {
                float r = 16f * heightInput.Data / span;
                ge = 1f / (1f + r * r);
            }
            float inducedK = outInducedK * (1f - ge);
            float liftGain = outLiftGain + (1f - outLiftGain) * ge;

            // 上流翼の吹き下ろし角。前ステップの公開値を読む。
            outEpsilonDeg = 0f;
            if (sourceWing != null)
                outEpsilonDeg = outDownwashGain * sourceWing.outCL;

            // 機体座標で見た対気速度と角速度。V1 と同じく「風 - 機体速度」を気流の正方向とする。
            Quaternion invRot = Quaternion.Inverse(body.rotation);
            Vector3 airspeed = invRot * (wind - body.velocity);
            Vector3 airAngularVelocity = invRot * body.angularVelocity;
            Vector3 cogOffset = body.centerOfMass;

            totalForce = Vector3.zero;
            totalMoment = Vector3.zero;
            float clAreaSum = 0f;
            for (int i = 0; i < coordPos.Length; i++)
            {
                // 装置の寄与を区間ごとに合算する。等価迎え角と揚力・抗力は加算、揚力倍率は積で効かせる。
                float csAlpha = 0f;
                float csCl = 0f;
                float csCd = 0f;
                float csScale = 1f;
                for (int j = 0; j < controlSurface.Length; j++)
                {
                    FDMiControlSurface cs = controlSurface[j];
                    csAlpha += cs.alpha[i];
                    csCl += cs.cl[i];
                    csCd += cs.cd[i];
                    csScale *= cs.scale[i];
                }

                // 区間の対気速度。並進に回転の誘起速度を足し、スパン方向成分を落とす。
                Vector3 sectionAir = airspeed - Vector3.Cross(airAngularVelocity, coordPos[i]);
                sectionAir = Vector3.ProjectOnPlane(sectionAir, spanNormal[i]);

                // 下流翼は上流翼の後流の分だけ迎え角が下がる。
                alpha[i] = Vector3.SignedAngle(chordNormal[i], sectionAir, spanNormal[i]) - outEpsilonDeg;

                // カーブを引く迎え角。装置の等価迎え角シフトはここだけに足す。
                float aeff = alpha[i] + csAlpha;

                // 動圧×面積。コサインは翼面の投影面積。スパン方向の積分はしない。
                float spd = sectionAir.magnitude;
                float c = 0.5f * rho * spd * spd * sectionArea[i] * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * aeff));
                c *= wingAreaCoef;
                float mach = spd / sonicSpeed;

                // 抗力は気流方向、揚力はそれと直交する向き。
                Vector3 cdNormal = Vector3.Normalize(sectionAir);
                Vector3 clNormal = Vector3.Cross(spanNormal[i], cdNormal);

                // 揚力は3次元の循環の倍率を掛ける。抗力は2次元の値に誘導抗力を足す。
                float machCl = clMach[i].Evaluate(mach);
                float clCurve = clAlpha[i].Evaluate(aeff);
                float clClean = clCurve * liftGain * machCl;
                float cl = (clCurve + csCl) * csScale * liftGain * machCl;
                float cd = (cdAlpha[i].Evaluate(aeff) + csCd) * cdMach[i].Evaluate(mach) + inducedK * cl * cl;
                // 装置が作った揚力増分が連れてくる 1/4 弦まわりのモーメント。
                float cm = cmAlpha[i].Evaluate(aeff) + _cmPerCl * (cl - clClean);
                Vector3 cForce = cl * clNormal + cd * cdNormal;
                totalForce += c * cForce;
                totalMoment += c * (-sectionChord[i] * (cm * spanNormal[i])
                    + Vector3.Cross(coordPos[i] - cogOffset, cForce));
                clAreaSum += cl * sectionArea[i];
            }
            outCL = clAreaSum / _wingArea;
            outGroundFactor = ge;
            body.AddRelativeForce(totalForce);
            body.AddRelativeTorque(totalMoment);
        }
    }
}
