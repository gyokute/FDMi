
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiWingAngleActuator : UdonSharpBehaviour
    {
        public FDMiFloat Angle;
        public FDMiWing wing;
        public Vector3 rotateAxis;
        private float[] angle;
        private float initialAngle;
        private Transform wingTransform;
        private Vector3 initialRotation;
        void Start()
        {
            angle = Angle.data;
            initialAngle = angle[0];
            wingTransform = wing.transform;
            initialRotation = wingTransform.localEulerAngles - angle[0] * rotateAxis;
        }
        void LateUpdate()
        {
            wingTransform.localEulerAngles = initialRotation + angle[0] * rotateAxis;
        }
    }
}