using System.Collections.Generic;
using FDMi.core;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 機体の範囲。core の "~/" (FDMiDataPath) と同じ起点を使う: 祖先で最初の名前空間ルート。
    // 名前空間ルートより外 (別の機体・共有の global クラスタ) を拾わないので、同じシーンに何機あっても
    // 混ざらない。FDMiSeatManager の allSeats を解く FillAllSeatsUseCase と同じ規則。
    //
    // Why not 祖先の Rigidbody / 親の Wing をそのまま使う: 部品の置き場所を強制しないため。
    // ただし装置 (FDMiControlSurface) だけは翼の子という規則にする。区間の並びが翼に従うので、
    // 機体には主翼・水平尾翼・垂直尾翼と複数の翼があり、名前空間の範囲では翼を特定できない。
    static class FDMiAircraftScope
    {
        // 祖先 (自分を含む) で最初の名前空間ルート。名前空間の中に居なければ null。
        public static FDMiNamespace Root(Transform t)
        {
            for (Transform current = t; current != null; current = current.parent)
            {
                FDMiNamespace ns = current.GetComponent<FDMiNamespace>();
                if (ns != null && ns.isNamespaceRoot)
                    return ns;
            }
            return null;
        }

        // 機体内のコンポーネントを階層パス順に返す。順序を固定して、再解決で配列の中身が
        // 入れ替わらないようにする (FindObjectsByType の順序は保証されない)。
        public static List<T> FindAll<T>(Component context)
            where T : Component
        {
            List<T> found = new List<T>();
            FDMiNamespace root = Root(context.transform);
            if (root == null)
                return found;

            T[] all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<KeyValuePair<string, T>> ordered = new List<KeyValuePair<string, T>>();
            for (int i = 0; i < all.Length; i++)
                if (Root(all[i].transform) == root)
                    ordered.Add(new KeyValuePair<string, T>(Path(all[i].transform), all[i]));
            ordered.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            for (int i = 0; i < ordered.Count; i++)
                found.Add(ordered[i].Value);
            return found;
        }

        // ルートまでの "Parent/Child" 形式のパス。並び順の安定化にのみ使う。
        static string Path(Transform t)
        {
            string path = t.name;
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
