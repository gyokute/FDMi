using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiRelativeSyncOnlyIsRoot : FDMiBehaviour
    {
        public Rigidbody body;

        void Start()
        {
            transform.SetParent(body.transform.parent);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
