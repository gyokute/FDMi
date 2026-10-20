using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Application.UseCases
{
    // Entity -> transform + Bake値の唯一の書込者。生成/吸着/ステップ移動を内包する。
    // Why-not: BakeCalc を再実装しない。FDMiReferencePointEditor.BakeCalc (internal static)
    // と同一式 (500m閾値・対称丸め・CARRY_KM_MAX反復・MM_PER_CHUNK繰上げ) を使い dirty にならない。
    // Why-not: Place 後に Resolve を自動実行しない。全件走査で重く別manager警告を誘発する。
    // 未設定時の manager 補完に留め、決定的解決は既存メニュー/自動フックに委ねる。
    public sealed class PlaceGroundsUseCase
    {
        public const string GridRootName = "WideEarth_Grid";

        public int Generate(double originLatDeg, double originLonDeg, float tileSizeM,
            int nX, int nZ, float altM, GameObject prefab)
        {
            GameObject root = EnsureGridRoot();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Spherical Grounds");
            int created = 0;

            double dLat = EarthMath.DeltaLatDeg(tileSizeM, altM);
            for (int iz = 0; iz < nZ; iz++)
            {
                double lat = originLatDeg + (iz - (nZ - 1) * 0.5) * dLat;
                double dLon = EarthMath.DeltaLonDeg(tileSizeM, lat, altM);
                for (int ix = 0; ix < nX; ix++)
                {
                    double lon = originLonDeg + (ix - (nX - 1) * 0.5) * dLon;
                    string name = GeoTile.PlaceName(lat, lon);
                    if (root.transform.Find(name) != null)
                        continue;
                    GameObject go = InstantiateTile(prefab, root.transform, name);
                    PlaceOne(go, lat, lon, altM);
                    created++;
                }
            }

            Undo.CollapseUndoOperations(group);
            return created;
        }

        // モード(a): 選択中タイルをグリッドへ吸着。Bake済み total を逆算して最近傍ます目に寄せる。
        public int SnapSelectionToGrid(double originLatDeg, double originLonDeg, float tileSizeM, float altM)
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
                return 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Snap Grounds To Grid");
            int moved = 0;
            double dLat = EarthMath.DeltaLatDeg(tileSizeM, altM);
            for (int i = 0; i < selected.Length; i++)
            {
                FDMiReferencePoint rp = selected[i].GetComponent<FDMiReferencePoint>();
                if (rp == null)
                    continue;
                Vector3 total = EarthMath.TotalM(rp._position, rp._posMm, rp._chunkPosition);
                EarthMath.EcefToLatLon(total, out double lat, out double lon, out double _);
                double snappedLat = originLatDeg + System.Math.Round((lat - originLatDeg) / dLat) * dLat;
                double dLon = EarthMath.DeltaLonDeg(tileSizeM, snappedLat, altM);
                double snappedLon = originLonDeg + System.Math.Round((lon - originLonDeg) / dLon) * dLon;
                PlaceOne(selected[i], snappedLat, snappedLon, altM);
                selected[i].name = GeoTile.PlaceName(snappedLat, snappedLon);
                moved++;
            }
            Undo.CollapseUndoOperations(group);
            return moved;
        }

        // 選択中タイル群をメートルで移動し即時再配置+Bakeする。
        // Why-not: deg ではなく m で受ける。dLon はタイル自身の緯度で変わるためループ内で換算する (originLat 固定では広域で誤差)。
        public int StepMoveSelection(double stepNorthM, double stepEastM, float altM)
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
                return 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Step Move Grounds");
            int moved = 0;
            double dLatDeg = EarthMath.DeltaLatDeg(stepNorthM, altM);
            for (int i = 0; i < selected.Length; i++)
            {
                FDMiReferencePoint rp = selected[i].GetComponent<FDMiReferencePoint>();
                if (rp == null)
                    continue;
                Vector3 total = EarthMath.TotalM(rp._position, rp._posMm, rp._chunkPosition);
                EarthMath.EcefToLatLon(total, out double lat, out double lon, out double _);
                double dLonDeg = EarthMath.DeltaLonDeg(stepEastM, lat, altM);
                double nextLat = lat + dLatDeg;
                double nextLon = lon + dLonDeg;
                PlaceOne(selected[i], nextLat, nextLon, altM);
                selected[i].name = GeoTile.PlaceName(nextLat, nextLon);
                moved++;
            }
            Undo.CollapseUndoOperations(group);
            return moved;
        }

        // transform + Bake値を同時に書く唯一の経路。
        public static void PlaceOne(GameObject go, double latDeg, double lonDeg, float altM)
        {
            GeoTile tile = new GeoTile(latDeg, lonDeg, altM);
            Transform tr = go.transform;
            Undo.RecordObject(tr, "Place Spherical Ground");
            tr.position = tile.Ecef;
            tr.rotation = tile.Rotation;

            FDMiReferencePoint rp = go.GetComponent<FDMiReferencePoint>();
            if (rp == null)
                rp = go.GetComponent<FDMiRelativeObjectSyncManager>();
            if (rp == null)
                return;
            FDMiReferencePointEditor.BakeCalc(tr.position, new int[3], Vector3.zero,
                out Vector3 pos, out int[] mm, out Vector3 chunk);
            Undo.RecordObject(rp, "Bake Spherical Ground");
            rp._position = pos;
            rp._posMm[0] = mm[0];
            rp._posMm[1] = mm[1];
            rp._posMm[2] = mm[2];
            rp._chunkPosition = chunk;
            rp._rotation = tr.rotation;
            ComplementManagerRef(rp);
            EditorUtility.SetDirty(rp);
        }

        static GameObject InstantiateTile(GameObject prefab, Transform parent, string name)
        {
            GameObject go;
            if (prefab != null)
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            else
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Generate Spherical Ground");
                go.AddComponent<FDMiRelativeGroundSync>();
            }
            go.name = name;
            go.transform.SetParent(parent, false);
            if (prefab != null)
                Undo.RegisterCreatedObjectUndo(go, "Generate Spherical Ground");
            return go;
        }

        public static GameObject EnsureGridRoot()
        {
            GameObject root = GameObject.Find(GridRootName);
            if (root != null)
                return root;
            root = new GameObject(GridRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create WideEarth Grid Root");
            return root;
        }

        // Why-not: 全件 Resolve はしない。未設定の syncManager/parent/root だけ manager で補完する。
        static void ComplementManagerRef(FDMiReferencePoint rp)
        {
            if (rp.syncManager != null && rp.parentRefPoint != null && rp.rootRefPoint != null)
                return;
            FDMiRelativeObjectSyncManager manager = Object.FindFirstObjectByType<FDMiRelativeObjectSyncManager>();
            if (manager == null)
                return;
            if (rp.syncManager == null && !(rp is FDMiRelativeObjectSyncManager))
                rp.syncManager = manager;
            if (rp.parentRefPoint == null)
                rp.parentRefPoint = manager;
            if (rp.rootRefPoint == null)
                rp.rootRefPoint = manager;
        }
    }
}
