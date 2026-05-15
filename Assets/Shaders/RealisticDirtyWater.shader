Shader "Custom/RealisticDirtyWater"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Edge Color", Color) = (0.28, 0.25, 0.15, 0.8)
        _DeepColor ("Deep Murky Color", Color) = (0.05, 0.06, 0.03, 1.0)
        _DepthDistance ("Depth Fade Distance", Range(0.01, 10.0)) = 5.0
        
        [Header(AAA Physical Lighting)]
        _Glossiness ("Surface Polish", Range(0,1)) = 0.98
        _Metallic ("Specular Liquid", Range(0,1)) = 0.2
        _FresnelPower ("Fresnel Reflectivity", Range(1, 10)) = 5.0
        
        [Header(Refraction and Caustics)]
        _Refraction ("Light Refraction Distortion", Range(0, 100)) = 15.0
        _CausticsIntensity ("Fake Caustics / Silt Brightness", Range(0, 2)) = 0.5
        
        [Header(Scum and Foam)]
        _ScumColor ("Floating Scum Color", Color) = (0.45, 0.4, 0.28, 1.0)
        _ScumThickness ("Scum Shoreline Size", Range(0.0, 1.0)) = 0.3
        
        [Header(Wave Simulation)]
        _WaveScale ("Ripple Scale", Float) = 1.5
        _WaveSpeed ("Current Speed", Vector) = (0.05, 0.1, 0, 0)
        _NormalIntensity ("Normals Strength", Range(0, 5)) = 1.5
        
        [Header(Splash Displacement)]
        _DisplacementTex ("Wave Displacement", 2D) = "black" {}
        _DisplacementArea ("Displacement Area (x, y, w, h)", Vector) = (0,0,1,1)
        _DisplacementHeight ("Displacement Scale", Float) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 400
        Cull Off // Fixes invisible water when camera is underwater

        // Grab screen behind water for hyper-realistic refraction processing
        GrabPass { "_WaterRefraction" }

        CGPROGRAM
        // keepalpha ensures Unity's lighting keeps specular reflections even if water is completely transparent!
        #pragma surface surf Standard alpha:blend vertex:vert keepalpha
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
            float4 screenPos;
            float3 viewDir;
        };

        fixed4 _ShallowColor, _DeepColor, _ScumColor;
        half _Glossiness, _Metallic, _FresnelPower;
        float _DepthDistance, _Refraction, _CausticsIntensity, _ScumThickness;
        float _WaveScale, _NormalIntensity;
        float2 _WaveSpeed;
        
        sampler2D _CameraDepthTexture;
        sampler2D _WaterRefraction;
        float4 _WaterRefraction_TexelSize;
        
        sampler2D _DisplacementTex;
        float4 _DisplacementArea;
        float _DisplacementHeight;

        // High-end mathematical noise algorithm for fluid organics
        float hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123); }
        float noise(float2 p) {
            float2 i = floor(p); float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(hash(i + float2(0,0)), hash(i + float2(1,0)), f.x),
                        lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
        }
        
        // Multi-layered Fractal Brownian Motion for complex fluid intertwining
        float fbm(float2 p) {
            float v = 0.0; float a = 0.5;
            float2x2 rot = float2x2(0.866, -0.5, 0.5, 0.866);
            for(int i = 0; i < 4; i++) {
                v += a * noise(p);
                p = mul(rot, p) * 2.0;
                a *= 0.5;
            }
            return v;
        }

        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Gerstner Wave approximation for AAA geometry displacement
            float time = _Time.y;
            float2 uv = mul(unity_ObjectToWorld, v.vertex).xz * _WaveScale * 0.1;
            
            // Simulate rolling liquid volume instead of just flat bumps
            float wave = sin(uv.x * 2.0 + time * _WaveSpeed.x * 5.0) * cos(uv.y * 2.0 + time * _WaveSpeed.y * 5.0);
            v.vertex.y += wave * 0.05 + (fbm(uv + time * _WaveSpeed) * 0.05);
            
            // Splash displacement overlay
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float2 dispUV = (worldPos.xz - _DisplacementArea.xy) / _DisplacementArea.zw;
            float disp = tex2Dlod(_DisplacementTex, float4(dispUV, 0, 0)).r * _DisplacementHeight;
            v.vertex.y += disp;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // ------------- WAVES & NORMAL MAPPING -------------
            float time = _Time.y;
            float2 uv = IN.worldPos.xz * _WaveScale;
            
            // Generate two independent flowing currents that interact
            float2 scroll1 = uv + _WaveSpeed * time;
            float2 scroll2 = uv * 1.5 - float2(_WaveSpeed.y, -_WaveSpeed.x) * time * 1.4;
            
            float n1 = fbm(scroll1);
            float n2 = fbm(scroll2 + float2(n1, n1 * 0.5));
            float fluidDistortion = (n1 + n2) * 0.5;

            // Compute high-definition normal mapping from derivatives
            float delta = 0.02;
            float nx = fbm(scroll1 + float2(delta, 0)) - n1;
            float nz = fbm(scroll1 + float2(0, delta)) - n1;
            float3 normalMap = normalize(float3(-nx * _NormalIntensity * 15.0, 1.0, -nz * _NormalIntensity * 15.0));
            
            // ------------- SECURE DEPTH BUFFER FIX -------------
            // Extract genuine world-depth distance using the camera buffer
            float screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float waterDepth = max(0.001, screenDepth - IN.screenPos.w);
            
            // SAFEGUARD: If depth texture is broken or off, cap the depth so it doesn't turn pitch black
            waterDepth = min(waterDepth, 15.0); 
            
            // Optical physics: Beer's Law approximation for volumetric light scattering
            float murkiness = saturate(waterDepth / max(0.01, _DepthDistance));
            fixed4 waterColor = lerp(_ShallowColor, _DeepColor, murkiness);

            // ------------- CAUSTICS / SILT -------------
            // Create a fake sunlight caustic/silt effect under the water
            float causticNoise = fbm(uv * 5.0 - time * _WaveSpeed * 2.0);
            float causticMask = pow(causticNoise, 3.0) * _CausticsIntensity;
            // Only show caustics in the shallow/medium depth
            causticMask *= (1.0 - saturate(waterDepth / 2.0)); 
            waterColor.rgb += _ShallowColor.rgb * causticMask;

            // ------------- SEWER SCUM / EDGE FOAM -------------
            // Dynamically generate gross foam / sludge clumps specifically clinging to the walls
            float scumNoise = fbm(uv * 3.0 + time * _WaveSpeed * 0.5);
            float scumFactor = saturate((_ScumThickness - waterDepth) / max(0.01, _ScumThickness));
            scumFactor = smoothstep(0.1, 0.9, scumFactor * scumNoise * 2.5);
            waterColor.rgb = lerp(waterColor.rgb, _ScumColor.rgb, scumFactor);
            waterColor.a = saturate(waterColor.a + scumFactor);

            // ------------- REAL-TIME REFRACTION -------------
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            
            // Bend the background pixels depending on ripple intensity
            float2 refractionOffset = normalMap.xz * _Refraction * _WaterRefraction_TexelSize.xy;
            // Smoothly reduce refraction near edges to prevent ugly tearing
            refractionOffset *= saturate(waterDepth * 2.0); 
            
            half4 refrColor = tex2D(_WaterRefraction, screenUV + refractionOffset);
            
            // ------------- AAA FRESNEL OUTPUT -------------
            // Schlick's approximation for water reflectiveness at angles
            float fresnelAngle = saturate(1.0 - dot(normalMap, normalize(IN.viewDir)));
            float fresnel = pow(fresnelAngle, _FresnelPower);
            
            o.Albedo = waterColor.rgb;
            
            // Mix the refracted background pixel directly with the surface color
            o.Emission = refrColor.rgb * (1.0 - waterColor.a) * (1.0 - fresnel);
            
            // Finalize PBR traits
            o.Normal = normalMap;
            o.Metallic = _Metallic * (1.0 - scumFactor);
            o.Smoothness = lerp(_Glossiness * 0.8, _Glossiness, fluidDistortion) * (1.0 - scumFactor); 
            
            o.Alpha = 1.0; 
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}