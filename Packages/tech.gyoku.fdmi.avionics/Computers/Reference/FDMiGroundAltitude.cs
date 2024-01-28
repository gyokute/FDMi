

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiGroundAltitude : FDMiBehaviour
    {
        [SerializeField] FDMiFloat GroundAltitude;
        [SerializeField] FDMiQuaternion Rotation;
        [SerializeField] float maxLength = 914.4f, offset;
        [SerializeField] LayerMask groundLayer;
        RaycastHit[] hit = new RaycastHit[32];

        int i;
        float minimum;
        Quaternion[] rot;
        void Start()
        {
            rot = Rotation.data;
        }
        void Update()
        {
            minimum = maxLength;
            for (i = 0; i < Physics.RaycastNonAlloc(transform.position, Quaternion.Inverse(rot[0]) * Vector3.down, hit, maxLength, groundLayer); i++)
                minimum = Mathf.Min(minimum, hit[i].distance);
            GroundAltitude.Data = minimum + offset;
        }
    }
}