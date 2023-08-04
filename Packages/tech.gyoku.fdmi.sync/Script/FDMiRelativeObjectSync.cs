using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeObjectSync : FDMiReferencePoint
    {
        Vector3 tpos = Vector3.zero;
        Quaternion trot = Quaternion.identity, prevBodyRot;
        Vector3 prevBodyPos;
        Rigidbody rootBody;
        public override void waitUpdate()
        {
            base.waitUpdate();
            tpos = Vector3.zero;
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

            prevBodyPos = transform.position;
            prevBodyRot = transform.rotation;

            if (onlyIsRoot)
            {
                onlyIsRoot.transform.position = Vector3.zero;
                onlyIsRoot.transform.rotation = Quaternion.identity;
            }
        }

        void FixedUpdate()
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (isRoot)
            {
                tpos += body.position;
                body.position = Vector3.zero;
                trot *= body.rotation;
                body.rotation = Quaternion.identity;
            }
            else
            {
                Vector3 diff = body.position - prevBodyPos;
                tpos += diff;
                prevBodyPos = tpos + transform.position;
                body.position = prevBodyPos;

                Quaternion diffrot = body.rotation * Quaternion.Inverse(prevBodyRot);
                trot *= diffrot;
                prevBodyRot = (transform.rotation * trot).normalized;
                body.rotation = prevBodyRot;
            }
        }

        public override void Update()
        {
            if (stopUpdate) return;
            if (Networking.IsOwner(gameObject))
            {
                Quaternion dir = (direction * trot).normalized;
                if (isRoot)
                {
                    setPosition(dir * tpos + Position);
                    setRotation(dir);
                }
                else
                {
                    dir *= Quaternion.Inverse(rootRefPoint.direction);
                    Vector3 tmp = dir * tpos + Position;
                    setPosition(tmp);
                    prevBodyPos = getViewPosition();
                    body.position = prevBodyPos;
                    transform.position = prevBodyPos;
                    setRotation((direction * trot).normalized);
                    prevBodyRot = getViewRotation();
                    body.rotation = prevBodyRot;
                    transform.rotation = prevBodyRot;
                }
                RequestSerialization();
            }
            else
            {
                tpos = getViewPositionInterpolated();
                trot = getViewRotationInterpolated();
                transform.rotation = trot;
                transform.position = tpos;
                body.rotation = trot;
                body.position = tpos;
            }

            tpos = Vector3.zero;
            trot = Quaternion.identity;
        }
    }
}