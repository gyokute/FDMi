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
        public FDMiHandInputElement[] InputElements = new FDMiHandInputElement[0];
        private FDMiHandTracker grabHand;

        [HideInInspector]
        public Vector3 handPos;

        [HideInInspector]
        public Vector3 prevHandPos;

        [HideInInspector]
        public Quaternion handRot;

        [HideInInspector]
        public Quaternion prevHandRot;

        [HideInInspector]
        public float[] axes = new float[(int)FDMiHandAxisType.Length];

        protected virtual void GetHandInput(FDMiHandTracker tracker)
        {
            prevHandPos = handPos;
            prevHandRot = handRot;
            handPos = transform.InverseTransformPoint(tracker.handPos);
            handRot = Quaternion.Inverse(transform.rotation) * tracker.handRot;
            axes = tracker.axes;
        }

        protected virtual void OnStartHandInput(FDMiHandTracker tracker)
        {
            handPos = transform.InverseTransformPoint(tracker.handPos);
            handRot = Quaternion.Inverse(transform.rotation) * tracker.handRot;
            prevHandPos = handPos;
            prevHandRot = handRot;
            axes = tracker.axes;
        }

        public virtual void OnSelect(FDMiHandTracker tracker)
        {
            GetHandInput(tracker);
            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.WhileSelect(this);
            }
        }

        public virtual void OnSelectStart(FDMiHandTracker tracker)
        {
            OnStartHandInput(tracker);
            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.OnSelectStart(this);
            }
        }

        public virtual void OnSelectEnd()
        {
            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.OnSelectEnd();
            }
        }

        public virtual void OnGrab(FDMiHandTracker tracker)
        {
            if (grabHand != tracker)
                return;

            GetHandInput(tracker);
            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.WhileGrab(this);
            }
        }

        public virtual void OnGrabStart(FDMiHandTracker tracker)
        {
            if (grabHand && grabHand != tracker)
                grabHand.YieldGrab(this);
            grabHand = tracker;

            OnStartHandInput(tracker);
            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.OnGrabStart(this);
            }
        }

        public virtual void OnGrabEnd(FDMiHandTracker tracker)
        {
            if (grabHand != tracker)
                return;
            grabHand = null;

            foreach (var inputElement in InputElements)
            {
                if (!inputElement)
                    continue;
                inputElement.OnGrabEnd();
            }
        }
    }
}
