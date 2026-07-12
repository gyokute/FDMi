using System;
using FDMi.core.Editor.Domain.Entities;
using UnityEngine;

namespace FDMi.core.Editor.Domain.Repositories
{
    /// <summary>
    /// シーン上の FDMiData コンポーネントを検索するリポジトリの抽象。
    /// </summary>
    public interface IFDMiDataRepository
    {
        /// <summary>
        /// 指定したコンテキストとパスに一致する FDMiData をすべて検索する。
        /// 一致が無ければ空配列を返す。例外は投げない。
        /// </summary>
        /// <param name="context">属性を持つ MonoBehaviour の GameObject。</param>
        /// <param name="path">解決するパス。</param>
        /// <param name="fieldType">フィールドの宣言型（配列の場合は要素型。FDMiBool 等）。</param>
        FDMiData[] FindAll(GameObject context, FDMiDataPath path, Type fieldType);
    }
}
