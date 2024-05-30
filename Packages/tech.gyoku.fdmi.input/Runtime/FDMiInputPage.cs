
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputPage : FDMiBehaviour
    {
        public bool enable = true;
        public FDMiInputManager inputManager;
        public FDMiInputAddon[] InputAddons;
        public float inputThreshold = 0.25f;

        public virtual void whileTouch()
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i])
                    InputAddons[i].whileTouch();
            }
        }

        public virtual void whileGrab()
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i])
                    InputAddons[i].whileGrab();
            }
        }

        #region Finger Input
        public virtual void OnFingerEnter(FDMiFingerTracker finger)
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i]) InputAddons[i].OnFingerEnter(finger);
            }
        }
        public virtual void OnFingerLeave(FDMiFingerTracker finger)
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i]) InputAddons[i].OnFingerLeave(finger);
            }
        }
        public virtual void OnGrab(FDMiFingerTracker finger)
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i]) InputAddons[i].OnGrab(finger);
            }
        }
        public virtual void OnRelease(FDMiFingerTracker finger)
        {
            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (InputAddons[i]) InputAddons[i].OnRelease(finger);
            }
        }
        #endregion
    }
}