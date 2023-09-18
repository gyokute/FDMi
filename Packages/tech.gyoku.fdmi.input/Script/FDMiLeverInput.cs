
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiLeverInput : FDMiInput
    {
        public FDMiSyncedFloat LeverOutput;
        public Vector3 rotationAxis;
        public float initialValue;
        public float multiply, min, max;
        public float[] detents;
        private Vector3 initDir;
        public override void OnStartGrab()
        {
            base.OnStartGrab();
            initDir = transform.InverseTransformPoint(pickup.transform.position);
            initialValue = LeverOutput.data[0];
        }
        public override void OnDropGrab()
        {
            base.OnDropGrab();
            int detentIndex = -1;
            float minDetDiff = max;
            for (int i = 0; i < detents.Length; i++)
            {
                float detDiff = Mathf.Abs(detents[i] - LeverOutput.data[0]);
                if (detDiff < minDetDiff)
                {
                    detentIndex = i;
                    minDetDiff = detDiff;
                }
            }
            if (detentIndex >= 0) LeverOutput.set(detents[detentIndex]);
            Debug.Log(detentIndex);
            Debug.Log(LeverOutput.data[0]);

        }
        void LateUpdate()
        {
            if(!isGrab) return;
            Vector3 currentDir = transform.InverseTransformPoint(pickup.transform.position);
            LeverOutput.set(Mathf.Clamp(initialValue + multiply * Vector3.SignedAngle(initDir, currentDir, rotationAxis), min, max));
        }

    }
}