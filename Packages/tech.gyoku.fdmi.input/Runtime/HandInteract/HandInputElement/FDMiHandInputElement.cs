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
        [Tooltip("この入力の説明。手元の入力表示に出す")]
        public string description;

        public virtual void WhileSelect(FDMiHandInputGroup group) { }

        public virtual void OnSelectStart(FDMiHandInputGroup group) { }

        public virtual void OnSelectEnd() { }

        public virtual void WhileGrab(FDMiHandInputGroup group) { }

        public virtual void OnGrabStart(FDMiHandInputGroup group) { }

        public virtual void OnGrabEnd() { }
    }
}
