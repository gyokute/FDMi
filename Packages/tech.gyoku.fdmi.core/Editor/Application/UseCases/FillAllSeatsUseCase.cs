using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Application.UseCases
{
    /// <summary>
    /// FDMiSeatManager の allSeats を、manager と同じ名前空間ルート配下の FDMiSeat で自動解決するユースケース。
    /// クラスタ範囲は pilot 状態の共有データと ~/ の起点 (isNamespaceRoot=true の FDMiNamespace) が同一.
    /// 非アクティブな seat も含み、並び順は階層パスの辞書順に固定して再解決時の配列表記の振れを防ぐ。
    /// 手動調整した allSeats は Inspector 選択時に自動で上書きされる (仕様: 自動解決)。
    /// </summary>
    public class FillAllSeatsUseCase : IFDMiAutoSetupUseCase
    {
        /// <summary>
        /// 対象が FDMiSeatManager 以外のときは何もしない。
        /// </summary>
        /// <param name="target">解決対象の Unity オブジェクト。</param>
        public void Execute(UnityEngine.Object target)
        {
            if (!(target is FDMiSeatManager manager))
                return;

            var so = new SerializedObject(target);
            var sp = so.FindProperty(nameof(FDMiSeatManager.allSeats));
            if (sp == null)
                return;

            var seats = CollectClusterSeats(manager);
            sp.arraySize = seats.Length;
            for (int i = 0; i < seats.Length; i++)
                sp.GetArrayElementAtIndex(i).objectReferenceValue = seats[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// シーン内の全 MonoBehaviour を対象に Execute を実行する。
        /// </summary>
        public void ExecuteAll()
        {
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                Execute(mb);
        }

        /// <summary>
        /// manager と同じ名前空間ルート配下の FDMiSeat をすべて収集する。
        /// </summary>
        static FDMiSeat[] CollectClusterSeats(FDMiSeatManager manager)
        {
            var anchor = FindRootNamespace(manager.transform);
            return UnityEngine.Object
                .FindObjectsByType<FDMiSeat>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(seat => FindRootNamespace(seat.transform) == anchor)
                .OrderBy(seat => GetHierarchyPath(seat.transform), StringComparer.Ordinal)
                .ToArray();
        }

        // SceneFDMiDataRepository.FindNearestAncestorRootNamespace は parent 開始のため、
        // namespace ルートと同 GameObject に置かれた manager 自身を見つけられない。ここでは自身を含めて探索する。
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
