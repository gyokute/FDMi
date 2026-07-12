using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Application.UseCases
{
    public interface IFDMiAutoSetupUseCase
    {
        /// <summary>
        /// 対象オブジェクトの [FDMiDataPathAttribute] 付きフィールドを解決して代入し、[FDMiRegisterCallbackAttribute] 付きフィールドを登録する。
        /// </summary>
        /// <param name="target">解決対象の Unity オブジェクト。</param>
        void Execute(UnityEngine.Object target);

        /// <summary>
        /// シーン内の全 MonoBehaviour を対象に Execute を実行する。
        /// </summary>
        void ExecuteAll();
    }
}
