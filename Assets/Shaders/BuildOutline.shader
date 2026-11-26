Shader "Survivor/BuildOutline"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.75, 0.9, 1, 1)
        _Width ("Width (pixels)", Float) = 2.5
        _Alpha ("Alpha", Range(0, 1)) = 0.95
        _Brightness ("Brightness", Range(0.5, 2)) = 1.15
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Width;
                half _Alpha;
                half _Brightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float3 smoothNormal : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 normal = dot(input.smoothNormal, input.smoothNormal) > 0.25 ? input.smoothNormal : input.normalOS;
                float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(normal);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = max(length(nCS), 1e-5);
                clip.xy += (nCS / len) * (_Width * 2.0 / _ScreenParams.xy) * clip.w;
                o.positionCS = clip;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(saturate(_BaseColor.rgb * _Brightness), _Alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
