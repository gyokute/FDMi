using System.Collections;
using System.Collections.Generic;
using FDMi.core;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;

namespace FDMi.input
{
    public class FDMiHandInputGroup : FDMiBehaviour
    {
        public FDMiHandTracker[] Trackers;
        public FDMiHandInputElement[] InputElements;

        public virtual void OnSelect(FDMiHandTracker tracker) { }

        public virtual void OnSelectStart(FDMiHandTracker tracker) { }

        public virtual void OnSelectEnd() { }

        public virtual void OnGrab(FDMiHandTracker tracker) { }

        public virtual void OnGrabStart(FDMiHandTracker tracker) { }

        public virtual void OnGrabEnd() { }
    }
}
