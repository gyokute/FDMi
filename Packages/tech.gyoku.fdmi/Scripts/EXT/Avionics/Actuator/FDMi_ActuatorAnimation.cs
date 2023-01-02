
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace SaccFlight_FDMi
{
    public class FDMi_ActuatorAnimation : FDMi_ControlFilter
    {
        public float min, max;
        public AnimationClip clip;
        public GameObject AvatarRoot;

        public override float filter(float input)
        {
            clip.SampleAnimation(AvatarRoot, clip.length * Mathf.InverseLerp(min, max, input));
            return input;
        }
    }
}