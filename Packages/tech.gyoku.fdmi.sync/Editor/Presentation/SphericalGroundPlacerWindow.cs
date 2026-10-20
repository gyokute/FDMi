using System.Collections.Generic;
using FDMi.sync.Editor.Application.UseCases;
using FDMi.sync.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor.Presentation
{
    // ミニマップ+操作パネル。状態 (中心/グリッド設定/プレビュー中心) は Window内 SerializeField のみ。.asset に残さない。
    // Why-not: Repository を作らない。対象は Hierarchy 直操作で Asset 化しない。
    // 層を挟むと .asset 自動生成汚染リスクが増えるだけなので FindObjectsByType 直読みに留める。
    public sealed class SphericalGroundPlacerWindow : EditorWindow
    {
        [SerializeField] double originLatDeg = 35.681;
        [SerializeField] double originLonDeg = 139.767;
        [SerializeField] float tileSizeM = 5000f;
        [SerializeField] int nX = 3;
        [SerializeField] int nZ = 3;
        [SerializeField] float altM;
        [SerializeField] GameObject prefab;

        [SerializeField] double previewLatDeg = 35.681;
        [SerializeField] double previewLonDeg = 139.767;
        [SerializeField] float previewAltM;
        [SerializeField] bool isPreviewing;
        [SerializeField] bool groundOnly;

        // Why-not: インスタンスではなく static で公開。Inspector 側 (別Window) が Preview 中かを知る唯一の経路にするため。
        public static bool IsPreviewingGlobal;

        [SerializeField] double mapCenterLatDeg = 35.681;
        [SerializeField] double mapCenterLonDeg = 139.767;
        [SerializeField] float mapScale = 8f;
        [SerializeField] float stepM = 1000f;
        [SerializeField] float hideKm = 50f;
        [SerializeField] Vector2 listScroll;

        readonly List<CachedTile> cached = new List<CachedTile>();
        bool dragging;

        struct CachedTile
        {
            public GameObject Go;
            public double LatDeg;
            public double LonDeg;
            public string Name;
            public bool IsRoot;
            public bool IsSelected;
        }

        [MenuItem("FDMi/Sync/Spherical Ground Placer")]
        public static void Open()
        {
            GetWindow<SphericalGroundPlacerWindow>("Ground Placer");
        }

        void OnEnable()
        {
            IsPreviewingGlobal = false;
            RefreshTiles();
        }

        void OnFocus()
        {
            RefreshTiles();
        }

        void RefreshTiles()
        {
            cached.Clear();
            FDMiReferencePoint[] all = FindObjectsByType<FDMiReferencePoint>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            HashSet<int> selectedIds = new HashSet<int>();
            foreach (GameObject go in Selection.gameObjects)
                selectedIds.Add(go.GetInstanceID());
            for (int i = 0; i < all.Length; i++)
            {
                FDMiReferencePoint rp = all[i];
                // Why-not: Manager の Bake total は 0 で EcefToLatLon(0) は NaN になるため除外。PlayerPosition は ECEF 系なので残す。
                if (rp is FDMiRelativeObjectSyncManager) continue;
                if (rp == null)
                    continue;
                Vector3 total = EarthMath.TotalM(rp._position, rp._posMm, rp._chunkPosition);
                EarthMath.EcefToLatLon(total, out double lat, out double lon, out double _);
                CachedTile c;
                c.Go = rp.gameObject;
                c.LatDeg = lat;
                c.LonDeg = lon;
                c.Name = rp.gameObject.name;
                c.IsRoot = rp._isRoot;
                c.IsSelected = selectedIds.Contains(rp.gameObject.GetInstanceID());
                cached.Add(c);
            }
            cached.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
        }

        void OnGUI()
        {
            if (isPreviewing)
            {
                // Why-not: 原点再適用では復元にならない (適用前 transform=絶対ECEF に対し適用後は相対値のため)。真の復元は Undo のみ。
                EditorGUILayout.HelpBox("PREVIEW中 — Bake/Place禁止。解除は Undo で復元する。", MessageType.Warning);
                if (GUILayout.Button("プレビュー解除 (Undo)"))
                {
                    Undo.PerformUndo();
                    Debug.Log("[GroundPlacer] Preview cleared (Undo)");
                    isPreviewing = false;
                    IsPreviewingGlobal = false;
                    RefreshTiles();
                }
            }

            EditorGUILayout.LabelField("グリッド定義 (タイル毎高度・回転・可変サイズ・LODなし)", EditorStyles.boldLabel);
            originLatDeg = EditorGUILayout.DoubleField("originLat [deg]", originLatDeg);
            originLonDeg = EditorGUILayout.DoubleField("originLon [deg]", originLonDeg);
            tileSizeM = EditorGUILayout.FloatField("tileSize [m]", tileSizeM);
            nX = EditorGUILayout.IntField("nX (東西)", nX);
            nZ = EditorGUILayout.IntField("nZ (南北)", nZ);
            altM = EditorGUILayout.FloatField("alt [m] (全タイル共通)", altM);
            prefab = (GameObject)EditorGUILayout.ObjectField("地面Prefab1種", prefab, typeof(GameObject), false);

            using (new EditorGUI.DisabledScope(isPreviewing))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate"))
                {
                    int created = new PlaceGroundsUseCase().Generate(
                        originLatDeg, originLonDeg, tileSizeM, nX, nZ, altM, prefab);
                    Debug.Log($"[GroundPlacer] Generate: created={created}");
                    RefreshTiles();
                }
                if (GUILayout.Button("選択を吸着(a)"))
                {
                    int moved = new PlaceGroundsUseCase().SnapSelectionToGrid(
                        originLatDeg, originLonDeg, tileSizeM, altM);
                    Debug.Log($"[GroundPlacer] Snap: moved={moved}");
                    RefreshTiles();
                }
                EditorGUILayout.EndHorizontal();
                if (isPreviewing)
                    EditorGUILayout.HelpBox("Preview中は誤焼付け防止のため Place/Generate を無効化。", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("間隔移動 (対象=Hierarchy選択中。InspectorではなくWindowで操作)", EditorStyles.boldLabel);
            stepM = EditorGUILayout.FloatField("ステップ [m] (自由入力)", stepM);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("100m"))
                stepM = 100f;
            if (GUILayout.Button("1000m"))
                stepM = 1000f;
            if (GUILayout.Button("5000m"))
                stepM = 5000f;
            if (GUILayout.Button("1タイル"))
                stepM = tileSizeM;
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(isPreviewing))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("N (+)"))
                    Step(1, 0);
                if (GUILayout.Button("S (-)"))
                    Step(-1, 0);
                if (GUILayout.Button("E (+)"))
                    Step(0, 1);
                if (GUILayout.Button("W (-)"))
                    Step(0, -1);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("プレビューシフト (見た目だけ。Bake値・配線に触らない)", EditorStyles.boldLabel);
            previewLatDeg = EditorGUILayout.DoubleField("previewLat [deg]", previewLatDeg);
            previewLonDeg = EditorGUILayout.DoubleField("previewLon [deg]", previewLonDeg);
            previewAltM = EditorGUILayout.FloatField("previewAlt [m]", previewAltM);
            groundOnly = EditorGUILayout.Toggle("地面のみ (既定=OFFでsync全件)", groundOnly);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview適用"))
            {
                int n = new PreviewShiftUseCase().Apply(previewLatDeg, previewLonDeg, previewAltM, groundOnly);
                Debug.Log($"[GroundPlacer] Preview apply: shifted={n} (Bake unchanged)");
                isPreviewing = true;
                IsPreviewingGlobal = true;
            }
            if (GUILayout.Button("Resolve配線 (手動)"))
            {
                new ResolveSyncWiringUseCase().ExecuteAll();
                RefreshTiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("広域ミニマップ (正距円筒。単色海+矩形+経緯線)", EditorStyles.boldLabel);
            mapScale = EditorGUILayout.Slider("ズーム (1〜64)", mapScale, 1f, 64f);
            Rect mapRect = GUILayoutUtility.GetRect(position.width - 10, 220f, GUILayout.ExpandWidth(true));
            List<MinimapView.Tile> tiles = new List<MinimapView.Tile>(cached.Count);
            for (int i = 0; i < cached.Count; i++)
            {
                MinimapView.Tile t;
                t.LatDeg = cached[i].LatDeg;
                t.LonDeg = cached[i].LonDeg;
                t.Name = cached[i].Name;
                t.IsRoot = cached[i].IsRoot;
                t.IsSelected = cached[i].IsSelected;
                tiles.Add(t);
            }
            MinimapView.Draw(mapRect, tiles, mapCenterLatDeg, mapCenterLonDeg, mapScale,
                previewLatDeg, previewLonDeg, originLatDeg, originLonDeg);
            HandleMapEvent(mapRect, tiles);
            EditorGUILayout.LabelField("左:選択+Preview中心 / 右:origin設定 / 輪:ズーム / 遍:パン / Wクリック:Scene俯瞰");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
                RefreshTiles();
            if (GUILayout.Button("Focus選択"))
                FocusSelection();
            if (GUILayout.Button("遠方タイル表示切替"))
                ToggleFarTiles();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"タイル一覧 ({cached.Count}件。可視行のみGUI)", EditorStyles.boldLabel);
            float rowH = 18f;
            int totalRows = cached.Count;
            float listH = Mathf.Min(220f, Mathf.Max(60f, totalRows * rowH));
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(listH));
            int start = Mathf.Clamp((int)(listScroll.y / rowH) - 2, 0, Mathf.Max(0, totalRows - 1));
            int visible = Mathf.Clamp((int)(listH / rowH) + 4, 0, totalRows - start);
            GUILayout.Space(start * rowH);
            for (int i = start; i < start + visible; i++)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
                EditorGUILayout.LabelField(cached[i].Name, GUILayout.Width(220f));
                EditorGUILayout.LabelField($"{cached[i].LatDeg:F3}, {cached[i].LonDeg:F3}", GUILayout.Width(160f));
                if (GUILayout.Button("選択", GUILayout.Width(50f)))
                {
                    Selection.activeGameObject = cached[i].Go;
                    previewLatDeg = cached[i].LatDeg;
                    previewLonDeg = cached[i].LonDeg;
                    RefreshTiles();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(Mathf.Max(0f, (totalRows - start - visible) * rowH));
            EditorGUILayout.EndScrollView();
        }

        void Step(int dNorth, int dEast)
        {
            // Why-not: deg 換算は UseCase 側に寄せる。dLon はタイル自身の緯度で変わるため Window ではメートルで渡す。
            int moved = new PlaceGroundsUseCase().StepMoveSelection(dNorth * stepM, dEast * stepM, altM);
            Debug.Log($"[GroundPlacer] Step: moved={moved}");
            RefreshTiles();
        }

        void HandleMapEvent(Rect mapRect, List<MinimapView.Tile> tiles)
        {
            Event e = Event.current;
            if (!mapRect.Contains(e.mousePosition))
                return;
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                Vector2 delta = e.delta;
                double cosLat = System.Math.Cos(mapCenterLatDeg * System.Math.PI / 180.0);
                mapCenterLonDeg -= delta.x / (mapScale * mapRect.width * 0.02) / cosLat;
                mapCenterLatDeg += delta.y / (mapScale * mapRect.height * 0.02);
                dragging = true;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (dragging)
                {
                    dragging = false;
                    e.Use();
                    return;
                }
                MinimapView.GuiToLatLon(e.mousePosition, mapCenterLatDeg, mapCenterLonDeg,
                    mapScale, mapRect, out double lat, out double lon);
                int idx = MinimapView.NearestTileIndex(e.mousePosition, tiles,
                    mapCenterLatDeg, mapCenterLonDeg, mapScale, mapRect);
                if (idx >= 0 && idx < cached.Count)
                {
                    Selection.activeGameObject = cached[idx].Go;
                    previewLatDeg = cached[idx].LatDeg;
                    previewLonDeg = cached[idx].LonDeg;
                    if (e.clickCount == 2)
                        FocusSelection();
                    RefreshTiles();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 1)
            {
                MinimapView.GuiToLatLon(e.mousePosition, mapCenterLatDeg, mapCenterLonDeg,
                    mapScale, mapRect, out double lat, out double lon);
                originLatDeg = lat;
                originLonDeg = lon;
                mapCenterLatDeg = lat;
                mapCenterLonDeg = lon;
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                float dir = Mathf.Sign(e.delta.y);
                mapScale = Mathf.Clamp(mapScale * (dir > 0 ? 0.9f : 1.1f), 1f, 64f);
                e.Use();
                Repaint();
            }
        }

        void FocusSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null)
                return;
            sv.FrameSelected();
            // 上空俯瞰 (60deg) で周辺確認。距離はタイルサイズ連動。
            sv.LookAt(go.transform.position, Quaternion.Euler(60f, 0f, 0f), tileSizeM * 4f);
        }

        void ToggleFarTiles()
        {
            // 最小限1ボタンのみ (GameObjectを消さない)。SceneVisibilityManager で退避する。
            bool anyHidden = false;
            for (int i = 0; i < cached.Count; i++)
            {
                if (cached[i].Go == null)
                    continue;
                if (SceneVisibilityManager.instance.IsHidden(cached[i].Go, false))
                {
                    anyHidden = true;
                    break;
                }
            }
            for (int i = 0; i < cached.Count; i++)
            {
                if (cached[i].Go == null)
                    continue;
                if (anyHidden)
                {
                    SceneVisibilityManager.instance.Show(cached[i].Go, false);
                }
                else
                {
                    double dLat = (cached[i].LatDeg - mapCenterLatDeg) * 111000.0;
                    double dLon = (cached[i].LonDeg - mapCenterLonDeg) * 111000.0
                        * System.Math.Cos(mapCenterLatDeg * System.Math.PI / 180.0);
                    double distM = System.Math.Sqrt(dLat * dLat + dLon * dLon);
                    if (distM > hideKm * 1000.0)
                        SceneVisibilityManager.instance.Hide(cached[i].Go, false);
                }
            }
        }
    }
}
