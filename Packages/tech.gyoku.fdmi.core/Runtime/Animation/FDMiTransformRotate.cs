using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiTransformRotate : FDMiBehaviour
    {
        public string dataPath;

        [FDMiDataPath(nameof(dataPath))]
        [FDMiRegisterCallback(nameof(OnChange))]
        public FDMiData Value;
        public AnimationCurve outputCurve;

        public string targetPath;

        [FDMiDataPath(nameof(targetPath))]
        public FDMiTransformRef target;

        [SerializeField]
        Vector3 axis;

        public void Initialize()
        {
            OnChange();
        }

        public void OnChange()
        {
            target.data[0].localEulerAngles = axis * outputCurve.Evaluate(Value.GetFloat());
        }
    }
}
