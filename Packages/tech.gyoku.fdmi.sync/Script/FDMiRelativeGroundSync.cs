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
        public float disableKmPos = 9999f;
        void Start()
        {
            setPosition(transform.position);
            _rotation = transform.rotation;
        }
        public void LateUpdate()
        {
            Vector3 relativePos = getViewPosition();
            transform.SetPositionAndRotation(relativePos, getViewRotation());
            if (relativePos.magnitude * 0.001f > disableKmPos + 1)
            {
                gameObject.SetActive(false);
                SendCustomEventDelayedSeconds("UpdatePosWhenDisable", 1f);
            }
        }
        public void UpdatePosWhenDisable()
        {
            Vector3 relativePos = getViewPosition();
            if (relativePos.magnitude * 0.001f < disableKmPos - 1)
            {
                gameObject.SetActive(true);
            }
            else
            {
                SendCustomEventDelayedSeconds("UpdatePosWhenDisable", 1f);
            }
        }
        public override void windupPositionAndRotation()
        {
            base.windupPositionAndRotation();
            Vector3 relativePos = getViewPosition();
            if (relativePos.magnitude * 0.001f < disableKmPos - 1)
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
                SendCustomEventDelayedSeconds("UpdatePosWhenDisable", 1f);
            }
        }
    }
}