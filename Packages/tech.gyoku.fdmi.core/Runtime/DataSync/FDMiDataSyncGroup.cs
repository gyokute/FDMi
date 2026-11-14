using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    // 1 人の操作者が同時に扱う値の集合（操作者パネル）を 1 つの UdonBehaviour にまとめて送る。
    // 同期の単位を「値」から「操作者パネル × 更新レート」へ上げることで、パケット数とヘッダを減らし、
    // 同一パネル内の値を同一パケットで届ける（値ごとの到着順の入れ替わりをなくす）。
    // 所有権は GameObject 単位なので、グループの GameObject が 1 人の操作者に対応する。
    // Why-not: 値ごとにコンポーネントを置かない。MC-10 では 44 behaviour / 最大 176 request/s になり、
    // 11 KB/s の予算と 10 request/s/object の目安に対して過剰だった。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiDataSyncGroup : FDMiDataSync
    {
        // チャネルの量子化規則。wire 上の整数への写し方だけを決める。
        public const byte ScaleRawU = 0; // 整数そのまま（符号なし幅）
        public const byte ScaleBool = 1; // 0/1 を 1 bit
        public const byte ScaleUnit = 2; // 0..1 を 0..255
        public const byte ScaleSigned = 3; // -1..1 を -127..127（0 が厳密に 0）
        public const byte ScaleRange = 4; // min..max を 0..(2^width-1)
        public const byte ScaleRawS = 5; // 整数そのまま（符号付き幅）

        // Editor が焼くチャネル定義。U# は直列化可能クラスを持てないため並列配列にする。
        [HideInInspector] public string[] channelPaths = new string[0];
        [HideInInspector] public byte[] channelWidths = new byte[0]; // 1 / 8 / 16 / 32 [bit]
        [HideInInspector] public byte[] channelScales = new byte[0];
        [HideInInspector] public float[] channelMins = new float[0];
        [HideInInspector] public float[] channelMaxs = new float[0];

        [HideInInspector]
        [FDMiRegisterCallback(nameof(OnDataChanged))]
        public FDMiData[] channels = new FDMiData[0];

        // 同期する語。長さは Editor が全クライアント共通の prefab 値として焼く。
        [UdonSynced, HideInInspector] public uint[] words = new uint[1];

        // チャネルの語内配置。Editor の焼き値から Start で導く（同期しない）。
        [System.NonSerialized] public byte[] channelWords;
        [System.NonSerialized] public byte[] channelShifts;
        // 受信時に変化したチャネルだけを FDMiData へ流すための前回値。
        [System.NonSerialized] public uint[] lastValues;
        [System.NonSerialized] public bool initialized;
        [System.NonSerialized] public bool whenDeserializing;

        void Start()
        {
            EnsureLayout();
        }

        public void OnDataChanged()
        {
            if (whenDeserializing)
                return;
            TrySerialize();
        }

        public override void OnPreSerialization()
        {
            EnsureLayout();
            Pack();
        }

        public override void OnDeserialization()
        {
            EnsureLayout();
            Unpack();
        }

        /// <summary>
        /// チャネル幅の列から必要な語数を返す。Editor の焼きと Runtime のレイアウトで同じ規則を使う。
        /// </summary>
        public static int ComputeWordCount(byte[] widths)
        {
            return ComputeLayout(widths, null, null);
        }

        /// <summary>
        /// チャネル幅の列から語内配置を導く。8bit は byte、16bit は 2byte、32bit は 4byte 境界に揃え、
        /// チャネルが語をまたがないようにする。wordIndex / shifts が null なら計算だけ行う。
        /// </summary>
        public static int ComputeLayout(byte[] widths, byte[] wordIndex, byte[] shifts)
        {
            int cursor = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                int w = widths[i];
                int align = w < 8 ? 1 : w;
                cursor = (cursor + align - 1) / align * align;
                if (wordIndex != null)
                    wordIndex[i] = (byte)(cursor >> 5);
                if (shifts != null)
                    shifts[i] = (byte)(cursor & 31);
                cursor += w;
            }
            int count = (cursor + 31) / 32;
            return count < 1 ? 1 : count;
        }

        void EnsureLayout()
        {
            int n = channels.Length;
            if (
                channelWords != null
                && channelWords.Length == n
                && lastValues != null
                && lastValues.Length == n
            )
                return;

            channelWords = new byte[n];
            channelShifts = new byte[n];
            lastValues = new uint[n];
            ComputeLayout(channelWidths, channelWords, channelShifts);
            initialized = false;
        }

        void Pack()
        {
            for (int w = 0; w < words.Length; w++)
                words[w] = 0u;

            for (int i = 0; i < channels.Length; i++)
            {
                if (!channels[i])
                    continue;
                words[channelWords[i]] |= Encode(i) << channelShifts[i];
            }
        }

        void Unpack()
        {
            whenDeserializing = true;
            for (int i = 0; i < channels.Length; i++)
            {
                if (!channels[i])
                    continue;

                uint raw = Mask(words[channelWords[i]] >> channelShifts[i], channelWidths[i]);
                if (initialized && raw == lastValues[i])
                    continue;

                lastValues[i] = raw;
                Apply(i, raw);
            }
            initialized = true;
            whenDeserializing = false;
        }

        uint Encode(int i)
        {
            return EncodeValue(
                channels[i].GetFloat(),
                channelScales[i],
                channelWidths[i],
                channelMins[i],
                channelMaxs[i]
            );
        }

        void Apply(int i, uint raw)
        {
            byte scale = channelScales[i];
            int width = channelWidths[i];

            if (scale == ScaleBool)
            {
                channels[i].Set(raw != 0 ? 1 : 0);
                return;
            }

            if (scale == ScaleRawU)
            {
                channels[i].Set(FromBits(raw, width));
                return;
            }

            if (scale == ScaleRawS)
            {
                channels[i].Set(SignExtend(raw, width));
                return;
            }

            channels[i].Set(DecodeValue(raw, scale, width, channelMins[i], channelMaxs[i]));
        }

        /// <summary>
        /// 量子化の唯一の実装。float の値を wire の整数へ写す。
        /// </summary>
        public static uint EncodeValue(float value, byte scale, int width, float min, float max)
        {
            if (scale == ScaleBool)
                return value >= 0.5f ? 1u : 0u;

            if (scale == ScaleRawU || scale == ScaleRawS)
                return ToBits(Mathf.RoundToInt(value), width);

            if (scale == ScaleUnit)
                return (uint)Mathf.RoundToInt(Mathf.Clamp(value, 0f, 1f) * 255f);

            if (scale == ScaleSigned)
                return ToBits(Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * 127f), width);

            float clamped = Mathf.Clamp(value, min, max);
            return (uint)Mathf.RoundToInt((clamped - min) / (max - min) * (float)MaxCode(width));
        }

        /// <summary>
        /// wire の整数を float へ戻す。Raw 系は呼び出し側が整数として扱う。
        /// </summary>
        public static float DecodeValue(uint raw, byte scale, int width, float min, float max)
        {
            if (scale == ScaleUnit)
                return raw / 255f;

            if (scale == ScaleSigned)
                return SignExtend(raw, width) / 127f;

            return min + (max - min) * (raw / (float)MaxCode(width));
        }

        /// <summary>
        /// width ビットの符号付き整数へ符号拡張する。width 32 はビット列をそのまま int として読む。
        /// </summary>
        public static int SignExtend(uint raw, int width)
        {
            if (width >= 32)
                return FromBits(raw, 32);
            int shift = 32 - width;
            return ((int)raw << shift) >> shift;
        }

        /// <summary>
        /// int の下位 width bit を uint に写す。
        /// Why-not: (uint)負値 を書かない。Udon の int→uint 変換は Convert.ToUInt32 に落ち、
        /// 負値で例外を投げて UdonBehaviour が停止する（C# の unchecked キャストと違う）。
        /// マスクして非負にしてから変換すれば 2 の補数のビット列は同じになる。
        /// </summary>
        public static uint ToBits(int value, int width)
        {
            if (width >= 32)
                return value >= 0 ? (uint)value : ((uint)(value & 0x7FFFFFFF) | 0x80000000u);
            return (uint)(value & ((1 << width) - 1));
        }

        /// <summary>
        /// uint の下位 width bit を int として読む。32bit で最上位 bit が立つ場合は符号付きとして解釈する。
        /// </summary>
        public static int FromBits(uint raw, int width)
        {
            if (width < 32 || raw <= 0x7FFFFFFFu)
                return (int)raw;
            return (int)(raw & 0x7FFFFFFFu) | int.MinValue;
        }

        static uint MaxCode(int width)
        {
            if (width >= 32)
                return 0xFFFFFFFFu;
            return (1u << width) - 1u;
        }

        public static uint Mask(uint value, int width)
        {
            if (width >= 32)
                return value;
            return value & ((1u << width) - 1u);
        }
    }
}
