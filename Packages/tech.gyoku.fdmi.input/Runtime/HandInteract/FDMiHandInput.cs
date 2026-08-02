using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiHandInput : FDMiBehaviour
    {
        #region Finger Input
        public virtual void OnFingerEnter() { }

        public virtual void OnFingerLeave() { }

        public virtual void OnGrab() { }

        public virtual void OnRelease() { }
        #endregion
    }
}
