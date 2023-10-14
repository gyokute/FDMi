using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncOnlyIsRoot : FDMiAttribute
    {
        public override void init()
        {
            base.init();
            transform.SetParent(body.transform.parent);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
