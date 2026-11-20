using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 機体の質量特性 (質量・重心・慣性テンソル) を構造から一度だけ焼き付ける。
    //
    // 質量配分は体積比例。翼区間 = 薄板、胴体 = 一様円柱、エンジン = ナセル込みの円柱とみなし、
    // 生の体積の合計が designMass になるよう一様密度で正規化する。構造の大小 (翼面積・胴体径・
    // エンジン径) が配分と慣性に効き、designMass は燃料・搭載物を含む総質量として外から与える。
    // 密度そのものは出力しない。部品質量の比だけが意味を持つ。
    //
    // 重心の前後位置は主翼の MAC重心率 で与える (0.0 = MAC前縁、1.0 = MAC後縁)。
    // 左右・上下は質量配分から出した質量重心の x/y を使う。MAC の前縁と向きは、機体スコープ内で
    // 平面形面積が最大の FDMiWing の区間配列から求める。主翼の区間配列は FDMiWingPlanform が
    // body ローカルへ焼き付けたものを読む (Inspector を開いていない翼でも古い配列を読まないよう
    // ここで焼き直す)。
    //
    // 慣性テンソルは左右対称な機体を前提に対角項だけを出す。翼の後退角・上反角による非対角項は
    // 左右で相殺し、胴体軸とエンジン軸は機体 z 軸に沿う。非対称な配置 (片翼だけのエンジン等) は
    // このモデルの対象外。対角にすれば実行時の平行軸補正も不要になり、U# 側は焼き付け値を
    // Rigidbody に渡すだけで済む。
    //
    // テンソルは「焼き付け時の MAC重心率」の合成重心まわりで求める。実行時に率が変わっても
    // テンソルは固定 (FDMiBodyMass の注記)。設計点では基準点と CoM が一致する。
    //
    // 翼区間の質量重心は中央弦に置く。coordPos は空力中心 (1/4 弦) であり、質量重心ではない。
    // 翼の構造質量は翼箱 (15〜60% 弦) に集まるので、その中央にあたる 50% 弦を代表点にする。
    static class FDMiBodyMassBake
    {
        // 翼の厚み比。体積と薄板の厚み方向慣性にだけ効く。旅客機の主翼・尾翼の代表値。
        const float _wingThicknessRatio = 0.12f;

        // エンジンの代表長さ ÷ fan 直径。エンジン単体は約 2D だが、ナセルとアクセサリを含めると
        // 4D 程度になる。質量配分は体積比例なので、この値がエンジン質量の割合を決める。
        const float _engineLengthPerDiameter = 4f;

        // 体積を質量の代理として積算する。最後に一様密度を掛けるので、単位は m^3 のまま。
        struct Accumulator
        {
            public float volume;
            public Vector3 firstMoment;
            public float ixx;
            public float iyy;
            public float izz;

            public void Add(Vector3 center, float partVolume, Vector3 localInertia)
            {
                volume += partVolume;
                firstMoment += center * partVolume;
                ixx += localInertia.x + partVolume * (center.y * center.y + center.z * center.z);
                iyy += localInertia.y + partVolume * (center.x * center.x + center.z * center.z);
                izz += localInertia.z + partVolume * (center.x * center.x + center.y * center.y);
            }
        }

        public static void Bake(FDMiBodyMass mass)
        {
            Rigidbody rb = mass.body;
            if (rb == null)
                return;

            // 設計質量は初回だけ Rigidbody から拾う。実行時に質量が変わっても、ここで上書きしない。
            bool changed = false;
            if (mass.designMass <= 0f)
            {
                mass.designMass = rb.mass > 0f ? rb.mass : 1f;
                changed = true;
            }

            List<FDMiWing> wings = FDMiAircraftScope.FindAll<FDMiWing>(mass);
            List<FDMiBodyAero> bodyAeros = FDMiAircraftScope.FindAll<FDMiBodyAero>(mass);
            List<FDMiJetEngine> engines = FDMiAircraftScope.FindAll<FDMiJetEngine>(mass);

            Accumulator accumulator = new Accumulator();
            float wingVolume = 0f;
            FDMiWing mainWing = null;
            float mainWingArea = 0f;
            for (int i = 0; i < wings.Count; i++)
            {
                if (FDMiWingPlanform.Bake(wings[i]))
                    EditorUtility.SetDirty(wings[i]);
                wingVolume += AddWing(wings[i], ref accumulator);

                float area = WingArea(wings[i]);
                if (area > mainWingArea)
                {
                    mainWingArea = area;
                    mainWing = wings[i];
                }
            }

            float fuselageVolume = 0f;
            if (bodyAeros.Count > 0)
                fuselageVolume = AddFuselage(bodyAeros[0], rb, ref accumulator);

            float engineVolume = 0f;
            for (int i = 0; i < engines.Count; i++)
                engineVolume += AddEngine(engines[i], rb, ref accumulator);

            // 形状が未配置だと体積 0 になり、割り算が NaN になる。焼き付けを諦めて既存値を残す。
            if (accumulator.volume <= 0f || mainWing == null)
                return;

            SolveMac(mainWing, out Vector3 macLeadingEdge, out Vector3 macDirection, out float macChord);
            if (macChord <= 0f)
                return;

            // MAC重心率は焼き付け時のシーン値を読む。設計値フィールドは持たない。
            // プロキシの data はシーンに保存されている編集値。Udon 側から読み戻すと、
            // まだ CopyProxyToUdon していない古いヒープ値で上書きされることがあるため読まない。
            float macFraction = mass.MacCG.data[0];

            Vector3 massCenterOfMass = accumulator.firstMoment / accumulator.volume;
            Vector3 centerOfMass = new Vector3(
                massCenterOfMass.x,
                massCenterOfMass.y,
                macLeadingEdge.z + macDirection.z * macChord * macFraction
            );

            float density = mass.designMass / accumulator.volume;

            // 原点まわりの慣性から、合成重心まわりの慣性へ平行軸の定理で移す。
            // 体積で積算しているので、最後に密度を掛けると kg m^2 になる。
            Vector3 inertiaTensor = new Vector3(
                Mathf.Max(0f, accumulator.ixx - accumulator.volume * (centerOfMass.y * centerOfMass.y + centerOfMass.z * centerOfMass.z)) * density,
                Mathf.Max(0f, accumulator.iyy - accumulator.volume * (centerOfMass.x * centerOfMass.x + centerOfMass.z * centerOfMass.z)) * density,
                Mathf.Max(0f, accumulator.izz - accumulator.volume * (centerOfMass.x * centerOfMass.x + centerOfMass.y * centerOfMass.y)) * density
            );

            changed |= Assign(ref mass.centerOfMass, centerOfMass);
            changed |= Assign(ref mass.inertiaTensor, inertiaTensor);
            changed |= Assign(ref mass.macZ0, macLeadingEdge.z);
            changed |= Assign(ref mass.macSlope, macDirection.z * macChord);
            changed |= Assign(ref mass.bakedMacFraction, macFraction);
            changed |= Assign(ref mass.macLeadingEdge, macLeadingEdge);
            changed |= Assign(ref mass.macDirection, macDirection);
            changed |= Assign(ref mass.macChord, macChord);
            changed |= Assign(ref mass.wingMass, wingVolume * density);
            changed |= Assign(ref mass.fuselageMass, fuselageVolume * density);
            changed |= Assign(ref mass.engineMass, engineVolume * density);
            if (mass.mainWing != mainWing)
            {
                mass.mainWing = mainWing;
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(mass);
                // 焼き付け値は Udon のヒープにも書く。プロキシだけを dirty にすると、次回の
                // シーン読み込みで Udon 側の古い値がプロキシへ戻され、ランタイムが古い重心・
                // テンソルを使う。All は public 配列や参照も含めて丸ごとコピーする。
                UdonSharpEditorUtility.CopyProxyToUdon(mass, ProxySerializationPolicy.All);
                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(mass));
            }

            // Editor の Rigidbody も焼き付け結果に合わせる。合わせないと Scene view の CoM 表示や
            // 他コンポーネントの Inspector が古い値を見る。自動重心・自動テンソルは切る。切らないと
            // centerOfMass / inertiaTensor への代入が無視される。
            bool rbChanged = false;
            if (rb.automaticCenterOfMass)
            {
                rb.automaticCenterOfMass = false;
                rbChanged = true;
            }
            if (rb.automaticInertiaTensor)
            {
                rb.automaticInertiaTensor = false;
                rbChanged = true;
            }
            if (!Mathf.Approximately(rb.mass, mass.designMass))
            {
                rb.mass = mass.designMass;
                rbChanged = true;
            }
            if (rb.centerOfMass != centerOfMass)
            {
                rb.centerOfMass = centerOfMass;
                rbChanged = true;
            }
            if (rb.inertiaTensor != inertiaTensor)
            {
                rb.inertiaTensor = inertiaTensor;
                rbChanged = true;
            }
            if (rb.inertiaTensorRotation != Quaternion.identity)
            {
                rb.inertiaTensorRotation = Quaternion.identity;
                rbChanged = true;
            }
            if (rbChanged)
                EditorUtility.SetDirty(rb);
        }

        // 主翼の MAC (平均空力翼弦)。面積加重平均で、区間配列に対する厳密な離散 MAC。
        // 前縁は各区間の 1/4 弦点から 0.25c 戻った点。向きは面積加重平均の弦方向。
        static void SolveMac(FDMiWing wing, out Vector3 leadingEdge, out Vector3 direction, out float chord)
        {
            leadingEdge = Vector3.zero;
            direction = Vector3.back;
            chord = 0f;
            if (wing.coordPos == null || wing.chordNormal == null || wing.sectionChord == null || wing.sectionArea == null)
                return;

            int count = Mathf.Min(
                Mathf.Min(wing.coordPos.Length, wing.chordNormal.Length),
                Mathf.Min(wing.sectionChord.Length, wing.sectionArea.Length)
            );

            float areaSum = 0f;
            float chordSum = 0f;
            Vector3 leadingEdgeSum = Vector3.zero;
            Vector3 directionSum = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                float sectionChord = wing.sectionChord[i];
                float area = wing.sectionArea[i] * wing.wingAreaCoef;
                if (sectionChord <= 0f || area <= 0f)
                    continue;

                Vector3 chordDir = wing.chordNormal[i].normalized;
                Vector3 sectionLeadingEdge = wing.coordPos[i] - chordDir * (0.25f * sectionChord);
                areaSum += area;
                chordSum += area * sectionChord;
                leadingEdgeSum += sectionLeadingEdge * area;
                directionSum += chordDir * area;
            }

            if (areaSum <= 0f)
                return;

            chord = chordSum / areaSum;
            leadingEdge = leadingEdgeSum / areaSum;
            if (directionSum.sqrMagnitude > 0f)
                direction = directionSum.normalized;
        }

        static float WingArea(FDMiWing wing)
        {
            if (wing.sectionArea == null)
                return 0f;
            float area = 0f;
            for (int i = 0; i < wing.sectionArea.Length; i++)
                area += wing.sectionArea[i];
            return area * wing.wingAreaCoef;
        }

        // 翼1枚ぶんの体積を積算する。戻り値は翼の体積。
        static float AddWing(FDMiWing wing, ref Accumulator accumulator)
        {
            float volume = 0f;
            if (wing.coordPos == null || wing.spanNormal == null || wing.chordNormal == null
                || wing.sectionChord == null || wing.sectionArea == null)
                return 0f;

            int count = Mathf.Min(
                Mathf.Min(wing.coordPos.Length, wing.spanNormal.Length),
                Mathf.Min(wing.chordNormal.Length, Mathf.Min(wing.sectionChord.Length, wing.sectionArea.Length))
            );

            for (int i = 0; i < count; i++)
            {
                float chord = wing.sectionChord[i];
                float area = wing.sectionArea[i] * wing.wingAreaCoef;
                if (chord <= 0f || area <= 0f)
                    continue;

                Vector3 chordDir = wing.chordNormal[i].normalized;
                Vector3 spanDir = Vector3.ProjectOnPlane(wing.spanNormal[i], chordDir);
                if (spanDir.sqrMagnitude <= 0f)
                    continue;
                spanDir.Normalize();
                Vector3 thicknessDir = Vector3.Cross(spanDir, chordDir).normalized;

                float width = area / chord;
                float thickness = chord * _wingThicknessRatio;
                float sectionVolume = area * thickness;
                float iSpan = sectionVolume * (chord * chord + thickness * thickness) / 12f;
                float iChord = sectionVolume * (width * width + thickness * thickness) / 12f;
                float iThickness = sectionVolume * (width * width + chord * chord) / 12f;

                // coordPos は 1/4 弦。0.25c 後ろへずらすと 50% 弦 (質量重心の代表点) になる。
                Vector3 center = wing.coordPos[i] + chordDir * (0.25f * chord);
                accumulator.Add(center, sectionVolume, Diagonal(spanDir, chordDir, thicknessDir, iSpan, iChord, iThickness));
                volume += sectionVolume;
            }
            return volume;
        }

        // 胴体1本ぶんの体積を積算する。戻り値は胴体の体積。
        static float AddFuselage(FDMiBodyAero bodyAero, Rigidbody rb, ref Accumulator accumulator)
        {
            float radius = bodyAero.diameter * 0.5f;
            float length = bodyAero.length;
            if (radius <= 0f || length <= 0f)
                return 0f;

            Vector3 axis = bodyAero.axis.normalized;
            if (axis.sqrMagnitude <= 0f)
                axis = Vector3.forward;
            Basis(axis, out Vector3 u, out Vector3 v);

            float volume = Mathf.PI * radius * radius * length;
            float iAxis = 0.5f * volume * radius * radius;
            float iTransverse = volume * (3f * radius * radius + length * length) / 12f;
            Vector3 center = rb.transform.InverseTransformPoint(bodyAero.transform.position);
            accumulator.Add(center, volume, Diagonal(axis, u, v, iAxis, iTransverse, iTransverse));
            return volume;
        }

        // エンジン1基ぶんの体積を積算する。戻り値はエンジンの体積。
        static float AddEngine(FDMiJetEngine engine, Rigidbody rb, ref Accumulator accumulator)
        {
            float diameter = engine.fanDiameter;
            if (diameter <= 0f)
                return 0f;

            Vector3 axis = rb.transform.InverseTransformDirection(engine.transform.forward).normalized;
            if (axis.sqrMagnitude <= 0f)
                axis = Vector3.forward;
            Basis(axis, out Vector3 u, out Vector3 v);

            float radius = diameter * 0.5f;
            float length = diameter * _engineLengthPerDiameter;
            float volume = Mathf.PI * radius * radius * length;
            float iAxis = 0.5f * volume * radius * radius;
            float iTransverse = volume * (3f * radius * radius + length * length) / 12f;
            Vector3 center = rb.transform.InverseTransformPoint(engine.transform.position);
            accumulator.Add(center, volume, Diagonal(axis, u, v, iAxis, iTransverse, iTransverse));
            return volume;
        }

        // 直交基底 (axis, u, v)。軸が上下に近いときは right を基準にして縮退を避ける。
        static void Basis(Vector3 axis, out Vector3 u, out Vector3 v)
        {
            Vector3 reference = Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right;
            u = Vector3.Cross(axis, reference).normalized;
            v = Vector3.Cross(axis, u).normalized;
        }

        // 直交基底 (e0, e1, e2) と主慣性モーメント (i0, i1, i2) から、body 軸の対角成分を出す。
        // 非対角成分は左右対称で相殺するので計算しない (クラスの注記を参照)。
        static Vector3 Diagonal(Vector3 e0, Vector3 e1, Vector3 e2, float i0, float i1, float i2)
        {
            return new Vector3(
                i0 * e0.x * e0.x + i1 * e1.x * e1.x + i2 * e2.x * e2.x,
                i0 * e0.y * e0.y + i1 * e1.y * e1.y + i2 * e2.y * e2.y,
                i0 * e0.z * e0.z + i1 * e1.z * e1.z + i2 * e2.z * e2.z
            );
        }

        // 値が変わったときだけ書く。Inspector を開くたびに呼ばれるので scene を無駄に汚さない。
        static bool Assign(ref float target, float value)
        {
            if (Mathf.Approximately(target, value))
                return false;
            target = value;
            return true;
        }

        static bool Assign(ref Vector3 target, Vector3 value)
        {
            if (target == value)
                return false;
            target = value;
            return true;
        }
    }
}
