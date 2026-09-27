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

        public virtual void WhileSelect() { }

        public virtual void OnSelectStart() { }

        public virtual void OnSelectEnd() { }

        public virtual void WhileGrab() { }

        public virtual void OnGrabStart() { }

        public virtual void OnGrabEnd() { }
    }
}
