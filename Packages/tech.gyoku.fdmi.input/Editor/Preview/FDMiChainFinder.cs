using System.Collections.Generic;
using FDMi.core;
using UnityEngine;

namespace FDMi.input.Editor
{
    public static class FDMiChainFinder
    {
        public static List<FDMiTransformMove> FindMoves(FDMiData data)
        {
            var result = new List<FDMiTransformMove>();
            if (data == null)
                return result;

            var all = UnityEngine.Object.FindObjectsByType<FDMiTransformMove>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < all.Length; i++)
            {
                var move = all[i];
                if (move == null || !move.isActiveAndEnabled)
                    continue;
                if (move.Value == data)
                    result.Add(move);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        public static List<FDMiTransformRotate> FindRotates(FDMiData data)
        {
            var result = new List<FDMiTransformRotate>();
            if (data == null)
                return result;

            var all = UnityEngine.Object.FindObjectsByType<FDMiTransformRotate>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < all.Length; i++)
            {
                var rotate = all[i];
                if (rotate == null || !rotate.isActiveAndEnabled)
                    continue;
                if (rotate.Value == data)
                    result.Add(rotate);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }
    }
}
