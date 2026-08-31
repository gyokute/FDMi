
using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiDialFunctionGroup : FDMiHandInputGroup
    {
        #region Finger Input
        public virtual void OnSelect() {}

        public virtual void OnEnterSelect() { }

        public virtual void OnLeaveSelect() { }

        public virtual void OnGrab() { }

        public virtual void OnGrabStart() { }

        public virtual void OnGrabEnd() { }
        #endregion

        #region Gesture Input
        //straight-move gesture
        //twist gesture
        #endregion
        #region Axis Input
        // VR Controller/Mouse-Button input
        #endregion
    }
}
