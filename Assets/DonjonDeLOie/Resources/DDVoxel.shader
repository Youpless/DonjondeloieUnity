// Donjon de l'Oie — les voxels : couleur par sommet (occlusion comprise), éclairage d'URP (soleil, lampes en Forward+,
// ombres, brouillard), sans texture. Bâti sur le « Simple Lit » d'URP 17.3 (Unity 6.3) ; repli pour le pipeline intégré.
Shader "DonjonDeLOie/Voxel"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture (inutile)", 2D) = "white" {}
        [MainColor] _BaseColor("Teinte", Color) = (1, 1, 1, 1)
        _SpecColor("Reflet", Color) = (0.12, 0.12, 0.12, 0.25)
        [HDR] _EmissionColor("Émission (éclair de coup, surbrillance)", Color) = (0, 0, 0, 0)
        _Cutoff("Coupure", Range(0, 1)) = 0.5
        [HideInInspector] _Surface("__surface", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "SimpleLit" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VoxVert
            #pragma fragment VoxFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing
            #define BUMP_SCALE_NOT_SUPPORTED 1
            #define _SPECULAR_COLOR 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  color      : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                half3  vertexSH   : TEXCOORD4;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3  vertexLight : TEXCOORD5;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VoxVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = NormalizeNormalPerVertex(vn.normalWS);
                // Les couleurs des sommets sont en sRGB : on les passe en linéaire si le projet est en espace linéaire.
                half4 c = input.color;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                o.color = c * _BaseColor;
                o.fogFactor = ComputeFogFactor(vp.positionCS.z);
                o.vertexSH = SampleSH(o.normalWS);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                o.vertexLight = VertexLighting(vp.positionWS, o.normalWS);
                #endif
                return o;
            }

            half4 VoxFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = input.vertexLight;
                #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = input.vertexSH;
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surf = (SurfaceData)0;
                surf.albedo = input.color.rgb;
                surf.alpha = 1;
                surf.specular = _SpecColor.rgb;
                surf.smoothness = _SpecColor.a;
                surf.normalTS = half3(0, 0, 1);
                surf.occlusion = 1;
                surf.emission = _EmissionColor.rgb;

                half4 color = UniversalFragmentBlinnPhong(inputData, surf);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    // Pipeline intégré (projet « 3D » sans URP) : même rendu, en surface shader.
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf BlinnPhong vertex:vert fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _BaseColor;
        fixed4 _EmissionColor;
        fixed4 _SpecColor;
        struct Input { float4 vcol; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vcol = v.color;
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = IN.vcol;
            #ifndef UNITY_COLORSPACE_GAMMA
            c.rgb = GammaToLinearSpace(c.rgb);
            #endif
            o.Albedo = c.rgb * _BaseColor.rgb;
            o.Emission = _EmissionColor.rgb;
            o.Specular = 0.2;
            o.Gloss = 0.2;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
