
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiAnimatorDriver : UdonSharpBehaviour
    {
        [SerializeField] private FDMiFloat Input;
        private float[] input = new float[1];
        [SerializeField] private Animator animator;
        [SerializeField] private string paramator;
        [SerializeField] private AnimationCurve outputValue;
        public
        void Start()
        {
            input = Input.data;
        }

        void Update()
        {
            animator.SetFloat(paramator, outputValue.Evaluate(input[0]));
        }

    }
}