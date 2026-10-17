using System;

namespace FDMi.core
{
    /// <summary>
    /// パス文字列を属性へ直接指定する属性。FDMiData を解決して代入するフィールドに付与する。
    /// 値は型に固定され、全インスタンスで同一パスとなり、Inspector から変更できない。
    /// ペアの FDMiDataPathAttribute とは契約が正反対（パス値をインスタンスの string フィールドが持ち、
    /// インスタンスごとに編集可能）であるため、派生せず別型として区別する。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        Inherited = true,
        AllowMultiple = false
    )]
    public sealed class FDMiDataPathConstAttribute : Attribute
    {
        /// <summary>リテラルパス。例: "~/Inzone"。</summary>
        public readonly string Path;

        /// <summary>
        /// リテラルパスを指定して属性を作成する。
        /// </summary>
        /// <param name="path">解決するパス（例: "~/Inzone"）。</param>
        public FDMiDataPathConstAttribute(string path)
        {
            Path = path;
        }
    }
}
