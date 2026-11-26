Shader "Survivor/Flame"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (2, 0.7, 0.15, 0.85)
        _EdgeSoftness ("Edge Softness", Range(0, 4)) = 1.2
        _Flicker ("Flicker", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Flame"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _EdgeSoftness;
                half _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewWS     : TEXCOORD1;
                float  seed       : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(positionWS);
                o.seed = positionWS.y * 7.0 + positionWS.x * 3.0;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                half edge = pow(facing, _EdgeSoftness);
                half flicker = 1.0 + _Flicker * sin(_Time.y * 23.0 + i.seed);
                return half4(_BaseColor.rgb * flicker, _BaseColor.a * lerp(0.35, 1.0, edge));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
