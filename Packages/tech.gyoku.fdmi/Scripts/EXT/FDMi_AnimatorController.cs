
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum AnimatorParamaterType
    {
        AirData, SyncObject, ControlSurface, Engine, Wheel,
        Length
    }
    public class FDMi_AnimatorController : FDMi_Attributes
    {
        public Animator[] animator;
        public FDMi_SyncObject[] so;
        public FDMi_ControlSurface[] cs;
        public FDMi_TurboFan_2A[] Engine;
        public FDMi_LandingGear[] Wheel;
        public string[] paramator;
        public int[] attributeType, animateAttribute;
        public float[] mul, offset;
        private float val;

        private void Update()
        {
            if (!sharedBool[0]) return;
            AnimatorUpdate();
        }

        public virtual void AnimatorUpdate()
        {
            foreach (Animator anim in animator)
            {
                for (int i = 0; i < paramator.Length; i++)
                {
                    switch (attributeType[i])
                    {
                        case (int)AnimatorParamaterType.AirData:
                            anim.SetFloat(paramator[i], airData[animateAttribute[i]] * mul[i] + offset[i]);
                            break;
                        case (int)AnimatorParamaterType.SyncObject:
                            anim.SetFloat(paramator[i], so[i].val * mul[i] + offset[i]);
                            break;
                        // case (int)AnimatorParamaterType.ControlSurface:
                        //     anim.SetFloat(paramator[i], cs[i].deg * mul[i] + offset[i]);
                        //     break;
                        case (int)AnimatorParamaterType.Engine:
                            anim.SetFloat(paramator[i], Engine[i].val[animateAttribute[i]] * mul[i] + offset[i]);
                            break;
                        case (int)AnimatorParamaterType.Wheel:
                            anim.SetFloat(paramator[i], Wheel[i].val * mul[i] + offset[i]);
                            break;
                    }
                }
            }
        }
    }
}