
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using SaccFlightAndVehicles;


namespace SaccFlight_FDMi
{
    public enum WindType { Override, Sacc }
    public class FDMi_wind : FDMi_Attributes
    {
        [SerializeField] private WindType windType;
        [SerializeField] private Vector3 Wind;

        [Header("Sacc wind System")]
        public SAV_WindChanger windChanger;
        [SerializeField] private float WindGustStrength = 15;
        [SerializeField] private float WindGustiness = 0.03f;
        [SerializeField] private float WindTurbulanceScale = 0.0001f;
        [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
        [System.NonSerializedAttribute] public float AtmosphereHeightThing;
        public override void FDMi_Local_Start() => gameObject.SetActive(false);
        public override void SFEXT_O_PilotEnter() => gameObject.SetActive(true);
        public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);
        public override void SFEXT_P_PassengerEnter() => gameObject.SetActive(true);
        public override void SFEXT_P_PassengerExit() => gameObject.SetActive(false);

        private void Update()
        {
            if (!sharedBool[0] || airData == null) return;
            switch (windType)
            {
                case WindType.Override:
                    setWind(Wind);
                    break;
                case WindType.Sacc:
                    if (windChanger != null) getWindParam();
                    setWind(calcSaccWind());
                    break;
            }
        }

        private void getWindParam()
        {
            Wind = windChanger.WindStrenth_3;
            WindGustStrength = windChanger.WindGustStrength;
            WindGustiness = windChanger.WindGustiness;
            WindTurbulanceScale = windChanger.WindTurbulanceScale;
        }
        private Vector3 calcSaccWind()
        {
            float Atmosphere = airData[(int)AirData.rho];
            float TimeGustiness = Time.time * WindGustiness;
            float gustx = TimeGustiness + (transform.position.x * WindTurbulanceScale);
            float gustz = TimeGustiness + (transform.position.z * WindTurbulanceScale);
            Vector3 Gust = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), 0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
            return (Gust + Wind) * Atmosphere;
        }
        private void setWind(Vector3 wind)
        {
            airData[(int)AirData.windX] = wind.x;
            airData[(int)AirData.windY] = wind.y;
            airData[(int)AirData.windZ] = wind.z;
            Vector3 wind2D = Vector3.ProjectOnPlane(wind, Vector3.up);
            airData[(int)AirData.windDeg] = Vector3.SignedAngle(Vector3.forward, wind2D, Vector3.up);
            airData[(int)AirData.windDeg] += Mathf.Ceil(-airData[(int)AirData.windDeg] / 360f) * 360f;
            airData[(int)AirData.windSpeed] = wind2D.magnitude;
        }
    }
}