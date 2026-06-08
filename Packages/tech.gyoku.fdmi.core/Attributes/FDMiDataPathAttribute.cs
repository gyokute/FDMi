using System;

namespace FDMi.core.Attributes
{
    /// <summary>
    /// 指定したパスからFDMi変数を解決する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class FDMiDataPathAttribute : Attribute
    {
        /// <summary>
        /// 変数のパス。
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// 指定したパスでFDMi変数を解決する属性を作成。
        /// </summary>
        /// <param name="path">変数のパス。</param>
        public FDMiDataPathAttribute(string path)
        {
            Path = path;
        }
    }
}
