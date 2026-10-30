using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    public class FDMiJetEngine : FDMiBehaviour
    {
        [Tooltip("推力を加える機体の Rigidbody")]
        public Rigidbody body;

        [Header("Input Variables")]
        // throttle 0~1
        [Tooltip("スロットル値の解決パス")]
        public string throttlePath = "Throttle";

        [FDMiDataPath(nameof(throttlePath))]
        [Tooltip("スロットル [0,1]。1 超は定格超過")]
        public FDMiFloat Throttle;

        // starter switch (AirSW equivalent, 0/1, optional). Adds rotation to N2.
        [Tooltip("スターター値の解決パス")]
        public string starterPath = "Starter";

        [FDMiDataPath(nameof(starterPath))]
        [Tooltip("スタータースイッチ。N2 を回す")]
        public FDMiBool Starter;

        // fuel switch (AirSW equivalent, 0/1, optional). Adds rotation to N2.
        [Tooltip("燃料スイッチの解決パス")]
        public string fuelSWPath = "FuelSW";

        [FDMiDataPath(nameof(fuelSWPath))]
        [Tooltip("燃料スイッチ。点火と燃料供給")]
        public FDMiBool FuelSW;

        // thrust reverser lever (0 = stowed, 1 = full reverse)
        [Tooltip("リバーサーの解決パス")]
        public string reverserPath = "Reverser";

        [FDMiDataPath(nameof(reverserPath))]
        [Tooltip("リバーサーレバー [0,1]。0 格納、1 全開")]
        public FDMiFloat Reverser;

        // Aerodynamic state
        // Mach number, optional (null = V/sqrt(gamma R T))
        [Tooltip("真対気速度の解決パス")]
        public string TASPath = "~/Physics/References/TAS";

        [FDMiDataPath(nameof(TASPath))]
        [Tooltip("真対気速度 [m/s]")]
        public FDMiFloat TAS;

        // Mach number, optional (null = V/sqrt(gamma R T))
        [Tooltip("マッハ数の解決パス")]
        public string MachPath = "~/Physics/References/Mach";

        [FDMiDataPath(nameof(MachPath))]
        [Tooltip("マッハ数")]
        public FDMiFloat Mach;

        // Static air temperature
        [Tooltip("静気温の解決パス")]
        public string SATPath = "~/Physics/References/SAT";

        [FDMiDataPath(nameof(SATPath))]
        [Tooltip("静気温 [K]")]
        public FDMiFloat SAT;

        // Outside air temperature
        [Tooltip("全温の解決パス")]
        public string OATPath = "~/Physics/References/OAT";

        [FDMiDataPath(nameof(OATPath))]
        [Tooltip("全温 [K]")]
        public FDMiFloat OAT;

        // Static air pressure
        [Tooltip("静圧の解決パス")]
        public string StaticPressurePath = "~/Physics/References/StaticPressure";

        [FDMiDataPath(nameof(StaticPressurePath))]
        [Tooltip("静圧 [Pa]")]
        public FDMiFloat StaticPressure;

        // Outside air pressure
        [Tooltip("全圧の解決パス")]
        public string DynamicPressurePath = "~/Physics/References/DynamicPressure";

        [FDMiDataPath(nameof(DynamicPressurePath))]
        [Tooltip("全圧 [Pa]")]
        public FDMiFloat DynamicPressure;

        // --- outputs ---
        // fuel flow [kg/s]
        [Tooltip("燃料流量の出力パス")]
        public string fuelPath = "FuelFlow";

        [FDMiDataPath(nameof(fuelPath))]
        [Tooltip("燃料流量 [kg/s]")]
        public FDMiFloat fuelFlow;

        // ITT[K]: HPT exit (tt5). Thermocouple-measured -> 2-node sensor lag in FixedUpdate.
        [Tooltip("ITT の出力パス")]
        public string ittPath = "ITT";

        [FDMiDataPath(nameof(ittPath))]
        [Tooltip("ITT（HPT 出口温度）[K]")]
        public FDMiFloat ITT;

        // EGT[K]: LPT exit (tt15). Thermocouple-measured (CF6=T49, JT9D=Tt7) -> 2-node sensor lag in FixedUpdate.
        [Tooltip("EGT の出力パス")]
        public string egtPath = "EGT";

        [FDMiDataPath(nameof(egtPath))]
        [Tooltip("EGT（LPT 出口温度）[K]")]
        public FDMiFloat EGT;

        // fan spool
        [Tooltip("N1 の出力パス")]
        public string n1Path = "N1";

        [FDMiDataPath(nameof(n1Path))]
        [Tooltip("ファンスプール回転 [定格比]")]
        public FDMiFloat n1;

        // core spool
        [Tooltip("N2 の出力パス")]
        public string n2Path = "N2";

        [FDMiDataPath(nameof(n2Path))]
        [Tooltip("コアスプール回転 [定格比]")]
        public FDMiFloat n2;

        #region "Design parameters"
        [Header("Spool transient (first-order lag)")]
        [Tooltip("HP スプールの時定数 [s]")]
        public float tauN2 = 0.4f; // HP spool lag [s]: N2^3 target-reservoir lag (rate only, steady-state free)
        [Tooltip("ファンスプールの時定数 [s]")]
        public float tauN1 = 0.75f; // fan spool lag [s]: N1^2 target-reservoir lag (rate only, steady-state free)
        [Tooltip("スロットル 0 のバルブ開度")]
        public float fuelIdle = 0.10f; // valve opening at thr=0; idle N2 emerges as (fuelIdle·ign)^(1/3) (load ∝ N³). 燃料比 = 回転³ の恒等式。
        [Tooltip("スターターの出力。N2 の目標回転")]
        public float starterAccel = 0.3f; // starter output rating (motoring N2 target; spool-up shaped by tauStarter)
        [Tooltip("スターターの立ち上がり時定数 [s]")]
        public float tauStarter = 1.8f; // starter spool-up lag [s]: air-starter/duct fill (rate only, steady-state free)
        [Tooltip("ラム圧のスプール駆動効率")]
        public float etaWind = 0.07f; // fraction of ram enthalpy driving the spools (M0.8 cruise -> N1~0.21)
        [Tooltip("ラム空気の時定数 [s]")]
        public float tauRam = 5.0f; // ram-air reservoir lag [s]: bus jitter absorbed before the cbrt map (rate only)
        [Tooltip("回転停止の解除しきい値 [定格比]")]
        public float spoolStopN = 0.025f; // rotation-stop release [rating ratio]: latched 0 releases only above this
        [Tooltip("ITT/EGT のセンサー時定数 [s]")]
        public float tauProbe = 2.5f; // ITT/EGT sensor+metal lumped heat capacity [s] (shared)
        [Tooltip("点火完了度の S カーブ尺度")]
        public float lightOffN2 = 0.08f; // ignition S-curve scale [0,1]; idle で ign≈1 になる尺度(点火窓は N2 5–15%)
        [Tooltip("過回転クランプ倍率")]
        public float overspeedMargin = 1.20f; // overspeed clamp factor vs rating (N2/N1 clamp only)

        [Header("Spool transient (torque balance)")]
        [Tooltip("スターターカットアウト回転 [定格比]")]
        public float cutoutN2 = 0.42f; // starter cutout speed [rating ratio] (below fuel-on idle ~0.445 so a lit start cuts out; calibrate, do not reuse other engines)
        [Tooltip("軸受・風損の係数")]
        public float spoolFric = 0.02f; // bearing/windage coefficient (dimensionless torque at N=1; Hashmi §4.1 loss factor)
        [Tooltip("LP タービンからコアへの駆動結合")]
        public float ltCoupling = 1.0f; // LP turbine -> HP core power coupling (N1=1 at rating by design anchor)
        [Tooltip("スロットル上限 [定格比]")]
        public float thrMax = 1.2f; // throttle upper clamp (rating-ratio semantics; matches overspeedMargin)

        [Header("Engine design parameters")]
        [Tooltip("ファン直径 [m]")]
        public float fanDiameter = 2.19f;
        [Tooltip("ファン圧力比")]
        public float fanPR = 1.745f;
        [Tooltip("全体圧力比")]
        public float OPR = 29.3f;
        [Tooltip("バイパス比")]
        public float bypassRatio = 4.4f;

        // リバース対象の流路。高バイパス比のカスケード型はファン流のみ反転しコアは前向きを維持する (N2ReverseEnable=false)。
        // ターゲット型 (旧式低バイパス) はコア流も反転するため N2ReverseEnable=true。
        [Tooltip("N1 流路を反転する。カスケード型は true")]
        public bool N1ReverseEnable = true;
        [Tooltip("コア流路を反転する。ターゲット型は true")]
        public bool N2ReverseEnable = false;

        [Tooltip("バーナー圧力比")]
        public float burnerPR = 1.0f;
        [Tooltip("定格のバーナー出口温度 [degC]")]
        public float tt4maxC = 1096f; // burner exit at 100%

        // 区間定数近似: 圧縮側は cp が式中で相殺し温度不変 (γのみ寄与)、実効があるのは膨張側 ΔT = W/cp のみ。
        // EngineSim-UA 変数モード (cp(T) 3次式+燃料質量) との設計点差は 8-14K で tt4maxC 較定余裕に埋もれ、step 増分ゼロ。
        [Tooltip("膨張側燃焼ガスの比熱 [J/(kg K)]")]
        public float cpHot = 1220f; // 膨張側燃焼ガス平均比熱 [J/(kg K)] (燃料質量込み)

        // 断熱効率は設計値ではなく固定係数。調整しないので Inspector に出さない。焼き付け (FDMiJetEngineBake) が読む。
        [HideInInspector] public float etaFan = 1.0f;
        [HideInInspector] public float etaComp = 0.959f;
        [HideInInspector] public float etaBurn = 0.984f;
        [HideInInspector] public float etaTurb = 0.982f;
        [HideInInspector] public float etaNoz = 1.0f;

        // Editor の焼き付け結果。手で編集しないが永続化は必要なので public のまま隠す。
        [HideInInspector] public float corePR = 21.86f;
        [HideInInspector] public float _a8 = 0.22631f;
        [HideInInspector] public float coreAreaM2;
        [HideInInspector] public float _tt4dK = 1363.15f;
        [HideInInspector] public float _wfMax = 2.0f; // SL static full-throttle valve flow [kg/s] (Bake sets, provisional until baked)
        [HideInInspector] public float _dHfD = 49932f;  // 設計ファン比仕事 [J/kg-core] (Bake sets)
        [HideInInspector] public float _dHcD = 438593f; // 設計 HPC 比仕事 [J/kg] (Bake sets)
        #endregion

        #region "Constants"
        private const float R = 287.05f; // specific gas constant for air [J/(kg K)]
        private const float adiabaticFactor = 3.5f; // gamma/(gamma-1) (gamma = 1.4)
        private const float cpAir = 1005f; // 圧縮側冷間比熱 [J/(kg K)]
        #endregion

        // Bake 済み値 (_tt4dK/coreAreaM2/_a8/corePR/_dHfD/_dHcD) を使う。計算は Editor (FDMiJetEngineBake) のみ。

        void Start()
        {
            _sat = SAT.data;
            _staticPressure = StaticPressure.data;
            _mach = Mach.data;
            _oat = OAT.data;
            _dynamicPressure = DynamicPressure.data;
            _tas = TAS.data;
            _throttle = Throttle.data;
            _starter = Starter.data;
            _fuelSw = FuelSW.data;
            _reverser = Reverser.data;
            _fuelFlow = fuelFlow.data;
            _itt = ITT.data;
            _egt = EGT.data;
            _n1Arr = n1.data;
            _n2Arr = n2.data;
        }

        float _n1 = 0f;
        float _n2 = 0f;
        float _c2 = 0f; // HP lag state: N2^3 target reservoir (fuel/ignition/starter), lagged by tauN2
        float _c1 = 0f; // LP lag state: N1^2 target reservoir (core drive), lagged by tauN1
        float _ramC2 = 0f; // HP ram reservoir (windmill), lagged by tauN2
        float _ramC1 = 0f; // LP ram reservoir (windmill), lagged by tauN1
        float _starterC = 0f; // starter output reservoir (spool-up lag), lagged by tauStarter
        float _thrEff = 0f;
        float _ittS = 288.15f; // ITT sensor/metal node [K]
        float _ittGas = 288.15f; // SolveThrust writes gas node (tt5) [K]
        float _egtS = 288.15f; // EGT sensor/metal node [K]
        float _egtGas = 288.15f; // SolveThrust writes gas node (tt15) [K]

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // rating-ratio semantics: throttle >1 allows >100% (upper bound thrMax, N protected by overspeedMargin)
            float thr = Mathf.Clamp(_throttle[0], 0f, thrMax);
            float tt0 = _oat[0];
            float ps0 = _staticPressure[0];
            float pt0 = _dynamicPressure[0];

            // discrete inputs only (valves are physical). No state-dependent branch.
            float fuelV = _fuelSw[0] ? 1f : 0f;
            float starterV = _starter[0] ? 1f : 0f;

            float n2sq = _n2 * _n2;

            // [T1] valve opening u in [fuelIdle, 1] (fuel command)
            float u = fuelV * (fuelIdle + (1f - fuelIdle) * thr);

            // [T2] ignition completeness: threshold-free S-curve (starter spins N2 into self-light)
            float ign = n2sq / (n2sq + lightOffN2 * lightOffN2);

            // [T3] ram total-temperature rise ratio: (Pt0/Ps0)^((g-1)/g) - 1 = Tt0/T0 - 1
            float ramH = Mathf.Pow(Mathf.Max(1f, pt0 / Mathf.Max(1f, ps0)), 0.285714f) - 1f;

            // -- HP spool (N2): target reservoir + first-order lag, then static map N = cbrt(C) --
            // cutout/friction are evaluated on the RAW (pre-latch) speed so the stop latch
            // introduces no discontinuity into the reservoir feed (else land->recharge->release limit cycle).
            // Steady state: C = u*ign + starterC*cutout(N) - fric*N + ramC = N^3 (same roots as before).
            // tauN2 sets the rate only (TR-515 eq43: N/Wf = KE/(1+TE*S)).
            float k2 = dt / (tauN2 + dt);
            _starterC += (starterAccel * starterV - _starterC) * (dt / (tauStarter + dt));
            float n2raw = Mathf.Pow(Mathf.Clamp(_c2 + _ramC2, 0f, overspeedMargin * overspeedMargin * overspeedMargin), 0.3333333f);
            float ct2 = u * ign + _starterC
                      * Mathf.Clamp01(1f - n2raw / Mathf.Max(cutoutN2, 1e-3f)) // starter (vanishes at cutout)
                      - spoolFric * n2raw; // bearing/windage inside the lag, on raw speed (no latch jump)
            _c2 += (ct2 - _c2) * k2;
            _ramC2 += (etaWind * ramH - _ramC2) * (dt / (tauRam + dt)); // ram-only lag: jitter absorbed pre-map
            float n2cand = Mathf.Pow(Mathf.Clamp(_c2 + _ramC2, 0f, overspeedMargin * overspeedMargin * overspeedMargin), 0.3333333f);
            // rotation-stop latch (single-sided release): latched 0 releases only above spoolStopN.
            // Spinning N always follows the candidate (no hold-to-zero: decay reaches 0 by itself).
            _n2 = _n2 <= 0f && n2cand < spoolStopN ? 0f : n2cand;

            // -- LP spool (N1): same structure (Gauss-Seidel: fresh N2). N1^2 = C1 + ramC1 --
            float k1 = dt / (tauN1 + dt);
            float n2sqNew = _n2 * _n2;
            float n1raw = Mathf.Sqrt(Mathf.Clamp(_c1 + _ramC1, 0f, overspeedMargin * overspeedMargin));
            _c1 += (ltCoupling * n2sqNew * n2sqNew - spoolFric * n1raw - _c1) * k1; // friction on raw speed
            _ramC1 += (etaWind * ramH * (1f + bypassRatio) - _ramC1) * (dt / (tauRam + dt)); // ram-only lag
            float n1cand = Mathf.Sqrt(Mathf.Clamp(_c1 + _ramC1, 0f, overspeedMargin * overspeedMargin));
            _n1 = _n1 <= 0f && n1cand < spoolStopN ? 0f : n1cand;

            _n1Arr[0] = _n1;
            _n2Arr[0] = _n2;

            // 準定常熱力学は実スプールで駆動 (TR-515 p.3-4)。Tt4/Tt4max ≈ N2/N2max の熱スロットル則。
            // 床は不要(根因2の修理で ITT が N2 に対して単調になる)。C12 gate も不要。
            _thrEff = fuelV * _n2;
            float thrustSS = SolveThrust();
            body.AddForceAtPosition(transform.forward * thrustSS, transform.position, ForceMode.Force);

            // metered valve flow (unchanged)
            float densCorr = (pt0 / 101325f) / Mathf.Sqrt(Mathf.Max(0.01f, tt0 / 288.15f));
            _fuelFlow[0] = u * _wfMax * densCorr;

            // ITT/EGT: gas node (SolveThrust writes _ittGas = tt5 / _egtGas = tt15, already throttle-scaled)
            //      -> sensor/metal single-node first-order lag. Blend by ign so crank (unlit) stays at OAT.
            float kProbe = dt / (tauProbe + dt);
            _ittS += (Mathf.Max(tt0 + (_ittGas - tt0) * ign, tt0) - _ittS) * kProbe;
            _itt[0] = _ittS;
            _egtS += (Mathf.Max(tt0 + (_egtGas - tt0) * ign, tt0) - _egtS) * kProbe;
            _egt[0] = _egtS;
        }

        // bus references (FDMiData .data arrays, bound in Start()). No arg structs (UdonSharp).
        float[] _throttle;
        bool[] _starter;
        bool[] _fuelSw;
        float[] _reverser;
        float[] _sat;
        float[] _staticPressure;
        float[] _mach;
        float[] _oat;
        float[] _dynamicPressure;
        float[] _tas;
        float[] _fuelFlow;
        float[] _itt;
        float[] _egt;
        float[] _n1Arr;
        float[] _n2Arr;

        float SolveThrust()
        {
            // free stream (ISA, SI)
            float ts0 = _sat[0];
            float ps0 = _staticPressure[0];
            float fmach = _mach[0];
            float tt0 = _oat[0];
            float pt0 = _dynamicPressure[0];
            float u0ms = _tas[0];

            // inlet (Mil-spec)
            float pr2 = 1f;
            if (fmach > 1f)
                pr2 = 1f - 0.075f * Mathf.Pow(fmach - 1f, 1.35f);
            float tt2 = tt0;
            float pt2 = pt0 * pr2;

            // ---- 圧縮機: 比仕事 ∝ N² (固定幾何)。設計比仕事を N² 則で縮尺する ----
            float dHf = _dHfD * _n1 * _n1;
            float tt13 = tt2 + dHf / cpAir;
            float tr13 = tt13 / tt2;
            float pr13 = Mathf.Pow(1f + etaFan * (tr13 - 1f), adiabaticFactor);
            float dHc = _dHcD * _n2 * _n2;
            float tt3 = tt13 + dHc / cpAir;
            float tr3 = tt3 / tt13;
            float pr3 = Mathf.Pow(1f + etaComp * (tr3 - 1f), adiabaticFactor);

            // ---- バーナ ----
            float tt4 = Mathf.Max(_tt4dK * _thrEff, tt3); // 燃料 OFF でも Tt4 ≥ Tt3 (数値ガード兼 物理下限)

            // ---- タービン: 軸仕事収支 (secant 廃止) ----
            float dHht = -dHc;                       // HPT は HPC を駆動 (負 = ガスが仕事を渡す)
            float tH = 1f + dHht / (cpHot * tt4);
            float tt5 = tt4 * tH;
            float pr5 = Mathf.Pow(1f - (1f / etaTurb) * (1f - tH), adiabaticFactor);
            float dHlt = -(1f + bypassRatio) * dHf;  // LPT はファン(+バイパス)を駆動
            float tL = 1f + dHlt / (cpHot * Mathf.Max(1f, tt5));
            float tt15 = tt5 * tL;
            float pr15 = Mathf.Pow(1f - (1f / etaTurb) * (1f - tL), adiabaticFactor);

            float pt13 = pt2 * pr13;
            float epr = pr15 * pr5 * burnerPR * pr3 * pr13; // PR7 = 1 (no AB)
            float etr = tt15 / tt2; // = (tt15/tt5)(tt5/tt4)(tt4/tt3)tr3 tr13 (telescoping、KISS 化)

            // performance: choked core flow, mdot in kg/s
            float pt8 = epr * pr2 * pt0;
            float eair = 0.04041f * _a8 * pt8 / Mathf.Sqrt(Mathf.Max(0.01f, etr * tt0));

            // core nozzle: F = mdot*V + (pe-ps)*A
            float fac = 1.0f / adiabaticFactor;
            float npr = pt8 / Mathf.Max(1e-6f, ps0);
            float uexit = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * etr * tt0 * etaNoz * (1f - Mathf.Pow(npr, -fac)))
            );
            float pexit = npr <= 1.893f ? ps0 : 0.52828f * pt8;
            // Reverser: 有効流路のグロス推力 (運動量+圧力項) を (1-2r) で反転 (r=0.5 で 0、r=1 で全反転)。
            // ラムドラグは反転させない (吸込は依然 V0 で入る): 全反転時の制動力 = 逆噴射グロス + ラム。
            float rev = Mathf.Clamp(_reverser[0], 0f, 1f);
            float coreRev = N2ReverseEnable ? 1f - 2f * rev : 1f;
            float fanRev = N1ReverseEnable ? 1f - 2f * rev : 1f;
            float fg = coreRev * (uexit + (pexit - ps0) * _a8 / Mathf.Max(1e-6f, eair));
            // fan nozzle
            float snpr = pt13 / Mathf.Max(1e-6f, ps0);
            float ues = Mathf.Sqrt(
                Mathf.Max(0f, 2f * R / Mathf.Max(1e-6f, fac) * tt13 * etaNoz * (1f - Mathf.Pow(snpr, -fac)))
            );
            float pfexit = snpr <= 1.893f ? ps0 : 0.52828f * pt13;
            fg += fanRev * (bypassRatio * ues + (pfexit - ps0) * bypassRatio * coreAreaM2 / Mathf.Max(1e-6f, eair));

            float dram = u0ms * (1f + bypassRatio);
            float fnet = fg - dram;
            float fnN = fnet * eair;

            // gas node for the 2-node heat capacity (FixedUpdate lags the sensor node).
            // Fuel output moved to metered valve flow in FixedUpdate (Bake-scaled _wfMax).
            _ittGas = tt5; // ITT プローブ: HPT 出口 (Tt5)
            _egtGas = tt15; // EGT プローブ: LPT 出口 (Tt15) — CF6=T49, JT9D=Tt7 の計測位置
            return fnN;
        }

    }
}
