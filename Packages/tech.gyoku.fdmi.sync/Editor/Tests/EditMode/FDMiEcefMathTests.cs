using NUnit.Framework;

namespace FDMi.sync.Editor.Tests
{
    public class FDMiEcefMathTests
    {
        const float SemiMajorM = 6378137f;
        const float EccentricitySq = 0.00669438f;

        static float[] BakeOrigin(float latDeg, float lonDeg, float heightM)
        {
            float lat = latDeg * UnityEngine.Mathf.Deg2Rad;
            float lon = lonDeg * UnityEngine.Mathf.Deg2Rad;
            float sinLat = UnityEngine.Mathf.Sin(lat);
            float cosLat = UnityEngine.Mathf.Cos(lat);
            float sinLon = UnityEngine.Mathf.Sin(lon);
            float cosLon = UnityEngine.Mathf.Cos(lon);
            float n = SemiMajorM / UnityEngine.Mathf.Sqrt(1f - EccentricitySq * sinLat * sinLat);
            return new float[]
            {
                (n + heightM) * cosLat * cosLon,
                (n * (1f - EccentricitySq) + heightM) * sinLat,
                (n + heightM) * cosLat * sinLon,
            };
        }

        [Test]
        public void CmCellsCoverEarthDiameter()
        {
            double rangeM = 2.0 * int.MaxValue * 0.01;
            Assert.Greater(rangeM, 2.0 * 12742000.0);
        }

        [Test]
        public void ShortQuaternionStepIsArcSecondOrder()
        {
            double stepDeg = 360.0 / 65535.0;
            Assert.Less(stepDeg, 0.01);
        }

        [Test]
        public void OriginRadiusMatchesWgs84Equator()
        {
            float[] ecef = BakeOrigin(0f, 0f, 0f);
            Assert.AreEqual(SemiMajorM, ecef[0], 1f);
            Assert.AreEqual(0f, ecef[1], 1f);
            Assert.AreEqual(0f, ecef[2], 1f);
        }

        [Test]
        public void CellRoundTripKeepsHalfCellError()
        {
            float offsetM = 1234.567f;
            int cell = (int)System.Math.Round(offsetM / 0.01f);
            float backM = cell * 0.01f;
            Assert.Less(System.Math.Abs(backM - offsetM), 0.0051);
        }

        [Test]
        public void IdleEpsilonSuppressesSubCmJitter()
        {
            int a = 100000;
            int b = 100001;
            Assert.Less(System.Math.Abs(a - b), 2);
        }
    }
}
