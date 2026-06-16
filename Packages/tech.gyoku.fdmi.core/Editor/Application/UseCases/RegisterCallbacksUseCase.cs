using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Application.UseCases
{
    public class RegisterCallbacksUseCase
    {
        static readonly Dictionary<Type, (FieldInfo field, FDMiRegisterCallbackAttribute[] attrs)[]> _fieldCache
            = new Dictionary<Type, (FieldInfo, FDMiRegisterCallbackAttribute[])[]>();

        static readonly Dictionary<MonoBehaviour, HashSet<FDMiData>> _registrations
            = new Dictionary<MonoBehaviour, HashSet<FDMiData>>();

        public void Execute(MonoBehaviour target)
        {
            if (target == null) return;

            var entries = GetCachedEntries(target.GetType());
            if (entries.Length == 0) return;

            // 前回登録先のみを対象に target のエントリを削除（全スキャン不要）
            if (_registrations.TryGetValue(target, out var prev))
                foreach (var data in prev)
                    if (data != null) RemoveEntriesForTarget(data, target);

            // 現在のフィールド値で再登録
            var next = new HashSet<FDMiData>();
            foreach (var (field, attrs) in entries)
            {
                var data = field.GetValue(target) as FDMiData;
                if (data == null) continue;

                var so = new SerializedObject(data);
                so.Update();
                var bProp = so.FindProperty("callbackBehaviour");
                var fProp = so.FindProperty("callbackFunction");

                foreach (var attr in attrs)
                {
                    int idx = bProp.arraySize;
                    bProp.arraySize = idx + 1;
                    fProp.arraySize = idx + 1;
                    bProp.GetArrayElementAtIndex(idx).objectReferenceValue = target;
                    fProp.GetArrayElementAtIndex(idx).stringValue = attr.FunctionName;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                next.Add(data);
            }

            _registrations[target] = next;
        }

        public static void RegisterAll()
        {
            _registrations.Clear();

            // null/破棄済みエントリと、auto-managed コンポーネントの既存エントリをすべて除去
            foreach (var data in UnityEngine.Object.FindObjectsByType<FDMiData>(FindObjectsSortMode.None))
                PurgeAutoManagedAndNullEntries(data);

            // 全件再登録（_registrations が空なので Execute の cleanup は空振りして登録のみ実行）
            var useCase = new RegisterCallbacksUseCase();
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                useCase.Execute(mb);
        }

        static void PurgeAutoManagedAndNullEntries(FDMiData data)
        {
            var so    = new SerializedObject(data);
            so.Update();
            var bProp = so.FindProperty("callbackBehaviour");
            var fProp = so.FindProperty("callbackFunction");
            bool changed = false;

            for (int i = bProp.arraySize - 1; i >= 0; i--)
            {
                var obj = bProp.GetArrayElementAtIndex(i).objectReferenceValue;
                // null/破棄済み、または [FDMiRegisterCallback] を持つクラスのコンポーネント。
                // [FDMiRegisterCallback] を持つクラスのエントリは RegisterAll で全件再構築するため、
                // そのクラスの全登録（手動含む）を一括削除して再登録で上書きする設計。
                bool remove = obj == null
                    || (obj is MonoBehaviour mb && GetCachedEntries(mb.GetType()).Length > 0);
                if (!remove) continue;

                bProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
                bProp.DeleteArrayElementAtIndex(i);
                fProp.DeleteArrayElementAtIndex(i);
                changed = true;
            }

            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ClearCacheForTesting()
        {
            _fieldCache.Clear();
            _registrations.Clear();
        }

        static void RemoveEntriesForTarget(FDMiData data, MonoBehaviour target)
        {
            var so = new SerializedObject(data);
            so.Update();
            var bProp = so.FindProperty("callbackBehaviour");
            var fProp = so.FindProperty("callbackFunction");
            bool changed = false;

            for (int i = bProp.arraySize - 1; i >= 0; i--)
            {
                var elem = bProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue != (UnityEngine.Object)target) continue;
                elem.objectReferenceValue = null; // object ref は null にしてから削除
                bProp.DeleteArrayElementAtIndex(i);
                fProp.DeleteArrayElementAtIndex(i);
                changed = true;
            }

            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static (FieldInfo field, FDMiRegisterCallbackAttribute[] attrs)[]
            GetCachedEntries(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached)) return cached;

            var result = new List<(FieldInfo, FDMiRegisterCallbackAttribute[])>();
            foreach (var field in type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attrs = field.GetCustomAttributes<FDMiRegisterCallbackAttribute>().ToArray();
                if (attrs.Length > 0) result.Add((field, attrs));
            }

            var entries = result.ToArray();
            _fieldCache[type] = entries;
            return entries;
        }
    }
}
