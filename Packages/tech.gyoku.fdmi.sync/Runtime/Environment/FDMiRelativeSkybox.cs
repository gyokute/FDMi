
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
            if (syncManager.localPlayerPosition)
                transform.position = syncManager.localPlayerPosition._position;
            transform.rotation = getViewRotation();
        }
        public override void windupPositionAndRotation()
        {

        }
    }
}