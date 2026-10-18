using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 空力ジオメトリのワイヤーフレーム。FDMiWing / FDMiControlSurface / FDMiBodyAero の Inspector が
    // 同じ絵を使うので1箇所に置く (V1 は FDMi_wing_Editor が static で持っていた)。
    //
    // 座標はすべて body ローカルで描く。呼び出し側が body の localToWorldMatrix を
    // Handles.DrawingScope に立てる。コンポーネント自身の transform を基準にしない理由: ランタイムが
    // transform を使わないので、置いた場所と空力の位置は一致しない (body 未配線のときだけ自分の
    // transform で代用し、配線前でも置いた場所で見えるようにする)。
    static class FDMiAeroGeometry
    {
        // スポイラのヒンジ位置。前縁からコード比で測る。FDMiWing は揚力の欠損を 0.7 弦の位置と
        // みなして Cm を組んでいるので、その位置にパネルを描く (幾何は設計値ではない)。
        const float _spoilerHinge = 0.7f;

        // 機体座標の基準。body が未配線なら自分自身。
        public static Transform Frame(Rigidbody body, Transform self)
        {
            return body != null ? body.transform : self;
        }

        // 区間の翼幅方向の幅。V1 は「面積 = 幅 × 平均コード」で焼き付けていたので、その逆算。
        public static float SectionWidth(FDMiWing wing, int i)
        {
            float chord = wing.sectionChord[i];
            return chord > 0f ? wing.sectionArea[i] / chord : 0f;
        }

        // 機体座標の軸。色は Unity の Scene 表示と同じ (赤 = +X 右、緑 = +Y 上、青 = +Z 前)。
        public static void DrawBodyFrame(float length)
        {
            Handles.color = new Color(1f, 0.3f, 0.3f, 1f);
            Handles.DrawLine(Vector3.zero, Vector3.right * length);
            Handles.color = new Color(0.3f, 1f, 0.3f, 1f);
            Handles.DrawLine(Vector3.zero, Vector3.up * length);
            Handles.color = new Color(0.4f, 0.6f, 1f, 1f);
            Handles.DrawLine(Vector3.zero, Vector3.forward * length);
            Handles.Label(Vector3.forward * length, "body +Z (前)");
        }

        // 重心。body.centerOfMass は body ローカルなのでこの座標系でそのまま置ける。
        public static void DrawCenterOfMass(Vector3 centerOfMass, float size)
        {
            Handles.color = Color.white;
            Handles.DrawLine(centerOfMass - Vector3.right * size, centerOfMass + Vector3.right * size);
            Handles.DrawLine(centerOfMass - Vector3.up * size, centerOfMass + Vector3.up * size);
            Handles.DrawLine(centerOfMass - Vector3.forward * size, centerOfMass + Vector3.forward * size);
            Handles.Label(centerOfMass + Vector3.up * size, "CoM");
        }

        // 区間の四隅 (前縁2点、後縁2点)。coordPos は 1/4 弦点として描く (V1 の CoordPos は
        // 前後2站の 25% 弦点の平均で、力の作用点でもある)。よって前縁が -0.25c、後縁が +0.75c。
        // chordNormal は後ろ向き (V1 と同じ)。
        public static void Corners(FDMiWing wing, int i, out Vector3 le0, out Vector3 le1, out Vector3 te1, out Vector3 te0)
        {
            float chord = wing.sectionChord[i];
            Vector3 le = wing.coordPos[i] - wing.chordNormal[i] * (0.25f * chord);
            Vector3 te = wing.coordPos[i] + wing.chordNormal[i] * (0.75f * chord);
            Vector3 half = wing.spanNormal[i] * (0.5f * SectionWidth(wing, i));
            le0 = le - half;
            le1 = le + half;
            te1 = te + half;
            te0 = te - half;
        }

        // 区間の枠と 1/4 弦点の印。枠の幅は区間の面積とコード長から出したもの。
        public static void DrawSection(FDMiWing wing, int i, Color color)
        {
            Corners(wing, i, out Vector3 a, out Vector3 b, out Vector3 c, out Vector3 d);
            Handles.color = color;
            Handles.DrawLine(a, b);
            Handles.DrawLine(b, c);
            Handles.DrawLine(c, d);
            Handles.DrawLine(d, a);
            Vector3 tick = wing.chordNormal[i] * (0.05f * wing.sectionChord[i]);
            Handles.DrawLine(wing.coordPos[i] - tick, wing.coordPos[i] + tick);
        }

        // 区間の姿勢。色は V1 の FDMi_wing_Editor に合わせる (赤 = 翼面法線、青 = 翼幅、緑 = 弦)。
        public static void DrawSectionAxes(FDMiWing wing, int i, float length)
        {
            Vector3 span = wing.spanNormal[i];
            Vector3 chord = wing.chordNormal[i];
            Handles.color = new Color(1f, 0.3f, 0.3f, 1f);
            Handles.DrawLine(wing.coordPos[i], wing.coordPos[i] + Vector3.Cross(span, chord).normalized * length);
            Handles.color = new Color(0.4f, 0.6f, 1f, 1f);
            Handles.DrawLine(wing.coordPos[i], wing.coordPos[i] + span.normalized * length);
            Handles.color = new Color(0.3f, 1f, 0.3f, 1f);
            Handles.DrawLine(wing.coordPos[i], wing.coordPos[i] + chord.normalized * length);
        }

        // 装置の舵角。入力が未配線なら 0 (付け根の位置に描く)。
        public static float InputDeg(FDMiControlSurface device)
        {
            return device.input != null ? device.input.Data : 0f;
        }

        // 装置が覆う区間の舵面。ヒンジ線と、舵角ぶん動かしたパネルを描く。
        //   動翼・フラップ: 後縁から chordRatio を舵面とし、ヒンジまわりに下げる
        //   スラット      : 前縁から chordRatio をパネルとし、前へ出す (回転はしない)
        //   スポイラ      : 上翼面のヒンジまわりに跳ね上げる
        // 正の舵角は後縁下げ・前縁突出・開度なので、その向きに描く。
        public static void DrawDevice(FDMiWing wing, FDMiControlSurface device, Color color)
        {
            if (device.sectionMul == null)
                return;
            float deg = InputDeg(device);
            bool labelled = false;
            int count = Mathf.Min(wing.coordPos.Length, device.sectionMul.Length);
            for (int i = 0; i < count; i++)
            {
                if (device.sectionMul[i] == 0f)
                    continue;
                Vector3 span = wing.spanNormal[i].normalized;
                Vector3 chord = wing.chordNormal[i].normalized;
                float chordLength = wing.sectionChord[i];
                // 覆う区間は sectionMul が 0 以外。値は効きの倍率であって幅ではないので、
                // 描く幅は区間の幅そのもの。
                Vector3 half = span * (0.5f * SectionWidth(wing, i));
                Vector3 le = wing.coordPos[i] - chord * (0.25f * chordLength);
                float panelChord = device.chordRatio * chordLength;

                Handles.color = color;
                if (device.type == FDMiWingDeviceType.Slat)
                {
                    // 前縁パネル。展開量だけ前へ出す。格納位置は点線で残す。
                    float outRatio = Mathf.Clamp01(deg / device.maxDeflectionDeg);
                    Vector3 center = le - chord * (panelChord * outRatio);
                    Handles.DrawDottedLine(le - half, le + half, 4f);
                    Handles.DrawLine(center - half, center + half);
                    Handles.DrawLine(center - half, center - half + chord * panelChord);
                    Handles.DrawLine(center + half, center + half + chord * panelChord);
                    Handles.DrawLine(center - half + chord * panelChord, center + half + chord * panelChord);
                    if (!labelled)
                        Handles.Label(center, device.type + " " + deg.ToString("F0") + " deg");
                }
                else
                {
                    // 後縁の舵面とスポイラ。ヒンジ線まわりに舵角ぶん回す。
                    bool spoiler = device.type == FDMiWingDeviceType.Spoiler;
                    float hinge = (spoiler ? _spoilerHinge : 1f) - device.chordRatio;
                    Vector3 hingeCenter = le + chord * (hinge * chordLength);
                    Vector3 panel = Quaternion.AngleAxis((spoiler ? 1f : -1f) * deg, span) * chord;
                    Vector3 aft = hingeCenter + panel * panelChord;
                    Handles.DrawLine(hingeCenter - half, hingeCenter + half);
                    Handles.DrawLine(hingeCenter - half, aft - half);
                    Handles.DrawLine(hingeCenter + half, aft + half);
                    Handles.DrawLine(aft - half, aft + half);
                    if (!labelled)
                        Handles.Label(hingeCenter, device.type + " " + deg.ToString("F0") + " deg");
                }
                labelled = true;
            }
        }

        // 胴体の円筒。位置を持つフィールドが centerOfPressure だけなので、そこを中心に描く
        // (Editor の焼き付けは transform の位置を CP に入れる。胴体の空力中心はほぼ中央なので
        // 実機の外殻と概ね重なる)。
        public static void DrawBodyCylinder(Vector3 centerOfPressure, Vector3 axis, float length, float diameter)
        {
            Vector3 a = axis.sqrMagnitude > 0f ? axis.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(a, Vector3.up);
            if (side.sqrMagnitude < 1e-6f)
                side = Vector3.Cross(a, Vector3.right);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, a);
            float radius = 0.5f * diameter;
            Vector3 nose = centerOfPressure + a * (0.5f * length);
            Vector3 tail = centerOfPressure - a * (0.5f * length);

            Handles.color = new Color(0.4f, 0.6f, 1f, 1f);
            Handles.DrawWireDisc(nose, a, radius);
            Handles.DrawWireDisc(tail, a, radius);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 offset = (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                Handles.DrawLine(tail + offset, nose + offset);
            }
            Handles.color = Color.white;
            Handles.DrawLine(centerOfPressure - a * radius, centerOfPressure + a * radius);
            Handles.Label(nose, "胴体 L=" + length.ToString("F1") + " m d=" + diameter.ToString("F1") + " m");
        }
    }
}
