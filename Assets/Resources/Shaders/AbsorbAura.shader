// 흡수 대상 표시용 노란 오러 셸 셰이더.
// 적을 감싸는 큐브 셸의 바깥 면(Cull Back)을 가산 블렌딩으로 그리고,
// _Time 기반 sin으로 알파를 맥동시켜 "출렁이는 오러" 느낌을 낸다.
// MainScene은 Bloom이 꺼져 있으므로 Emission 없이 알파 블렌딩만 사용한다.
Shader "Custom/AbsorbAura"
{
    Properties
    {
        _Color ("Aura Color", Color) = (1, 0.85, 0.1, 0.5)
        _PulseSpeed ("Pulse Speed", Float) = 6
        _PulseAmount ("Pulse Amount", Float) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"

            // 셸 바깥 면만 그려 안쪽 적 몸통이 비치지 않게 하고, 깊이 기록 없이 가산 합성
            Cull Back
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // _Time 기반 sin으로 알파 맥동: (1 - _PulseAmount) ~ 1 배율
                float pulse = 1.0 - _PulseAmount * 0.5 * (1.0 - sin(_Time.y * _PulseSpeed));
                half alpha = _Color.a * pulse;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
