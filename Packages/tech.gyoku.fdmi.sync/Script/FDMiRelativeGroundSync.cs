﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeGroundSync : FDMiReferencePoint
    {
        void Start()
        {
            setPosition(transform.position);
            _rotation = transform.rotation;
        }
        public void LateUpdate()
        {
            transform.SetPositionAndRotation(getViewPosition(), getViewRotation());
        }
    }
}