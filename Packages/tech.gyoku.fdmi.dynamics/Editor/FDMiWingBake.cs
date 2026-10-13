using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 翼の平面形ジオメトリと干渉係数を焼き付ける。Editor 側の通常 C#。
    // 実行時は焼き付け済み (spanNormal/chordNormal/controlPoint/cp*/Qij/rhoRef) を読むだけ。
    // 設計 (Section 位置・コード長・affectWing) を変えたら押し直す。
    public static class FDMiWingBake
    {
        public static void Bake(FDMiWing wing, Rigidbody body, float rhoRef)
        {
            Transform bt = body.transform;

            Vector3 spl = bt.InverseTransformPoint(wing.SectionL.transform.position);
            Vector3 spr = bt.InverseTransformPoint(wing.SectionR.transform.position);

            Vector3 spn = (spl - spr).normalized;
            Vector3 chordn = -bt.InverseTransformDirection(
                (wing.SectionL.transform.forward + wing.SectionR.transform.forward) * 0.5f).normalized;
            Vector3 planfn = Vector3.Cross(chordn, spn).normalized;
            Vector3 span = Vector3.Cross(planfn, chordn).normalized;

            Vector3 cpl = spl - 0.25f * wing.SectionL.chordLength * bt.InverseTransformDirection(wing.SectionL.transform.forward).normalized;
            Vector3 cpr = spr - 0.25f * wing.SectionR.chordLength * bt.InverseTransformDirection(wing.SectionR.transform.forward).normalized;
            Vector3 cp = (cpl + cpr) * 0.5f;

            wing.spanNormal = span;
            wing.chordNormal = Vector3.Cross(span, planfn).normalized;
            wing.controlPoint = cp;
            wing.cpChordLength = Mathf.Max((wing.SectionL.chordLength + wing.SectionR.chordLength) * 0.5f, 0.01f);
            wing.cpSpanLength = Vector3.Project(spr - spl, spn).magnitude;
            wing.cpArea = wing.cpChordLength * wing.cpSpanLength;
            wing.rhoRef = Mathf.Max(rhoRef, 1e-4f);

            wing.transform.position = bt.TransformPoint(cp);
            wing.transform.rotation = bt.rotation * Quaternion.LookRotation(Vector3.Cross(planfn, span), planfn);

            if (wing.affectWing == null)
                wing.affectWing = new FDMiWing[0];
            if (wing.Qij == null || wing.Qij.Length != wing.affectWing.Length)
                wing.Qij = new Vector3[wing.affectWing.Length];
            for (int i = 0; i < wing.affectWing.Length; i++)
            {
                FDMiWing aw = wing.affectWing[i];
                if (aw == null || aw.SectionL == null || aw.SectionR == null)
                    wing.Qij[i] = Vector3.zero;
                else
                    wing.Qij[i] = Downwash(bt, aw, cp);
            }
        }

        // 馬蹄渦モデルの吹き下ろし係数 [1/m]。旧 aerodynamics と同式。
        // 起点は相手翼の両翼端 (spL/spR 相当をその場計算)。焼き付け順序に依存しない。
        static Vector3 Downwash(Transform bt, FDMiWing affectWing, Vector3 cp)
        {
            Vector3 tipR = bt.InverseTransformPoint(affectWing.SectionR.transform.position);
            Vector3 tipL = bt.InverseTransformPoint(affectWing.SectionL.transform.position);
            Vector3 u = -Vector3.forward;
            Vector3 rc = cp - tipR;
            Vector3 cl = tipL - cp;
            float rcLen = Mathf.Max(rc.magnitude, 1e-3f);
            float clLen = Mathf.Max(cl.magnitude, 1e-3f);
            Vector3 qr = Vector3.Cross(u, rc).normalized / rcLen;
            Vector3 ql = Vector3.Cross(u, cl).normalized / clLen;
            return (0.25f / Mathf.PI) * (qr + ql);
        }
    }
}
