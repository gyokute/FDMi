using System;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Domain.Entities;

namespace tech.gyoku.FDMi.core.Editor.Domain.Repositories
{
    /// <summary>
    /// シーン上の FDMiData コンポーネントを検索するリポジトリの抽象。
    /// </summary>
    public interface IFDMiDataRepository
    {
        /// <summary>
        /// 指定したコンテキストとパスに基づいて FDMiData コンポーネントを検索する。
        /// 見つからない場合は null を返す。例外は投げない。
        /// </summary>
        /// <param name="context">属性を持つ MonoBehaviour の GameObject。</param>
        /// <param name="path">解決するパス。</param>
        /// <param name="fieldType">フィールドの宣言型（FDMiBool 等）。</param>
        FDMiData Find(GameObject context, FDMiDataPath path, Type fieldType);
    }
}
