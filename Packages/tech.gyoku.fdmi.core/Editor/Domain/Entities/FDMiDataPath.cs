using System;
using System.Collections.Generic;

namespace FDMi.core.Editor.Domain.Entities
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
        /// 先頭の "/" は除去してから解析する（"/NS_A/myBool" は "NS_A/myBool" と同義）。
        /// </summary>
        /// <param name="raw">解析するパス文字列。</param>
        public static FDMiDataPath Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new FDMiDataPath(new string[0], string.Empty);

            if (raw.StartsWith("/", StringComparison.Ordinal))
                raw = raw.Substring(1);

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

        /// <summary>
        /// 与えられた名前空間連鎖（ルートから順）がこのパスのパターンに一致するかを判定する。
        /// "*" は任意のちょうど1セグメント、"**" は任意の0個以上のセグメントにマッチする。
        /// それ以外のセグメントはリテラル文字列として完全一致が必要。
        /// 純粋な文字列比較のみで完結し、Unity API に依存しない。
        /// </summary>
        /// <param name="candidate">照合する名前空間連鎖（ルートから順の文字列配列）。</param>
        public bool MatchesNamespaceChain(IReadOnlyList<string> candidate)
        {
            return MatchesNamespaceChain(candidate, 0);
        }

        /// <summary>
        /// パターンの先頭から patternOffset 個のセグメントを読み飛ばした残りのパターンが、
        /// 与えられた名前空間連鎖に一致するかを判定する。
        /// "~" 等の特殊な先頭セグメントを Repository 側で固有の起点に解決した後、
        /// 残りのパターンだけを照合したい場合に用いる。
        /// </summary>
        /// <param name="candidate">照合する名前空間連鎖（解決済みの起点からの相対、順番通り）。</param>
        /// <param name="patternOffset">パターンの先頭から読み飛ばすセグメント数。</param>
        public bool MatchesNamespaceChain(IReadOnlyList<string> candidate, int patternOffset)
        {
            return MatchesFrom(Namespaces, patternOffset, candidate, 0);
        }

        /// <summary>
        /// pattern[pi..] と candidate[ci..] が一致するかを再帰的に判定する。
        /// "**" は 0 個以上のセグメント消費をバックトラッキングで試す。
        /// </summary>
        static bool MatchesFrom(IReadOnlyList<string> pattern, int pi, IReadOnlyList<string> candidate, int ci)
        {
            while (pi < pattern.Count)
            {
                var segment = pattern[pi];

                if (segment == "**")
                {
                    for (int skip = 0; ci + skip <= candidate.Count; skip++)
                    {
                        if (MatchesFrom(pattern, pi + 1, candidate, ci + skip))
                            return true;
                    }
                    return false;
                }

                if (ci >= candidate.Count) return false;

                if (segment != "*" && segment != candidate[ci]) return false;

                pi++;
                ci++;
            }
            return ci == candidate.Count;
        }
    }
}
