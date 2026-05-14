Shader "Hidden/UnderwaterPostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterColor ("Water Color", Color) = (0.08, 0.32, 0.14, 1.0)
        _FogColor ("Fog Color", Color) = (0.04, 0.18, 0.08, 1.0)
        _DepthDistance ("Depth Fade Distance", Float) = 30.0
        _Distortion ("Distortion", Float) = 0.012
        _BaseTint ("Base Tint Strength", Float) = 0.15
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _WaterColor;
            float4 _FogColor;
            float _DepthDistance;
            float _Distortion;
            float _BaseTint;
            float _EffectAlpha;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                uv.x += sin(uv.y * 12.0 + _Time.y * 2.5) * _Distortion * _EffectAlpha;
                uv.y += cos(uv.x * 12.0 + _Time.y * 2.0) * _Distortion * _EffectAlpha;

                fixed4 col = tex2D(_MainTex, uv);

                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float depth = LinearEyeDepth(rawDepth);

                bool hasDepthTexture = (rawDepth < 1.0);
                float fogFactor = hasDepthTexture ? saturate(depth / _DepthDistance) : 0.5;

                fogFactor = pow(fogFactor, 0.7);

                fixed4 foggedColor = lerp(col, _FogColor, fogFactor * 0.85);

                foggedColor = lerp(foggedColor, _WaterColor, _BaseTint * (1.0 - fogFactor * 0.5));

                float edgeDarkening = 1.0 - length(i.uv - 0.5) * 0.4;
                foggedColor.rgb *= edgeDarkening;

                return lerp(col, foggedColor, _EffectAlpha);
            }
            ENDCG
        }
    }
}
