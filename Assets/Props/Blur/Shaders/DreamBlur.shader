// Separated Gaussian blur for use with OnRenderImage (Built-in pipeline).
// One pass blurs in one direction; call twice (H then V) per blur iteration.
Shader "Hidden/DreamBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Offset  ("Blur Direction & Spread (xy)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Cull Off  ZWrite Off  ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _Offset;

            // 5-tap Gaussian (linear-sampling trick — effectively a 9-tap kernel)
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv  = i.uv;
                float2 off = _Offset.xy;

                fixed4 col  = tex2D(_MainTex, uv)                            * 0.2270270270;
                       col += tex2D(_MainTex, uv + off * 1.3846153846)       * 0.3162162162;
                       col += tex2D(_MainTex, uv - off * 1.3846153846)       * 0.3162162162;
                       col += tex2D(_MainTex, uv + off * 3.2307692308)       * 0.0702702703;
                       col += tex2D(_MainTex, uv - off * 3.2307692308)       * 0.0702702703;
                return col;
            }
            ENDCG
        }
    }
}
