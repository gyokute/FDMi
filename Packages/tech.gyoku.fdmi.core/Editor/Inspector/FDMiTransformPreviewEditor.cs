using FDMi.core.Editor.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    // Move / Rotate 共通のプレビュー UI。派生が対象の読み取りと数値表示だけを持つ。
    public abstract class FDMiTransformPreviewEditor : FDMiBehaviourEditor
    {
        protected FDMiTransformPreviewBase Preview;
        protected FDMiPreviewSession.Participant Participant;
        protected float Value;
        protected float RangeMin;
        protected float RangeMax;
        protected float StartValue;

        string _invalidReason;
        bool _rangeInitialized;

        protected abstract bool TryCreate(out FDMiTransformPreviewBase preview, out string reason);

        protected abstract void DrawReadout();

        protected override void OnEnable()
        {
            base.OnEnable();
            Preview = null;
            Participant = null;
            _rangeInitialized = false;
            Rebuild();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (!FDMiPreviewSession.IsOwner(this))
                Rebuild();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (Preview == null)
            {
                EditorGUILayout.HelpBox(_invalidReason, MessageType.Info);
                return;
            }

            bool previewing = FDMiPreviewSession.IsOwner(this);
            if (!previewing)
            {
                if (GUILayout.Button("Preview"))
                    StartPreview();
            }
            else
            {
                if (GUILayout.Button("Stop"))
                    StopPreview();
            }

            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(!previewing))
            {
                float value = EditorGUILayout.Slider("値", Value, RangeMin, RangeMax);
                value = EditorGUILayout.FloatField("値 (数値)", value);
                value = Mathf.Clamp(value, RangeMin, RangeMax);
                if (!Mathf.Approximately(value, Value))
                {
                    Value = value;
                    Preview.Value = value;
                    if (previewing && Participant != null)
                        Participant.SetValue(value);
                    SceneView.RepaintAll();
                }
            }

            RangeMin = EditorGUILayout.FloatField("範囲 Min", RangeMin);
            RangeMax = EditorGUILayout.FloatField("範囲 Max", RangeMax);
            if (RangeMax <= RangeMin)
                RangeMax = RangeMin + 0.01f;
            if (GUILayout.Button("カーブから再取得"))
            {
                var range = FDMiTransformPreview.CurveRange(Preview.Curve);
                RangeMin = range.min;
                RangeMax = range.max;
            }
            if (EditorGUI.EndChangeCheck())
            {
                Preview.RangeMin = RangeMin;
                Preview.RangeMax = RangeMax;
                SceneView.RepaintAll();
            }

            DrawReadout();
        }

        protected void StartPreview()
        {
            if (Preview == null)
                return;

            FDMiPreviewSession.Begin(this);
            Participant = FDMiPreviewSession.Track(Preview.Target, Preview.SetValue, Preview.Draw);
            if (Participant == null)
            {
                FDMiPreviewSession.Stop();
                return;
            }

            Value = Preview.BaseValue;
            if (Value < RangeMin)
                RangeMin = Value;
            if (Value > RangeMax)
                RangeMax = Value;
            Preview.RangeMin = RangeMin;
            Preview.RangeMax = RangeMax;
            StartValue = Value;
            Participant.SetValue(Value);
        }

        protected void StopPreview()
        {
            FDMiPreviewSession.StopIfOwner(this);
            Participant = null;
            if (Preview != null)
                Value = Preview.BaseValue;
        }

        protected void DrawScene()
        {
            if (Preview == null)
                return;
            if (FDMiPreviewSession.Contains(Preview.Target))
                return;
            Preview.DrawAt(Value);
        }

        void Rebuild()
        {
            if (
                PrefabStageUtility.GetCurrentPrefabStage() != null
                || EditorApplication.isPlayingOrWillChangePlaymode
            )
            {
                Preview = null;
                _invalidReason = "プレハブ編集モード / Play モードではプレビューできません";
                return;
            }

            if (!TryCreate(out FDMiTransformPreviewBase preview, out string reason))
            {
                Preview = null;
                _invalidReason = reason;
                return;
            }

            Preview = preview;
            _invalidReason = null;

            if (!_rangeInitialized)
            {
                RangeMin = Preview.RangeMin;
                RangeMax = Preview.RangeMax;
                _rangeInitialized = true;
            }

            Preview.RangeMin = RangeMin;
            Preview.RangeMax = RangeMax;
            Value = Preview.BaseValue;
        }
    }
}
