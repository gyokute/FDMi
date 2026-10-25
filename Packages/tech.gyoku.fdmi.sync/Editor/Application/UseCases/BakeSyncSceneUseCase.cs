using System.Collections.Generic;
using System.Text;
using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

namespace FDMi.sync.Editor.Application.UseCases
{
    // §11.2 Setup 手順 1〜8 の唯一の実装。Editor 焼き (E-01〜E-07) をここに閉じる。
    // Why-not: Runtime で FindObjectsOfType を呼ばない (§5.3)。列挙と焼きは Editor で 1 回だけ行い、
    // 結果を [UdonSynced] フィールドの初期値として保存する (T-ENV-01)。
    public static class BakeSyncSceneUseCase
    {
        public static void Execute(FDMiSyncManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[FDMiSync] V-01: FDMiSyncManager が見つかりません。");
                return;
            }
            if (manager.initialAnchor == null)
            {
                Debug.LogError("[FDMiSync] V-11: initialAnchor が未設定です。焼きの基準 (P_ref / q_ref) を決められません。");
                return;
            }
            // V-11: 基準は緯度経度高度を持つ静的アンカーに限る。動的を基準にすると自分自身のスポーン姿勢が基準になり循環する。
            if (!(manager.initialAnchor is FDMiStaticObjectSync))
            {
                Debug.LogError("[FDMiSync] V-11: initialAnchor は FDMiStaticObjectSync である必要があります。");
                return;
            }

            // 手順 2: 階層の深さ優先・sibling index 昇順 (アクティブのみ)。
            // V-16: VRCPlayerObject の子孫は登録表に入れない。FDMiSyncPlayer は FDMiSyncAnchor の派生なので
            // 型だけでは除外できない (§11.1)。
            List<FDMiSyncAnchor> anchors = Collect<FDMiSyncAnchor>(false);
            List<FDMiStationAnchor> stations = Collect<FDMiStationAnchor>(false);
            for (int i = anchors.Count - 1; i >= 0; i--)
                if (anchors[i] is FDMiSyncPlayer || IsUnderPlayerObject(anchors[i]))
                    anchors.RemoveAt(i);
            for (int i = stations.Count - 1; i >= 0; i--)
                if (IsUnderPlayerObject(stations[i]))
                    stations.RemoveAt(i);

            // 手順 3 / 6: 先に基準姿勢を確定し、その後に全アンカーを焼く。
            // Why-not: full 精度の basis を基準にしない。Runtime の originRot は UnpackQuat(_rot) なので、
            // 焼きだけ full 精度にすると Editor と Runtime で view が δ_ref だけずれる (§3.6)。
            // 原点から 10 km で最大 0.61 m のずれになる。基準は焼き値そのものを使う。
            FDMiStaticObjectSync initial = (FDMiStaticObjectSync)manager.initialAnchor;
            FDMiEarth.LatLonAltToAbsMm(initial.latitude, initial.longitude,
                initial.height, FDMiSyncAnchorFields.AbsMm(initial));
            FDMiSyncMath.PackQuat(FDMiEarth.BasisWithYaw(initial.latitude,
                initial.longitude, initial.yaw), FDMiSyncAnchorFields.Rot(initial));

            long[] refAbs = FDMiSyncAnchorFields.AbsMm(manager.initialAnchor);
            long[] pRef = new long[3];
            pRef[0] = refAbs[0];
            pRef[1] = refAbs[1];
            pRef[2] = refAbs[2];
            Quaternion qRef = FDMiSyncMath.UnpackQuat(FDMiSyncAnchorFields.Rot(manager.initialAnchor));

            for (int i = 0; i < anchors.Count; i++)
                BakeAnchor(anchors[i], pRef, qRef);

            // 手順 5 の後。親アンカー (E-08) と親子化。
            for (int i = 0; i < anchors.Count; i++)
                BakeParent(anchors[i]);

            // 手順 4: 席の相対姿勢 (E-03 / E-04)。
            for (int i = 0; i < stations.Count; i++)
                BakeStation(stations[i], manager, i);

            // 手順 6: 登録表。
            manager.anchors = anchors.ToArray();
            manager.stations = stations.ToArray();

            // 手順 7: VRCPlayerObject テンプレート 1 個の配線と初期姿勢 (E-06)。
            BakePlayerTemplate(manager, pRef, qRef);

            EditorUtility.SetDirty(manager);
        }

        // E-01 / E-02 / E-07。静的アンカーは焼き入力 (lat/lon/height/yaw)、動的アンカーはスポーン姿勢から焼く。
        static void BakeAnchor(FDMiSyncAnchor a, long[] pRef, Quaternion qRef)
        {
            if (a == null) return;

            // 緯度経度高度 yaw は静的アンカーだけが持つ (§9.2.2)。
            FDMiStaticObjectSync staticAnchor = a as FDMiStaticObjectSync;
            if (staticAnchor != null)
            {
                FDMiEarth.LatLonAltToAbsMm(staticAnchor.latitude, staticAnchor.longitude, staticAnchor.height,
                    FDMiSyncAnchorFields.AbsMm(a));
                FDMiSyncMath.PackQuat(FDMiEarth.BasisWithYaw(staticAnchor.latitude, staticAnchor.longitude,
                    staticAnchor.yaw), FDMiSyncAnchorFields.Rot(a));
            }
            else
            {
                // 動的アンカーはスポーン姿勢。Unity のワールド座標がそのまま view 座標である (§7.1)。
                Transform t = (a.body != null) ? a.body.transform : (a.view != null ? a.view : a.transform);
                FDMiEarth.ViewToAbsMm(pRef, qRef, t.position, FDMiSyncAnchorFields.AbsMm(a));
                FDMiSyncMath.PackQuat(qRef * t.rotation, FDMiSyncAnchorFields.Rot(a));
            }

            // E-07: Snap 補助。transform.rotation = q_ref⁻¹ ⊗ q_basis ⊗ R_y(yaw)。静的だけが緯度経度を持つ。
            if (staticAnchor != null)
            {
                Transform t = (a.body != null) ? a.body.transform : a.transform;
                t.SetPositionAndRotation(
                    FDMiEarth.AbsMmToView(pRef, qRef, FDMiSyncAnchorFields.AbsMm(a)),
                    Quaternion.Inverse(qRef) * FDMiSyncMath.UnpackQuat(FDMiSyncAnchorFields.Rot(a)));
            }

            EditorUtility.SetDirty(a);
        }

        // E-08。親アンカーは transform 階層の直上にあるアンカー。位置は親との差分で管理する (§9.2)。
        // Why-not: body を持つアンカーを親子化しない。入れ子の Rigidbody は Unity の物理が
        // 親の運動を子上書きする。動的アンカーは world 姿勢で書く (§9.2.1)。
        static void BakeParent(FDMiSyncAnchor a)
        {
            if (a == null) return;

            a.parent = null;
            for (Transform t = a.transform.parent; t != null; t = t.parent)
            {
                FDMiSyncAnchor p = t.GetComponent<FDMiSyncAnchor>();
                if (p == null || p is FDMiSyncPlayer) continue;
                a.parent = p;
                break;
            }

            if (a.parent == null) return;
            if (a.GetComponent<Rigidbody>() != null) return;

            Transform self = (a.view != null) ? a.view : a.transform;
            Transform parentT = (a.parent.view != null) ? a.parent.view : a.parent.transform;
            if (self.parent == parentT) return;
            Undo.SetTransformParent(self, parentT, "FDMi Sync Parent");
            EditorUtility.SetDirty(a);
        }

        // E-03 / E-04。基準は vehicleAnchor の body (無ければ view)。§9.5 の GetSeatAbs と同じ式。
        static void BakeStation(FDMiStationAnchor s, FDMiSyncManager manager, int index)
        {
            if (s == null) return;

            s.manager = manager;
            s.seatIndex = index;
            // §9.5: takeOwnershipOnEnter は isPilotSeat と同値。
            s.takeOwnershipOnEnter = s.isPilotSeat;

            VRCStation station = s.GetComponent<VRCStation>();
            if (s.vehicleAnchor != null && station != null)
                FDMiEarth.SeatRel(s, s._bakedSeatRelMm, out s._bakedSeatRelRot);

            EditorUtility.SetDirty(s);
        }

        // E-06。スポーン地点を view 姿勢とし、焼いた initialAnchor の絶対姿勢で FEF に写す (§3.6)。
        // Why-not: コピーごとに焼かない。テンプレートは 1 個で、各プレイヤーの実姿勢は
        // player.GetPosition() から毎 tick 求める (§9.3)。初期姿勢は最初のパケットまでの表示にだけ使う。
        static void BakePlayerTemplate(FDMiSyncManager manager, long[] pRef, Quaternion qRef)
        {
            // §9.4。テンプレートからシーンを参照する。シーンからテンプレートは参照できないため一方通行である。
            Transform spawn = null;
            VRCSceneDescriptor descriptor = Object.FindObjectOfType<VRCSceneDescriptor>();
            if (descriptor != null && descriptor.spawns != null && descriptor.spawns.Length != 0)
                spawn = descriptor.spawns[0];

            FDMiSyncPlayer[] players = Object.FindObjectsOfType<FDMiSyncPlayer>(true);
            for (int i = 0; i < players.Length; i++)
            {
                if (!IsUnderPlayerObject(players[i])) continue;

                players[i].manager = manager;

                // E-09。プレイヤーの定位置に使う station は FDMiSyncPlayer と同じ GameObject に置く (①)。
                // Why-not: コピーは同一階層内の参照を張り替えるので、テンプレートに焼けば各コピーに解決される (§9.4)。
                VRC.SDKBase.VRCStation playerStation = players[i].GetComponent<VRC.SDKBase.VRCStation>();
                if (playerStation == null)
                    Debug.LogError("[FDMiSync] V-19: " + players[i].name + " と同じ GameObject に VRCStation がありません。");
                else
                    players[i].station = playerStation;

                if (spawn != null)
                {
                    FDMiEarth.ViewToAbsMm(pRef, qRef, spawn.position, players[i]._absMm);
                    FDMiSyncMath.PackQuat(qRef * spawn.rotation, players[i]._rot);
                }
                EditorUtility.SetDirty(players[i]);
            }
        }

        // V-16。VRCPlayerObject の子孫か。コピーの生成元はシーンに 1 個だけ置く (§9.4)。
        static bool IsUnderPlayerObject(Component c)
        {
            for (Transform t = c.transform; t != null; t = t.parent)
                if (t.GetComponent<VRCPlayerObject>() != null) return true;
            return false;
        }

        // 階層の深さ優先・sibling index 昇順の走査 (§11.1 E-05)。transform の hierarchy path でソートする。
        static List<T> Collect<T>(bool includeInactive) where T : Component
        {
            T[] found = Object.FindObjectsOfType<T>(includeInactive);

            List<T> list = new List<T>(found.Length);
            for (int i = 0; i < found.Length; i++) list.Add(found[i]);
            list.Sort((x, y) => string.CompareOrdinal(HierarchyKey(x.transform), HierarchyKey(y.transform)));
            return list;
        }

        static string HierarchyKey(Transform t)
        {
            List<Transform> chain = new List<Transform>();
            while (t != null)
            {
                chain.Add(t);
                t = t.parent;
            }

            StringBuilder sb = new StringBuilder(chain.Count * 5);
            for (int i = chain.Count - 1; i >= 0; i--)
                sb.Append(chain[i].GetSiblingIndex().ToString("D4")).Append('/');
            return sb.ToString();
        }
    }
}
