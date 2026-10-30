using FDMi.core;
using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiHandLeverInput), true)]
    public class FDMiHandLeverInputEditor : FDMiBehaviourEditor
    {
        struct Settings
        {
            public float multiplier;
            public float min;
            public float max;
            public Vector3 axis;
            public bool doRepeat;
            public bool useShiftMultiplier;
            public float shiftedMultiplier;
            public string invalid;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiHandLeverInput lever = (FDMiHandLeverInput)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Range Preview", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Chain Preview"))
                FDMiChainPreviewWindow.Open((FDMiHandInputElement)target);

            if (!TryRead(lever, out Settings settings, out FDMiFloat output, out FDMiHandInputGroup group))
            {
                EditorGUILayout.HelpBox(settings.invalid, MessageType.Info);
                return;
            }

            bool pull = lever is FDMiHandPullInput;
            string unit = pull ? "m" : "°";
            float current = output.GetFloat();
            float toMax = FDMiHandPreviewMath.TravelToBound(current, settings.max, settings.multiplier);
            float toMin = FDMiHandPreviewMath.TravelToBound(current, settings.min, settings.multiplier);

            EditorGUILayout.LabelField("Max まで", Format(toMax, unit, settings));
            EditorGUILayout.LabelField("Min まで", Format(toMin, unit, settings));

            if (settings.doRepeat)
                EditorGUILayout.HelpBox("周回あり（min/max 同一視）", MessageType.None);
        }

        void OnSceneGUI()
        {
            FDMiHandLeverInput lever = (FDMiHandLeverInput)target;
            if (!TryRead(lever, out Settings settings, out FDMiFloat output, out FDMiHandInputGroup group))
                return;

            bool pull = lever is FDMiHandPullInput;
            string unit = pull ? "m" : "°";
            float current = output.GetFloat();
            float toMax = FDMiHandPreviewMath.TravelToBound(current, settings.max, settings.multiplier);
            float toMin = FDMiHandPreviewMath.TravelToBound(current, settings.min, settings.multiplier);

            Vector3 origin = lever.transform.position;
            Vector3 axis = group.transform.TransformDirection(settings.axis);
            FDMiHandPreviewDraw.DrawTravel(
                origin,
                axis,
                pull,
                toMax,
                new Color(0.3f, 0.9f, 1f),
                "Max " + Format(toMax, unit, settings)
            );
            FDMiHandPreviewDraw.DrawTravel(
                origin,
                axis,
                pull,
                toMin,
                new Color(0.4f, 0.6f, 0.8f),
                "Min " + Format(toMin, unit, settings)
            );
        }

        static bool TryRead(
            FDMiHandLeverInput lever,
            out Settings settings,
            out FDMiFloat output,
            out FDMiHandInputGroup group
        )
        {
            settings = new Settings();
            output = lever.output as FDMiFloat;
            group = lever.GetComponentInParent<FDMiHandInputGroup>();

            if (output == null)
            {
                settings.invalid = "output が FDMiFloat ではありません";
                return false;
            }
            if (group == null)
            {
                settings.invalid = "Group が見つかりません";
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(lever);
            settings.multiplier = serializedObject.FindProperty("multiplier").floatValue;
            settings.min = serializedObject.FindProperty("min").floatValue;
            settings.max = serializedObject.FindProperty("max").floatValue;
            settings.axis = serializedObject.FindProperty("inputAxis").vector3Value;
            settings.doRepeat = serializedObject.FindProperty("doRepeat").boolValue;
            settings.useShiftMultiplier = serializedObject.FindProperty("useShiftMultiplier").boolValue;
            settings.shiftedMultiplier = serializedObject.FindProperty("shiftedMultiplier").floatValue;

            if (Mathf.Approximately(settings.multiplier, 0f))
            {
                settings.invalid = "倍率0";
                return false;
            }
            if (settings.axis.sqrMagnitude < 1e-8f)
            {
                settings.invalid = "軸0";
                return false;
            }

            settings.invalid = null;
            return true;
        }

        static string Format(float travel, string unit, Settings settings)
        {
            string text = travel.ToString("+0.###;-0.###;0") + " " + unit;
            if (!settings.useShiftMultiplier)
                return text;
            if (Mathf.Approximately(settings.shiftedMultiplier, 0f))
                return text + "（シフト 倍率0）";

            float shifted = travel * settings.multiplier / settings.shiftedMultiplier;
            return text + "（シフト " + shifted.ToString("+0.###;-0.###;0") + " " + unit + "）";
        }
    }
}
