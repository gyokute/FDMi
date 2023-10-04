
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiTransformDriver : UdonSharpBehaviour
    {
        [SerializeField] private FDMiFloat Input;
        private float[] input = new float[1];
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 zeroPosition, onePosition;
        [SerializeField] private AnimationCurve outputValue;
        public
        void Start()
        {
            input = Input.data;
        }

        void Update()
        {
            targetTransform.localPosition = Vector3.Lerp(zeroPosition, onePosition, outputValue.Evaluate(input[0]));
        }

    }
}
