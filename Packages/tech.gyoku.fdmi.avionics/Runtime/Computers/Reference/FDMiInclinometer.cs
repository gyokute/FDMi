using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiInclinometer : FDMiAttribute
    {
        public FDMiFloat Slip;
        public FDMiQuaternion Rotation;
        public float gravity = 9.78f;
        [SerializeField] float cutoffFrequency = 0.1f;
        Quaternion[] rot;
        private float omega;
        void Start()
        {
            rot = Rotation.data;
            omega = 1 / cutoffFrequency;
        }

        Vector3 centrifugalAcc, prevVelocity;
        void Update()
        {
            // Ball
            centrifugalAcc = Vector3.Lerp(centrifugalAcc, (body.velocity - prevVelocity) / Time.deltaTime, Time.deltaTime * omega);
            Vector3 a = Vector3.ProjectOnPlane(centrifugalAcc + gravity * (Quaternion.Inverse(rot[0]) * Vector3.down), Vector3.forward);
            Slip.Data = Vector3.SignedAngle(Vector3.down, a, Vector3.forward);
            prevVelocity = body.velocity;

        }
    }
}