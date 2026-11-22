using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    // §13.1。FDMiSyncPredict の純粋計算。実時間・シーン・ネットワークに依存しない。
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
        public void P04_減速時も軌道ブレンドは後退しない()
        {
            // 旧軌道: t=0, p=0, v=200 m/s。新軌道: t=250 ms, p=50000 mm, v=100 m/s。
            long[] prevAbs = { 0L, 0L, 0L };
            long[] latestAbs = { 50000L, 0L, 0L };
            long[] oldPose = new long[3];
            long[] newPose = new long[3];
            long[] blended = new long[3];

            long last = -1L;
            for (int now = 250; now <= 500; now += 5)
            {
                int oldAhead = now;
                int newAhead = now - 250;
                FDMiSyncPredict.PredictAbs(prevAbs, new Vector3(200f, 0f, 0f), Vector3.zero, oldAhead, oldPose);
                FDMiSyncPredict.PredictAbs(latestAbs, new Vector3(100f, 0f, 0f), Vector3.zero, newAhead, newPose);

                float u = FDMiSyncPredict.BlendU(now, 250, 250);
                FDMiSyncPredict.BlendAbs(oldPose, newPose, u, blended);

                if (last >= 0L)
                    Assert.GreaterOrEqual(blended[0], last, "旧/新軌道のブレンドで後退しない");
                last = blended[0];
            }
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
        public void P07_軌道ブレンドはuで旧新を線形補間する()
        {
            long[] from = { 0L, 0L, 0L };
            long[] to = { 1000L, 2000L, -3000L };
            long[] dst = new long[3];

            FDMiSyncPredict.BlendAbs(from, to, 0.25f, dst);
            Assert.AreEqual(250L, dst[0]);
            Assert.AreEqual(500L, dst[1]);
            Assert.AreEqual(-750L, dst[2]);

            FDMiSyncPredict.BlendAbs(from, to, 1f, dst);
            CollectionAssert.AreEqual(to, dst);
        }

        [Test]
        public void P08_加速は速度差を秒で割る()
        {
            Vector3 a = FDMiSyncPredict.AccelFromVelocities(
                new Vector3(10f, 0f, 0f), Vector3.zero, 250);
            Assert.AreEqual(new Vector3(40f, 0f, 0f), a, "10 m/s / 0.25 s = 40 m/s²");
        }

        [Test]
        public void P09_受信と表示は旧軌道から新軌道へ連続する()
        {
            // _IntegrateReceive → _ComputeViewPose の実経路。到着フレームで旧軌道の位置を維持し、
            // 減速サンプルでもフレーム間で後退しないことを固定する。
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

                a._ComputeViewPose(1299);
                Assert.AreEqual(59800L, a._poseMm[0], "サンプル 1 からの等速予測");

                Feed(a, 1250, 50000L, 200f, 1300);
                a._ComputeViewPose(1300);
                Assert.AreEqual(60000L, a._poseMm[0], "到着フレームは旧/新軌道が一致する");

                a._ComputeViewPose(1425);
                Assert.AreEqual(85000L, a._poseMm[0], "ブレンド中間でも等速");

                // 200 → 190 m/s (4 g) の現実的な減速。旧/新軌道のどちらも前進するため、
                // ブレンド経路も後退しない。
                Feed(a, 1500, 98750L, 190f, 1550);
                long last = a._poseMm[0];
                long worstBack = 0L;
                for (int now = 1550; now <= 1800; now += 10)
                {
                    a._ComputeViewPose(now);
                    long d = a._poseMm[0] - last;
                    if (d < worstBack) worstBack = d;
                    last = a._poseMm[0];
                }
                Assert.GreaterOrEqual(worstBack, -1L, "現実的な減速では後退しない (丸め 1 mm を許容)");
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
                a._ComputeViewPose(2000);
                Assert.Greater(a._poseMm[0], 0L, "アクティブに戻ると予測を再開する");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void Feed(FDMiDynamicObjectSync a, int stamp, long xMm, float vx, int nowMs)
        {
            a._recvAbs[0] = xMm; a._recvAbs[1] = 0L; a._recvAbs[2] = 0L;
            FDMiSyncMath.PackQuat(Quaternion.identity, a._recvRot);
            a._recvVel[0] = new Vector3(vx, 0f, 0f);
            a._recvStamp[0] = stamp;
            a._recvCount = 1;
            a._received = true;
            a._IntegrateReceive(nowMs);
        }
    }
}
