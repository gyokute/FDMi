
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;
namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiWingExtendActuator : UdonSharpBehaviour
    {
        public FDMiFloat Angle;
        public FDMiWing wing;
        public AnimationCurve multiply;
        private float[] angle;
        private float initialAngle;
        private Transform wingTransform;
        void Start()
        {
            angle = Angle.data;
            initialAngle = angle[0];
        }
        void LateUpdate()
        {
            wing.cpChordLength = multiply.Evaluate(angle[0] - initialAngle);
        }
    }
}