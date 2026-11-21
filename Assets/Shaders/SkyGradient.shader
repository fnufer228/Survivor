Shader "Survivor/SkyGradient"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.12, 0.55, 0.96, 1)
        _HorizonColor ("Horizon", Color) = (0.43, 0.9, 1.0, 1)
        _BottomColor ("Bottom", Color) = (0.35, 0.6, 0.75, 1)
        _GradientPower ("Gradient Power", Range(0.1, 3)) = 0.6
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunGlowColor ("Sun Glow", Color) = (1, 0.8, 0.5, 1)
        _SunGlowSize ("Sun Glow Size", Range(1, 64)) = 8
        _SunGlowStrength ("Sun Glow Strength", Range(0, 2)) = 0.5
        _MoonDir ("Moon Direction", Vector) = (0, -1, 0, 0)
        _MoonGlowColor ("Moon Glow", Color) = (0.6, 0.6, 1, 1)
        _MoonGlowStrength ("Moon Glow Strength", Range(0, 2)) = 0.2
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                float _GradientPower;
                float4 _SunDir;
                half4 _SunGlowColor;
                float _SunGlowSize;
                float _SunGlowStrength;
                float4 _MoonDir;
                half4 _MoonGlowColor;
                float _MoonGlowStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 col = d.y >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(d.y), _GradientPower))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-d.y * 3.0), 0.5));
                float sun = pow(saturate(dot(d, normalize(_SunDir.xyz))), _SunGlowSize);
                float moon = pow(saturate(dot(d, normalize(_MoonDir.xyz))), 24.0);
                col += _SunGlowColor.rgb * sun * _SunGlowStrength;
                col += _MoonGlowColor.rgb * moon * _MoonGlowStrength;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
