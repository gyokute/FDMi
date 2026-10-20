using System;
using System.Collections.Generic;
using UnityEngine;

namespace FDMi.sync.Editor.Presentation
{
    // 描画のみの static helper。ロジックを持たない。
    // Why-not: Handles/GL を使わない。2D配置確認に3Dギズモは要らず、
    // GL はドメインリロード・SRP干渉の温床。Scene確認は既存SceneViewに任せる。
    // Why-not: EditorApplication.update 駆動しない。Repaint はドラッグ中のみで足りる。
    public static class MinimapView
    {
        public struct Tile
        {
            public double LatDeg;
            public double LonDeg;
            public string Name;
            public bool IsRoot;
            public bool IsSelected;
        }

        static Texture2D whiteTex;

        static Texture2D WhiteTex()
        {
            if (whiteTex == null)
            {
                whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
            }
            return whiteTex;
        }

        static void FillRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, WhiteTex());
            GUI.color = prev;
        }

        // 正距円筒 x=(lon-originLon)*cos(originLat)、y=(lat-originLat)。縦横比だけ cos 補正。
        public static Vector2 LatLonToGui(double latDeg, double lonDeg,
            double centerLatDeg, double centerLonDeg, float scale, Rect rect)
        {
            double cosLat = Math.Cos(centerLatDeg * Math.PI / 180.0);
            double dx = (lonDeg - centerLonDeg) * cosLat;
            double dy = (latDeg - centerLatDeg);
            float cx = rect.x + rect.width * 0.5f;
            float cy = rect.y + rect.height * 0.5f;
            return new Vector2(
                cx + (float)(dx * scale * rect.width * 0.02),
                cy - (float)(dy * scale * rect.height * 0.02));
        }

        public static void GuiToLatLon(Vector2 gui, double centerLatDeg, double centerLonDeg,
            float scale, Rect rect, out double latDeg, out double lonDeg)
        {
            double cosLat = Math.Cos(centerLatDeg * Math.PI / 180.0);
            float cx = rect.x + rect.width * 0.5f;
            float cy = rect.y + rect.height * 0.5f;
            double dx = (gui.x - cx) / (scale * rect.width * 0.02);
            double dy = -(gui.y - cy) / (scale * rect.height * 0.02);
            lonDeg = centerLonDeg + dx / cosLat;
            latDeg = centerLatDeg + dy;
        }

        // 最近傍タイルを数学的に特定 (ピッキング用GL不要)。
        public static int NearestTileIndex(Vector2 gui, IList<Tile> tiles,
            double centerLatDeg, double centerLonDeg, float scale, Rect rect)
        {
            int best = -1;
            float bestD2 = float.MaxValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                Vector2 p = LatLonToGui(tiles[i].LatDeg, tiles[i].LonDeg,
                    centerLatDeg, centerLonDeg, scale, rect);
                float d2 = (p - gui).sqrMagnitude;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = i;
                }
            }
            return best;
        }

        public static void Draw(Rect rect, IList<Tile> tiles,
            double centerLatDeg, double centerLonDeg, float scale,
            double previewLatDeg, double previewLonDeg,
            double originLatDeg, double originLonDeg)
        {
            // 背景は単色海ベタ rect。ミニマップ実地図/衛星画像/外部通信キャッシュは作らない (YAGNI)。
            FillRect(rect, new Color(0.08f, 0.16f, 0.28f));

            // 経緯線 (10degグリッド)。
            double cosLat = Math.Cos(centerLatDeg * Math.PI / 180.0);
            for (int g = -18; g <= 18; g++)
            {
                double lon = Math.Floor(centerLonDeg / 10.0) * 10.0 + g * 10.0;
                Vector2 a = LatLonToGui(centerLatDeg - 90, lon, centerLatDeg, centerLonDeg, scale, rect);
                Vector2 b = LatLonToGui(centerLatDeg + 90, lon, centerLatDeg, centerLonDeg, scale, rect);
                DrawLine(a, b, new Color(1f, 1f, 1f, 0.08f), rect);
            }
            for (int g = -9; g <= 9; g++)
            {
                double lat = Math.Floor(centerLatDeg / 10.0) * 10.0 + g * 10.0;
                Vector2 a = LatLonToGui(lat, centerLonDeg - 180 * cosLat, centerLatDeg, centerLonDeg, scale, rect);
                Vector2 b = LatLonToGui(lat, centerLonDeg + 180 * cosLat, centerLatDeg, centerLonDeg, scale, rect);
                DrawLine(a, b, new Color(1f, 1f, 1f, 0.08f), rect);
            }

            // タイル矩形は GUI.Box。
            for (int i = 0; i < tiles.Count; i++)
            {
                Vector2 p = LatLonToGui(tiles[i].LatDeg, tiles[i].LonDeg,
                    centerLatDeg, centerLonDeg, scale, rect);
                if (!rect.Contains(p))
                    continue;
                Rect box = new Rect(p.x - 4, p.y - 4, 8, 8);
                Color c = tiles[i].IsSelected ? Color.yellow : new Color(0.4f, 0.9f, 0.5f);
                FillRect(box, c);
                if (tiles[i].IsRoot)
                    GUI.Label(new Rect(p.x + 6, p.y - 8, 20, 16), "▲");
            }

            // 原点⊕・プレビュー中心◎。
            Vector2 o = LatLonToGui(originLatDeg, originLonDeg, centerLatDeg, centerLonDeg, scale, rect);
            if (rect.Contains(o))
                GUI.Label(new Rect(o.x - 8, o.y - 8, 16, 16), "⊕");
            Vector2 pv = LatLonToGui(previewLatDeg, previewLonDeg, centerLatDeg, centerLonDeg, scale, rect);
            if (rect.Contains(pv))
                GUI.Label(new Rect(pv.x - 8, pv.y - 8, 16, 16), "◎");
        }

        static void DrawLine(Vector2 a, Vector2 b, Color c, Rect clip)
        {
            // KISS: 1px水平/垂直に畳んで描く。斜線の厳密描画は要らない。
            if (Math.Abs(a.x - b.x) >= Math.Abs(a.y - b.y))
            {
                float y = Mathf.Clamp((a.y + b.y) * 0.5f, clip.y, clip.y + clip.height);
                float x0 = Mathf.Clamp(Mathf.Min(a.x, b.x), clip.x, clip.x + clip.width);
                float x1 = Mathf.Clamp(Mathf.Max(a.x, b.x), clip.x, clip.x + clip.width);
                FillRect(new Rect(x0, y, Mathf.Max(1f, x1 - x0), 1f), c);
            }
            else
            {
                float x = Mathf.Clamp((a.x + b.x) * 0.5f, clip.x, clip.x + clip.width);
                float y0 = Mathf.Clamp(Mathf.Min(a.y, b.y), clip.y, clip.y + clip.height);
                float y1 = Mathf.Clamp(Mathf.Max(a.y, b.y), clip.y, clip.y + clip.height);
                FillRect(new Rect(x, y0, 1f, Mathf.Max(1f, y1 - y0)), c);
            }
        }
    }
}
