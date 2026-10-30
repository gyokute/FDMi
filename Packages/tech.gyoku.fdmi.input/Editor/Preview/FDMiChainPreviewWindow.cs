using System;
using System.Collections.Generic;
using FDMi.core;
using FDMi.core.Editor.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FDMi.input.Editor
{
    // 入力要素 → 出力データ → Move / Rotate の連鎖を確認する。
    // データは書かず、セッションの上書き値として Transform に適用する。
    public class FDMiChainPreviewWindow : EditorWindow
    {
        class Entry
        {
            public string Label;
            public FDMiPreviewSession.Participant Participant;
            public Func<string> Describe;
        }

        class Channel
        {
            public string Name;
            public FDMiData Data;
            public string Unit;
            public float BaseValue;
            public bool Valid;
            public string InvalidReason;
            public float HandValue;
            public float MinHand;
            public float MaxHand;
            public bool PreventWhileHold;
            public Func<float, float> Eval;
            public Func<float, float> ReleaseEval;
            public readonly List<Entry> Entries = new List<Entry>();
            public readonly List<string> Warnings = new List<string>();
        }

        readonly List<Channel> _channels = new List<Channel>();
        FDMiHandInputElement _input;
        bool _previewing;
        string _message;

        [MenuItem("FDMi/Preview/Chain Preview")]
        static void OpenWindow()
        {
            GetWindow<FDMiChainPreviewWindow>("FDMi Chain Preview");
        }

        public static void Open(FDMiHandInputElement input)
        {
            var window = GetWindow<FDMiChainPreviewWindow>("FDMi Chain Preview");
            window.SetInput(input);
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("FDMi Chain Preview");
        }

        void OnDisable()
        {
            StopPreview();
        }

        void Update()
        {
            if (_previewing && !FDMiPreviewSession.IsOwner(this))
            {
                _previewing = false;
                _channels.Clear();
                _message = "別のプレビューが開始されました";
                Repaint();
            }
        }

        void OnSelectionChange()
        {
            FDMiHandInputElement element = FindSelection();
            if (element != null && element != _input)
                SetInput(element);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("FDMi Chain Preview", EditorStyles.boldLabel);

            FDMiHandInputElement newInput = (FDMiHandInputElement)EditorGUILayout.ObjectField(
                "Input",
                _input,
                typeof(FDMiHandInputElement),
                true
            );
            if (newInput != _input)
                SetInput(newInput);

            if (GUILayout.Button("Use Selection"))
                SetInput(FindSelection());

            if (_input == null)
            {
                EditorGUILayout.HelpBox("入力要素を選択してください", MessageType.Info);
                return;
            }
            if (!(_input is FDMiHandLeverInput) && !(_input is FDMiHandYokeInput))
            {
                EditorGUILayout.HelpBox("Lever / YokeInput のみ対応", MessageType.Info);
                return;
            }

            if (
                PrefabStageUtility.GetCurrentPrefabStage() != null
                || EditorApplication.isPlayingOrWillChangePlaymode
            )
            {
                EditorGUILayout.HelpBox(
                    "プレハブ編集モード / Play モードではプレビューできません",
                    MessageType.Info
                );
                return;
            }

            if (!_previewing)
            {
                if (GUILayout.Button("Preview"))
                    StartPreview();
            }
            else
            {
                if (GUILayout.Button("Stop"))
                    StopPreview();
                if (GUILayout.Button("Release"))
                    ReleasePreview();
            }

            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, MessageType.Info);

            if (!_previewing)
                return;

            for (int i = 0; i < _channels.Count; i++)
                DrawChannel(_channels[i]);
        }

        void DrawChannel(Channel channel)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(channel.Name, EditorStyles.boldLabel);

            if (!channel.Valid)
            {
                EditorGUILayout.HelpBox(channel.InvalidReason, MessageType.Info);
                return;
            }

            float output = channel.Eval(channel.HandValue);
            EditorGUILayout.LabelField(
                channel.PreventWhileHold ? "Release 時" : "出力",
                output.ToString("F3")
            );
            EditorGUILayout.LabelField(
                "手の移動量",
                channel.HandValue.ToString("F3") + " " + channel.Unit
            );

            EditorGUI.BeginChangeCheck();
            float hand = EditorGUILayout.Slider(channel.HandValue, channel.MinHand, channel.MaxHand);
            if (EditorGUI.EndChangeCheck())
            {
                channel.HandValue = hand;
                if (!channel.PreventWhileHold)
                    ApplyChannel(channel, channel.Eval(hand));
                Repaint();
                SceneView.RepaintAll();
            }

            if (channel.PreventWhileHold)
                EditorGUILayout.HelpBox("保持中は反映しません。Release で反映します。", MessageType.None);

            if (channel.Entries.Count == 0)
                EditorGUILayout.LabelField("作用する Move / Rotate はありません");

            for (int i = 0; i < channel.Entries.Count; i++)
            {
                Entry entry = channel.Entries[i];
                EditorGUILayout.LabelField(entry.Label);
                EditorGUILayout.LabelField("  " + entry.Describe());
            }

            for (int i = 0; i < channel.Warnings.Count; i++)
                EditorGUILayout.HelpBox(channel.Warnings[i], MessageType.Warning);
        }

        void StartPreview()
        {
            if (
                PrefabStageUtility.GetCurrentPrefabStage() != null
                || EditorApplication.isPlayingOrWillChangePlaymode
            )
            {
                _message = "プレハブ編集モード / Play モードではプレビューできません";
                return;
            }

            StopPreview();
            BuildChannels();

            if (_channels.Count == 0)
            {
                _message = "対応する入力がありません";
                return;
            }

            FDMiPreviewSession.Begin(this);
            bool any = false;
            for (int i = 0; i < _channels.Count; i++)
            {
                Channel channel = _channels[i];
                if (!channel.Valid)
                    continue;

                BuildEntries(channel);
                if (channel.Entries.Count == 0)
                    continue;

                any = true;
                channel.HandValue = 0f;
                ApplyChannel(channel, channel.Eval(0f));
            }

            if (!any)
            {
                FDMiPreviewSession.Stop();

                string reason = null;
                for (int i = 0; i < _channels.Count; i++)
                {
                    if (!_channels[i].Valid)
                    {
                        reason = _channels[i].InvalidReason;
                        break;
                    }
                }

                _channels.Clear();
                _message = reason ?? "このデータを参照する Move / Rotate がありません";
                return;
            }

            _previewing = true;
            _message = null;
            SceneView.RepaintAll();
        }

        void StopPreview()
        {
            FDMiPreviewSession.StopIfOwner(this);
            _previewing = false;
            _channels.Clear();
            Repaint();
        }

        void ReleasePreview()
        {
            if (!_previewing)
                return;

            for (int i = 0; i < _channels.Count; i++)
            {
                Channel channel = _channels[i];
                if (!channel.Valid)
                    continue;
                ApplyChannel(channel, channel.ReleaseEval(channel.HandValue));
            }
            SceneView.RepaintAll();
        }

        void ApplyChannel(Channel channel, float value)
        {
            for (int i = 0; i < channel.Entries.Count; i++)
                channel.Entries[i].Participant.SetValue(value);
        }

        void BuildEntries(Channel channel)
        {
            channel.Entries.Clear();
            channel.Warnings.Clear();

            var moves = FDMiChainFinder.FindMoves(channel.Data);
            for (int i = 0; i < moves.Count; i++)
            {
                FDMiTransformMove move = moves[i];
                if (!FDMiTransformPreview.TryCreateMove(move, out FDMiMovePreview preview, out string reason))
                {
                    channel.Warnings.Add(move.name + ": " + reason);
                    continue;
                }

                FDMiPreviewSession.Participant participant = FDMiTransformPreview.Track(preview);
                if (participant == null)
                    continue;

                channel.Entries.Add(
                    new Entry
                    {
                        Label = move.name + " → " + preview.Target.name,
                        Participant = participant,
                        Describe = () => preview.Describe(channel.BaseValue),
                    }
                );
            }

            var rotates = FDMiChainFinder.FindRotates(channel.Data);
            for (int i = 0; i < rotates.Count; i++)
            {
                FDMiTransformRotate rotate = rotates[i];
                if (
                    !FDMiTransformPreview.TryCreateRotate(
                        rotate,
                        out FDMiRotatePreview preview,
                        out string reason
                    )
                )
                {
                    channel.Warnings.Add(rotate.name + ": " + reason);
                    continue;
                }

                FDMiPreviewSession.Participant participant = FDMiTransformPreview.Track(preview);
                if (participant == null)
                    continue;

                channel.Entries.Add(
                    new Entry
                    {
                        Label = rotate.name + " → " + preview.Target.name,
                        Participant = participant,
                        Describe = () => preview.Describe(channel.BaseValue),
                    }
                );
            }
        }

        void BuildChannels()
        {
            _channels.Clear();

            if (_input is FDMiHandLeverInput lever)
            {
                _channels.Add(BuildLever(lever));
                return;
            }

            if (_input is FDMiHandYokeInput yoke)
            {
                _channels.Add(
                    BuildYokeChannel(
                        "Pitch",
                        yoke.pitch,
                        yoke.pitchType,
                        yoke.pitchMultiplier
                    )
                );
                _channels.Add(
                    BuildYokeChannel(
                        "Roll",
                        yoke.roll,
                        yoke.rollType,
                        yoke.rollMultiplier
                    )
                );
                _channels.Add(
                    BuildYokeChannel(
                        "Yaw",
                        yoke.yaw,
                        yoke.yawType,
                        yoke.yawMultiplier
                    )
                );
            }
        }

        static Channel BuildLever(FDMiHandLeverInput lever)
        {
            var channel = new Channel
            {
                Name = "Lever",
                Data = lever.output,
                Unit = lever is FDMiHandPullInput ? "m" : "°",
            };

            if (!(lever.output is FDMiFloat output))
            {
                channel.InvalidReason = "output が FDMiFloat ではありません";
                return channel;
            }

            var serializedObject = new SerializedObject(lever);
            float multiplier = serializedObject.FindProperty("multiplier").floatValue;
            float min = serializedObject.FindProperty("min").floatValue;
            float max = serializedObject.FindProperty("max").floatValue;
            float step = serializedObject.FindProperty("outputStep").floatValue;
            bool doRepeat = serializedObject.FindProperty("doRepeat").boolValue;
            bool prevent = serializedObject.FindProperty("preventSetWhileHold").boolValue;
            float[] detents = ReadFloatArray(serializedObject, "detents");

            if (Mathf.Approximately(multiplier, 0f))
            {
                channel.InvalidReason = "倍率0";
                return channel;
            }

            float baseValue = output.GetFloat();
            channel.Valid = true;
            channel.BaseValue = baseValue;
            channel.PreventWhileHold = prevent;
            channel.Eval = d =>
                FDMiHandPreviewMath.LeverHeld(baseValue, d, multiplier, min, max, step, doRepeat);
            channel.ReleaseEval = d =>
                FDMiHandPreviewMath.LeverRelease(
                    baseValue,
                    d,
                    multiplier,
                    min,
                    max,
                    step,
                    doRepeat,
                    detents
                );

            if (doRepeat)
            {
                float period = Mathf.Abs((max - min) / multiplier);
                channel.MinHand = -period;
                channel.MaxHand = period;
            }
            else
            {
                float toMax = FDMiHandPreviewMath.TravelToBound(baseValue, max, multiplier);
                float toMin = FDMiHandPreviewMath.TravelToBound(baseValue, min, multiplier);
                channel.MinHand = Mathf.Min(toMin, toMax);
                channel.MaxHand = Mathf.Max(toMin, toMax);
            }

            ExpandRange(channel);
            return channel;
        }

        static Channel BuildYokeChannel(
            string name,
            FDMiData data,
            FDMiHandGestureType type,
            float multiplier
        )
        {
            var channel = new Channel
            {
                Name = name,
                Data = data,
                Unit = type == FDMiHandGestureType.Pull ? "m" : "°",
            };

            if (!(data is FDMiFloat))
            {
                channel.InvalidReason = "未解決 / 非 Float";
                return channel;
            }
            if (Mathf.Approximately(multiplier, 0f))
            {
                channel.InvalidReason = "倍率0";
                return channel;
            }

            channel.Valid = true;
            channel.BaseValue = 0f;
            channel.Eval = d => FDMiHandPreviewMath.YokeOutput(d, multiplier);
            channel.ReleaseEval = d => 0f;

            if (type == FDMiHandGestureType.Pull)
            {
                channel.MinHand = Mathf.Min(-1f / multiplier, 1f / multiplier);
                channel.MaxHand = Mathf.Max(-1f / multiplier, 1f / multiplier);
                ExpandRange(channel);
            }
            else
            {
                channel.MinHand = -180f;
                channel.MaxHand = 180f;
            }

            return channel;
        }

        static void ExpandRange(Channel channel)
        {
            if (channel.MaxHand <= channel.MinHand)
            {
                channel.MinHand -= 1f;
                channel.MaxHand += 1f;
            }

            float margin = Mathf.Max((channel.MaxHand - channel.MinHand) * 0.1f, 1e-3f);
            channel.MinHand -= margin;
            channel.MaxHand += margin;
        }

        static float[] ReadFloatArray(SerializedObject serializedObject, string name)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property == null || !property.isArray)
                return new float[0];

            var result = new float[property.arraySize];
            for (int i = 0; i < property.arraySize; i++)
                result[i] = property.GetArrayElementAtIndex(i).floatValue;
            return result;
        }

        static FDMiHandInputElement FindSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return null;
            return selected.GetComponentInParent<FDMiHandInputElement>(true);
        }

        void SetInput(FDMiHandInputElement input)
        {
            if (_input == input)
            {
                Repaint();
                return;
            }

            StopPreview();
            _input = input;
            _channels.Clear();
            _message = null;
            Repaint();
        }
    }
}
