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

                    // V-19: プレイヤーの定位置 station は同じ GameObject にあり、参照もそこを指している (§9.3)。
                    if (own[j].station == null)
                        r.Errors.Add("V-19: " + Name(own[j]) + " の station が未設定です。E-09 の焼き直しが必要です。");
                    else if (own[j].station.gameObject != own[j].gameObject)
                        r.Errors.Add("V-19: " + Name(own[j]) + " の station が同じ GameObject にありません。");
                }

                // V-14: 永続化しない。前回の _absMm を復元させない (§11.3)。
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

            // V-10: FDMiSyncMath が GameObject に付いていない。
            FDMiSyncMath[] maths = Object.FindObjectsOfType<FDMiSyncMath>(true);
            if (maths.Length != 0)
                r.Warnings.Add("V-10: FDMiSyncMath が " + maths.Length + " 個の GameObject に付いています。static メソッドだけを使います。");

            // V-11: initialAnchor。焼きの基準は緯度経度高度を持つ静的アンカーに限る (§11.1)。
            if (manager.initialAnchor == null)
                r.Errors.Add("V-11: initialAnchor が未設定です。");
            else if (!(manager.initialAnchor is FDMiStaticObjectSync))
                r.Errors.Add("V-11: initialAnchor は FDMiStaticObjectSync である必要があります。");
            else if (!Contains(anchors, manager.initialAnchor))
                r.Errors.Add("V-11: initialAnchor が anchors[] に含まれません。");

            // V-13: 動的アンカーの GameObject に ObjectSync を付けない (§11.3)。
            // 物理状態は外部が所有するため、ここでは検証しない。
            for (int i = 0; i < anchors.Length; i++)
            {
                FDMiSyncAnchor a = anchors[i];
                if (a == null) continue;

                bool isStatic = FDMiSyncAnchorFields.IsStatic(a);

                // Why-not: VRC_ObjectSync は静止時に同期を止め、同じ GameObject の [UdonSynced] も凍結させる。
                // SDK3 の型名は VRC_ObjectSync ではなく VRCObjectSync である。
                if (!isStatic && a.GetComponent<VRCObjectSync>() != null)
                    r.Errors.Add("V-13: " + Name(a) + " は動的アンカーですが ObjectSync が付いています。");
            }

            // V-18: 親アンカーの規則。親は anchors[] に含まれ、body を持たない。
            // Why-not: body を持つアンカーの親を許さない。入れ子の Rigidbody は Unity の物理が
            // 親の運動で上書きする。動的アンカーは world 姿勢で書く (§9.2.1)。
            for (int i = 0; i < anchors.Length; i++)
            {
                FDMiSyncAnchor a = anchors[i];
                if (a == null || a.parent == null) continue;
                if (!Contains(anchors, a.parent))
                    r.Errors.Add("V-18: " + Name(a) + " の parent が anchors[] に含まれません。");
                if (a.parent.GetComponent<Rigidbody>() != null)
                    r.Errors.Add("V-18: " + Name(a.parent) + " は body を持つので親にできません。");
                if (HasParentCycle(a))
                    r.Errors.Add("V-18: " + Name(a) + " の parent に循環があります。");
            }

            for (int i = 0; i < r.Errors.Count; i++) Debug.LogError("[FDMiSync] " + r.Errors[i]);
            for (int i = 0; i < r.Warnings.Count; i++) Debug.LogWarning("[FDMiSync] " + r.Warnings[i]);
            return r;
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

        // V-18。親関係の循環。焼きは transform 階層から取るので作らない。手作業で設定した誤りを拾う。
        static bool HasParentCycle(FDMiSyncAnchor a)
        {
            FDMiSyncAnchor cur = a.parent;
            for (int i = 0; i < 64 && cur != null; i++)
            {
                if (cur == a) return true;
                cur = cur.parent;
            }
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
