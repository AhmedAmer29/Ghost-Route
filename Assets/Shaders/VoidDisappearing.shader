Shader "GhostRoute/VoidDisappearing"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color ("Void Color", Color) = (0.002, 0.004, 0.01, 0.82)
        _EdgeColor ("Edge Mist Color", Color) = (0.035, 0.055, 0.095, 0.24)
        _Alpha ("Alpha", Range(0, 1)) = 0.68
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.75)) = 0.42
        _TopFade ("Top Fade", Range(0.01, 1)) = 0.82
        _Dissolve ("Disappearance", Range(0, 1)) = 0.34
        _NoiseScale ("Cloud Scale", Range(1, 40)) = 7
        _NoiseStrength ("Cloud Breakup", Range(0, 1)) = 0.86
        _WispStrength ("Wisp Strength", Range(0, 1)) = 0.42
        _DriftSpeed ("Drift Speed", Range(0, 3)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float _Alpha;
            float _EdgeSoftness;
            float _TopFade;
            float _Dissolve;
            float _NoiseScale;
            float _NoiseStrength;
            float _WispStrength;
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

            float HashNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float CloudNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                value += HashNoise(p) * amplitude;
                p = p * 2.03 + 9.13;
                amplitude *= 0.5;

                value += HashNoise(p) * amplitude;
                p = p * 2.11 - 4.27;
                amplitude *= 0.5;

                value += HashNoise(p) * amplitude;
                p = p * 2.07 + 2.71;
                amplitude *= 0.5;

                value += HashNoise(p) * amplitude;
                return saturate(value);
            }

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float2 centeredUv = uv * 2.0 - 1.0;
                centeredUv.x *= 0.72;

                float edgeDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float edgeFade = smoothstep(0.0, _EdgeSoftness, edgeDistance);
                float cloudShape = smoothstep(1.05, 0.08, length(centeredUv));
                float topFade = smoothstep(0.0, _TopFade, 1.0 - uv.y);

                float drift = _Time.y * _DriftSpeed;
                float cloud = CloudNoise(uv * _NoiseScale + float2(drift, -drift * 0.55));
                float wisps = CloudNoise(uv * (_NoiseScale * 2.65) + float2(-drift * 1.6, drift * 0.9));
                float cloudMask = smoothstep(_Dissolve, 1.0, cloud);
                float wispMask = smoothstep(0.48, 1.0, wisps) * _WispStrength;

                float alpha = _Alpha * topFade;
                alpha *= saturate(cloudShape + wispMask * 0.55);
                alpha *= edgeFade;
                alpha *= lerp(1.0, cloudMask + wispMask, _NoiseStrength);

                fixed4 color = lerp(_EdgeColor, _Color, saturate(cloudShape + cloudMask));
                color.a *= alpha;
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
