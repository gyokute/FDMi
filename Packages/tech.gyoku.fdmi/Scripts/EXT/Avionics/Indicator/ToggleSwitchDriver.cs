
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    public class ToggleSwitchDriver : FDMi_Indicator
    {
        public FDMi_SyncObject Obj;
        public Transform SW;
        float baseRotation;
        public LeverAxis axis;
        public float Angle;
        private Vector3 axisVec = Vector3.zero;

        void Start()
        {
            baseRotation = SW.localEulerAngles[(int)axis];
            axisVec[(int)axis] = 1f;
            whenChange();
        }
        public override void whenChange()
        {
            Vector3 local = SW.localEulerAngles;
            local[(int)axis] = (Obj.val * Angle + baseRotation);
            SW.localEulerAngles = local;
        }
    }
}