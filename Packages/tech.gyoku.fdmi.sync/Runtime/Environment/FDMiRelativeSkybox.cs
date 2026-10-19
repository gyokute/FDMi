
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace FDMi.sync
{
    public class FDMiRelativeSkybox : FDMiReferencePoint
    {
        void Start()
        {
            setPositionAbsolute(transform.position);
            _rotation = transform.rotation;
        }
        void Update()
        {
            if (syncManager.localPlayerPosition)
                transform.position = syncManager.localPlayerPosition.getViewPosition();
            transform.rotation = getViewRotation();
        }
        public override void windupPositionAndRotation()
        {

        }
    }
}