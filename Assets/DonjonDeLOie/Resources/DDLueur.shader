// Donjon de l'Oie — ce qui brille (cristaux, lave, flammes, yeux, runes) : couleur par sommet × intensité, sans éclairage.
// Avec un post-traitement « halo » (bloom), les intensités > 1 rayonnent.
Shader "DonjonDeLOie/Lueur"
{
    Properties
    {
        [MainColor] _BaseColor("Teinte", Color) = (1, 1, 1, 1)
        _Intensite("Intensité", Range(0, 8)) = 2.2
        _Pulsation("Pulsation (vitesse)", Range(0, 6)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Lueur"
            Tags { "LightMode" = "UniversalForwardOnly" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LuVert
            #pragma fragment LuFrag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensite;
                half _Pulsation;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : TEXCOORD0; half fogFactor : TEXCOORD1; float3 positionWS : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings LuVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs vp = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                half4 c = v.color;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                half p = _Pulsation > 0 ? (0.82 + 0.18 * sin(_Time.y * _Pulsation + vp.positionWS.x * 1.7 + vp.positionWS.z * 1.3)) : 1;
                o.color = c * _BaseColor * (_Intensite * p);
                o.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            half4 LuFrag(Varyings i) : SV_Target
            {
                half3 c = i.color.rgb;
                c = MixFog(c, InitializeInputDataFog(float4(i.positionWS, 1), i.fogFactor));
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepVert
            #pragma fragment DepFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensite;
                half _Pulsation;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V DepVert(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half DepFrag(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _BaseColor; half _Intensite; half _Pulsation;
            struct v2f { float4 pos : SV_POSITION; half4 col : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata_full v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                half4 c = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = GammaToLinearSpace(c.rgb);
                #endif
                o.col = c * _BaseColor * _Intensite;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            half4 frag(v2f i) : SV_Target { half4 c = i.col; UNITY_APPLY_FOG(i.fogCoord, c); return half4(c.rgb, 1); }
            ENDCG
        }
    }
}
