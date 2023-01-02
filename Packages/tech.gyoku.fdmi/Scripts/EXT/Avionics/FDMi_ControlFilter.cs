
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ControlFilter : FDMi_Attributes
    {
        public virtual float filter(float input) { return input; }
    }
}