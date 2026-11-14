using NUnit.Framework;

namespace FDMi.core.Editor.Tests
{
    /// <summary>
    /// 同期グループのチャネル配置と量子化の仕様。
    /// 配置が語をまたぐと shift/mask が壊れ、量子化の端点がずれると同期値が往復しない。
    /// </summary>
    public class FDMiDataSyncGroupTests
    {
        static readonly byte[] YokeWidths = { 8, 8, 8 };

        // MC-10 OverHead: Bool×2 + U8×4 + Bool×4 を 2 語に詰める。
        static readonly byte[] OverHeadWidths = { 1, 8, 8, 8, 1, 1, 1, 1, 8, 1 };

        // MC-10 FGCP: Bool×3 + U8×2 + U16×5 + S8 + U32×2 を 6 語に詰める。
        static readonly byte[] FgcpWidths = { 1, 8, 8, 1, 1, 16, 16, 16, 16, 16, 8, 32, 32 };

        // MC-10 Pedestal: U8×6 + S8 + Bool×3 を 2 語に詰める。
        static readonly byte[] PedestalWidths = { 8, 8, 8, 8, 8, 8, 8, 1, 1, 1 };

        [Test]
        public void 全チャネルが単一の語に収まる()
        {
            AssertNoChannelStraddlesWord(OverHeadWidths);
            AssertNoChannelStraddlesWord(FgcpWidths);
            AssertNoChannelStraddlesWord(PedestalWidths);
        }

        [Test]
        public void 語数はチャネル幅の合計を32bit境界へ切り上げたもの()
        {
            Assert.That(FDMiDataSyncGroup.ComputeWordCount(YokeWidths), Is.EqualTo(1));
            Assert.That(FDMiDataSyncGroup.ComputeWordCount(OverHeadWidths), Is.EqualTo(2));
            Assert.That(FDMiDataSyncGroup.ComputeWordCount(PedestalWidths), Is.EqualTo(2));
            Assert.That(FDMiDataSyncGroup.ComputeWordCount(FgcpWidths), Is.EqualTo(6));
        }

        [Test]
        public void チャネルが無くても1語を返す()
        {
            Assert.That(FDMiDataSyncGroup.ComputeWordCount(new byte[0]), Is.EqualTo(1));
        }

        [Test]
        public void バイト幅チャネルはバイト境界に並ぶ()
        {
            var words = new byte[3];
            var shifts = new byte[3];
            int count = FDMiDataSyncGroup.ComputeLayout(YokeWidths, words, shifts);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(words, Is.EqualTo(new byte[] { 0, 0, 0 }));
            Assert.That(shifts, Is.EqualTo(new byte[] { 0, 8, 16 }));
        }

        [Test]
        public void ワード半幅チャネルは十六ビット境界に揃う()
        {
            var widths = new byte[] { 1, 16 };
            var words = new byte[2];
            var shifts = new byte[2];
            FDMiDataSyncGroup.ComputeLayout(widths, words, shifts);

            Assert.That(shifts[0], Is.EqualTo(0));
            Assert.That(shifts[1], Is.EqualTo(16));
        }

        [Test]
        public void Unitは0から1を0から255へ写す()
        {
            byte scale = FDMiDataSyncGroup.ScaleUnit;
            Assert.That(FDMiDataSyncGroup.EncodeValue(0f, scale, 8, 0f, 1f), Is.EqualTo(0u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(0.5f, scale, 8, 0f, 1f), Is.EqualTo(128u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(1f, scale, 8, 0f, 1f), Is.EqualTo(255u));
            Assert.That(
                FDMiDataSyncGroup.DecodeValue(255u, scale, 8, 0f, 1f),
                Is.EqualTo(1f).Within(1e-6f)
            );
        }

        [Test]
        public void Unitは範囲外をクランプする()
        {
            byte scale = FDMiDataSyncGroup.ScaleUnit;
            Assert.That(FDMiDataSyncGroup.EncodeValue(-0.5f, scale, 8, 0f, 1f), Is.EqualTo(0u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(1.5f, scale, 8, 0f, 1f), Is.EqualTo(255u));
        }

        [Test]
        public void Signedは0を厳密に0へ写す()
        {
            byte scale = FDMiDataSyncGroup.ScaleSigned;
            Assert.That(FDMiDataSyncGroup.EncodeValue(0f, scale, 8, 0f, 1f), Is.EqualTo(0u));

            uint raw = FDMiDataSyncGroup.EncodeValue(-1f, scale, 8, 0f, 1f);
            Assert.That(raw, Is.EqualTo(129u)); // 0x81 = -127 の 8bit
            Assert.That(FDMiDataSyncGroup.SignExtend(raw, 8), Is.EqualTo(-127));
            Assert.That(
                FDMiDataSyncGroup.DecodeValue(raw, scale, 8, 0f, 1f),
                Is.EqualTo(-1f).Within(1e-6f)
            );
        }

        [Test]
        public void RawSは符号拡張して往復する()
        {
            byte scale = FDMiDataSyncGroup.ScaleRawS;
            uint raw = FDMiDataSyncGroup.EncodeValue(-45f, scale, 8, 0f, 0f);

            Assert.That(raw, Is.EqualTo(211u)); // -45 の 8bit = 0xD3
            Assert.That(FDMiDataSyncGroup.SignExtend(raw, 8), Is.EqualTo(-45));
        }

        [Test]
        public void 負値のビット化はマスクしてからuintへ写す()
        {
            // Udon の int→uint 変換は負値で例外になるため、EncodeValue は負値を直接キャストしない。
            Assert.That(FDMiDataSyncGroup.ToBits(-1, 8), Is.EqualTo(255u));
            Assert.That(FDMiDataSyncGroup.ToBits(-127, 8), Is.EqualTo(129u));
            Assert.That(FDMiDataSyncGroup.ToBits(-45, 8), Is.EqualTo(211u));
            Assert.That(FDMiDataSyncGroup.ToBits(200, 8), Is.EqualTo(200u));
            Assert.That(FDMiDataSyncGroup.ToBits(-1, 16), Is.EqualTo(65535u));
            Assert.That(FDMiDataSyncGroup.ToBits(-1, 32), Is.EqualTo(0xFFFFFFFFu));
        }

        [Test]
        public void ビット列から符号付き整数へ復元する()
        {
            Assert.That(FDMiDataSyncGroup.SignExtend(129u, 8), Is.EqualTo(-127));
            Assert.That(FDMiDataSyncGroup.SignExtend(127u, 8), Is.EqualTo(127));
            Assert.That(FDMiDataSyncGroup.SignExtend(0xFFFFu, 16), Is.EqualTo(-1));
            Assert.That(FDMiDataSyncGroup.FromBits(0xFFFFFFFFu, 32), Is.EqualTo(-1));
            Assert.That(FDMiDataSyncGroup.FromBits(117950u, 32), Is.EqualTo(117950));
        }

        [Test]
        public void RawUは整数をそのまま保つ()
        {
            byte scale = FDMiDataSyncGroup.ScaleRawU;
            Assert.That(FDMiDataSyncGroup.EncodeValue(359f, scale, 16, 0f, 0f), Is.EqualTo(359u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(117950f, scale, 32, 0f, 0f), Is.EqualTo(117950u));
        }

        [Test]
        public void Rangeはminからmaxを全幅へ写す()
        {
            byte scale = FDMiDataSyncGroup.ScaleRange;
            Assert.That(FDMiDataSyncGroup.EncodeValue(0f, scale, 8, 0f, 35f), Is.EqualTo(0u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(35f, scale, 8, 0f, 35f), Is.EqualTo(255u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(17.5f, scale, 8, 0f, 35f), Is.EqualTo(128u));
            Assert.That(
                FDMiDataSyncGroup.DecodeValue(128u, scale, 8, 0f, 35f),
                Is.EqualTo(128f / 255f * 35f).Within(1e-4f)
            );
        }

        [Test]
        public void Boolはしきい値0_5で0か1になる()
        {
            byte scale = FDMiDataSyncGroup.ScaleBool;
            Assert.That(FDMiDataSyncGroup.EncodeValue(0.49f, scale, 1, 0f, 1f), Is.EqualTo(0u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(0.5f, scale, 1, 0f, 1f), Is.EqualTo(1u));
            Assert.That(FDMiDataSyncGroup.EncodeValue(1f, scale, 1, 0f, 1f), Is.EqualTo(1u));
        }

        static void AssertNoChannelStraddlesWord(byte[] widths)
        {
            var words = new byte[widths.Length];
            var shifts = new byte[widths.Length];
            FDMiDataSyncGroup.ComputeLayout(widths, words, shifts);

            for (int i = 0; i < widths.Length; i++)
            {
                int start = words[i] * 32 + shifts[i];
                int end = start + widths[i];
                int wordEnd = (words[i] + 1) * 32;
                Assert.That(
                    end,
                    Is.LessThanOrEqualTo(wordEnd),
                    $"channel {i} (width {widths[i]}) straddles word {words[i]}"
                );
            }
        }
    }
}
