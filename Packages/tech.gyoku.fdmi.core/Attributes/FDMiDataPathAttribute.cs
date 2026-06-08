using System;

namespace FDMi.core
{
    /// <summary>
    /// 解決結果を受け取るフィールドと、パス文字列を保持するフィールドをペアリング宣言する属性。
    /// パスの値そのものはインスタンスの文字列フィールドが持つため、インスタンスごとに編集可能になる。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        Inherited = true,
        AllowMultiple = false
    )]
    public sealed class FDMiDataPathAttribute : Attribute
    {
        /// <summary>
        /// パス文字列を保持するフィールドの名前（nameof() で指定する）。
        /// </summary>
        public readonly string PathFieldName;

        /// <summary>
        /// 指定した名前のフィールドとペアリングする属性を作成する。
        /// </summary>
        /// <param name="pathFieldName">パス文字列を保持するフィールドの名前。nameof() で指定する。</param>
        public FDMiDataPathAttribute(string pathFieldName)
        {
            PathFieldName = pathFieldName;
        }
    }
}
