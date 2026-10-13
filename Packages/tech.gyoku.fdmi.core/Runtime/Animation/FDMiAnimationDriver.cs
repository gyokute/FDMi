using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiAnimationDriver : FDMiBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string parameterName;

        public string valuePath;

        [FDMiDataPath(nameof(valuePath))]
        public FDMiData parameterValue;

        [Header("Output = (Value + Offset) * Scale")]
        [SerializeField]
        private float scale = 1f;

        [SerializeField]
        private float offset = 0f;

        void Update()
        {
            animator.SetFloat(parameterName, (parameterValue.GetFloat() + offset) * scale);
        }
    }
}
