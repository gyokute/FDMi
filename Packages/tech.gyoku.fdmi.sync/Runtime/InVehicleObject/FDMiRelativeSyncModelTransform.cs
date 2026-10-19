using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiRelativeSyncModelTransform : FDMiBehaviour
    {
        public Rigidbody body;

        [FDMiDataPathConst("InZone"), FDMiRegisterCallback("OnChangeInZone")]
        public FDMiBool InZone;

        [SerializeField]
        Transform onlyIsRoot;

        public void OnChangeInZone()
        {
            if (InZone.data[0])
                transform.SetParent(onlyIsRoot);
            if (!InZone.data[0] && body)
                transform.SetParent(body.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
