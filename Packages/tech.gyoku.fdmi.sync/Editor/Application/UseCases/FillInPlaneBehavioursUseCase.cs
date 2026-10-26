using System;
using System.Linq;
using FDMi.core;
using FDMi.core.Editor.Application.UseCases;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Application.UseCases
{
    /// <summary>
    /// FDMiBodyKinematicManagement.inPlaneBehaviours を "~" (自身の最近傍にある isNamespaceRoot=true の
    /// FDMiNamespace) 配下の全 FDMiBehaviour で自動解決するユースケース。
    /// ネストした名前空間・非アクティブな GameObject・自身を含む。
    /// 並び順は階層パスの辞書順に固定して再解決時の配列表記の振れを防ぐ。
    /// 手動調整した inPlaneBehaviours は Inspector 選択時に自動で上書きされる (仕様: 自動解決)。
    /// </summary>
    public class FillInPlaneBehavioursUseCase : IFDMiAutoSetupUseCase
    {
        /// <summary>
        /// 対象が FDMiBodyKinematicManagement 以外のときは何もしない。
        /// </summary>
        /// <param name="target">解決対象の Unity オブジェクト。</param>
        public void Execute(UnityEngine.Object target)
        {
            if (!(target is FDMiBodyKinematicManagement manager))
                return;

            // "~" の起点は自身の親から探す (FDMiDataPath の "~" と同じ意味論)。
            // Why-not: 起点なしで全件照合すると、ルートを持たない behaviour まで他機体から巻き込む。
            var anchor = FindRootNamespace(manager.transform.parent);
            if (anchor == null)
                return;

            var so = new SerializedObject(target);
            var sp = so.FindProperty(nameof(FDMiBodyKinematicManagement.inPlaneBehaviours));
            if (sp == null)
                return;

            var behaviours = UnityEngine.Object
                .FindObjectsByType<FDMiBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(behaviour => FindRootNamespace(behaviour.transform) == anchor)
                .OrderBy(behaviour => GetHierarchyPath(behaviour.transform), StringComparer.Ordinal)
                .ToArray();

            sp.arraySize = behaviours.Length;
            for (int i = 0; i < behaviours.Length; i++)
                sp.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// シーン内の全 FDMiBodyKinematicManagement を対象に Execute を実行する。
        /// </summary>
        public void ExecuteAll()
        {
            foreach (var manager in UnityEngine.Object.FindObjectsByType<FDMiBodyKinematicManagement>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                Execute(manager);
        }

        /// <summary>
        /// t 自身から祖先方向へ辿り、最初に見つかる isNamespaceRoot=true の FDMiNamespace を返す。
        /// 見つからなければ null を返す。
        /// </summary>
        static FDMiNamespace FindRootNamespace(Transform t)
        {
            for (var current = t; current != null; current = current.parent)
            {
                var ns = current.GetComponent<FDMiNamespace>();
                if (ns != null && ns.isNamespaceRoot)
                    return ns;
            }
            return null;
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
