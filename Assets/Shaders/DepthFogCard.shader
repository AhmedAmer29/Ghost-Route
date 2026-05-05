Shader "GhostRoute/DepthFogCard"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.45
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.22
        _NoiseScale ("Cloud Scale", Range(1, 30)) = 6
        _NoiseAmount ("Cloud Breakup", Range(0, 1)) = 0.35
        _NoiseCutoff ("Cloud Cutoff", Range(0, 1)) = 0.3
        _LayerOffset ("Layer Offset", Float) = 0
        _DriftSpeed ("Drift Speed", Range(0, 1)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Alpha;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseAmount;
            float _NoiseCutoff;
            float _LayerOffset;
            float _DriftSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(12.9898, 78.233))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(12.9898, 78.233))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(12.9898, 78.233))) * 43758.5453);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Cloud(float2 p)
            {
                float value = Noise(p) * 0.55;
                value += Noise(p * 2.07 + 8.31) * 0.30;
                value += Noise(p * 4.13 - 3.77) * 0.15;
                return saturate(value);
            }

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float edgeDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float edge = smoothstep(0.0, _EdgeFade, edgeDistance);

                float drift = _Time.y * _DriftSpeed;
                float cloud = Cloud(uv * _NoiseScale + float2(_LayerOffset + drift, _LayerOffset * 1.7 - drift));
                float breakup = smoothstep(_NoiseCutoff, 1.0, cloud);

                fixed4 color = _Color;
                color.a *= _Alpha * edge * lerp(1.0, breakup, _NoiseAmount);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
