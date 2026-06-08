using System;
using System.Collections.Generic;
using UnityEngine;
using FDMi.core.Editor.Domain.Entities;
using FDMi.core.Editor.Domain.Repositories;

namespace FDMi.core.Editor.Infrastructure.Repositories
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
            var results = path.IsAbsolute
                ? FindAllAbsolute(path, fieldType)
                : FindAllRelative(context, path, fieldType);
            return results.ToArray();
        }

        /// <summary>
        /// 相対パス解決。context の親（または context 自身）を起点に FDMiNamespace 境界まで探索し、全件を収集する。
        /// </summary>
        List<FDMiData> FindAllRelative(GameObject context, FDMiDataPath path, Type fieldType)
        {
            var root = context.transform.parent != null
                ? context.transform.parent
                : context.transform;
            var results = new List<FDMiData>();
            CollectInScope(root, path.DataName, fieldType, results);
            return results;
        }

        /// <summary>
        /// 絶対パス解決。isNamespaceRoot=true の FDMiNamespace から名前空間を順に辿り、最終スコープで全件を収集する。
        /// パスにワイルドカード（"*" / "**"）が含まれる場合は <see cref="FindAllAbsoluteWildcard"/> に委譲する。
        /// </summary>
        List<FDMiData> FindAllAbsolute(FDMiDataPath path, Type fieldType)
        {
            if (ContainsWildcardSegment(path.Namespaces))
                return FindAllAbsoluteWildcard(path, fieldType);

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
            if (current == null) return new List<FDMiData>();

            for (int i = 1; i < path.Namespaces.Count; i++)
            {
                current = FindChildNamespace(current.transform, path.Namespaces[i]);
                if (current == null) return new List<FDMiData>();
            }

            var results = new List<FDMiData>();
            CollectInScope(current.transform, path.DataName, fieldType, results);
            return results;
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
        /// 名前空間セグメント列にワイルドカード（"*" または "**"）が含まれるかを判定する。
        /// </summary>
        bool ContainsWildcardSegment(IReadOnlyList<string> namespaces)
        {
            foreach (var ns in namespaces)
                if (ns == "*" || ns == "**") return true;
            return false;
        }

        /// <summary>
        /// parent の子孫の FDMiNamespace をすべて収集する（境界規則は FindChildNamespace と同じ:
        /// FDMiNamespace に到達したらそれ自身を結果に加え、その中へは入らない）。
        /// </summary>
        void CollectChildNamespaces(Transform parent, List<FDMiNamespace> results)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var ns = child.GetComponent<FDMiNamespace>();
                if (ns != null)
                {
                    results.Add(ns);
                    continue;
                }
                CollectChildNamespaces(child, results);
            }
        }

        /// <summary>
        /// node を起点に、chain（ルートから node までの名前空間連鎖）が path のパターンに一致する場合は
        /// そのスコープ内の FDMiData を全件収集する。さらに子の名前空間へ chain を伸ばして再帰する。
        /// </summary>
        void CollectMatchingScopes(FDMiNamespace node, List<string> chain, FDMiDataPath path, Type fieldType, List<FDMiData> results)
        {
            if (path.MatchesNamespaceChain(chain))
                CollectInScope(node.transform, path.DataName, fieldType, results);

            var children = new List<FDMiNamespace>();
            CollectChildNamespaces(node.transform, children);
            foreach (var child in children)
            {
                chain.Add(child.nameSpace);
                CollectMatchingScopes(child, chain, path, fieldType, results);
                chain.RemoveAt(chain.Count - 1);
            }
        }

        /// <summary>
        /// ワイルドカードを含む絶対パス解決。isNamespaceRoot=true の各 FDMiNamespace を起点に
        /// 名前空間連鎖を列挙し、パターンに一致した全スコープから FDMiData を全件収集する。
        /// </summary>
        List<FDMiData> FindAllAbsoluteWildcard(FDMiDataPath path, Type fieldType)
        {
            var results = new List<FDMiData>();
            var allNamespaces = UnityEngine.Object.FindObjectsByType<FDMiNamespace>(FindObjectsSortMode.None);
            foreach (var ns in allNamespaces)
            {
                if (!ns.isNamespaceRoot) continue;
                CollectMatchingScopes(ns, new List<string> { ns.nameSpace }, path, fieldType, results);
            }
            return results;
        }

        /// <summary>
        /// root の子孫を深さ優先で探索し、名前と型が一致する FDMiData をすべて results に追加する。
        /// FDMiNamespace を持つ子の子孫には入らない（境界）。
        /// </summary>
        void CollectInScope(Transform root, string dataName, Type fieldType, List<FDMiData> results)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.GetComponent<FDMiNamespace>() != null) continue;

                if (child.name == dataName)
                {
                    var component = child.GetComponent(fieldType) as FDMiData;
                    if (component != null) results.Add(component);
                }

                CollectInScope(child, dataName, fieldType, results);
            }
        }
    }
}
