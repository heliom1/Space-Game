Shader "UI/AtmosphericCloudVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Cloud Color", Color) = (0.015, 0.015, 0.02, 1.0)
        _CloudDensity ("Cloud Density", Range(0.2, 3.0)) = 1.35
        _EdgeStart ("Edge Fade Start", Range(0.1, 1.0)) = 0.38
        _EdgeEnd ("Edge Softness", Range(0.5, 2.0)) = 1.15
        _DriftSpeed ("Drift Speed", Range(0.01, 0.5)) = 0.06
        _Scale ("Cloud Scale", Range(1.0, 10.0)) = 3.2
        _Turbulence ("Turbulence / Swirl", Range(0.0, 2.0)) = 0.7
        _Pulsation ("Atmospheric Breathing", Range(0.0, 0.5)) = 0.12

        // UI Canvas Masking Support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _CloudDensity;
            float _EdgeStart;
            float _EdgeEnd;
            float _DriftSpeed;
            float _Scale;
            float _Turbulence;
            float _Pulsation;

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 4; i++)
                {
                    val += amp * valueNoise(p * freq);
                    freq *= 2.02;
                    amp *= 0.5;
                }
                return val;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y * _DriftSpeed;

                // 16:9 Aspect-corrected coordinate space centered at (0, 0)
                float2 centeredUV = (uv - 0.5) * float2(1.7778, 1.0);
                float dist = length(centeredUV);

                // Subtle breathing pulsation
                float pulse = sin(_Time.y * 0.7) * _Pulsation;
                float effectiveDist = dist + pulse;

                // Multi-directional drift for organic swirling smoke
                float2 drift1 = float2(t * 0.45, t * 0.22);
                float2 drift2 = float2(-t * 0.35, t * 0.38);

                float2 p = centeredUV * _Scale;
                float warpX = fbm(p + drift1);
                float warpY = fbm(p + drift2 + float2(5.2, 1.3));
                float2 warpedP = p + float2(warpX, warpY) * _Turbulence;

                // Main cloud billows
                float clouds = fbm(warpedP + drift1 * 0.5);
                // Additional fine tendril detail
                clouds += 0.32 * valueNoise(warpedP * 2.6 - drift2);

                // Edge mask: 0 in center, 1 towards outer edges
                float edgeFactor = smoothstep(_EdgeStart, _EdgeEnd, effectiveDist);

                // Multiply cloud pattern by edge distance so clouds billow inward from borders
                float alpha = edgeFactor * (clouds * _CloudDensity);

                // Deep solid darkness at the furthest screen corners
                float outerVignette = smoothstep(0.72, 1.25, effectiveDist);
                alpha = saturate(max(alpha, outerVignette));

                fixed4 col = _Color;
                col.a *= alpha * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
