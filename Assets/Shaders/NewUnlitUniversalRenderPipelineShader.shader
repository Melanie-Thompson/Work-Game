Shader "Hidden/CRTEffectURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanLineIntensity ("Scan Line Intensity", Range(0, 1)) = 0.5
        _VignetteIntensity ("Vignette Intensity", Range(0, 0.5)) = 0.05
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "CRTEffect"
            
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
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float _ScanLineIntensity;
            float _VignetteIntensity;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Scan lines
                float scanLine = sin(IN.uv.y * 800.0 + _Time.y * 10.0) * 0.5 + 0.5;
                col.rgb *= 1.0 - (scanLine * _ScanLineIntensity * 0.3);
                
                // Vignette
                float2 center = IN.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * _VignetteIntensity * 4.0;
                col.rgb *= vignette;
                
                // Chromatic aberration
                float2 offset = (IN.uv - 0.5) * 0.002;
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - offset).r;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).b;
                col.r = r;
                col.b = b;
                
                return col;
            }
            ENDHLSL
        }
    }
}