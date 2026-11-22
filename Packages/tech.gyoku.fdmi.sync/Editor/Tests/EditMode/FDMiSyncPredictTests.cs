using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    // §13.1。FDMiSyncPredict の純粋計算と、受信→表示の実経路。
    // テスト名は仕様文として読めること (t-wada の 4 象限: テスト = What)。
    public class FDMiSyncPredictTests
    {
        [Test]
        public void P01_定速予測はサンプルからの経過msをmmとして進む()
        {
            long[] p0 = { 0L, 0L, 0L };
            long[] dst = new long[3];
            FDMiSyncPredict.PredictAbs(p0, new Vector3(200f, 0f, 0f), Vector3.zero, 250, dst);
            Assert.AreEqual(50000L, dst[0], "200 m/s × 250 ms = 50 m = 50000 mm");
            Assert.AreEqual(0L, dst[1]);
            Assert.AreEqual(0L, dst[2]);
        }

        [Test]
        public void P02_等加速度予測は半分at二乗をmmで返す()
        {
            long[] p0 = { 0L, 0L, 0L };
            long[] dst = new long[3];
            // 10 m/s² × 1 s² / 2 = 5 m = 5000 mm。
            FDMiSyncPredict.PredictAbs(p0, Vector3.zero, new Vector3(10f, 0f, 0f), 1000, dst);
            Assert.AreEqual(5000L, dst[0]);
        }

        [Test]
        public void P03_コーストはHで連続しH足すDで凍結する()
        {
            long[] p0 = { 0L, 0L, 0L };
            long[] atH = new long[3];
            long[] justAfterH = new long[3];
            long[] atEnd = new long[3];
            long[] afterEnd = new long[3];
            Vector3 vel = new Vector3(100f, 0f, 0f);

            FDMiSyncPredict.PredictAbs(p0, vel, Vector3.zero, FDMiSyncPredict.COAST_START_MS, atH);
            FDMiSyncPredict.PredictAbs(p0, vel, Vector3.zero, FDMiSyncPredict.COAST_START_MS + 1, justAfterH);
            FDMiSyncPredict.PredictAbs(p0, vel, Vector3.zero,
                FDMiSyncPredict.COAST_START_MS + FDMiSyncPredict.COAST_DECAY_MS, atEnd);
            FDMiSyncPredict.PredictAbs(p0, vel, Vector3.zero,
                FDMiSyncPredict.COAST_START_MS + FDMiSyncPredict.COAST_DECAY_MS + 1000, afterEnd);

            Assert.AreEqual(100000L, atH[0], "H 時点は 100 m/s × 1 s");
            Assert.AreEqual(100100L, justAfterH[0], "H の 1 ms 後は約 100 mm だけ進む");
            Assert.AreEqual(150000L, atEnd[0], "H..H+D の面積は v·D/2");
            Assert.AreEqual(150000L, afterEnd[0], "H+D 以降は凍結");
        }

        [Test]
        public void P04_位置の予測速度は加速度とコーストで連続する()
        {
            Assert.AreEqual(110f, FDMiSyncPredict.PredictVel(
                new Vector3(100f, 0f, 0f), new Vector3(10f, 0f, 0f), 1000).x, 1e-3f);
            Assert.AreEqual(100f, FDMiSyncPredict.PredictVel(
                new Vector3(100f, 0f, 0f), Vector3.zero, 1000).x, 1e-3f);
            Assert.AreEqual(50f, FDMiSyncPredict.PredictVel(
                new Vector3(100f, 0f, 0f), Vector3.zero, 1500).x, 1e-3f);
            Assert.AreEqual(0f, FDMiSyncPredict.PredictVel(
                new Vector3(100f, 0f, 0f), Vector3.zero, 2001).x, 1e-3f);
        }

        [Test]
        public void P05_角速度は一秒あたりに正規化され720度毎秒でclampされる()
        {
            Quaternion q0 = Quaternion.identity;
            Quaternion q1 = Quaternion.AngleAxis(90f, Vector3.up);
            Vector3 omega = FDMiSyncPredict.AngVelPerSec(q0, q1, 250);
            Quaternion at500 = FDMiSyncPredict.PredictRot(q0, omega, 500);
            Assert.Less(Quaternion.Angle(at500, Quaternion.AngleAxis(180f, Vector3.up)), 1e-3f);

            // 100 ms で 170° は 1700°/s。720°/s に clamp される。
            Quaternion q170 = Quaternion.AngleAxis(170f, Vector3.up);
            Vector3 capped = FDMiSyncPredict.AngVelPerSec(q0, q170, 100);
            Quaternion at100 = FDMiSyncPredict.PredictRot(q0, capped, 100);
            Assert.AreEqual(72f, Quaternion.Angle(Quaternion.identity, at100), 1e-2f);
        }

        [Test]
        public void P06_加速反転と低速では加速度をゼロにする()
        {
            Vector3 raw = new Vector3(10f, 0f, 0f);
            Vector3 reversed = FDMiSyncPredict.SmoothAccel(raw, new Vector3(-5f, 0f, 0f), 250,
                new Vector3(10f, 0f, 0f));
            Assert.AreEqual(Vector3.zero, reversed, "反転時は bounce 防止でゼロ");

            Vector3 slow = FDMiSyncPredict.SmoothAccel(raw, Vector3.zero, 250, new Vector3(0.1f, 0f, 0f));
            Assert.AreEqual(Vector3.zero, slow, "停止扱いの速度ではゼロ");

            Vector3 same = FDMiSyncPredict.SmoothAccel(raw, Vector3.zero, 250, new Vector3(100f, 0f, 0f));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), same, "同方向は一次低域通過 (τ=250 ms, dt=250 ms)");
        }

        [Test]
        public void P07_姿勢の予測角速度は角加速度とコーストで連続する()
        {
            Vector3 omega = new Vector3(0f, Mathf.PI / 2f, 0f); // 90 deg/s
            Vector3 alpha = new Vector3(0f, -Mathf.PI, 0f);     // -180 deg/s²

            Vector3 at250 = FDMiSyncPredict.PredictAngVel(omega, alpha, 250);
            Assert.AreEqual(Mathf.PI / 4f, at250.y, 1e-3f, "0.25 s で 45 deg/s");

            Vector3 stopped = FDMiSyncPredict.PredictAngVel(omega, alpha, 1000);
            Assert.AreEqual(Vector3.zero, stopped, "0.5 s で停止した後はゼロ");

            Vector3 coast = FDMiSyncPredict.PredictAngVel(omega, Vector3.zero, 1500);
            Assert.AreEqual(Mathf.PI / 4f, coast.y, 1e-3f, "コースト中央は半分");
        }

        [Test]
        public void P08_加速は速度差を秒で割る()
        {
            Vector3 a = FDMiSyncPredict.AccelFromVelocities(
                new Vector3(10f, 0f, 0f), Vector3.zero, 250);
            Assert.AreEqual(new Vector3(40f, 0f, 0f), a, "10 m/s / 0.25 s = 40 m/s²");
        }

        [Test]
        public void P09_位置は平滑化しても前進し続ける()
        {
            GameObject go = new GameObject("sync");
            try
            {
                FDMiDynamicObjectSync a = go.AddComponent<FDMiDynamicObjectSync>();
                a._pilotGated = false;
                a._legacyPose = false;
                a._sendIntervalMs = 250;
                a._isLocalOwner = false;

                Feed(a, 1000, 0L, 200f, 1050);
                a._ComputeViewPose(1050);
                Assert.AreEqual(0L, a._poseMm[0]);

                long last = a._poseMm[0];
                long worstBack = 0L;
                bool fed = false;
                for (int now = 1066; now <= 1600; now += 16)
                {
                    if (!fed && now >= 1300)
                    {
                        Feed(a, 1250, 50000L, 200f, now);
                        fed = true;
                    }
                    a._ComputeViewPose(now);
                    long d = a._poseMm[0] - last;
                    if (d < worstBack) worstBack = d;
                    last = a._poseMm[0];
                }

                Assert.GreaterOrEqual(worstBack, -10L, "補正で大きく後退しない (1 mm 未満の丸めは許容)");
                Assert.Greater(a._poseMm[0], 50000L, "サンプル位置を越えて前進する");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void P10_パイロット非アクティブは最終サンプルでホールドする()
        {
            GameObject go = new GameObject("sync");
            try
            {
                FDMiDynamicObjectSync a = go.AddComponent<FDMiDynamicObjectSync>();
                a._pilotGated = true;
                a._pilotActive = false;
                a._legacyPose = false;
                a._sendIntervalMs = 250;

                Feed(a, 1000, 0L, 50f, 1050);
                a._ComputeViewPose(1050);

                a._ComputeViewPose(1300);
                Assert.AreEqual(0L, a._poseMm[0], "非アクティブ中は外挿しない");

                a._ComputeViewPose(2000);
                Assert.AreEqual(0L, a._poseMm[0], "保持は最終サンプルのまま");

                a._pilotActive = true;
                a._ComputeViewPose(2016);
                Assert.Greater(a._poseMm[0], 0L, "アクティブに戻ると予測を再開する");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void P11_角加速度は二次項で積分し停止時刻で止まる()
        {
            Quaternion q0 = Quaternion.identity;
            Vector3 omega = new Vector3(0f, Mathf.PI / 2f, 0f);
            Vector3 alpha = new Vector3(0f, -Mathf.PI, 0f);

            Quaternion atStop = FDMiSyncPredict.PredictRotAccel(q0, omega, alpha, 1000);
            Assert.AreEqual(22.5f, Quaternion.Angle(q0, atStop), 0.1f, "0.5 s で停止した 22.5 deg");

            Quaternion afterStop = FDMiSyncPredict.PredictRotAccel(q0, omega, alpha, 2000);
            Assert.AreEqual(22.5f, Quaternion.Angle(q0, afterStop), 0.1f, "停止後は凍結");

            Quaternion fromZero = FDMiSyncPredict.PredictRotAccel(
                q0, Vector3.zero, new Vector3(0f, Mathf.PI, 0f), 1000);
            Assert.AreEqual(90f, Quaternion.Angle(q0, fromZero), 0.1f, "0.5·α·t² = 90 deg");
        }

        [Test]
        public void P12_姿勢の開始は角速度の段差を作らない()
        {
            GameObject go = new GameObject("sync");
            try
            {
                FDMiDynamicObjectSync a = go.AddComponent<FDMiDynamicObjectSync>();
                a._pilotGated = false;
                a._legacyPose = false;
                a._sendIntervalMs = 250;
                a._isLocalOwner = false;

                Feed(a, 1000, 0L, 0f, 1050, Quaternion.identity);
                a._ComputeViewPose(1050);

                float previous = 0f;
                float maxRate = 0f;
                bool fed = false;
                for (int now = 1066; now <= 1400; now += 16)
                {
                    if (!fed && now >= 1300)
                    {
                        Feed(a, 1250, 0L, 0f, now, Quaternion.AngleAxis(7.5f, Vector3.up));
                        fed = true;
                    }
                    a._ComputeViewPose(now);
                    float angle = Quaternion.Angle(Quaternion.identity, a._poseRot);
                    float rate = (angle - previous) / 0.016f;
                    if (rate > maxRate) maxRate = rate;
                    previous = angle;
                }

                Assert.Greater(maxRate, 0f, "動き出す");
                Assert.Less(maxRate, 45f, "開始の角速度は定常 30 deg/s の 1.5 倍未満");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void P13_ステップ停止の前進は1サンプル区間以内で戻りは滑らか()
        {
            GameObject go = new GameObject("sync");
            try
            {
                FDMiDynamicObjectSync a = go.AddComponent<FDMiDynamicObjectSync>();
                a._pilotGated = false;
                a._legacyPose = false;
                a._sendIntervalMs = 250;
                a._isLocalOwner = false;

                Feed(a, 1000, 0L, 0f, 1050, Quaternion.identity);
                a._ComputeViewPose(1050);

                int[] stamps = { 1250, 1500, 1750, 2000, 2250 };
                float[] angles = { 7.5f, 15f, 22.5f, 30f, 30f };
                int si = 0;
                float lastAngle = 0f;
                float maxAngle = 0f;
                float maxBack = 0f;

                for (int now = 1060; now <= 2600; now += 10)
                {
                    if (si < stamps.Length && now >= stamps[si] + 50)
                    {
                        Feed(a, stamps[si], 0L, 0f, now,
                            Quaternion.AngleAxis(angles[si], Vector3.up));
                        si++;
                    }
                    a._ComputeViewPose(now);
                    float angle = Quaternion.Angle(Quaternion.identity, a._poseRot);
                    if (now > 2300)
                    {
                        if (angle - lastAngle < maxBack) maxBack = angle - lastAngle;
                        if (angle > maxAngle) maxAngle = angle;
                    }
                    lastAngle = angle;
                }

                Assert.Less(maxAngle - 30f, 10f, "ステップ停止の前進は 1 サンプル区間 (30 deg/s × 250 ms = 7.5 deg) 程度");
                Assert.Greater(maxBack, -1f, "逆回転は 1 frame あたり 1 deg 未満");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void P15_段階的な停止は戻りを3度未満に抑える()
        {
            GameObject go = new GameObject("sync");
            try
            {
                FDMiDynamicObjectSync a = go.AddComponent<FDMiDynamicObjectSync>();
                a._pilotGated = false;
                a._legacyPose = false;
                a._sendIntervalMs = 250;
                a._isLocalOwner = false;

                Feed(a, 1000, 0L, 0f, 1050, Quaternion.identity);
                a._ComputeViewPose(1050);

                int[] stamps = { 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 3250, 3500, 3750 };
                float[] angles =
                {
                    7.5f, 15f, 22.5f, 30f, 33.75f, 35.625f, 36.5625f, 37.03125f,
                    37.265625f, 37.3828125f, 37.3828125f
                };
                float finalAngle = 37.3828125f;
                int si = 0;
                float lastAngle = 0f;
                float maxAngle = 0f;
                float maxBack = 0f;

                for (int now = 1060; now <= 4200; now += 10)
                {
                    if (si < stamps.Length && now >= stamps[si] + 50)
                    {
                        Feed(a, stamps[si], 0L, 0f, now,
                            Quaternion.AngleAxis(angles[si], Vector3.up));
                        si++;
                    }
                    a._ComputeViewPose(now);
                    float angle = Quaternion.Angle(Quaternion.identity, a._poseRot);
                    if (now > 3800)
                    {
                        if (angle - lastAngle < maxBack) maxBack = angle - lastAngle;
                        if (angle > maxAngle) maxAngle = angle;
                    }
                    lastAngle = angle;
                }

                Assert.Less(maxAngle - finalAngle, 3f, "段階的な停止の戻りは 3 deg 未満");
                Assert.Greater(maxBack, -1f, "逆回転は 1 frame あたり 1 deg 未満");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void P14_位置の誤差と平滑化はmとsで扱う()
        {
            long[] raw = { 1000L, 0L, 0L };
            long[] pose = { 0L, 0L, 0L };
            Assert.AreEqual(1f, FDMiSyncPredict.PositionErrorM(raw, pose).x, 1e-4f);

            Vector3 full = FDMiSyncPredict.SmoothVec(
                Vector3.zero, new Vector3(10f, 0f, 0f), 4f, 250);
            Assert.AreEqual(10f, full.x, 1e-4f, "4/s × 250 ms で 1 区間ぶん");

            Vector3 part = FDMiSyncPredict.SmoothVec(
                Vector3.zero, new Vector3(10f, 0f, 0f), 4f, 25);
            Assert.AreEqual(1f, part.x, 1e-4f);
        }

        static void Feed(FDMiDynamicObjectSync a, int stamp, long xMm, float vx, int nowMs)
        {
            Feed(a, stamp, xMm, vx, nowMs, Quaternion.identity);
        }

        static void Feed(FDMiDynamicObjectSync a, int stamp, long xMm, float vx, int nowMs, Quaternion rot)
        {
            a._recvAbs[0] = xMm; a._recvAbs[1] = 0L; a._recvAbs[2] = 0L;
            FDMiSyncMath.PackQuat(rot, a._recvRot);
            a._recvVel[0] = new Vector3(vx, 0f, 0f);
            a._recvStamp[0] = stamp;
            a._recvCount = 1;
            a._received = true;
            a._IntegrateReceive(nowMs);
        }
    }
}
