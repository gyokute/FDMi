using System;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Domain.Entities;
using tech.gyoku.FDMi.core.Editor.Domain.Repositories;

namespace tech.gyoku.FDMi.core.Editor.Infrastructure.Repositories
{
    /// <summary>
    /// Unity シーン上の GameObject 階層を探索して FDMiData を見つけるリポジトリ実装。
    /// </summary>
    public class SceneFDMiDataRepository : IFDMiDataRepository
    {
        /// <summary>
        /// 指定したコンテキストとパスに一致する FDMiData をすべて検索する。
        /// 一致が無ければ空配列を返す。
        /// </summary>
        /// <param name="context">属性を持つ MonoBehaviour の GameObject。</param>
        /// <param name="path">解決するパス。</param>
        /// <param name="fieldType">フィールドの宣言型（配列の場合は要素型。FDMiBool 等）。</param>
        public FDMiData[] FindAll(GameObject context, FDMiDataPath path, Type fieldType)
        {
            if (context == null || string.IsNullOrEmpty(path.DataName)) return new FDMiData[0];
            var found = path.IsAbsolute
                ? FindAbsolute(path, fieldType)
                : FindRelative(context, path, fieldType);
            return found != null ? new[] { found } : new FDMiData[0];
        }

        /// <summary>
        /// 相対パス解決。context の親（または context 自身）を起点に FDMiNamespace 境界まで探索する。
        /// </summary>
        FDMiData FindRelative(GameObject context, FDMiDataPath path, Type fieldType)
        {
            var root = context.transform.parent != null
                ? context.transform.parent
                : context.transform;
            return SearchInScope(root, path.DataName, fieldType);
        }

        /// <summary>
        /// 絶対パス解決。isNamespaceRoot=true の FDMiNamespace から名前空間を順に辿り、最終スコープで探索する。
        /// </summary>
        FDMiData FindAbsolute(FDMiDataPath path, Type fieldType)
        {
            var allNamespaces = UnityEngine.Object.FindObjectsByType<FDMiNamespace>(FindObjectsSortMode.None);

            FDMiNamespace current = null;
            foreach (var ns in allNamespaces)
            {
                if (ns.isNamespaceRoot && ns.nameSpace == path.Namespaces[0])
                {
                    current = ns;
                    break;
                }
            }
            if (current == null) return null;

            for (int i = 1; i < path.Namespaces.Count; i++)
            {
                current = FindChildNamespace(current.transform, path.Namespaces[i]);
                if (current == null) return null;
            }

            return SearchInScope(current.transform, path.DataName, fieldType);
        }

        /// <summary>
        /// parent の子孫から指定した namespaceName を持つ FDMiNamespace を深さ優先で探す。
        /// 途中で別の FDMiNamespace に入った場合はその中を探索しない（境界）。
        /// </summary>
        FDMiNamespace FindChildNamespace(Transform parent, string namespaceName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var ns = child.GetComponent<FDMiNamespace>();
                if (ns != null)
                {
                    if (ns.nameSpace == namespaceName) return ns;
                    continue; // 別の NS には入らない
                }
                var found = FindChildNamespace(child, namespaceName);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// root の子孫を深さ優先で探索し、名前と型が一致する FDMiData を返す。
        /// FDMiNamespace を持つ子の子孫には入らない（境界）。
        /// </summary>
        FDMiData SearchInScope(Transform root, string dataName, Type fieldType)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.GetComponent<FDMiNamespace>() != null) continue;

                if (child.name == dataName)
                {
                    var component = child.GetComponent(fieldType) as FDMiData;
                    if (component != null) return component;
                }

                var found = SearchInScope(child, dataName, fieldType);
                if (found != null) return found;
            }
            return null;
        }
    }
}
