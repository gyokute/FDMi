using System;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 平面形 (区間ごとの前縁・後縁) から、FDMiWing が使う区間の値を焼き付ける。
    // 平面形は翼ローカル (コンポーネントの transform 基準) で、区間の値は body ローカル。
    // 翼を transform で動かす・回すと、その位置と取付角がそのまま効く (回転は迎角になる)。
    //
    // 区間 i は station i と i+1 の間にできる台形で、次のように決まる。
    //   コード長   = 2 station のコードの平均 (3次元の弦の長さ)
    //   面積       = 幅 × コード長。幅は弦に直交する向きの station 間距離 (台形の高さ)
    //   翼幅 (span)= 幅の合計。後退角を含まない投影翼幅になり、アスペクト比が下がる
    //   翼幅軸     = 25% 弦点 (空力中心) を結ぶ向き。後退角と上反角を含み、区間の気流はこの成分を落とす
    //
    // 後退角は翼幅軸の向きとして効く (simple sweep theory)。垂直流れが cosΛ になるので
    // 揚力傾斜が下がり、迎角は面内 (tanα/cosΛ) で測られ、マッハ補正は有効マッハ M cosΛ で引かれる。
    // 考え方は V1 の FDMi_wing_Editor.setPlanformData と同じ。V1 は面積に弦の Z 成分だけを使い、
    // 翼幅軸を「実際の向き」と「面内の向き」の半分ずつ混ぜていたが、ここは向きを分けて持つ。
    //
    // Editor 専用。ランタイムは焼き付いた値だけを読む (予め計算できるものは Editor で焼くという
    // FDMi の初期化の方針。FDMiWheelBake / FDMiJetEngineBake と同じ)。
    static class FDMiWingPlanform
    {
        // 区間の値。平面形から一意に決まる。
        public struct Section
        {
            public Vector3 coordPos;
            public Vector3 spanNormal;
            public Vector3 chordNormal;
            public float chord;
            public float width;
            public float area;
            // 後退角 [deg]。翼幅軸と面内の翼幅方向のなす角で、外側ほど後ろへずれる向きを正にする。
            public float sweepDeg;
        }

        // station の数。前縁・後縁の配列長 (区間数 + 1)。
        public static int Stations(FDMiWing wing)
        {
            return wing.leadingEdge != null ? wing.leadingEdge.Length : 0;
        }

        // 平面形がまだ置かれていない (station が無い、または全部が原点)。
        public static bool IsEmpty(FDMiWing wing)
        {
            if (wing.leadingEdge == null || wing.trailingEdge == null)
                return true;
            for (int j = 0; j < wing.leadingEdge.Length; j++)
                if (wing.leadingEdge[j] != Vector3.zero || wing.trailingEdge[j] != Vector3.zero)
                    return false;
            return true;
        }

        // 区間 i の値を求める。station が足りない・弦が潰れているときは false。
        public static bool Solve(Vector3[] leadingEdge, Vector3[] trailingEdge, int i, out Section section)
        {
            section = new Section();
            if (leadingEdge == null || trailingEdge == null)
                return false;
            if (i < 0 || i + 1 >= leadingEdge.Length || i + 1 >= trailingEdge.Length)
                return false;

            // 区間の中央の弦。coordPos はこの弦の 1/4 点 (V1 の CoordPos と同じ)。
            Vector3 le = (leadingEdge[i] + leadingEdge[i + 1]) * 0.5f;
            Vector3 te = (trailingEdge[i] + trailingEdge[i + 1]) * 0.5f;
            Vector3 chord = te - le;
            float chordLength = chord.magnitude;
            if (chordLength <= 0f)
                return false;
            Vector3 chordDir = chord / chordLength;

            // 翼幅軸は 25% 弦点 (空力中心) を結ぶ線の向き。亜音速の揚力は 1/4 弦線まわりに働くので、
            // これが後退角の基準になる (V1 も 25% 弦点で組んでいた)。直線テーパーの翼でも 1/4 弦線は
            // 後退するので、テーパーが後退角と同じ向きに効くこともここから出る。
            Vector3 q0 = leadingEdge[i] + (trailingEdge[i] - leadingEdge[i]) * 0.25f;
            Vector3 q1 = leadingEdge[i + 1] + (trailingEdge[i + 1] - leadingEdge[i + 1]) * 0.25f;
            Vector3 station = q1 - q0;
            float stationLength = station.magnitude;
            // 幅は弦に直交する向きの成分 (台形の高さ)。上反角は残るので、面積は翼面の面積になる。
            Vector3 planform = station - chordDir * Vector3.Dot(station, chordDir);
            float width = planform.magnitude;
            if (stationLength <= 0f || width <= 0f)
                return false;

            section.coordPos = le + chordDir * (0.25f * chordLength);
            section.chordNormal = chordDir;
            // 翼幅は右翼端向きにそろえる。迎角と揚力の向きがこの向きで決まるため (FDMiWing の注記)。
            // 垂直尾翼のように X 成分が無いときは並べた向きのままにする。
            section.spanNormal = station.x < 0f ? -station / stationLength : station / stationLength;
            section.chord = chordLength;
            section.width = width;
            section.area = chordLength * width;
            float sweep = Mathf.Acos(Mathf.Clamp(Vector3.Dot(section.spanNormal, planform / width), -1f, 1f)) * Mathf.Rad2Deg;
            // 外側の station が後ろへずれる向き (chordDir は後ろ向き) を正の後退角にする。
            section.sweepDeg = Vector3.Dot(station, chordDir) > 0f ? sweep : -sweep;
            return true;
        }

        // 平面形から区間の値を焼き付ける。区間数が変わっていれば配列もそろえる。
        // 値が変わらないときは書かない (Inspector や Scene を描くたびに呼ばれるため)。
        // 戻り値: 何か書き換えたかどうか。
        public static bool Bake(FDMiWing wing)
        {
            int sections = Stations(wing) - 1;
            // 2 station 未満では区間ができない。何もしない (手で入れた値を消さない)。
            if (sections < 1)
                return false;

            // 翼ローカルの平面形を body ローカルへ直してから解く。長さは剛体変換で変わらないので、
            // コード長・面積・翼幅はそのまま、位置と向きだけが transform に従う。
            Matrix4x4 toBody = ToBody(wing);
            Vector3[] leadingEdge = TransformPoints(wing.leadingEdge, toBody);
            Vector3[] trailingEdge = TransformPoints(wing.trailingEdge, toBody);

            bool changed = Align(wing, sections);
            float span = 0f;
            int solved = 0;
            for (int i = 0; i < sections; i++)
            {
                Section section;
                if (Solve(leadingEdge, trailingEdge, i, out section))
                {
                    solved++;
                    // 翼幅は投影翼幅 (弦に直交する幅) の合計。後退翼のアスペクト比はこれで下がる。
                    span += section.width;
                }
                else
                {
                    // 前縁と後縁が同じ (まだ置いていない) 区間。向きだけ既定にして面積0にする。
                    // 向きをゼロベクトルのままにすると、実行時の迎角と揚力が NaN になる。
                    section.coordPos = (leadingEdge[i] + leadingEdge[i + 1]) * 0.5f;
                    section.chordNormal = Vector3.back;
                    section.spanNormal = Vector3.right;
                    section.chord = 0f;
                    section.width = 0f;
                    section.area = 0f;
                    section.sweepDeg = 0f;
                }
                changed |= Write(wing, i, section);
            }
            // 1区間も解けないうちは翼幅を触らない (未配置の平面形で設計値を消さない)。
            if (solved > 0 && !Mathf.Approximately(wing.span, span))
            {
                wing.span = span;
                changed = true;
            }
            return changed;
        }

        // 区間数を変える。station (前縁・後縁の点) は区間数 + 1 点。
        public static bool SetSectionCount(FDMiWing wing, int sections)
        {
            int count = Mathf.Max(sections, 0);
            int stations = count + 1;
            int old = Stations(wing);
            Array.Resize(ref wing.leadingEdge, stations);
            Array.Resize(ref wing.trailingEdge, stations);
            if (IsEmpty(wing))
            {
                // 空の平面形は矩形から始める。ハンドルが原点に重ならないようにするため。
                Seed(wing, stations);
            }
            else
            {
                // 増えた station は直前の station の延長に置く (隣と重ならない初期値)。
                for (int j = Mathf.Max(old, 1); j < stations; j++)
                {
                    Vector3 leStep = j >= 2 ? wing.leadingEdge[j - 1] - wing.leadingEdge[j - 2] : Vector3.zero;
                    Vector3 teStep = j >= 2 ? wing.trailingEdge[j - 1] - wing.trailingEdge[j - 2] : Vector3.zero;
                    wing.leadingEdge[j] = wing.leadingEdge[j - 1] + leStep;
                    wing.trailingEdge[j] = wing.trailingEdge[j - 1] + teStep;
                }
            }
            return Bake(wing);
        }

        // 焼き付けで書き換わるオブジェクト (Undo の記録先)。翼と、その子の装置。
        public static UnityEngine.Object[] Targets(FDMiWing wing)
        {
            FDMiControlSurface[] devices = wing.GetComponentsInChildren<FDMiControlSurface>(true);
            UnityEngine.Object[] objects = new UnityEngine.Object[devices.Length + 1];
            objects[0] = wing;
            for (int i = 0; i < devices.Length; i++)
                objects[i + 1] = devices[i];
            return objects;
        }

        // 翼ローカル → body ローカル。body が未配線なら翼の transform を機体の基準にする
        // (描画の Frame の代用と同じ規則)。
        static Matrix4x4 ToBody(FDMiWing wing)
        {
            Transform body = wing.body != null ? wing.body.transform : wing.transform;
            return body.worldToLocalMatrix * wing.transform.localToWorldMatrix;
        }

        static Vector3[] TransformPoints(Vector3[] points, Matrix4x4 matrix)
        {
            Vector3[] result = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                result[i] = matrix.MultiplyPoint3x4(points[i]);
            return result;
        }

        // 差分があるときだけ書く。毎フレーム呼ばれるので scene を無駄に汚さない。
        static bool Write(FDMiWing wing, int i, Section section)
        {
            if (wing.coordPos[i] == section.coordPos
                && wing.spanNormal[i] == section.spanNormal
                && wing.chordNormal[i] == section.chordNormal
                && Mathf.Approximately(wing.sectionChord[i], section.chord)
                && Mathf.Approximately(wing.sectionArea[i], section.area))
                return false;
            wing.coordPos[i] = section.coordPos;
            wing.spanNormal[i] = section.spanNormal;
            wing.chordNormal[i] = section.chordNormal;
            wing.sectionChord[i] = section.chord;
            wing.sectionArea[i] = section.area;
            return true;
        }

        // 区間数の配列をそろえる。翼の区間配列と、翼の子の装置の区間配列。
        // 装置は区間ごとに値を出すので、長さが違うと実行時に添字が外れる。値は残し、伸びた分は既定値にする。
        static bool Align(FDMiWing wing, int sections)
        {
            bool changed = Resize(ref wing.coordPos, sections);
            changed |= Resize(ref wing.spanNormal, sections);
            changed |= Resize(ref wing.chordNormal, sections);
            changed |= Resize(ref wing.sectionChord, sections);
            changed |= Resize(ref wing.sectionArea, sections);
            changed |= Resize(ref wing.clAlpha, sections);
            changed |= Resize(ref wing.cdAlpha, sections);
            changed |= Resize(ref wing.clMach, sections);
            changed |= Resize(ref wing.cdMach, sections);
            changed |= Resize(ref wing.cmAlpha, sections);
            changed |= Resize(ref wing.alpha, sections);
            FillCurves(wing.clAlpha);
            FillCurves(wing.cdAlpha);
            FillCurves(wing.clMach);
            FillCurves(wing.cdMach);
            FillCurves(wing.cmAlpha);

            FDMiControlSurface[] devices = wing.GetComponentsInChildren<FDMiControlSurface>(true);
            for (int i = 0; i < devices.Length; i++)
            {
                Resize(ref devices[i].sectionMul, sections);
                Resize(ref devices[i].alpha, sections);
                Resize(ref devices[i].cl, sections);
                Resize(ref devices[i].cd, sections);
                Resize(ref devices[i].scale, sections);
            }
            return changed;
        }

        // 空の平面形 (station が無い・全部が原点) に矩形を置く。
        // アスペクト比6程度の矩形を初期値にする。あとはハンドルで直す前提。
        static void Seed(FDMiWing wing, int stations)
        {
            float span = wing.span > 0f ? wing.span : 12f;
            float chord = span / 6f;
            for (int j = 0; j < stations; j++)
            {
                float x = stations > 1 ? -span * 0.5f + span * j / (stations - 1) : 0f;
                wing.leadingEdge[j] = new Vector3(x, 0f, chord * 0.5f);
                wing.trailingEdge[j] = new Vector3(x, 0f, -chord * 0.5f);
            }
        }

        static bool Resize<T>(ref T[] array, int count)
        {
            if (array != null && array.Length == count)
                return false;
            Array.Resize(ref array, count);
            return true;
        }

        // 空のカーブは 0 を返すが、null は評価できない。
        static void FillCurves(AnimationCurve[] curves)
        {
            for (int i = 0; i < curves.Length; i++)
                if (curves[i] == null)
                    curves[i] = new AnimationCurve();
        }
    }
}
