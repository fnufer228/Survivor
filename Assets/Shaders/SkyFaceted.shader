Shader "Survivor/SkyFaceted"
{
    Properties
    {
        _LitColor ("Lit Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.6, 0.65, 0.95, 1)
        _LightDirWS ("Light Direction (towards light)", Vector) = (0.4, 0.8, -0.4, 0)
        _Hardness ("Terminator Softness", Range(0.01, 1)) = 0.8
        _Brightness ("Brightness", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+50" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor;
                half4 _ShadowColor;
                float4 _LightDirWS;
                float _Hardness;
                float _Brightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float ndl = dot(normalize(i.normalWS), normalize(_LightDirWS.xyz));
                float t = smoothstep(-_Hardness, _Hardness, ndl);
                half3 col = lerp(_ShadowColor.rgb, _LitColor.rgb, t) * i.color.rgb * _Brightness;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
