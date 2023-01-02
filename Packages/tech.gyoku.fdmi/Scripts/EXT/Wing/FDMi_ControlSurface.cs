
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ControlSurface : FDMi_Attributes
    {
        // public FDMi_SyncObject syncObject;
        public FDMi_ActuatorControl actuatorControl;
        // public float mainInputMul, delay = 0.1f, min, max;
        public float[] surfaceMul;
        // public float deg;

        // public float targetDeg;
        public float[] alpha;
        public float[] cl;
        public float[] cd;
        public float[] cm;
        public float[] area;

        public AnimationCurve Alpha_Deg;
        public AnimationCurve Cl_Deg;
        public AnimationCurve Cd_Deg;
        public AnimationCurve Cm_Deg;
        public AnimationCurve Area_Deg;
        private float surfaceDeg;

        private void LateUpdate()
        {
            if (!sharedBool[0]) return;
            // targetDeg = 0f;
            // if (syncObject != null) targetDeg += syncObject.val;
            // if (actuatorControl != null) targetDeg += actuatorControl.val;
            // targetDeg *= mainInputMul;
            // targetDeg = Mathf.Clamp(targetDeg, min, max);
            // deg = Mathf.MoveTowards(deg, targetDeg, Time.deltaTime / delay);
        }

        public void FixedUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot]) return;
            for (int i = 0; i < surfaceMul.Length; i++)
            {
                surfaceDeg = actuatorControl.val * surfaceMul[i];
                alpha[i] = Alpha_Deg.Evaluate(surfaceDeg);
                cl[i] = Cl_Deg.Evaluate(surfaceDeg);
                cd[i] = Cd_Deg.Evaluate(surfaceDeg);
                cm[i] = Cm_Deg.Evaluate(surfaceDeg);
                area[i] = Area_Deg.Evaluate(surfaceDeg);
            }
        }
    }
}