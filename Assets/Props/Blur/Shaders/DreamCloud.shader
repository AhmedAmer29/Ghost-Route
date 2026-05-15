// Wispy, soft-particle cloud shader for the Dream Zone particle system.
// Soft-particle depth fade requires "Depth Texture" enabled on the camera.
Shader "Custom/DreamCloud"
{
    Properties
    {
        _MainTex           ("Cloud Texture (RGBA)",    2D)      = "white" {}
        _NoiseTex          ("Noise / Wisp Texture",    2D)      = "white" {}
        _Color             ("Tint Color",              Color)   = (0.85, 0.92, 1.0, 0.45)
        _NoiseScale        ("Noise UV Scale",          Float)   = 1.5
        _NoiseSpeed        ("Noise Scroll (XY)",       Vector)  = (0.015, 0.008, 0, 0)
        _SoftParticleFactor("Soft Particle Factor",    Range(0.01, 10)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull   Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #include "UnityCG.cginc"

            sampler2D       _MainTex;
            sampler2D       _NoiseTex;
            sampler2D_float _CameraDepthTexture;
            float4          _MainTex_ST;
            fixed4          _Color;
            float           _NoiseScale;
            float4          _NoiseSpeed;
            float           _SoftParticleFactor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 noiseUV  : TEXCOORD1;
                fixed4 color    : COLOR;
                #ifdef SOFTPARTICLES_ON
                float4 projPos  : TEXCOORD2;
                #endif
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.noiseUV  = v.uv * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                o.color    = v.color * _Color;
                #ifdef SOFTPARTICLES_ON
                o.projPos  = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 cloud = tex2D(_MainTex,  i.uv);
                fixed  noise = tex2D(_NoiseTex, i.noiseUV).r;

                fixed4 col = i.color;
                // Noise gently erodes the edges, giving a wispy look
                col.a *= cloud.a * saturate(0.5 + noise * 0.7);
                col.rgb = lerp(col.rgb, col.rgb * (0.8 + noise * 0.4), 0.35);

                #ifdef SOFTPARTICLES_ON
                float sceneZ = LinearEyeDepth(
                    SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,
                                              UNITY_PROJ_COORD(i.projPos)));
                col.a *= saturate(_SoftParticleFactor * (sceneZ - i.projPos.z));
                #endif

                return col;
            }
            ENDCG
        }
    }
}
