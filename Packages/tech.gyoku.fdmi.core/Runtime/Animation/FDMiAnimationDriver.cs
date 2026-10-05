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

        void Update()
        {
            animator.SetFloat(parameterName, parameterValue.GetFloat());
        }
    }
}
