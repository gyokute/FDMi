using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FDMi.core.Attributes;
using FDMi.core.Editor.Domain.Entities;
using FDMi.core.Editor.Domain.Repositories;

namespace FDMi.core.Editor.Application.UseCases
{
    /// <summary>
    /// [FDMiDataPathAttribute] 付きフィールドに FDMiData を解決して代入するユースケース。
    /// </summary>
    public class ResolveDataPathsUseCase
    {
        static readonly Dictionary<Type, (FieldInfo field, FDMiDataPathAttribute attr)[]> _cache
            = new Dictionary<Type, (FieldInfo, FDMiDataPathAttribute)[]>();

        readonly IFDMiDataRepository _repository;

        /// <summary>
        /// コンストラクタ。IFDMiDataRepository を注入する。
        /// </summary>
        /// <param name="repository">FDMiData を検索するリポジトリ。</param>
        public ResolveDataPathsUseCase(IFDMiDataRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 対象オブジェクトの [FDMiDataPathAttribute] 付きフィールドを解決して代入する。
        /// パス文字列は属性が指す PathFieldName のフィールドの現在値（SerializedProperty 経由）から取得する。
        /// 配列フィールドには一致した FDMiData を全件、非配列フィールドには先頭の1件を割り当てる。
        /// ペアフィールドが存在しない・string型でない、または解決結果が0件の場合は何もしない（前回値を維持し、ログも出力しない）。
        /// </summary>
        /// <param name="target">解決対象の Unity オブジェクト。</param>
        public void Execute(UnityEngine.Object target)
        {
            var mb = target as MonoBehaviour;
            if (mb == null) return;

            var entries = GetCachedEntries(mb.GetType());
            if (entries.Length == 0) return;

            var so = new SerializedObject(target);
            foreach (var (field, attr) in entries)
            {
                var pathProp = so.FindProperty(attr.PathFieldName);
                if (pathProp == null || pathProp.propertyType != SerializedPropertyType.String)
                    continue;

                var path = FDMiDataPath.Parse(pathProp.stringValue);
                var isArray = field.FieldType.IsArray;
                var elementType = isArray ? field.FieldType.GetElementType() : field.FieldType;

                var found = _repository.FindAll(mb.gameObject, path, elementType);
                if (found.Length == 0) continue;

                var sp = so.FindProperty(field.Name);
                if (sp == null) continue;

                if (isArray)
                {
                    sp.arraySize = found.Length;
                    for (int i = 0; i < found.Length; i++)
                        sp.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
                }
                else
                {
                    sp.objectReferenceValue = found[0];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 型ごとの [FDMiDataPathAttribute] 付きフィールドをキャッシュから取得する。
        /// キャッシュがない場合はリフレクションで取得してキャッシュする。
        /// </summary>
        static (FieldInfo, FDMiDataPathAttribute)[] GetCachedEntries(Type type)
        {
            if (_cache.TryGetValue(type, out var cached)) return cached;

            var result = new List<(FieldInfo, FDMiDataPathAttribute)>();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = field.GetCustomAttribute<FDMiDataPathAttribute>();
                if (attr != null) result.Add((field, attr));
            }
            var entries = result.ToArray();
            _cache[type] = entries;
            return entries;
        }

        /// <summary>
        /// テスト用: 型キャッシュをクリアする。本番コードから呼び出してはならない。
        /// </summary>
        public static void ClearCacheForTesting() => _cache.Clear();
    }
}
