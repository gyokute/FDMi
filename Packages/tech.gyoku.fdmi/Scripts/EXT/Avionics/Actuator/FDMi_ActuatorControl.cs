
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ActuatorControl : FDMi_Attributes
    {
        public FDMi_ActuatorControl ActuatorControl;
        public FDMi_ControlFilter[] filter;
        public float val;
        private int i;
        public void LateUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.hasPilot]) return;
            val = ActuatorControl == null ? 0f : ActuatorControl.val;

            for (i = 0; i < filter.Length; i++)
                val = filter[i].filter(val);
        }
    }
}
