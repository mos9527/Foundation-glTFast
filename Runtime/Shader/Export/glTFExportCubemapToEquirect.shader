Shader "Hidden/glTFExportCubemapToEquirect"
{
    Properties
    {
        _CubeTex ("Cubemap", Cube) = "" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEXCUBE(_CubeTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float phi = i.uv.x * (2.0 * UNITY_PI);
                float theta = i.uv.y * UNITY_PI;
                float3 dir = float3(
                    sin(theta) * sin(phi),
                    cos(theta),
                    sin(theta) * cos(phi)
                );
                return UNITY_SAMPLE_TEXCUBE(_CubeTex, dir);
            }
            ENDCG
        }
    }
}
