Shader "Custom/FDMi/Light"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0)
        _EmissionColor ("EmissionColor", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _distMul ("distance-size multiplier", float) = 0
        _freq ("Flash frequency", float) = 0
        _emitIntensity ("emission Intensity", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        LOD 200
        Offset -1, -1

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:auto vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };


        fixed4 _Color;
        fixed4 _EmissionColor;
        float _freq, _distMul, _emitIntensity;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v) {
            float dist = length(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex));
            v.vertex.xyz += v.normal * dist*_distMul * atan(1 / unity_CameraProjection._m11);
        }
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            float flashMul = frac(_Time.y*_freq);
            // Metallic and smoothness come from slider variables
            o.Emission = _EmissionColor.rgb *  step(flashMul,0.5f) * _emitIntensity;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
