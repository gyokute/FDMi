using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

namespace FDMi.sync.Editor.Domain
{
    // §11.3 の検証 V-01〜V-17。結果は `[FDMiSync] V-xx: ...` で Console に出す。
    // Why-not: 検証は Editor に閉じる。Runtime は正常系だけを作り、設定ミスの回復を持たない (§1.2)。
    public static class FDMiSyncValidator
    {
        public const int BOARDING_COLLIDER_LAYER = 29;
        public const float LEVER_ARM_WARN_M = 130f;
        public const float LEVER_ARM_ERROR_M = 164f;

        public class Result
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
        }

        public static Result Validate(FDMiSyncManager manager)
        {
            Result r = new Result();
            if (manager == null)
            {
                r.Errors.Add("V-01: FDMiSyncManager が未設定です。");
                return r;
            }

            FDMiSyncManager[] managers = Object.FindObjectsOfType<FDMiSyncManager>();
            if (managers.Length != 1)
                r.Errors.Add("V-01: FDMiSyncManager はシーンに 1 つだけ必要です (現在 " + managers.Length + ")。");

            FDMiSyncAnchor[] anchors = manager.anchors ?? new FDMiSyncAnchor[0];
            FDMiStationAnchor[] stations = manager.stations ?? new FDMiStationAnchor[0];

            // V-02: 全アンカーが anchors[] に 1 回だけ登録されている。
            // FDMiSyncPlayer は VRCPlayerObject の子孫なので登録表の対象外 (§11.1 / V-16)。
            FDMiSyncAnchor[] sceneAnchors = Object.FindObjectsOfType<FDMiSyncAnchor>();
            for (int i = 0; i < sceneAnchors.Length; i++)
            {
                FDMiSyncAnchor a = sceneAnchors[i];
                if (a is FDMiSyncPlayer || IsUnderPlayerObject(a)) continue;
                int count = 0;
                for (int j = 0; j < anchors.Length; j++) if (anchors[j] == a) count++;
                if (count != 1)
                    r.Errors.Add("V-02: " + Name(a) + " が anchors[] に " + count + " 回登録されています。");
            }

            // V-04: anchors[] に FDMiSyncPlayer を含まない。FDMiSyncAnchor の派生なので型で判定する。
            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i] is FDMiSyncPlayer)
                    r.Errors.Add("V-04: " + Name(anchors[i]) + " はプレイヤーアンカーです。anchors[] に入れません。");

            // V-03: Renderer.bounds の 8 隅がアンカーの姿勢原点から 130 m (警告) / 164 m (エラー)。
            for (int i = 0; i < anchors.Length; i++)
                ValidateLeverArm(anchors[i], r);

            // V-05 / V-06。
            for (int i = 0; i < stations.Length; i++)
            {
                FDMiStationAnchor s = stations[i];
                if (s == null) { r.Errors.Add("V-05: stations[" + i + "] が null です。"); continue; }
                if (s.GetComponent<VRCStation>() == null)
                    r.Errors.Add("V-05: " + Name(s) + " と同じ GameObject に VRCStation がありません。");
                if (s.vehicleAnchor == null)
                    r.Errors.Add("V-05: " + Name(s) + " の vehicleAnchor が未設定です。");
                if (s.manager == null)
                    r.Errors.Add("V-05: " + Name(s) + " の manager が未設定です。");
                else if (s.manager != manager)
                    r.Errors.Add("V-05: " + Name(s) + " の manager がシーンの FDMiSyncManager を指していません。");
                if (s.seatIndex != i)
                    r.Errors.Add("V-06: " + Name(s) + " の seatIndex が stations[] の添字 " + i + " と一致しません (" + s.seatIndex + ")。");
                for (int j = i + 1; j < stations.Length; j++)
                    if (stations[j] != null && stations[j].seatIndex == s.seatIndex)
                        r.Errors.Add("V-06: seatIndex " + s.seatIndex + " が重複しています。");

                // E-03: E-04 と同じ式で再計算し、焼き値と一致することを確認する (§11.1)。
                if (s.vehicleAnchor != null && s.GetComponent<VRCStation>() != null)
                    ValidateSeatRel(s, r);
            }

            // V-07 / V-08: FDMiAnchorTrigger。
            FDMiAnchorTrigger[] triggers = Object.FindObjectsOfType<FDMiAnchorTrigger>();
            for (int i = 0; i < triggers.Length; i++)
            {
                FDMiAnchorTrigger t = triggers[i];
                Collider c = t.GetComponent<Collider>();
                if (c == null || !c.isTrigger)
                    r.Warnings.Add("V-07: " + Name(t) + " のコライダーが isTrigger ではありません。");
                if (t.gameObject.layer != BOARDING_COLLIDER_LAYER)
                    r.Warnings.Add("V-07: " + Name(t) + " の layer が " + BOARDING_COLLIDER_LAYER + " (BoardingCollider) ではありません。");
                if (!Contains(anchors, t.target))
                    r.Errors.Add("V-08: " + Name(t) + " の target が anchors[] に含まれません。");
                if (t.exitTarget != null && !Contains(anchors, t.exitTarget))
                    r.Errors.Add("V-08: " + Name(t) + " の exitTarget が anchors[] に含まれません。");
            }

            // V-09: VRCPlayerObject テンプレートは 1 個。その子孫に FDMiSyncPlayer が 1 個だけ。
            VRCPlayerObject[] templates = Object.FindObjectsOfType<VRCPlayerObject>(true);
            if (templates.Length != 1)
                r.Errors.Add("V-09: VRCPlayerObject はシーンに 1 つだけ必要です (現在 " + templates.Length + ")。");

            int playerAnchors = 0;
            FDMiSyncPlayer[] scenePlayers = Object.FindObjectsOfType<FDMiSyncPlayer>(true);
            for (int i = 0; i < scenePlayers.Length; i++)
            {
                if (IsUnderPlayerObject(scenePlayers[i])) { playerAnchors++; continue; }
                r.Errors.Add("V-09: " + Name(scenePlayers[i]) + " が VRCPlayerObject の子孫にありません。");
            }
            if (playerAnchors != 1)
                r.Errors.Add("V-09: テンプレートの FDMiSyncPlayer が " + playerAnchors + " 個です (1 必要)。");

            for (int i = 0; i < templates.Length; i++)
            {
                VRCPlayerObject template = templates[i];
                if (!template.gameObject.activeInHierarchy)
                    r.Errors.Add("V-09: " + Name(template) + " が Editor でアクティブではありません。");

                FDMiSyncPlayer[] own = template.GetComponentsInChildren<FDMiSyncPlayer>(true);
                for (int j = 0; j < own.Length; j++)
                {
                    if (own[j].manager == null)
                        r.Errors.Add("V-09: " + Name(own[j]) + " の manager が未設定です。");
                    else if (own[j].manager != manager)
                        r.Errors.Add("V-09: " + Name(own[j]) + " の manager がシーンの FDMiSyncManager を指していません。");
                }

                // V-14: 永続化しない。前回の _absMm / _seatIndex を復元させない (§11.3)。
                if (template.GetComponent<VRCEnablePersistence>() != null)
                    r.Warnings.Add("V-14: " + Name(template) + " に VRCEnablePersistence があります。永続化しません。");

                // V-15: コピーはテンプレートの親を継承する。親が動くと全コピーが一緒に動く (§11.5)。
                if (HasMovingAncestor(template.transform))
                    r.Warnings.Add("V-15: " + Name(template) + " の親が静的なホルダではありません。コピーが親の運動を継承します。");
            }

            // V-16: テンプレートの子孫を登録表に入れない。FDMiSyncPlayer は FDMiSyncAnchor の派生である (§11.1)。
            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i] != null && IsUnderPlayerObject(anchors[i]))
                    r.Errors.Add("V-16: " + Name(anchors[i]) + " が VRCPlayerObject の子孫ですが anchors[] にあります。");
            for (int i = 0; i < stations.Length; i++)
                if (stations[i] != null && IsUnderPlayerObject(stations[i]))
                    r.Errors.Add("V-16: " + Name(stations[i]) + " が VRCPlayerObject の子孫ですが stations[] にあります。");

            // V-10: FDMiSyncMath が GameObject に付いていない。
            FDMiSyncMath[] maths = Object.FindObjectsOfType<FDMiSyncMath>(true);
            if (maths.Length != 0)
                r.Warnings.Add("V-10: FDMiSyncMath が " + maths.Length + " 個の GameObject に付いています。static メソッドだけを使います。");

            // V-11: initialAnchor。
            if (manager.initialAnchor == null)
                r.Errors.Add("V-11: initialAnchor が未設定です。");
            else if (!Contains(anchors, manager.initialAnchor))
                r.Errors.Add("V-11: initialAnchor が anchors[] に含まれません。");

            // V-12: 静的なら body が kinematic、動的アンカーの body は非 kinematic。
            // V-13: 動的アンカーの GameObject に ObjectSync を付けない (§11.3)。
            for (int i = 0; i < anchors.Length; i++)
            {
                FDMiSyncAnchor a = anchors[i];
                if (a == null) continue;

                bool isStatic = FDMiSyncAnchorFields.IsStatic(a);

                // Why-not: VRC_ObjectSync は静止時に同期を止め、同じ GameObject の [UdonSynced] も凍結させる。
                // SDK3 の型名は VRC_ObjectSync ではなく VRCObjectSync である。
                if (!isStatic && a.GetComponent<VRCObjectSync>() != null)
                    r.Errors.Add("V-13: " + Name(a) + " は動的アンカーですが ObjectSync が付いています。");

                if (a.body == null) continue;
                if (isStatic && !a.body.isKinematic)
                    r.Warnings.Add("V-12: " + Name(a) + " は静的ですが body が kinematic ではありません。");
                if (!isStatic && a.body.isKinematic)
                    r.Warnings.Add("V-12: " + Name(a) + " は動的ですが body が kinematic です。");
            }

            // V-17: ワールドの Maximum Capacity ≤ PLAYER_SLOTS。超過分は登録されない (U-05)。
            VRCSceneDescriptor descriptor = Object.FindObjectOfType<VRCSceneDescriptor>();
            if (descriptor != null && descriptor.capacity > FDMiSyncManager.PLAYER_SLOTS)
                r.Warnings.Add("V-17: Maximum Capacity が " + descriptor.capacity + " です。"
                    + FDMiSyncManager.PLAYER_SLOTS + " を超える入室者はアンカーを持ちません。");

            for (int i = 0; i < r.Errors.Count; i++) Debug.LogError("[FDMiSync] " + r.Errors[i]);
            for (int i = 0; i < r.Warnings.Count; i++) Debug.LogWarning("[FDMiSync] " + r.Warnings[i]);
            return r;
        }

        // V-06 / E-03。E-04 の焼き値を、シーンから同じ式で再計算した値と比較する。
        // Why-not: 焼き直しを忘れたシーンを検出するのがこの比較の目的である。式は FDMiEarth.SeatRel に
        // 1 つだけ置き、焼き (E-04) と検証 (E-03) の両方がそれを呼ぶ (§11.1)。
        static void ValidateSeatRel(FDMiStationAnchor s, Result r)
        {
            int[] relMm = new int[3];
            Quaternion relRot;
            FDMiEarth.SeatRel(s, relMm, out relRot);

            bool sameMm = relMm[0] == s._bakedSeatRelMm[0]
                && relMm[1] == s._bakedSeatRelMm[1]
                && relMm[2] == s._bakedSeatRelMm[2];
            if (!sameMm || Quaternion.Angle(relRot, s._bakedSeatRelRot) > 1e-3f)
                r.Errors.Add("V-06: " + Name(s) + " の席の相対姿勢が E-04 の焼き値と一致しません。焼き直してください。");
        }

        // V-03。基準はアンカーの姿勢原点 (body またはアンカーの transform)。view.position を基準にしない。
        // Why-not: 静的アンカーを対象にしない。_rot を同期せず、焼き値が全クライアントで同一なので、
        // 量子化は「同期誤差」ではなく定数の tilt になる (§4.5「姿勢を同期しない静的部分」)。
        // 10 km の地形を 1 個の静的アンカーで持つ設計を許す。
        static void ValidateLeverArm(FDMiSyncAnchor a, Result r)
        {
            if (a == null || FDMiSyncAnchorFields.IsStatic(a)) return;
            Vector3 origin = (a.body != null) ? a.body.transform.position : a.transform.position;
            Renderer[] renderers = a.GetComponentsInChildren<Renderer>();
            float worst = 0f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds b = renderers[i].bounds;
                Vector3 c = b.center;
                Vector3 e = b.extents;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = new Vector3(
                        c.x + ((k & 1) == 0 ? -e.x : e.x),
                        c.y + ((k & 2) == 0 ? -e.y : e.y),
                        c.z + ((k & 4) == 0 ? -e.z : e.z));
                    float d = Vector3.Distance(origin, corner);
                    if (d > worst) worst = d;
                }
            }

            if (worst > LEVER_ARM_ERROR_M)
                r.Errors.Add("V-03: " + Name(a) + " のレバーアームが " + worst.ToString("F2") + " m です (エラー " + LEVER_ARM_ERROR_M + " m 超)。");
            else if (worst > LEVER_ARM_WARN_M)
                r.Warnings.Add("V-03: " + Name(a) + " のレバーアームが " + worst.ToString("F2") + " m です (警告 " + LEVER_ARM_WARN_M + " m 超)。");
        }

        static bool IsUnderPlayerObject(Component c)
        {
            for (Transform t = c.transform; t != null; t = t.parent)
                if (t.GetComponent<VRCPlayerObject>() != null) return true;
            return false;
        }

        // V-15。親が動くとコピーがその運動を継承する。
        // Why-not: VRCPlayerObject の親自身がアンカーでない場合があるため、Rigidbody とアンカーの型の両方を見る。
        static bool HasMovingAncestor(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (p.GetComponent<Rigidbody>() != null) return true;
                FDMiSyncAnchor a = p.GetComponent<FDMiSyncAnchor>();
                if (a != null && !FDMiSyncAnchorFields.IsStatic(a)) return true;
            }
            return false;
        }

        static bool Contains(FDMiSyncAnchor[] anchors, FDMiSyncAnchor target)
        {
            if (target == null) return false;
            for (int i = 0; i < anchors.Length; i++) if (anchors[i] == target) return true;
            return false;
        }

        static string Name(Object o)
        {
            if (o == null) return "<null>";
            StringBuilder sb = new StringBuilder(o.name);
            Transform t = (o as Component != null) ? (o as Component).transform : null;
            while (t != null && t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }
    }
}
