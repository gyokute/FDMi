using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Preview
{
    public abstract class FDMiTransformPreviewBase
    {
        public Transform Target;
        public AnimationCurve Curve;
        public float RangeMin;
        public float RangeMax;
        public float Value;
        public float BaseValue;

        public abstract void SetValue(float value);
        public abstract void Draw();
        public abstract string Describe(float baseValue);

        public void DrawAt(float value)
        {
            Value = value;
            Draw();
        }
    }

    public sealed class FDMiMovePreview : FDMiTransformPreviewBase
    {
        public Vector3 From;
        public Vector3 To;

        public override void SetValue(float value)
        {
            Value = value;
            Target.localPosition = LocalAt(value);
        }

        public Vector3 LocalAt(float value)
        {
            return FDMiTransformMath.EvaluatePosition(From, To, Curve, value);
        }

        public Vector3 WorldAt(float value)
        {
            Vector3 local = LocalAt(value);
            return Target.parent != null ? Target.parent.TransformPoint(local) : local;
        }

        public float Distance(float a, float b)
        {
            return Vector3.Distance(WorldAt(a), WorldAt(b));
        }

        public override string Describe(float baseValue)
        {
            return Distance(baseValue, Value).ToString("F3") + " m";
        }

        public override void Draw()
        {
            Vector3 start = WorldAt(RangeMin);
            Vector3 end = WorldAt(RangeMax);
            Vector3 current = WorldAt(Value);
            float size = Mathf.Max(HandleUtility.GetHandleSize(current) * 0.04f, 0.01f);

            Handles.color = new Color(0.4f, 0.8f, 1f);
            Handles.DrawLine(start, end);

            Handles.color = new Color(0.7f, 0.7f, 0.7f);
            Handles.DrawWireCube(start, Vector3.one * size);
            Handles.DrawWireCube(end, Vector3.one * size);

            Handles.color = new Color(1f, 0.6f, 0.2f);
            Handles.DrawLine(start, current);
            Handles.DrawWireCube(current, Vector3.one * size);
            Handles.Label(
                current + Vector3.up * size * 2f,
                Value.ToString("F3") + " / " + Vector3.Distance(start, end).ToString("F3") + " m"
            );
        }
    }

    public sealed class FDMiRotatePreview : FDMiTransformPreviewBase
    {
        public Vector3 Axis;

        public Vector3 LocalEulerAt(float value)
        {
            return Axis * Curve.Evaluate(value);
        }

        public Quaternion LocalRotationAt(float value)
        {
            return Quaternion.Euler(LocalEulerAt(value));
        }

        public Quaternion WorldRotationAt(float value)
        {
            Quaternion local = LocalRotationAt(value);
            return Target.parent != null ? Target.parent.rotation * local : local;
        }

        public float Angle(float a, float b)
        {
            return Quaternion.Angle(WorldRotationAt(a), WorldRotationAt(b));
        }

        public override string Describe(float baseValue)
        {
            return Angle(baseValue, Value).ToString("F1") + "°";
        }

        public override void SetValue(float value)
        {
            Value = value;
            Target.localEulerAngles = LocalEulerAt(value);
        }

        public override void Draw()
        {
            Vector3 position = Target.position;
            Vector3 axisWorld = Target.parent != null
                ? Target.parent.TransformDirection(Axis.normalized)
                : Axis.normalized;
            float size = Mathf.Max(HandleUtility.GetHandleSize(position) * 0.1f, 0.02f);
            float radius = Mathf.Max(HandleUtility.GetHandleSize(position) * 0.2f, 0.05f);

            if (axisWorld.sqrMagnitude > 0f)
            {
                Handles.color = new Color(1f, 0.9f, 0.3f);
                Handles.DrawLine(position, position + axisWorld * size * 3f);
            }

            if (IsSingleAxis(Axis, out float component) && axisWorld.sqrMagnitude > 0f)
            {
                float start = component * Curve.Evaluate(RangeMin);
                float end = component * Curve.Evaluate(RangeMax);
                Vector3 normal = axisWorld.normalized;
                Vector3 from = Perpendicular(normal);
                Handles.color = new Color(0.4f, 0.8f, 1f);
                Handles.DrawWireArc(
                    position,
                    normal,
                    Quaternion.AngleAxis(start, normal) * from,
                    end - start,
                    radius
                );
            }
            else
            {
                DrawAxes(WorldRotationAt(RangeMin), position, new Color(0.7f, 0.7f, 0.7f), size);
                DrawAxes(WorldRotationAt(RangeMax), position, new Color(0.7f, 0.7f, 0.7f), size);
                DrawAxes(WorldRotationAt(Value), position, new Color(1f, 0.6f, 0.2f), size);
            }

            Handles.Label(
                position + Vector3.up * size * 2f,
                Value.ToString("F3") + " / " + Angle(RangeMin, RangeMax).ToString("F1") + "°"
            );
        }

        static bool IsSingleAxis(Vector3 axis, out float component)
        {
            component = 0f;
            int count = 0;
            if (Mathf.Abs(axis.x) > 1e-4f)
            {
                count++;
                component = axis.x;
            }
            if (Mathf.Abs(axis.y) > 1e-4f)
            {
                count++;
                component = axis.y;
            }
            if (Mathf.Abs(axis.z) > 1e-4f)
            {
                count++;
                component = axis.z;
            }
            return count == 1;
        }

        static Vector3 Perpendicular(Vector3 normal)
        {
            Vector3 candidate = Vector3.ProjectOnPlane(Vector3.right, normal);
            if (candidate.sqrMagnitude < 1e-4f)
                candidate = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (candidate.sqrMagnitude < 1e-4f)
                candidate = Vector3.ProjectOnPlane(Vector3.forward, normal);
            return candidate.normalized;
        }

        static void DrawAxes(Quaternion rotation, Vector3 position, Color color, float size)
        {
            Handles.color = color;
            Handles.DrawLine(position, position + rotation * Vector3.right * size);
            Handles.DrawLine(position, position + rotation * Vector3.up * size);
            Handles.DrawLine(position, position + rotation * Vector3.forward * size);
        }
    }

    public static class FDMiTransformPreview
    {
        public static bool TryCreateMove(
            FDMiTransformMove move,
            out FDMiMovePreview preview,
            out string reason
        )
        {
            preview = null;
            if (move == null)
            {
                reason = "Move がありません";
                return false;
            }
            if (move.Value == null)
            {
                reason = "Value が未解決です";
                return false;
            }
            if (move.target == null || move.target.data == null || move.target.data.Length == 0 || move.target.data[0] == null)
            {
                reason = "target が未解決です";
                return false;
            }
            if (move.outputCurve == null)
            {
                reason = "outputCurve がありません";
                return false;
            }

            var serializedObject = new SerializedObject(move);
            var position = serializedObject.FindProperty("position");
            if (position == null || position.arraySize < 2)
            {
                reason = "position が2点未満です";
                return false;
            }

            var range = CurveRange(move.outputCurve);
            preview = new FDMiMovePreview
            {
                Target = move.target.data[0],
                From = position.GetArrayElementAtIndex(0).vector3Value,
                To = position.GetArrayElementAtIndex(1).vector3Value,
                Curve = move.outputCurve,
                RangeMin = range.min,
                RangeMax = range.max,
                Value = move.Value.GetFloat(),
                BaseValue = move.Value.GetFloat(),
            };
            reason = null;
            return true;
        }

        public static bool TryCreateRotate(
            FDMiTransformRotate rotate,
            out FDMiRotatePreview preview,
            out string reason
        )
        {
            preview = null;
            if (rotate == null)
            {
                reason = "Rotate がありません";
                return false;
            }
            if (!(rotate.Value is FDMiFloat data))
            {
                reason = "Value が FDMiFloat ではありません";
                return false;
            }
            if (rotate.target == null || rotate.target.data == null || rotate.target.data.Length == 0 || rotate.target.data[0] == null)
            {
                reason = "target が未解決です";
                return false;
            }
            if (rotate.outputCurve == null)
            {
                reason = "outputCurve がありません";
                return false;
            }

            var serializedObject = new SerializedObject(rotate);
            var axis = serializedObject.FindProperty("axis");
            if (axis == null)
            {
                reason = "axis がありません";
                return false;
            }

            var range = CurveRange(rotate.outputCurve);
            preview = new FDMiRotatePreview
            {
                Target = rotate.target.data[0],
                Axis = axis.vector3Value,
                Curve = rotate.outputCurve,
                RangeMin = range.min,
                RangeMax = range.max,
                Value = data.GetFloat(),
                BaseValue = data.GetFloat(),
            };
            reason = null;
            return true;
        }

        public static FDMiPreviewSession.Participant Track(FDMiMovePreview preview)
        {
            return FDMiPreviewSession.Track(preview.Target, preview.SetValue, preview.Draw);
        }

        public static FDMiPreviewSession.Participant Track(FDMiRotatePreview preview)
        {
            return FDMiPreviewSession.Track(preview.Target, preview.SetValue, preview.Draw);
        }

        public static (float min, float max) CurveRange(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return (0f, 1f);

            float min = curve.keys[0].time;
            float max = min;
            for (int i = 1; i < curve.length; i++)
            {
                float time = curve.keys[i].time;
                if (time < min)
                    min = time;
                if (time > max)
                    max = time;
            }

            if (max <= min)
            {
                min -= 0.5f;
                max += 0.5f;
            }
            return (min, max);
        }
    }
}
