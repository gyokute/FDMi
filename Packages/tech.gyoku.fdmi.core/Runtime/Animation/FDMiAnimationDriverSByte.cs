
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiAnimationDriverSByte : FDMiBehaviour
    {
        public FDMiSByte Input;
        private sbyte[] input = new sbyte[1];
        [SerializeField] private Animator animator;
        [SerializeField] private string paramator;
        [SerializeField] private AnimationCurve outputValue;
        void Start()
        {
            input = Input.data;
            Input.subscribe(this, "OnChange");
        }

        public void OnChange()
        {
            animator.SetFloat(paramator, outputValue.Evaluate(input[0]));
        }

    }
}