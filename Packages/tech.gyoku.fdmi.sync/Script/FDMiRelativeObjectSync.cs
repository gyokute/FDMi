using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeObjectSync : FDMiReferencePoint
    {
        public Vector3 gravity = new Vector3(0, -9.8f, 0);
        public FDMiObjectManager obejctManaer;

        void Start()
        {
            if (!body)
            {
                gameObject.SetActive(false);
                return;
            }
            if (obejctManaer) obejctManaer.SubscribeOwnerManagement(this);
            body.useGravity = false;

            setPosition(body.position);
            direction = body.rotation;

            if (onlyIsRoot)
            {
                onlyIsRoot.transform.position = Vector3.zero;
                onlyIsRoot.transform.rotation = Quaternion.identity;
            }
        }

        public override void windupPositionAndRotation()
        {
            if (body)
            {
                body.position = getViewPosition();
                body.rotation = getViewRotation();
            }
            body.transform.position = getViewPosition();
            body.transform.rotation = getViewRotation();
        }

        public void SetLocalPlayerAsOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        void FixedUpdate()
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (isRoot)
            {
                Vector3 g = Quaternion.Inverse(direction) * body.rotation * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
            if (parentRefPoint.index == rootRefPoint.index)
            {
                Vector3 g = Quaternion.Inverse(parentRefPoint.direction) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
        }

        public void Update()
        {
            if (stopUpdate) return;
            if (Networking.IsOwner(gameObject))
            {
                Quaternion btr = body.transform.rotation;
                Vector3 btp = body.transform.position;
                if (isRoot)
                {
                    setRotation((btr * direction).normalized);
                    setPosition(direction * btp + Position);
                    velocity = direction * body.velocity;
                    Vector3 tvel = Quaternion.Inverse(btr) * body.velocity;
                    Vector3 angVel = Quaternion.Inverse(btr) * body.angularVelocity;

                    body.transform.position = Vector3.zero;
                    body.transform.rotation = Quaternion.identity;
                    body.velocity = tvel;
                    body.angularVelocity = angVel;
                    RequestSerialization();
                }
                if (parentRefPoint.index == rootRefPoint.index)
                {
                    setRotation((btr * parentRefPoint.direction).normalized);
                    setPosition(parentRefPoint.direction * btp + parentRefPoint.Position + 1000f * (parentRefPoint._kmPosition - _kmPosition));
                    velocity = parentRefPoint.direction * body.velocity;
                    RequestSerialization();
                }
            }
        }
        public void LateUpdate()
        {
            if (stopUpdate || isRoot) return;
            if (!Networking.IsOwner(gameObject))
            {
                body.transform.rotation = getViewRotationInterpolated();
                body.transform.position = getViewPositionInterpolated();
                return;
            }
            if (parentRefPoint.index != rootRefPoint.index)
            {
                body.transform.position = getViewPosition();
                body.transform.rotation = getViewRotation();
            }
        }
    }
}