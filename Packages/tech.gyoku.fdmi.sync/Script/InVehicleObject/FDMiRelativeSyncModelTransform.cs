﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncModelTransform : FDMiAttribute
    {
        public FDMiBool InZone;
        public Transform onlyIsRoot, onlyNotRoot;
        void Start()
        {
            InZone.subscribe(this, "OnChangeInZone");
            OnChangeInZone();
        }
        public void OnChangeInZone()
        {
            if (InZone.data[0]) transform.SetParent(onlyIsRoot);
            if (!InZone.data[0]) transform.SetParent(onlyNotRoot);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}