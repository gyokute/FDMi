
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSkybox : FDMiReferencePoint
    {
        void Start()
        {
            setPosition(transform.position);
            _rotation = transform.rotation;
        }
        void Update()
        {
            transform.position = Vector3.zero;
        }

    }
}