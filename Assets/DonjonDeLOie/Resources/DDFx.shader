// Donjon de l'Oie — effets transparents : étincelles, halos de sélection, anneaux, éclats, eau.
// Mélange réglable (additif par défaut : _SrcBlend = One, _DstBlend = One ; alpha : SrcAlpha / OneMinusSrcAlpha).
// _Vague > 0 fait onduler les sommets (surface de l'eau).
Shader "DonjonDeLOie/Fx"
{
    Properties
    {
        [MainColor] _BaseColor("Teinte", Color) = (1, 1, 1, 1)
        _Vague("Ondulation", Range(0, 1)) = 0
        _Additif("Additif (brouillard noir)", Float) = 1
        [HideInInspector] _SrcBlend("Src", Float) = 1
        [HideInInspector] _DstBlend("Dst", Float) = 1
        [HideInInspector] _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Fx"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FxVert
            #pragma fragment FxFrag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Vague;
                half _Additif;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : TEXCOORD0; half fogFactor : TEXCOORD1; float3 positionWS : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings FxVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                half reflet = 1;
                if (_Vague > 0)
                {
                    float t = _Time.y;
                    float h = sin(ws.x * 0.55 + t * 1.1) * 0.5 + sin(ws.z * 0.7 - t * 0.9) * 0.35 + sin((ws.x + ws.z) * 1.3 + t * 2.0) * 0.15;
                    ws.y += h * _Vague;
                    reflet = 0.9 + 0.2 * h;
                }
                o.positionCS = TransformWorldToHClip(ws);
                o.positionWS = ws;
                half4 c = v.color;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                o.color = c * _BaseColor;
                o.color.rgb *= reflet;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 FxFrag(Varyings i) : SV_Target
            {
                half3 c = i.color.rgb;
                // En additif, le brouillard éteint l'effet ; en alpha, il le teinte.
                half fc = InitializeInputDataFog(float4(i.positionWS, 1), i.fogFactor);
                c = _Additif > 0.5 ? MixFogColor(c, half3(0, 0, 0), fc) : MixFog(c, fc);
                return half4(c, i.color.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _BaseColor; half _Vague; half _Additif;
            struct v2f { float4 pos : SV_POSITION; half4 col : COLOR; };
            v2f vert(appdata_full v)
            {
                v2f o;
                float4 ws = mul(unity_ObjectToWorld, v.vertex);
                if (_Vague > 0) ws.y += (sin(ws.x * 0.55 + _Time.y * 1.1) * 0.5 + sin(ws.z * 0.7 - _Time.y * 0.9) * 0.35) * _Vague;
                o.pos = mul(UNITY_MATRIX_VP, ws);
                half4 c = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = GammaToLinearSpace(c.rgb);
                #endif
                o.col = c * _BaseColor;
                return o;
            }
            half4 frag(v2f i) : SV_Target { return i.col; }
            ENDCG
        }
    }
}
