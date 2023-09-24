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
        }

        public override void windupPositionAndRotation()
        {
            transform.position = getViewPosition();
            transform.rotation = getViewRotation();
            if (body)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
                body.transform.position = transform.position;
                body.transform.rotation = transform.rotation;
                if (isRoot && onlyIsRoot)
                {
                    onlyIsRoot.transform.parent = body.transform.parent;
                    onlyIsRoot.transform.position = Vector3.zero;
                    onlyIsRoot.transform.rotation = Quaternion.identity;
                }
                if (!isRoot && onlyIsRoot)
                {
                    onlyIsRoot.transform.parent = body.transform;
                    onlyIsRoot.transform.localPosition = Vector3.zero;
                    onlyIsRoot.transform.localRotation = Quaternion.identity;
                }
            }

        }

        public void SetLocalPlayerAsOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        Vector3 tPos = Vector3.zero, tVel = Vector3.zero, tAngVel = Vector3.zero;
        Quaternion tRot = Quaternion.identity;
        void FixedUpdate()
        {
            if (!isInit) return;
            if (!Networking.IsOwner(gameObject) || body.isKinematic) return;
            if (isRoot)
            {
                Quaternion br = body.rotation;
                Vector3 bp = body.position;
                setRotation((direction * br).normalized);
                setPosition(direction * bp + Position);
                body.position = Vector3.zero;
                body.rotation = Quaternion.identity;
                body.velocity = Quaternion.Inverse(br) * body.velocity;

                // gravity
                Vector3 g = Quaternion.Inverse(direction) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
            if (parentRefPoint.index == rootRefPoint.index)
            {
                // gravity
                Vector3 g = Quaternion.Inverse(parentRefPoint.direction) * gravity;
                body.AddForce(g, ForceMode.Acceleration);
            }
        }

        public void Update()
        {
            if (!isInit || stopUpdate) return;
            if (Networking.IsOwner(gameObject))
            {
                Quaternion btr = body.transform.rotation;
                Vector3 btp = body.transform.position;
                if (isRoot)
                {
                    // setRotation((btr * direction).normalized);
                    // setPosition(direction * btp + Position);
                    velocity = direction * body.velocity;
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
                transform.rotation = getViewRotationInterpolated();
                transform.position = getViewPositionInterpolated();
                body.transform.rotation = transform.rotation;
                body.transform.position = transform.position;
                return;
            }
            transform.rotation = getViewRotation();
            transform.position = getViewPosition();
            body.transform.position = transform.position;
            body.transform.rotation = transform.rotation;
        }
    }
}