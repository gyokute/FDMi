using NUnit.Framework;
using UnityEngine;

namespace FDMi.sync.Editor.Tests
{
    /// <summary>
    /// 位置同期の仕様。細部 short(1cm 格子、short 全域 = 1 chunk) + 粗部 int(チャンクカウンタ)の
    /// 2 段分解が、どの距離でも 1cm 格子を保つことを実装参照で検証する。
    /// </summary>
    public class FDMiGeodeticMathTests
    {
        const float SemiMajorM = 6378137f;
        const float EccentricitySq = 0.00669438f;
        const float ChunkM = FDMiGeodeticReferencePoint.ChunkM;
        const int ChunkCounts = 65536;

        static FDMiGeodeticReferencePoint NewPoint(string name)
        {
            GameObject go = new GameObject(name);
            return go.AddComponent<FDMiGeodeticReferencePoint>();
        }

        static double Reconstruct(FDMiGeodeticReferencePoint p)
        {
            // 論理位置 = chunk * ChunkM + position (double で評価して float 誤差と分離する)
            return p._chunk.x * (double)ChunkM + p._position.x;
        }

        [Test]
        public void FineQuantizesToCentimeterGrid()
        {
            FDMiGeodeticReferencePoint point = NewPoint("fine");
            point.setPosition(new Vector3(1.2345678f, -0.9876543f, 0.004f));
            Assert.AreEqual(1.23f, point._position.x, 1e-6f);
            Assert.AreEqual(-0.99f, point._position.y, 1e-6f);
            Assert.AreEqual(0f, point._position.z, 1e-6f);
            Assert.AreEqual(Vector3Int.zero, point._chunk);
        }

        [Test]
        public void ChunkFoldIsLosslessWithinOneCentimeter()
        {
            FDMiGeodeticReferencePoint point = NewPoint("fold");
            float[] inputs = { 0f, 1.5f, -1.5f, ChunkM * 3f + 12.345f, -ChunkM * 3f - 12.345f, ChunkM * 0.5f - 0.01f };
            for (int i = 0; i < inputs.Length; i++)
            {
                point.setBaseChunk(Vector3Int.zero);
                point.setPosition(new Vector3(inputs[i], 0f, 0f));
                Assert.Less(Mathf.Abs(point._position.x), ChunkM * 0.5f, "cell component must stay inside half a chunk");
                Assert.AreEqual(inputs[i], Reconstruct(point), 0.005f, "input " + inputs[i]);
            }
        }

        [Test]
        public void MoonDistanceStaysOnCentimeterGrid()
        {
            // 月まで飛んでも細部は 1cm 格子のまま(chunk は int なので桁落ちしない)
            FDMiGeodeticReferencePoint point = NewPoint("moon");
            point.setBaseChunk(new Vector3Int(586547, -12, 34567));  // 約 384,400 km
            point.setPosition(new Vector3(0.123456f, -2.7f, 9.99f));
            Assert.AreEqual(0.12f, point._position.x, 1e-6f);
            Assert.AreEqual(-2.7f, point._position.y, 1e-6f);
            Assert.AreEqual(new Vector3Int(586547, -12, 34567), point._chunk);
            double expected = 586547 * (double)ChunkM + 0.12;
            Assert.AreEqual(expected, Reconstruct(point), 0.005);
        }

        [Test]
        public void FineResolutionDoesNotDegradeWithChunkMagnitude()
        {
            // 旧実装は MulKm(cell)+offset を float32 で実体化していたため距離に比例して細部が潰れた。
            // 2 段のまま扱えば、chunk が何であっても細部は 1cm を保つ。
            FDMiGeodeticReferencePoint near = NewPoint("near");
            FDMiGeodeticReferencePoint far = NewPoint("far");
            near.setBaseChunk(Vector3Int.zero);
            far.setBaseChunk(new Vector3Int(1000000, 1000000, 1000000));
            near.setPosition(new Vector3(0.123f, 0f, 0f));
            far.setPosition(new Vector3(0.123f, 0f, 0f));
            Assert.AreEqual(near._position.x, far._position.x, 1e-6f);
        }

        [Test]
        public void SyncedFieldsReproduceCentimeterValue()
        {
            FDMiGeodeticReferencePoint point = NewPoint("pack");
            point.setBaseChunk(new Vector3Int(1234, -5, 6));
            point.setPosition(new Vector3(1.005f, -2.994f, ChunkM * 2f + 3.33f));
            // 送信側の pack
            point.syncedChunk = point._chunk;
            point.syncedFineX = (short)Mathf.RoundToInt(point._position.x / FDMiGeodeticReferencePoint.FineUnitM);
            point.syncedFineY = (short)Mathf.RoundToInt(point._position.y / FDMiGeodeticReferencePoint.FineUnitM);
            point.syncedFineZ = (short)Mathf.RoundToInt(point._position.z / FDMiGeodeticReferencePoint.FineUnitM);
            Assert.AreEqual(point._chunk, point.syncedChunk);
            Assert.AreEqual(point._position.x, point.syncedFineM.x, 1e-6f);
            Assert.AreEqual(point._position.y, point.syncedFineM.y, 1e-6f);
            Assert.AreEqual(point._position.z, point.syncedFineM.z, 1e-6f);
            Assert.Less(Mathf.Abs(point.syncedFineM.z), ChunkM * 0.5f);
        }

        [Test]
        public void ChunkBoundaryWrapsFineWithoutLosingContinuity()
        {
            Assert.AreEqual(ChunkM, FDMiGeodeticReferencePoint.FineUnitM * ChunkCounts, 1e-3f);
            // short 全域 = 1 chunk なので、はみ出した分は次のチャンクへ繰り上がり、
            // セル内成分は short 域(-32768..32767 counts)に収まる
            FDMiGeodeticReferencePoint point = NewPoint("wrap");
            point.setBaseChunk(Vector3Int.zero);
            point.setPosition(new Vector3(ChunkM * 0.5f, 0f, 0f));  // 32768 counts
            Assert.AreEqual(1, point._chunk.x);
            Assert.AreEqual(-32768, Mathf.RoundToInt(point._position.x / FDMiGeodeticReferencePoint.FineUnitM));
            double before = Reconstruct(point);
            point.setPosition(new Vector3(point._position.x + 0.02f, 0f, 0f));
            Assert.AreEqual(1, point._chunk.x);
            Assert.AreEqual(before + 0.02, Reconstruct(point), 0.005);
        }

        [Test]
        public void ENUAtOriginMatchesBakedEcefBasis()
        {
            GameObject go = new GameObject("origin");
            FDMiEcefOrigin origin = go.AddComponent<FDMiEcefOrigin>();
            origin.originLatitudeDeg = 35.681236f;
            origin.originLongitudeDeg = 139.767125f;
            origin.originHeightM = 100f;
            origin.Bake();
            Vector3 offset = new Vector3(5000f, 1234f, -6789f);
            Vector3 viaQuaternion = origin.ENUAt(Vector3.zero) * offset;
            Vector3 viaBasis = new Vector3(
                Vector3.Dot(offset, origin.eastEcef),
                Vector3.Dot(offset, origin.upEcef),
                Vector3.Dot(offset, origin.northEcef));
            Assert.Less(Vector3.Distance(viaQuaternion, viaBasis), 0.01f);
        }

        [Test]
        public void ENUAtMoonDistanceKeepsBasisOrthonormal()
        {
            GameObject go = new GameObject("origin");
            FDMiEcefOrigin origin = go.AddComponent<FDMiEcefOrigin>();
            origin.originLatitudeDeg = 0f;
            origin.originLongitudeDeg = 0f;
            origin.Bake();
            Vector3 moonOffset = new Vector3(0f, 0f, 384400000f);
            Quaternion q = origin.ENUAt(moonOffset);
            Assert.IsFalse(float.IsNaN(q.x + q.y + q.z + q.w));
            Vector3 mapped = q * new Vector3(1f, 2f, 3f);
            Assert.Less(Mathf.Abs(mapped.magnitude - new Vector3(1f, 2f, 3f).magnitude), 1e-3f);
        }

        [Test]
        public void ShortPackedQuaternionRoundTripsWithinTwoSteps()
        {
            FDMiGeodeticReferencePoint point = NewPoint("quat");
            Quaternion q = Quaternion.Euler(12.3f, -45.6f, 78.9f);
            point.syncedRot = q;
            Assert.Less(Quaternion.Angle(point.syncedRot, q), 360f / 65535f * 2f);
        }

        [Test]
        public void PlayerCellRelativeViewKeepsNearbyObjectsNearZero()
        {
            FDMiGeodeticSyncManager manager = new GameObject("manager").AddComponent<FDMiGeodeticSyncManager>();
            FDMiGeodeticReferencePoint vehicle = NewPoint("vehicle");
            FDMiGeodeticPlayerPosition player = new GameObject("player").AddComponent<FDMiGeodeticPlayerPosition>();
            vehicle.syncManager = manager;
            vehicle.rootRefPoint = manager;
            manager.localPlayerPosition = player;
            player.syncManager = manager;

            vehicle.setBaseChunk(new Vector3Int(100, 0, 0));
            vehicle.setPosition(new Vector3(123f, 45f, 6f));
            player.setBaseChunk(new Vector3Int(100, 0, 0));
            Vector3 view = vehicle.getViewPosition();
            Assert.Less(view.magnitude, 200f);

            player.setBaseChunk(new Vector3Int(99, 0, 0));
            view = vehicle.getViewPosition();
            Assert.Less(Mathf.Abs(view.x - (123f + ChunkM)), 1f);
        }

        [Test]
        public void RootVehicleIsPinnedAtTheFrameOrigin()
        {
            // root 切替(着席): 車両がフレームそのものになり、常に原点・無回転に固定される。
            // 他はすべて車両からの相対位置になり、フレーム姿勢は車体の論理姿勢。
            FDMiGeodeticSyncManager manager = new GameObject("manager").AddComponent<FDMiGeodeticSyncManager>();
            FDMiGeodeticReferencePoint vehicle = NewPoint("vehicle");
            FDMiGeodeticWorldAnchor ground = new GameObject("ground").AddComponent<FDMiGeodeticWorldAnchor>();
            vehicle.syncManager = manager;
            ground.syncManager = manager;
            manager.localRootRefPoint = vehicle;

            vehicle.setBaseChunk(new Vector3Int(1000, 0, 0));
            vehicle.setPosition(new Vector3(200f, 5f, -100f));
            vehicle.setRotation(Quaternion.Euler(0f, 30f, 10f));
            ground.setBaseChunk(new Vector3Int(1001, 0, 0));   // 車両の 1 chunk 先(同じセル内成分)
            ground.setPosition(new Vector3(200f, 5f, -100f));

            Assert.AreEqual(Vector3.zero, vehicle.getViewPosition(), "root vehicle must sit exactly at the origin");
            Assert.AreEqual(ChunkM, ground.getViewPosition().x - vehicle.getViewPosition().x, 1f);
            // フレーム姿勢は車体の論理姿勢。車体自身は無回転で描かれ、世界はその分だけ逆に回る
            Assert.AreEqual(vehicle._rotation, manager.FrameRotationEcef);
            Assert.AreEqual(Quaternion.identity, manager.ToUnityRot(vehicle.getViewRotation()));
            Assert.AreEqual(0f, Quaternion.Angle(manager.ToUnityRot(ground.getViewRotation()), Quaternion.Inverse(vehicle._rotation)), 0.01f);
        }

        [Test]
        public void RootMotionMovesTheWorldNotTheVehicle()
        {
            // 毎フレーム root を固定する、が本質。機体が進んでも機体の描画位置は 0 のまま、
            // ワールド側がその分だけ後ろへ動く(相対幾何は真の論理距離のまま)。
            FDMiGeodeticSyncManager manager = new GameObject("manager").AddComponent<FDMiGeodeticSyncManager>();
            FDMiGeodeticReferencePoint vehicle = NewPoint("vehicle");
            FDMiGeodeticWorldAnchor ground = new GameObject("ground").AddComponent<FDMiGeodeticWorldAnchor>();
            vehicle.syncManager = manager;
            ground.syncManager = manager;
            manager.localRootRefPoint = vehicle;

            vehicle.setBaseChunk(new Vector3Int(10, 0, 0));
            vehicle.setPosition(new Vector3(100f, 0f, 0f));
            ground.setBaseChunk(Vector3Int.zero);
            ground.setPosition(new Vector3(1000f, 0f, 0f));

            Vector3 groundBefore = ground.getViewPosition();
            // 機体が 250 m 進む(論理位置が進み、フレーム原点もそこへ移る)
            vehicle.setPosition(new Vector3(vehicle._position.x + 250f, 0f, 0f));
            Assert.AreEqual(Vector3.zero, vehicle.getViewPosition(), "vehicle stays pinned at the origin");
            Assert.AreEqual(groundBefore.x - 250f, ground.getViewPosition().x, 0.05f, "world recedes by the travelled distance");
        }

        [Test]
        public void PinnedRootBodyAccumulatesPoseAndResetsIt()
        {
            // root 車体の物理は毎フレーム原点・無回転へ戻され、動いた分(位置と回転)は
            // 論理姿勢へ累算される。フレーム軸 = 車体ローカル軸なので、dynamics は
            // 重力・空力を FrameRotationEcef 経由でそのまま使える。
            FDMiGeodeticSyncManager manager = new GameObject("manager").AddComponent<FDMiGeodeticSyncManager>();
            GameObject vehicleGo = new GameObject("vehicle");
            FDMiGeodeticObjectSync vehicle = vehicleGo.AddComponent<FDMiGeodeticObjectSync>();
            vehicle.syncManager = manager;
            vehicle.body = vehicleGo.AddComponent<Rigidbody>();
            vehicle.body.useGravity = false;
            manager.localRootRefPoint = vehicle;

            // 並進だけ(無回転なので座標系は素直): 600 m 進んでも物理座標は原点のまま
            vehicle.setBaseChunk(new Vector3Int(7, 0, 0));
            vehicle.setPosition(new Vector3(300f, 0f, 0f));
            vehicle.body.position = new Vector3(600f, 0f, 0f);
            vehicle.PinRootBodyToFrame();
            Assert.AreEqual(Vector3.zero, vehicle.body.position, "body must be reset to the origin");
            Assert.AreEqual(7 * (double)ChunkM + 900.0, vehicle._chunk.x * (double)ChunkM + vehicle._position.x, 0.05);

            // 回転だけ: 累算されて体の姿勢は無回転に戻る
            vehicle.setRotation(Quaternion.identity);
            vehicle.body.rotation = Quaternion.Euler(0f, 5f, 0f);
            vehicle.PinRootBodyToFrame();
            Assert.AreEqual(Quaternion.identity, vehicle.body.rotation, "body attitude must be reset");
            Assert.AreEqual(5f, Quaternion.Angle(vehicle._rotation, Quaternion.identity), 0.01f, "rotation accumulates into the logical pose");
        }
    }
}
