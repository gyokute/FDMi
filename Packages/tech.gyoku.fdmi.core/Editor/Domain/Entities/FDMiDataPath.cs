using System;
using System.Collections.Generic;

namespace tech.gyoku.FDMi.core.Editor.Domain.Entities
{
    /// <summary>
    /// FDMiData を解決する際のパスを表す不変値オブジェクト。
    /// "/" 区切りで名前空間とデータ名を表現する。
    /// </summary>
    public sealed class FDMiDataPath
    {
        /// <summary>名前空間セグメント（読み取り専用リスト）。IsAbsolute=false の場合は空リスト。</summary>
        public IReadOnlyList<string> Namespaces { get; }

        /// <summary>末尾のデータ名セグメント。</summary>
        public string DataName { get; }

        /// <summary>名前空間指定を持つ絶対パスかどうか。</summary>
        public bool IsAbsolute => Namespaces.Count > 0;

        FDMiDataPath(string[] namespaces, string dataName)
        {
            Namespaces = System.Array.AsReadOnly(namespaces);
            DataName = dataName;
        }

        /// <summary>
        /// パス文字列を解析して FDMiDataPath を生成する。
        /// 解析は失敗しない。空・null 入力は DataName="" の無効パスとして扱う。
        /// </summary>
        /// <param name="raw">解析するパス文字列。</param>
        public static FDMiDataPath Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new FDMiDataPath(new string[0], string.Empty);

            var segments = raw.Split('/');
            var dataName = segments[segments.Length - 1];
            var namespaces = new string[segments.Length - 1];
            Array.Copy(segments, namespaces, namespaces.Length);
            return new FDMiDataPath(namespaces, dataName);
        }

        /// <summary>パスの等値比較。DataName と Namespaces の内容を比較する。</summary>
        public override bool Equals(object obj)
        {
            if (!(obj is FDMiDataPath other)) return false;
            if (DataName != other.DataName) return false;
            if (Namespaces.Count != other.Namespaces.Count) return false;
            for (int i = 0; i < Namespaces.Count; i++)
                if (Namespaces[i] != other.Namespaces[i]) return false;
            return true;
        }

        /// <summary>ハッシュコードを返す。</summary>
        public override int GetHashCode()
        {
            int hash = DataName?.GetHashCode() ?? 0;
            foreach (var ns in Namespaces) hash ^= ns?.GetHashCode() ?? 0;
            return hash;
        }
    }
}
