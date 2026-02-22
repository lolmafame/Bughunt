Shader "Custom/PSXStatic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1
        _NoiseSpeed ("Noise Speed", Range(0, 10)) = 3.0
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _NoiseStrength;
            float _NoiseSpeed;
            float _ScanlineStrength;

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the main game texture
                half4 col = tex2D(_MainTex, IN.uv);

                // Animated static noise
                float2 noiseUV = IN.uv * float2(200, 200);
                float time = _Time.y * _NoiseSpeed;
                float noise = random(noiseUV + time);
                col.rgb += noise * _NoiseStrength;

                // Scanlines
                float scanline = sin(IN.uv.y * 800) * 0.5 + 0.5;
                col.rgb -= scanline * _ScanlineStrength;

               

                return col;
            }
            ENDHLSL
        }
    }
}