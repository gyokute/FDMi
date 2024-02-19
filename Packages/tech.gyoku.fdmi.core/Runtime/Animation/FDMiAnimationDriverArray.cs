
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiAnimationDriverArray : FDMiDriverArray
    {
        [SerializeField] private Animator animator;
        [SerializeField] private FDMiFloat[] Input;
        [SerializeField] private string[] paramator;
        [SerializeField] private AnimationCurve[] outputValue;
        [SerializeField] private DriverUpdateMode updateMode;
        private bool anyOnUpdate = false;
        void Start()
        {
            for (int i = 0; i < Mathf.Min(Input.Length, 128); i++)
            {
                if (updateMode == DriverUpdateMode.OnValueChange)
                    Input[i].subscribe(this, "OnChange" + i.ToString());
            }
            gameObject.SetActive(updateMode == DriverUpdateMode.OnUpdate);
        }

        void Update()
        {
            if (updateMode != DriverUpdateMode.OnUpdate) return;
            for (int i = 0; i < Mathf.Min(Input.Length, 128); i++)
            {
                OnChange(i);
            }
        }

        public override void OnChange(int i)
        {
            animator.SetFloat(paramator[i], outputValue[i].Evaluate(Input[i].Data));
        }

    }
}
