using System;
using System.Collections.Generic;
using System.Reflection;
using FDMi.core;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.core.Editor.Inspector
{
    // FDMiSeat の station / InSeatBehaviours を FDMiData と同じタイミング（選択時・Inspector 変更時）で自動解決する。
    // station は自身の GameObject からのみ取得する。OnStationEntered/Exited は station と同一 GameObject の
    // UdonBehaviour にしか送られず、FDMiSeat は Interact() のために station と同一 GameObject であることが前提。
    // 名前空間探索をすると同一 Namespace 内の別座席の station を誤って取得する。
    // InSeatBehaviours は FDMiData の相対解決と同じ規約（親を起点に FDMiNamespace 境界までの子孫、0 件なら前回値維持）で
    // 着席イベントを持つ UdonSharpBehaviour を収集して上書きする。
    [CustomEditor(typeof(FDMiSeat))]
    public class FDMiSeatEditor : FDMiBehaviourEditor
    {
        const string SeatEnterEvent = "FDMiOnSeatEnter";
        const string SeatExitEvent = "FDMiOnSeatExit";

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveSeatReferences();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                ResolveSeatReferences();
        }

        void ResolveSeatReferences()
        {
            var seat = (FDMiSeat)target;

            serializedObject.Update();
            var station = seat.GetComponent<VRCStation>();
            if (station)
                serializedObject.FindProperty(nameof(FDMiSeat.station)).objectReferenceValue = station;

            var root = seat.transform.parent != null ? seat.transform.parent : seat.transform;
            var inSeats = new List<UdonSharpBehaviour>();
            CollectSeatBehaviours(root, inSeats);
            if (inSeats.Count > 0)
            {
                var arrayProp = serializedObject.FindProperty(nameof(FDMiSeat.InSeatBehaviours));
                arrayProp.arraySize = inSeats.Count;
                for (int i = 0; i < inSeats.Count; i++)
                    arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = inSeats[i];
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CollectSeatBehaviours(Transform node, List<UdonSharpBehaviour> results)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                if (child.GetComponent<FDMiNamespace>() != null)
                    continue; // FDMiNamespace は名前空間の境界。その中には入らない
                foreach (var usb in child.GetComponents<UdonSharpBehaviour>())
                    if (HasSeatEvent(usb.GetType()))
                        results.Add(usb);
                CollectSeatBehaviours(child, results);
            }
        }

        static bool HasSeatEvent(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            return type.GetMethod(SeatEnterEvent, flags) != null
                || type.GetMethod(SeatExitEvent, flags) != null;
        }
    }
}
