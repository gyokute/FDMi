using UnityEngine;

namespace FDMi.input.Editor
{
    // Runtime の Lever / Yoke と同じ式を Editor 側で再現する。
    // Runtime は毎フレームの差分を積算するため、総移動量からの一括計算と等価。
    public static class FDMiHandPreviewMath
    {
        public static float RoundToStep(float input, float step)
        {
            if (step <= 0f)
                return input;
            return Mathf.Round(input / step) * step;
        }

        public static float LeverHeld(
            float current,
            float handTravel,
            float multiplier,
            float min,
            float max,
            float step,
            bool doRepeat
        )
        {
            float inner = current + handTravel * multiplier;
            inner = Wrap(inner, min, max, doRepeat);
            return RoundToStep(inner, step);
        }

        public static float LeverRelease(
            float current,
            float handTravel,
            float multiplier,
            float min,
            float max,
            float step,
            bool doRepeat,
            float[] detents
        )
        {
            float inner = Wrap(current + handTravel * multiplier, min, max, doRepeat);

            if (detents != null && detents.Length > 0)
            {
                float nearest = 0f;
                float distance = float.MaxValue;
                for (int i = 0; i < detents.Length; i++)
                {
                    float candidate = Mathf.Abs(detents[i] - inner);
                    if (candidate < distance)
                    {
                        distance = candidate;
                        nearest = detents[i];
                    }
                }

                if (distance < float.MaxValue)
                    return Mathf.Clamp(nearest, min, max);
            }

            return RoundToStep(inner, step);
        }

        public static float YokeOutput(float handInput, float multiplier)
        {
            return Mathf.Clamp(handInput * multiplier, -1f, 1f);
        }

        public static float TravelToBound(float current, float bound, float multiplier)
        {
            if (Mathf.Approximately(multiplier, 0f))
                return 0f;
            return (bound - current) / multiplier;
        }

        static float Wrap(float inner, float min, float max, bool doRepeat)
        {
            if (!doRepeat)
                return Mathf.Clamp(inner, min, max);

            float range = max - min;
            if (range <= 0f)
                return min;
            return min + Mathf.Repeat(inner - min, range);
        }
    }
}
