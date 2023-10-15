
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiWingAngleActuator : UdonSharpBehaviour
    {
        [SerializeField] FDMiFloat InputValue;
        [SerializeField] FDMiWing wing;
        [SerializeField] Vector3 rotateAxis;
        [SerializeField] AnimationCurve curve;
        private float[] input;
        private float initialAngle;
        private Transform wingTransform;
        private Vector3 initialRotation;
        void Start()
        {
            input = InputValue.data;
            initialAngle = curve.Evaluate(input[0]);
            wingTransform = wing.transform;
            initialRotation = wingTransform.localEulerAngles - curve.Evaluate(input[0]) * rotateAxis;
        }
        void LateUpdate()
        {
            wingTransform.localEulerAngles = initialRotation + curve.Evaluate(input[0]) * rotateAxis;
        }
    }
}