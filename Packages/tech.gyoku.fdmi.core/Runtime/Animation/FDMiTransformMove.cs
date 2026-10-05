using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiTransformMove : FDMiBehaviour
    {
        public string dataPath;

        [FDMiDataPath(nameof(dataPath))]
        public FDMiFloat Value;
        public AnimationCurve outputCurve;

        public string targetPath;

        [FDMiDataPath(nameof(targetPath))]
        public FDMiTransformRef target;

        [SerializeField]
        Vector3[] position = new Vector3[2];

        public void Initialize()
        {
            OnChange();
        }

        public void OnChange()
        {
            target.data[0].localPosition = Vector3.LerpUnclamped(
                position[0],
                position[1],
                outputCurve.Evaluate(Value.GetFloat())
            );
        }
    }
}
