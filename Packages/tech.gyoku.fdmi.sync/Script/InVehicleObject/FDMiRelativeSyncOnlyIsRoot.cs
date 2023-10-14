using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncOnlyIsRoot : FDMiAttribute
    {
        void Start()
        {
            transform.SetParent(body.transform.parent);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
