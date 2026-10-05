using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.dynamics
{
    // 3-spool turbofan, EICAS-level. Static maps + 1st-order spool/fuel lags.
    // Transient: fuel leads, air lags -> FAR spike -> EGT overshoot + surge latch.
    // Choke: corrected-speed cap. Surge margin consumed by FAR excess + supersonic distortion.
    // Docs: NASA E3(Fn=G(uj-uf), EPR=Pt7/Pt2, ram Tt/T=1+0.2M^2),
    //   NAL-TR-283(dN:1st-order lag vs Wf, Nc=N/sqrt(theta)),
    //   NAL-TR-515(dNi/dt=dH/(I Ni), Fnet=Fgross-Rd, TSFC corr).
    public class FDMiJetEngine : FDMiBehaviour
    {
        // --- inputs (realtime bus) ---
        public string throttlePath = "Throttle";
        [FDMiDataPath(nameof(throttlePath))]
        public FDMiData throttle;
        public string densityPath = "Density";
        [FDMiDataPath(nameof(densityPath))]
        public FDMiData airDensity;
        public string speedPath = "TAS";
        [FDMiDataPath(nameof(speedPath))]
        public FDMiData airSpeed;
        public string tempPath = "OAT";
        [FDMiDataPath(nameof(tempPath))]
        public FDMiData airTemp;

        // --- outputs (realtime bus) ---
        public string thrustPath = "Thrust";
        [FDMiDataPath(nameof(thrustPath))]
        public FDMiData thrustN;
        public string fuelPath = "FuelFlow";
        [FDMiDataPath(nameof(fuelPath))]
        public FDMiData fuelFlow;
        public string eprPath = "EPR";
        [FDMiDataPath(nameof(eprPath))]
        public FDMiData epr;
        public string egtPath = "EGT";
        [FDMiDataPath(nameof(egtPath))]
        public FDMiData egt;
        public string n1Path = "N1";
        [FDMiDataPath(nameof(n1Path))]
        public FDMiData n1;
        public string n2Path = "N2";
        [FDMiDataPath(nameof(n2Path))]
        public FDMiData n2;
        public string n3Path = "N3";
        [FDMiDataPath(nameof(n3Path))]
        public FDMiData n3;
        public string surgePath = "Surge";
        [FDMiDataPath(nameof(surgePath))]
        public FDMiData surge;

        // --- params (CF6-50C2 preset; N3 unused: threeSpool=false) ---
        public bool threeSpool = false;
        public float n1Idle = 22f; // N1 ground idle [%]
        public float n2Idle = 60f; // N2 ground idle [%]
        public float maxThrust = 233600f; // Fn_SL,max [N] = 52500lbf
        public float maxFlow = 645f; // G_SL,max [kg/s] = 1423lb/s
        public float idleFlow = 0.15f; // mf_idle,SL [kg/s]
        public float sfc = 1.0e-05f; // dmf/dFn, TO SFC 0.376 fit [kg/N/s]
        public float eprMax = 1.6f; // EPR at N1c=100% (representative)
        public float egtMax = 1233f; // EGT redline 960degC [K]
        public float tau1 = 2f; // fan lag [s]
        public float tau2 = 1f; // core lag [s]
        public float tau3 = 0.5f; // (unused when !threeSpool)
        public float tauF = 0.15f; // fuel metering lag [s]
        public float accelTime = 4f; // accel schedule: idle->max fuel ramp [s]
        public float chokeLim = 1.05f; // corrected speed choke cap [-]

        private float _n1 = 22f;
        private float _n2 = 60f;
        private float _n3;
        private float _mfCmd;
        private float _mf;
        private bool _surged;
        private float _maxF;
        private float _FARref;
        private float _ujC;

        void Start()
        {
            _n1 = n1Idle;
            _n2 = n2Idle;
            _n3 = threeSpool ? 55f : 0f;
            _mf = idleFlow;
            _mfCmd = idleFlow;
            _surged = false;
            _maxF = idleFlow + sfc * maxThrust;
            _FARref = _maxF / maxFlow;
            _ujC = (maxThrust / maxFlow) / (Mathf.Sqrt(eprMax - 1f) * Mathf.Sqrt(288.15f));
        }

        void FixedUpdate()
        {
            if (!throttle || !airDensity || !airSpeed || !airTemp) return;
            if (!thrustN || !fuelFlow || !epr || !egt || !n1 || !n2 || !n3 || !surge) return;

            float dt = Time.fixedDeltaTime;
            float thr = Mathf.Clamp01(throttle.GetFloat() * 0.01f);
            float rho = airDensity.GetFloat();
            float v0 = airSpeed.GetFloat();
            float tamb = airTemp.GetFloat();

            // atmosphere: delta=sigma*theta, ram=1+0.2M^2
            float theta = tamb / 288.15f;
            float delta = rho / 1.225f * theta;
            float mach = v0 / Mathf.Sqrt(1.4f * 287.05f * tamb);
            float ram = 1f + 0.2f * mach * mach;
            float deltat = delta * Mathf.Pow(ram, 3.5f);
            float sq = Mathf.Sqrt(theta * ram);

            // fuel is fast (lever), air is slow (spool): FAR spikes on accel.
            // accel schedule (FADEC surrogate) slews the fuel command.
            float xs = deltat / sq;
            float mfTgt = (idleFlow + (_maxF - idleFlow) * thr) * xs;
            float mx = (_maxF - idleFlow) * xs * dt / accelTime;
            float dm = mfTgt - _mfCmd;
            if (dm > mx) dm = mx;
            else if (dm < -mx) dm = -mx;
            _mfCmd += dm;
            _mf += (_mfCmd - _mf) * dt / (tauF + dt);

            // spools: N+=(Nt-N)*dt/(tau+dt), stable for any dt
            _n1 += ((n1Idle + (100f - n1Idle) * thr) - _n1) * dt / (tau1 + dt);
            _n2 += ((n2Idle + (100f - n2Idle) * thr) - _n2) * dt / (tau2 + dt);
            if (threeSpool) _n3 += ((55f + 45f * thr) - _n3) * dt / (tau3 + dt);
            float n1c = Mathf.Min(_n1 / sq * 0.01f, chokeLim);
            float n2c = Mathf.Min(_n2 / sq * 0.01f, chokeLim);
            float n3c = Mathf.Min(_n3 / sq * 0.01f, chokeLim);
            float nxc = threeSpool ? n3c : n2c;

            // performance: EPR -> nozzle expansion uj, Fn=G(uj-v0), surge latch, EGT from FAR
            float g = maxFlow * xs * n1c;
            float eprV = 1f + (eprMax - 1f) * n1c * n1c;
            float uj = _ujC * Mathf.Sqrt((eprV - 1f) * tamb * ram);
            float far = _mf / g;
            float sm = 0.2f - (far / _FARref - 1f) - 0.1f * Mathf.Max(0f, mach - 1f);
            if (!_surged && sm < 0f) _surged = true;
            else if (_surged && thr < 0.3f) _surged = false;
            float fn = g * (uj - v0);
            if (_surged) fn *= 0.5f;
            if (fn < 0f) fn = 0f;
            float egtV = tamb * ram + (egtMax - 288.15f) * nxc * (0.5f + 0.5f * far / _FARref);
            if (_surged) egtV += 200f;

            thrustN.Set(fn);
            fuelFlow.Set(_mf);
            epr.Set(eprV);
            egt.Set(egtV);
            n1.Set(_n1);
            n2.Set(_n2);
            n3.Set(_n3);
            surge.Set(_surged ? 1f : 0f);
        }
    }
}
