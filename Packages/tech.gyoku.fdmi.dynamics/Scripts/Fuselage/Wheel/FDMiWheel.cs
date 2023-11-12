﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiWheel : FDMiAttribute
    {
        public WheelCollider wheel;
        public FDMiFloat TillerInput, BrakeInput, ParkBrakeInput, Retract;
        public float brakePressure = 3000f, parkBrakePressure = 3000f;
        [SerializeField] private float preLoadTorque = 0.0001f;
        [SerializeField] private float rpm;
        [SerializeField] private float rotateAngle = 60f;
        [SerializeField] private float retractThreshold = 0.5f;
        [SerializeField] private Vector3 retractDirection;
        [SerializeField] private AnimationCurve retractCurve;
        private Vector3 wheelOriginPos = Vector3.zero;
        private float[] tiller, brake, parkbrake;
        void Start()
        {
            tiller = TillerInput.data;
            brake = BrakeInput.data;
            parkbrake = ParkBrakeInput.data;
            wheelOriginPos = wheel.transform.localPosition;
            if (Retract) Retract.subscribe(this, nameof(OnRetract));
        }

        public void OnRetract()
        {
            wheel.transform.localPosition = wheelOriginPos + retractCurve.Evaluate(Retract.Data) * retractDirection;
            wheel.gameObject.SetActive(Retract.Data < retractThreshold);
        }
        void Update()
        {
            // Ground Detection
            if (isOwner)
            {
                wheel.brakeTorque = brake[0] * brakePressure + parkbrake[0] * parkBrakePressure;
                wheel.motorTorque = Mathf.Clamp01(-wheel.rpm + 0.01f) * Mathf.Clamp01(1f - parkbrake[0] - parkbrake[0]) * preLoadTorque;
                wheel.steerAngle = tiller[0] * rotateAngle;
            }
        }
    }
}