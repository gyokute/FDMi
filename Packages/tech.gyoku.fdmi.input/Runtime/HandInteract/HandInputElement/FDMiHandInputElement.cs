using System.Collections;
using System.Collections.Generic;
using FDMi.core;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;

namespace FDMi.input
{
    public abstract class FDMiHandInputElement : FDMiBehaviour
    {
        private float[] axisValues = new float[5];

        public virtual void WhileSelect(FDMiHandInputGroup group) { }

        public virtual void OnSelectStart(FDMiHandInputGroup group) { }

        public virtual void OnSelectEnd() { }

        public virtual void WhileGrab(FDMiHandInputGroup group) { }

        public virtual void OnGrabStart(FDMiHandInputGroup group) { }

        public virtual void OnGrabEnd() { }
    }
}
