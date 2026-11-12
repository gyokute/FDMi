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
            handMovement = FDMiHandInputMath.AngleAboutAxis(
                group.prevHandPos,
                group.handPos,
                inputAxis
            );
            base.WhileGrab(group);
        }
    }
}
