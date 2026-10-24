namespace FDMi.sync.Editor.Domain
{
    // E-01 / E-02 の焼き値は動的と静的で別のフィールドに住む (§11.1)。
    // Why-not: Runtime の共通基底に焼き値を置かない。静的アンカーは [UdonSynced] を 1 つも持てず
    // (NoVariableSync)、動的の _absMm は送信スナップショットを兼ねる (§4.4)。1 つの欄にはできない。
    public static class FDMiSyncAnchorFields
    {
        // V-03 / V-12 / V-13 と焼きの分岐は、isStatic フラグではなく型で判定する。
        public static bool IsStatic(FDMiSyncAnchor anchor)
        {
            return anchor is FDMiStaticObjectSync;
        }

        public static long[] AbsMm(FDMiSyncAnchor anchor)
        {
            FDMiStaticObjectSync s = anchor as FDMiStaticObjectSync;
            return (s != null) ? s._absMm : ((FDMiDynamicObjectSync)anchor)._absMm;
        }

        public static short[] Rot(FDMiSyncAnchor anchor)
        {
            FDMiStaticObjectSync s = anchor as FDMiStaticObjectSync;
            return (s != null) ? s._rot : ((FDMiDynamicObjectSync)anchor)._rot;
        }
    }
}
