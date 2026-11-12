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
            handMovement = FDMiHandInputMath.TwistAngleAboutAxis(delta, inputAxis);

            base.WhileGrab(group);
        }
    }
}
