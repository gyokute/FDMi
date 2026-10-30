using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Preview
{
    // プレビュー中の Transform 書き換えを1箇所で管理する。
    // グローバルに1セッションだけ: 新しい Begin は前のセッションを復元してから始める。
    public static class FDMiPreviewSession
    {
        public sealed class Participant
        {
            public Transform Transform;
            public Action<float> Apply;
            public Action Draw;
            public float Value;

            public void SetValue(float value)
            {
                Value = value;
                if (Apply != null)
                    Apply(value);
                SceneView.RepaintAll();
            }
        }

        class Snapshot
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public bool HadPositionOverride;
            public bool HadRotationOverride;
        }

        static readonly List<Participant> _participants = new List<Participant>();
        static readonly Dictionary<Transform, Snapshot> _snapshots = new Dictionary<Transform, Snapshot>();
        static UnityEngine.Object _owner;

        public static bool IsActive => _owner != null;
        public static UnityEngine.Object Owner => _owner;
        public static IReadOnlyList<Participant> Participants => _participants;

        public static void Begin(UnityEngine.Object owner)
        {
            Stop();
            _owner = owner;
            SceneView.duringSceneGui += OnSceneGui;
        }

        public static Participant Track(Transform transform, Action<float> apply, Action draw)
        {
            if (transform == null)
                return null;

            if (!_snapshots.ContainsKey(transform))
                _snapshots.Add(transform, Capture(transform));

            var participant = new Participant
            {
                Transform = transform,
                Apply = apply,
                Draw = draw,
            };
            _participants.Add(participant);
            return participant;
        }

        public static void StopIfOwner(UnityEngine.Object owner)
        {
            if (_owner == owner)
                Stop();
        }

        public static bool IsOwner(UnityEngine.Object owner) => _owner == owner;

        public static bool Contains(Transform transform)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                if (_participants[i].Transform == transform)
                    return true;
            }
            return false;
        }

        public static void Stop()
        {
            if (_owner == null && _participants.Count == 0 && _snapshots.Count == 0)
                return;

            SceneView.duringSceneGui -= OnSceneGui;
            RestoreAll();
            _participants.Clear();
            _snapshots.Clear();
            _owner = null;
            SceneView.RepaintAll();
        }

        static void OnSceneGui(SceneView sceneView)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                var participant = _participants[i];
                if (participant.Draw != null)
                    participant.Draw();
            }
        }

        static Snapshot Capture(Transform transform)
        {
            var snapshot = new Snapshot
            {
                LocalPosition = transform.localPosition,
                LocalRotation = transform.localRotation,
            };

            if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
            {
                snapshot.HadPositionOverride = HasModification(transform, "m_LocalPosition");
                snapshot.HadRotationOverride = HasModification(transform, "m_LocalRotation");
            }

            return snapshot;
        }

        static bool HasModification(Transform transform, string prefix)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject);
            if (root == null)
                return false;

            var modifications = PrefabUtility.GetPropertyModifications(root);
            if (modifications == null)
                return false;

            for (int i = 0; i < modifications.Length; i++)
            {
                var modification = modifications[i];
                if (modification.target != transform)
                    continue;
                var path = modification.propertyPath;
                if (path != null && path.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static void RestoreAll()
        {
            foreach (var pair in _snapshots)
            {
                var transform = pair.Key;
                if (transform == null)
                    continue;

                var snapshot = pair.Value;
                transform.localPosition = snapshot.LocalPosition;
                transform.localRotation = snapshot.LocalRotation;

                if (!PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                    continue;

                if (!snapshot.HadPositionOverride)
                    Revert(transform, "m_LocalPosition");
                if (!snapshot.HadRotationOverride)
                    Revert(transform, "m_LocalRotation");
            }
        }

        static void Revert(Transform transform, string propertyName)
        {
            var serializedObject = new SerializedObject(transform);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
                PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
        }
    }
}
