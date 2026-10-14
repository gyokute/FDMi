using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiGroundAltitude : FDMiBehaviour
    {
        public string GroundAltitudePath = "GroundAltitude";

        [FDMiDataPath(nameof(GroundAltitudePath))]
        public FDMiFloat GroundAltitude;

        public string RotationPath = "Rotation";

        [FDMiDataPath(nameof(RotationPath))]
        public FDMiQuaternion Rotation;

        [SerializeField]
        float maxLength = 914.4f;
        [SerializeField]
        float offset;
        [SerializeField]
        LayerMask groundLayer;

        RaycastHit[] hit;

        int i;
        float minimum;

        void Start()
        {
            _rot = Rotation.data;
            hit = new RaycastHit[32]; // Udonではフィールド初期化子の配列new不可
        }

        void Update()
        {
            minimum = maxLength;
            for (i = 0; i < Physics.RaycastNonAlloc(transform.position, Quaternion.Inverse(_rot[0]) * Vector3.down, hit, maxLength, groundLayer); i++)
                minimum = Mathf.Min(minimum, hit[i].distance);
            // ヒットなし/上限到達時は clamp 上限扱いせず maxLength + offset をそのまま出力（動作は従来どおり）
            GroundAltitude.Data = minimum + offset;
        }

        Quaternion[] _rot;
    }
}
