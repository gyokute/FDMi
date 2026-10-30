using FDMi.core;
using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiHandYokeInput))]
    public class FDMiHandYokeInputEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiHandYokeInput yoke = (FDMiHandYokeInput)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Range Preview", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Chain Preview"))
                FDMiChainPreviewWindow.Open((FDMiHandInputElement)target);

            if (yoke.GetComponentInParent<FDMiHandInputGroup>() == null)
            {
                EditorGUILayout.HelpBox("Group が見つかりません", MessageType.Info);
                return;
            }

            DrawChannel("Pitch", yoke.pitch, yoke.pitchType, yoke.pitchMultiplier);
            DrawChannel("Roll", yoke.roll, yoke.rollType, yoke.rollMultiplier);
            DrawChannel("Yaw", yoke.yaw, yoke.yawType, yoke.yawMultiplier);
        }

        void OnSceneGUI()
        {
            FDMiHandYokeInput yoke = (FDMiHandYokeInput)target;
            FDMiHandInputGroup group = yoke.GetComponentInParent<FDMiHandInputGroup>();
            if (group == null)
                return;

            Vector3 origin = yoke.transform.position;
            DrawChannel(
                origin,
                group,
                yoke.pitch,
                yoke.pitchType,
                yoke.pitchMultiplier,
                yoke.pitchAxis,
                "P",
                new Color(0.3f, 0.9f, 0.4f)
            );
            DrawChannel(
                origin,
                group,
                yoke.roll,
                yoke.rollType,
                yoke.rollMultiplier,
                yoke.rollAxis,
                "R",
                new Color(1f, 0.6f, 0.2f)
            );
            DrawChannel(
                origin,
                group,
                yoke.yaw,
                yoke.yawType,
                yoke.yawMultiplier,
                yoke.yawAxis,
                "Y",
                new Color(0.9f, 0.4f, 1f)
            );
        }

        static void DrawChannel(string name, FDMiData data, FDMiHandGestureType type, float multiplier)
        {
            if (!(data is FDMiFloat))
            {
                EditorGUILayout.LabelField(name, "未解決 / 非 Float");
                return;
            }
            if (Mathf.Approximately(multiplier, 0f))
            {
                EditorGUILayout.LabelField(name, "倍率0");
                return;
            }

            string unit = type == FDMiHandGestureType.Pull ? "m" : "°";
            EditorGUILayout.LabelField(
                name,
                "+1 まで " + Format(1f / multiplier, unit)
                    + " / -1 まで " + Format(-1f / multiplier, unit)
                    + " / 離すと 0"
            );
        }

        static void DrawChannel(
            Vector3 origin,
            FDMiHandInputGroup group,
            FDMiData data,
            FDMiHandGestureType type,
            float multiplier,
            Vector3 axis,
            string prefix,
            Color color
        )
        {
            if (!(data is FDMiFloat))
                return;
            if (Mathf.Approximately(multiplier, 0f) || axis.sqrMagnitude < 1e-8f)
                return;

            Vector3 worldAxis = group.transform.TransformDirection(axis);
            bool pull = type == FDMiHandGestureType.Pull;
            string unit = pull ? "m" : "°";
            float toPlus = 1f / multiplier;
            float toMinus = -1f / multiplier;

            FDMiHandPreviewDraw.DrawTravel(
                origin,
                worldAxis,
                pull,
                toPlus,
                color,
                prefix + " +1 " + Format(toPlus, unit)
            );
            FDMiHandPreviewDraw.DrawTravel(
                origin,
                worldAxis,
                pull,
                toMinus,
                color,
                prefix + " -1 " + Format(toMinus, unit)
            );
        }

        static string Format(float travel, string unit)
        {
            return travel.ToString("+0.###;-0.###;0") + " " + unit;
        }
    }
}
