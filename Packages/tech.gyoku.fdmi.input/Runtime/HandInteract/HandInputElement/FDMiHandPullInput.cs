using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiHandPullInput : FDMiHandLeverInput
    {
        public override void WhileGrab(FDMiHandInputGroup group)
        {
            handMovement = Vector3.Dot(group.handPos - group.prevHandPos, inputAxis);
            base.WhileGrab(group);
        }
    }
}
