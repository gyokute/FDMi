using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiHandRotateInput : FDMiHandLeverInput
    {
        public override void WhileGrab(FDMiHandInputGroup group)
        {
            Quaternion delta = Quaternion.FromToRotation(group.prevHandPos, group.handPos);
            handMovement = Vector3.Dot(delta.eulerAngles, inputAxis);
            handMovement = Mathf.Repeat(handMovement + 180, 360) - 180;
            // handMovement = Vector3.SignedAngle(
            //     Vector3.ProjectOnPlane(group.prevHandPos, inputAxis),
            //     Vector3.ProjectOnPlane(group.handPos, inputAxis),
            //     inputAxis
            // );
            base.WhileGrab(group);
        }
    }
}
