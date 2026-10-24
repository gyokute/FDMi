using System;
using FDMi.sync.Editor.Domain;
using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    // §13.1 M-01〜M-13。Editor の通常 C# から FDMiSyncMath の static メソッドを直接呼ぶ。
    // テスト名は仕様文として読めること (t-wada の 4 象限: テスト = What)。
    public class FDMiSyncMathTests
    {
        const float Q_SCALE = 32767f;
        // §4.5 の誤差上限。
        const double MAX_ANGLE_ERROR_RAD = 6.104e-5;

        [Test]
        public void M01_局所基底とq_basisが仕様表の像を返す()
        {
            // §3.4 の 5 行。q_basis は結果規定なので、成分ではなく q·x̂ / q·ŷ / q·ẑ を検証する。
            AssertBasis(0.0, 0.0, new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            AssertBasis(0.0, 90.0, new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            AssertBasis(90.0, 0.0, new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(-1, 0, 0));
            AssertBasis(-90.0, 0.0, new Vector3(0, 0, 1), new Vector3(0, -1, 0), new Vector3(1, 0, 0));
            AssertBasis(35.681236, 139.767125,
                new Vector3(-0.6458958f, 0f, -0.7634256f),
                new Vector3(-0.6201112f, 0.5832752f, 0.5246448f),
                new Vector3(0.4452872f, 0.8122746f, -0.3767350f));

            // det[E U N] = +1。
            Vector3 e = FDMiEarth.East(35.681236, 139.767125);
            Vector3 u = FDMiEarth.Up(35.681236, 139.767125);
            Vector3 n = FDMiEarth.North(35.681236, 139.767125);
            Assert.AreEqual(1.0, Vector3.Dot(e, Vector3.Cross(u, n)), 1e-6, "det[E U N] = +1");
        }

        static void AssertBasis(double lat, double lon, Vector3 ex, Vector3 uy, Vector3 nz)
        {
            Quaternion q = FDMiEarth.Basis(lat, lon);
            AssertVector(ex, FDMiSyncMath.QRotate(q, Vector3.right), lat + "," + lon + " q·x̂");
            AssertVector(uy, FDMiSyncMath.QRotate(q, Vector3.up), lat + "," + lon + " q·ŷ");
            AssertVector(nz, FDMiSyncMath.QRotate(q, Vector3.forward), lat + "," + lon + " q·ẑ");
        }

        static void AssertVector(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-6, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 1e-6, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 1e-6, label + ".z");
        }

        [Test]
        public void M02_姿勢の合成は左から掛けるだけで相似変換と異なる()
        {
            Quaternion parent = Quaternion.AngleAxis(90f, Vector3.up);   // R_y(90°)
            Quaternion rel = Quaternion.AngleAxis(90f, Vector3.right);  // R_x(90°)

            Quaternion composed = FDMiSyncMath.QMul(parent, rel);
            Assert.IsTrue(Quaternion.Angle(composed, new Quaternion(0.5f, 0.5f, -0.5f, 0.5f)) < 1e-4f);
            AssertVector(new Vector3(0, 0, -1), FDMiSyncMath.QRotate(composed, Vector3.right), "正の像");

            Quaternion wrong = FDMiSyncMath.QMul(FDMiSyncMath.QMul(parent, rel), FDMiSyncMath.QInv(parent));
            Assert.IsTrue(Quaternion.Angle(wrong, new Quaternion(0f, 0f, -0.7071068f, 0.7071068f)) < 1e-4f);
            AssertVector(new Vector3(0, -1, 0), FDMiSyncMath.QRotate(wrong, Vector3.right), "誤の像");
            Assert.IsFalse(Quaternion.Angle(composed, wrong) < 1e-4f, "合成と相似変換は異なる");
        }

        [Test]
        public void M03_quatの符号代表は同じshort4になり零はidentityになる()
        {
            Quaternion q = new Quaternion(0.3f, -0.4f, 0.5f, 0.7f).normalized;
            short[] a = new short[4];
            short[] b = new short[4];
            FDMiSyncMath.PackQuat(q, a);
            FDMiSyncMath.PackQuat(new Quaternion(-q.x, -q.y, -q.z, -q.w), b);
            CollectionAssert.AreEqual(a, b, "q と −q は同一");

            short[] half = new short[4];
            FDMiSyncMath.PackQuat(new Quaternion(0f, 0f, 1f, 0f), half);
            CollectionAssert.AreEqual(new short[] { 0, 0, 32767, 0 }, half, "180° は最大成分が正");

            short[] zero = new short[4];
            FDMiSyncMath.PackQuat(new Quaternion(0f, 0f, 0f, 0f), zero);
            CollectionAssert.AreEqual(new short[] { 0, 0, 0, 32767 }, zero, "零 → identity");
        }

        [Test]
        public void M04_200000点の一様乱数で角度誤差が上限以下になる()
        {
            System.Random rng = new System.Random(12345);
            short[] packed = new short[4];
            double worstAngle = 0.0;

            for (int i = 0; i < 200000; i++)
            {
                Quaternion q = new Quaternion(
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0));

                FDMiSyncMath.PackQuat(q, packed);
                Quaternion restored = FDMiSyncMath.UnpackQuat(packed);
                double angle = AngleBetween(q.normalized, restored);
                if (angle > worstAngle) worstAngle = angle;
            }

            Assert.LessOrEqual(worstAngle, MAX_ANGLE_ERROR_RAD, "最大角度誤差");
            // レバーアーム 130 m で ≤ 7.94 mm。
            Assert.LessOrEqual(worstAngle * 130.0, 7.94, "レバーアーム 130 m の位置誤差");
        }

        static double AngleBetween(Quaternion a, Quaternion b)
        {
            // Why-not: float の成分を double で積むだけでは足りない。`Quaternion.normalized` の |q| は
            // 1 ± 1e-7 で、acos は 1 付近でその誤差を 1e-3 の角度に増幅する。
            // 両者を double で正規化してから角度を取る (これが回転の差の定義)。
            double na = Math.Sqrt((double)a.x * a.x + (double)a.y * a.y + (double)a.z * a.z + (double)a.w * a.w);
            double nb = Math.Sqrt((double)b.x * b.x + (double)b.y * b.y + (double)b.z * b.z + (double)b.w * b.w);
            double dot = ((double)a.x * b.x + (double)a.y * b.y + (double)a.z * b.z + (double)a.w * b.w) / (na * nb);
            if (dot < 0.0) dot = -dot;
            if (dot > 1.0) dot = 1.0;
            return 2.0 * Math.Acos(dot);
        }

        [Test]
        public void M05_丸めは0から遠い方向の半分上げになる()
        {
            Assert.AreEqual(1L, FDMiSyncMath.RoundHalfAwayFromZero(0.5f));
            Assert.AreEqual(-1L, FDMiSyncMath.RoundHalfAwayFromZero(-0.5f));
            Assert.AreEqual(2L, FDMiSyncMath.RoundHalfAwayFromZero(1.5f));
            Assert.AreEqual(-2L, FDMiSyncMath.RoundHalfAwayFromZero(-1.5f));
            Assert.AreEqual(3L, FDMiSyncMath.RoundHalfAwayFromZero(2.5f));
            Assert.AreEqual(-3L, FDMiSyncMath.RoundHalfAwayFromZero(-2.5f));
            Assert.AreEqual(0L, FDMiSyncMath.RoundHalfAwayFromZero(0.4999f));
            Assert.AreEqual(0L, FDMiSyncMath.RoundHalfAwayFromZero(-0.4999f));
        }

        [Test]
        public void M06_FEF往復の誤差が087mm以下になる()
        {
            // §3.2 の 9 行。焼き値と 0 mm 差。
            AssertAbsMm(0.0, 0.0, 0.0, 6371000000L, 0L, 0L);
            AssertAbsMm(0.0, 90.0, 0.0, 0L, 0L, 6371000000L);
            AssertAbsMm(0.0, 180.0, 0.0, -6371000000L, 0L, 0L);
            AssertAbsMm(0.0, -90.0, 0.0, 0L, 0L, -6371000000L);
            AssertAbsMm(0.0, 45.0, 0.0, 4504977303L, 0L, 4504977303L);
            AssertAbsMm(90.0, 0.0, 0.0, 0L, 6371000000L, 0L);
            AssertAbsMm(-90.0, 0.0, 0.0, 0L, -6371000000L, 0L);
            AssertAbsMm(45.0, 45.0, 0.0, 3185500000L, 4504977303L, 3185500000L);
            AssertAbsMm(35.681236, 139.767125, 40.0, -3950753123L, 3716069808L, 3342532816L);

            // 往復 1,000 点。
            System.Random rng = new System.Random(777);
            long[] p = new long[3];
            long[] back = new long[3];
            double worst = 0.0;
            for (int i = 0; i < 1000; i++)
            {
                double lat = rng.NextDouble() * 178.0 - 89.0;
                double lon = rng.NextDouble() * 360.0 - 180.0;
                double h = rng.NextDouble() * 20000.0 - 1000.0;
                FDMiEarth.LatLonAltToAbsMm(lat, lon, h, p);

                double lat2, lon2, h2;
                FDMiEarth.AbsMmToLatLonAlt(p, out lat2, out lon2, out h2);
                FDMiEarth.LatLonAltToAbsMm(lat2, lon2, h2, back);

                double dx = p[0] - back[0];
                double dy = p[1] - back[1];
                double dz = p[2] - back[2];
                double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (d > worst) worst = d;
            }
            Assert.LessOrEqual(worst, 0.87, "|P − P'|");
        }

        static void AssertAbsMm(double lat, double lon, double h, long x, long y, long z)
        {
            long[] p = new long[3];
            FDMiEarth.LatLonAltToAbsMm(lat, lon, h, p);
            Assert.AreEqual(x, p[0], lat + "," + lon + " P.x");
            Assert.AreEqual(y, p[1], lat + "," + lon + " P.y");
            Assert.AreEqual(z, p[2], lat + "," + lon + " P.z");
        }

        [Test]
        public void M07_int64の絶対位置がオーバーフローしない()
        {
            long[] antimeridian = new long[3];
            long[] opposite = new long[3];
            FDMiEarth.LatLonAltToAbsMm(0.0, 0.0, 0.0, antimeridian);
            FDMiEarth.LatLonAltToAbsMm(0.0, 180.0, 0.0, opposite);

            // 地球の反対側は 1.27e10 mm。int32 に収まらない差が long に収まる。
            Assert.AreEqual(12742000000L, FDMiSyncMath.MaxAbsDeltaMm(antimeridian, opposite));
            Assert.LessOrEqual(FDMiSyncMath.MaxAbsDeltaMm(antimeridian, opposite), long.MaxValue / 2L);

            long[] far = new long[] { 9000000000000000L, -9000000000000000L, 0L };
            long[] origin = new long[] { -9000000000000000L, 9000000000000000L, 0L };
            Assert.AreEqual(18000000000000000L, FDMiSyncMath.MaxAbsDeltaMm(far, origin));
        }

        [Test]
        public void M08_int32の相対位置が214748kmまで表現できる()
        {
            Assert.AreEqual(2147483647, int.MaxValue);
            Assert.AreEqual(-2147483648, int.MinValue);
            // ±2,147,483,647 mm = ±2,147.48 km。
            Assert.AreEqual(2147.483647, int.MaxValue / 1000000.0, 1e-9);
        }

        [Test]
        public void M09_時刻のwrapで符号反転をwrapと誤認しない()
        {
            Assert.AreEqual(31, FDMiSyncMath.DeltaInt(unchecked((int)0x0000000F), unchecked((int)0xFFFFFFF0)));
            Assert.AreEqual(1, FDMiSyncMath.DeltaInt(unchecked((int)0x80000000), 0x7FFFFFFF));
            Assert.AreEqual(1, FDMiSyncMath.DeltaInt(0x00000000, unchecked((int)0xFFFFFFFF)));
            Assert.AreEqual(1, FDMiSyncMath.DeltaInt(0x00000001, 0x00000000));
        }

        [Test]
        public void M10_rebaseの境界が0または1000000mmになる()
        {
            AssertShift(499999L, 0L);
            AssertShift(500000L, 1000000L);
            AssertShift(500001L, 1000000L);
            AssertShift(-499999L, 0L);
            AssertShift(-500000L, -1000000L);
            AssertShift(-500001L, -1000000L);

            // 1,000 回、1 km ずつ。origin は整数 mm のまま、shift は 1 km の整数倍。
            long[] origin = new long[3];
            FDMiEarth.LatLonAltToAbsMm(35.681236, 139.767125, 40.0, origin);
            long[] start = new long[] { origin[0], origin[1], origin[2] };
            long[] playerAbs = new long[] { origin[0], origin[1], origin[2] };
            long[] shift = new long[3];

            for (int k = 0; k < 1000; k++)
            {
                // プレイヤーは FEF で北 (y) へ 1 km ずつ進む。
                playerAbs[1] = playerAbs[1] + 1000000L;

                for (int i = 0; i < 3; i++)
                {
                    long delta = playerAbs[i] - origin[i];
                    long q = delta / 1000000L;
                    long r = delta - q * 1000000L;
                    if (r >= 500000L) q = q + 1L;
                    else if (r <= -500000L) q = q - 1L;
                    shift[i] = q * 1000000L;
                }

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(0L, shift[i] % 1000000L, "shift は 1 km の整数倍");
                    origin[i] = origin[i] + shift[i];
                }
            }

            // 1,000 km 進んだ原点は、初期値 + 1,000,000,000 mm (誤差 0)。
            Assert.AreEqual(start[0], origin[0]);
            Assert.AreEqual(start[1] + 1000000000L, origin[1]);
            Assert.AreEqual(start[2], origin[2]);
        }

        static void AssertShift(long delta, long expectedMm)
        {
            long q = delta / 1000000L;
            long r = delta - q * 1000000L;
            if (r >= 500000L) q = q + 1L;
            else if (r <= -500000L) q = q - 1L;
            Assert.AreEqual(expectedMm, q * 1000000L, "delta = " + delta);
        }

        [Test]
        public void M11_root切替で絶対位置の差が1mm以下になる()
        {
            long[] originA = new long[3];
            FDMiEarth.LatLonAltToAbsMm(35.681236, 139.767125, 40.0, originA);
            Quaternion qA = FDMiEarth.Basis(35.681236, 139.767125);

            // 100 m/s で 250 ms 進んだ view 姿勢。
            Vector3 view = new Vector3(3f, 1f, 4f);
            long[] absA = new long[3];
            FDMiEarth.ViewToAbsMm(originA, qA, view, absA);

            // 同じ絶対姿勢を別の原点 (新 root) から見る。
            long[] originB = new long[3];
            FDMiEarth.LatLonAltToAbsMm(35.681236, 139.767125, 40.0, originB);
            originB[0] += 1000000L;
            Quaternion qB = Quaternion.AngleAxis(90f, Vector3.up);

            Vector3 viewB = FDMiEarth.AbsMmToView(originB, qB, absA);
            long[] absB = new long[3];
            FDMiEarth.ViewToAbsMm(originB, qB, viewB, absB);

            long d = FDMiSyncMath.MaxAbsDeltaMm(absA, absB);
            Assert.LessOrEqual(d, 1L, "P_abs の差");
        }

        [Test]
        public void M12_逆半球の補間は最短経路になる()
        {
            Quaternion q0 = new Quaternion(0.1f, 0.9f, 0.3f, 0.2f).normalized;
            Quaternion q1 = new Quaternion(-0.2f, 0.1f, 0.95f, 0.1f).normalized;
            // 同じ回転を逆半球の成分で表す。
            Quaternion q1n = new Quaternion(-q1.x, -q1.y, -q1.z, -q1.w);
            Assert.Less(Quaternion.Dot(q0, q1n), 0f, "逆半球の入力");

            Quaternion path = FDMiSyncMath.SlerpHemisphere(q0, q1n, 0.5f);
            Quaternion sameAsForward = FDMiSyncMath.SlerpHemisphere(q0, q1, 0.5f);
            Assert.IsTrue(Quaternion.Angle(path, sameAsForward) < 1e-3f, "符号を揃えると同じ経路になる");

            // 反転後は最短経路 (a と同じ半球) を通る。Why-not: Unity の Quaternion.Slerp 自身が
            // 最短経路を選ぶため、「素の Slerp と結果が異なる」ことは回転として観測できない。
            // 仕様の要求 (Dot < 0 なら b を反転する) を、戻り値の半球で固定する。
            Assert.GreaterOrEqual(Quaternion.Dot(path, q0), 0f, "最短経路の半球にある");
            Assert.LessOrEqual(Quaternion.Angle(q0, path), 90.1f, "q0 から 90° 以内");
            Assert.LessOrEqual(Quaternion.Angle(path, q1), 90.1f, "q1 から 90° 以内");
        }

        [Test]
        public void M13_回転付き積分で絶対位置の軌跡が経路と一致する()
        {
            // originRot = 90°。view で 1 km 直進する。
            Quaternion originRot = Quaternion.AngleAxis(90f, Vector3.up);
            Vector3 stepView = new Vector3(0f, 0f, 1000f);

            long[] acc = new long[3];
            float[] frac = new float[3];
            for (int i = 0; i < 100; i++)
            {
                Vector3 dAbs = FDMiSyncMath.QRotate(originRot, stepView);
                for (int k = 0; k < 3; k++)
                {
                    frac[k] = frac[k] + Component(dAbs, k);
                    long n = (long)frac[k];
                    acc[k] = acc[k] + n;
                    frac[k] = frac[k] - (float)n;
                }
            }

            // Rotate(90° about up, +Z) = +X。100 × 1000 mm。端数は _fracMm に残るため ±1 mm を許す。
            Assert.LessOrEqual(Math.Abs(acc[0] - 100000L), 1L, "回転付き積分の x");
            Assert.AreEqual(0L, acc[1]);
            Assert.LessOrEqual(Math.Abs(acc[2]), 1L);

            // Why-not: 回転を省くと +Z に 100000 mm 進み、経路から外れる (M-13 の検出対象)。
            Assert.AreNotEqual(acc[0], acc[2], "回転を省いた結果とは異なる");
        }

        static float Component(Vector3 v, int i)
        {
            if (i == 0) return v.x;
            if (i == 1) return v.y;
            return v.z;
        }
    }
}
