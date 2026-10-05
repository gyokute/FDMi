using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiHandTwistInput : FDMiHandLeverInput
    {
        public override void WhileGrab(FDMiHandInputGroup group)
        {
            Quaternion delta = Quaternion.Inverse(group.prevHandRot) * group.handRot;
            handMovement = Vector3.Dot(delta.eulerAngles, inputAxis);
            handMovement = Mathf.Repeat(handMovement + 180, 360) - 180;

            base.WhileGrab(group);
        }
    }
}
