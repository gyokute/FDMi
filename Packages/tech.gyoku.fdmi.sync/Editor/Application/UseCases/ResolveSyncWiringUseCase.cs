// Why-not: 新Hook仕様 (IFDMiCompositionHook/Order帯域) には寄せない。core.editor に未着陸のIF参照は現行コンパイルを壊すため。
// 将来移行: IF着陸後に Order 200 の薄いフック (Menu/TargetModified-skip 方針は維持) を Hooks/ へ追加する。
using System;
using System.Collections.Generic;
using System.Linq;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Application.UseCases
{
    /// <summary>
    /// FDMiRelativeObjectSyncManager の refPoints 配線を決定的順序で再解決するユースケース。
    /// manager 自身を必ず refPoints に含め、収集順を index に採番する。
    /// 未設定の syncManager / parentRefPoint / rootRefPoint / localRootRefPoint を補完し、
    /// localPlayerPosition はシーン内の FDMiPlayerPosition がちょうど1つのときのみ設定する。
    /// 何度実行しても同じ結果になる (冪等)。
    /// core の IFDMiAutoSetupUseCase を実装するため独自 Interface は設けない。
    /// FDMiBehaviourEditor / FDMiEditorHooks が同 Interface 経由で自動発見する。
    /// </summary>
    public class ResolveSyncWiringUseCase : IFDMiAutoSetupUseCase
    {
        /// <summary>
        /// 対象が FDMiRelativeObjectSyncManager 以外のときは何もしない。
        /// </summary>
        /// <param name="target">解決対象の Unity オブジェクト。</param>
        public void Execute(UnityEngine.Object target)
        {
            if (!(target is FDMiRelativeObjectSyncManager manager))
                return;
            ResolveManager(manager);
        }

        /// <summary>
        /// シーン内の全 manager を対象に Execute を実行する。
        /// </summary>
        public void ExecuteAll()
        {
            foreach (var manager in CollectManagers())
                ResolveManager(manager);
        }

        static FDMiRelativeObjectSyncManager[] CollectManagers() =>
            UnityEngine.Object
                .FindObjectsByType<FDMiRelativeObjectSyncManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(m => GetHierarchyPath(m.transform), StringComparer.Ordinal)
                .ToArray();

        static void ResolveManager(FDMiRelativeObjectSyncManager manager)
        {
            var warnings = new List<string>();

            // Why-not: FindObjectsByType の返順は不定。ParentIndex 同期は index 位置で相手を特定するため、
            // 順序が振れると index 対応が壊れる (旧T18)。階層パス昇順に固定して決定的にする。
            var refPoints = UnityEngine.Object
                .FindObjectsByType<FDMiReferencePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(rp => GetHierarchyPath(rp.transform), StringComparer.Ordinal)
                .ToList();

            int managerIndex = refPoints.IndexOf(manager);
            if (managerIndex < 0)
            {
                warnings.Add($"manager '{GetHierarchyPath(manager.transform)}' が収集中に見つからないため末尾に追加する。");
                refPoints.Add(manager);
                managerIndex = refPoints.Count - 1;
            }

            var managerSo = new SerializedObject(manager);
            managerSo.Update();

            var arrayProp = managerSo.FindProperty(nameof(FDMiRelativeObjectSyncManager.refPoints));
            arrayProp.arraySize = refPoints.Count;
            for (int i = 0; i < refPoints.Count; i++)
            {
                var rp = refPoints[i];
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = rp;

                var rpSo = new SerializedObject(rp);
                rpSo.Update();

                var syncManagerProp = rpSo.FindProperty(nameof(FDMiReferencePoint.syncManager));
                var current = syncManagerProp.objectReferenceValue as FDMiRelativeObjectSyncManager;
                if (current == null)
                    syncManagerProp.objectReferenceValue = manager;
                else if (current != manager)
                    warnings.Add($"'{GetHierarchyPath(rp.transform)}' は別 manager 配下のため上書きしない。");

                // FDMiReferencePoint.initReferencePoint と同じフォールバック (未設定は manager)。
                var parentProp = rpSo.FindProperty(nameof(FDMiReferencePoint.parentRefPoint));
                if (parentProp.objectReferenceValue == null)
                    parentProp.objectReferenceValue = manager;
                var rootProp = rpSo.FindProperty(nameof(FDMiReferencePoint.rootRefPoint));
                if (rootProp.objectReferenceValue == null)
                    rootProp.objectReferenceValue = manager;

                rpSo.FindProperty(nameof(FDMiReferencePoint.index)).intValue = i;
                rpSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // manager 自身の index も配列内位置に上書きする (prefab 既定の index:0 に依存しない)。
            managerSo.FindProperty(nameof(FDMiReferencePoint.index)).intValue = managerIndex;

            managerSo.FindProperty(nameof(FDMiRelativeObjectSyncManager.localRootRefPoint)).objectReferenceValue = manager;

            var players = UnityEngine.Object.FindObjectsByType<FDMiPlayerPosition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length == 1)
                managerSo.FindProperty(nameof(FDMiRelativeObjectSyncManager.localPlayerPosition)).objectReferenceValue = players[0];
            else
                // マルチプレイヤー割当は本タスク範囲外のため、0件・複数件とも設定せず警告のみ。
                warnings.Add(players.Length == 0
                    ? "FDMiPlayerPosition がシーンにないため localPlayerPosition を設定しない。"
                    : $"FDMiPlayerPosition が {players.Length} 件あるため localPlayerPosition を設定しない (マルチプレイヤー割当は対象外)。");

            // manager のみ _isRoot=true を保証する。他の isRoot には触らない。
            managerSo.FindProperty(nameof(FDMiReferencePoint._isRoot)).boolValue = true;

            managerSo.ApplyModifiedPropertiesWithoutUndo();

            foreach (var warning in warnings)
                Debug.LogWarning($"[ResolveSyncWiring] {warning}");
            Debug.Log($"[ResolveSyncWiring] manager '{GetHierarchyPath(manager.transform)}': refPoints={refPoints.Count} (manager index={managerIndex}), warnings={warnings.Count}");
        }

        /// <summary>ルートまでの "Parent/Child" 形式のパス。並び順の安定化にのみ使う。</summary>
        static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            for (var parent = t.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
