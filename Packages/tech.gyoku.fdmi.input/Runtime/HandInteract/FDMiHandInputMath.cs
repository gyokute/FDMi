using UnityEngine;

namespace FDMi.input
{
    // Lever / Yoke の回転入力の角度抽出。Runtime (UdonSharp) と Editor で共有する。
    // U# は同一アセンブリ内の plain な static class の static メソッドを呼べる。
    public static class FDMiHandInputMath
    {
        // 軸まわりの符号付き角度 [deg]。from / to は回転中心からの位置ベクトル (基準フレーム)。
        // FromToRotation + eulerAngles の内積は、軸方向の手のずれが直交成分に化けて角度にならない。
        // 軸平面への符号付き角度は cross と dot の atan2 で直接取る。
        // 半径ゼロでは Atan2(0,0)=0 を返すため特異点の分岐は要らない。
        public static float AngleAboutAxis(Vector3 from, Vector3 to, Vector3 axis)
        {
            Vector3 n = axis.normalized;
            return Mathf.Atan2(Vector3.Dot(Vector3.Cross(from, to), n), Vector3.Dot(from, to))
                * Mathf.Rad2Deg;
        }

        // 回転差分から軸まわりの成分を抽出する (swing-twist 分解の twist)。
        public static float TwistAngleAboutAxis(Quaternion delta, Vector3 axis)
        {
            Vector3 n = axis.normalized;
            Vector3 v = new Vector3(delta.x, delta.y, delta.z);
            float angle = 2f * Mathf.Atan2(Vector3.Dot(v, n), delta.w) * Mathf.Rad2Deg;
            if (angle > 180f)
                angle -= 360f;
            else if (angle < -180f)
                angle += 360f;
            return angle;
        }
    }
}
