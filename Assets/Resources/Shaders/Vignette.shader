Shader "UI/ProceduralVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,1)
        
        [Header(Vignette Settings)]
        _VignetteSize ("Vignette Size (어둠 시작점)", Range(0.1, 3.0)) = 1.0
        _VignettePower ("Vignette Power (부드러움)", Range(0.1, 5.0)) = 2.0

        // UI 마스크 등 캔버스 시스템 호환을 위한 필수 속성들
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _VignettePower;
            float _VignetteSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                // UI Image 컴포넌트의 Color(알파 포함)를 그대로 가져옴
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV 좌표를 0~1에서 -0.5~0.5로 변환 (중앙이 0,0이 되도록)
                float2 uv = i.texcoord - 0.5;

                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;
                
                // 중앙으로부터의 거리 계산 (가운데는 0, 끝은 1)
                float dist = length(uv) * 2.0;
                
                // 비네트 크기와 부드러움(제곱) 적용
                float v = pow(saturate(dist * _VignetteSize), _VignettePower);
                
                // UI 이미지의 기존 색상에 계산된 투명도(v)를 곱함
                fixed4 color = i.color;
                color.a *= v; 
                
                return color;
            }
            ENDCG
        }
    }
}