using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeObjectSync : FDMiReferencePoint
    {
        Quaternion prevRootRot = Quaternion.identity;
        Vector3 prevRootPos = Vector3.zero;
        public Vector3 gravity = new Vector3(0, -9.8f, 0);
        Rigidbody rootBody;

        public override void waitUpdate()
        {
            base.waitUpdate();
            rootBody = syncManager.localRootRefPoint.body;
        }

        void Start()
        {
            if (!body)
            {
                gameObject.SetActive(false);
                return;
            }

            Position = transform.position;
            direction = transform.rotation;

            if (onlyIsRoot)
            {
                onlyIsRoot.transform.position = Vector3.zero;
                onlyIsRoot.transform.rotation = Quaternion.identity;
            }
        }

        void FixedUpdate()
        {
            if (Networking.IsOwner(gameObject) && isRoot)
            {
                Vector3 g = Quaternion.Inverse(direction) * body.rotation * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
        }

        public override void Update()
        {
            if (stopUpdate) return;
            if (Networking.IsOwner(gameObject))
            {
                Quaternion btr = body.transform.rotation;
                Vector3 btp = body.transform.position;
                Quaternion dir;
                if (isRoot)
                {
                    setRotation((btr * direction).normalized);
                    setPosition(direction * btp + Position);
                    Vector3 tvel = Quaternion.Inverse(btr) * body.velocity;
                    Vector3 angVel = Quaternion.Inverse(btr) * body.angularVelocity;

                    body.transform.position = Vector3.zero;
                    body.transform.rotation = Quaternion.identity;
                    body.velocity = tvel;
                    body.angularVelocity = angVel;
                    RequestSerialization();
                }
            }
        }
        public void LateUpdate()
        {
            if (!Networking.IsOwner(gameObject))
            {
                body.transform.rotation = getViewRotationInterpolated();
                body.transform.position = getViewPositionInterpolated();
                return;
            }
            if (!isRoot)
            {
                body.transform.position = getViewPosition();
                body.transform.rotation = getViewRotation();
            }
        }
    }
}