
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class DrumIndicator : FDMi_Indicator
    {
        public FDMi_SyncObject SRC;
        public Transform[] Drum;
        float[] baseRotation;
        public LeverAxis axis;
        public float mul = -36f;
        private Vector3 axisVec = Vector3.zero;

        void Start()
        {
            baseRotation = new float[Drum.Length];
            for (int i = 0; i < Drum.Length; i++)
                baseRotation[i] = Drum[i].localEulerAngles[(int)axis];
            axisVec[(int)axis] = 1f;
        }
        public override void whenChange()
        {
            if (baseRotation == null) return;
            float val = SRC.val;
            for (int i = 0; i < Drum.Length; i++)
            {
                Drum[i].localEulerAngles = (mul * Mathf.Floor(val % 10) + baseRotation[i]) * axisVec;
                val *= 0.1f;
            }
        }
    }
}
