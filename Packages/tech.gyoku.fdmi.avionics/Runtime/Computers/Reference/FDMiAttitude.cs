using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiAttitude : FDMiBehaviour
    {
        public string PitchPath = "Pitch";

        [FDMiDataPath(nameof(PitchPath))]
        public FDMiFloat Pitch;

        public string RollPath = "Roll";

        [FDMiDataPath(nameof(RollPath))]
        public FDMiFloat Roll;

        public string HDGPath = "HDG";

        [FDMiDataPath(nameof(HDGPath))]
        public FDMiFloat HDG;

        public string RotationPath = "Rotation";

        [FDMiDataPath(nameof(RotationPath))]
        public FDMiQuaternion Rotation;

        void Start()
        {
            _rot = Rotation.data;
        }

        void Update()
        {
            Vector3 fwd = _rot[0] * Vector3.forward;
            Vector3 right = _rot[0] * Vector3.right;
            Vector3 projFwd = Vector3.ProjectOnPlane(fwd, Vector3.up);
            // 真上/真下では ProjectOnPlane がゼロベクトルになり SignedAngle が NaN になるため前回値を維持
            if (projFwd.sqrMagnitude > 1e-8f)
                HDG.Data = Mathf.Repeat(Vector3.SignedAngle(Vector3.forward, projFwd, Vector3.up), 360f);
            Pitch.Data = -Vector3.SignedAngle(Vector3.Cross(right, Vector3.up), fwd, right);
            Roll.Data = -Vector3.SignedAngle(Vector3.Cross(Vector3.up, fwd), right, fwd);
        }

        Quaternion[] _rot;
    }
}
