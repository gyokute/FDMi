
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiAnimatorDriver : FDMiBehaviour
    {
        public FDMiFloat Input;
        private float[] input = new float[1];
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