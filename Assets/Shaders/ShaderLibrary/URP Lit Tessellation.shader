// Based on https://gist.github.com/phi-lira
// When creating shaders for Universal Render Pipeline you can you the ShaderGraph which is super AWESOME!
// However, if you want to author shaders in shading language you can use this teamplate as a base.
// Please note, this shader does not necessarily match perfomance of the built-in URP Lit shader.
// This shader works with URP 7.1.x and above
Shader "Kit/Universal Render Pipeline/URP Lit Tessellation"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.5,0.5,0.5,1)
        [MainTexture][noscaleOffset] _BaseMap("Albedo", 2D) = "white" {}
        _NormalScale("Normal Map Scale", Range(0.0, 1.0)) = 1.0
        [Normal][noscaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}

        [Header(Detail Texture)]
        _DetailFader ("Detail Texture Fader", Range(0.0,1.0)) = 1.0
        _DetailGrayScale ("Detail Grayscale", Range(0.0, 1.0)) = 1.0
        _DetailMap("Detail Texture", 2D) = "white" {}
        [Header(Detail Normal)]
        _DetailNormalScale ("Detail Normal Scale", Range(0.0, 1.0)) = 1.0
        [Normal] _DetailNormalMap("Detail Normal", 2D) = "bump" {}
        [Toggle(_DebugNormal)] _DebugNormal("Debug Normal (default = false)", Float) = 0
        
        [space]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [noscaleOffset] _SpecGlossMap("Specular Map", 2D) = "black" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [noscaleOffset] _OcclusionMap("Occlusion Map", 2D) = "white" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,0)
        [noscaleOffset] _EmissionMap("Emission Map", 2D) = "black" {}


        [Header(LOD)]
        [IntRange] _LODDistance ("LOD Map to distance", Range(0,20)) = 10.0
        _LODBias ("LOD Mapping bias", Range(0.1, 10)) = 1.0
        [Toggle(_DebugLod)] _DebugLod("Debug Lod (default = false)", Float) = 0

        [Header(Translucency Scattering)]
        [Toggle(_TSEnable)] _TSEnable ("Translucency", Float) = 1
        [Toggle(_TSDebug)] _TSDebug("Debug Translucency", Float) = 0
        _TSAlbedo ("Subcutaneous Color", color) = (0.2,0.14,0.08,1)
        // translucency that is always present, both front and back.
        _LightAmbient("Translucency Ambient", 2D) = "white" {}
        // Local thickness map, used for both direct and indirect translucency
        _LightThickness("Local Thickness Map", 2D) = "white" {}
        // Subsurface distortion, Breaks continuity, view-dependent, allows for more organic, Fresnel-like
        _LightDistortion("Subsurface Distortion", Range(0,1)) = 0.5
        // power value for direct translucency
        _LightPower("Translucency Power", Range(0,1)) = 1
        // Direct/Back translucency, View-oriented, Should be defined per-light to control the central point.
        _LightScale("Translucency Scale(view)", Range(0.01,10)) = 1

        [Header(Tessellation)]
        [KeywordEnum(fractional_odd,fractional_even,pow2,integer)]
        _Partitioning("Partitioning (default = integer)", Float) = 3
        _Tessellation("Tessellation", Range(1, 32)) = 3
        _MinTessDistance("Min Tess Distance", Range(0.01, 32)) = 0.01
        _MaxTessDistance("Max Tess Distance", Range(0.01, 32)) = 3

        [Space]
        [HideInInspector]_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags{
            "RenderType" = "Opaque"
            "Queue" = "Geometry" // Queue : { Background, Geometry, AlphaTest, Transparent, Overlay }
            "RenderPipeline" = "UniversalRenderPipeline"
            // "IgnoreProjector" = "True"
        }
        // https://docs.unity3d.com/Manual/SL-ShaderLOD.html
        LOD 300

        // ------------------------------------------------------------------
        // Forward pass. Shades GI, emission, fog and all lights in a single pass.
        // Compared to Builtin pipeline forward renderer, LWRP forward renderer will
        // render a scene with multiple lights with less drawcalls and less overdraw.
        Pass
        {
            // "Lightmode" tag must be "UniversalForward" or not be defined in order for
            // to render objects.
            Name "CustomCanisLit"
            Tags {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
            #define UNITY_CAN_COMPILE_TESSELLATION 1
            #   define UNITY_domain                 domain
            #   define UNITY_partitioning           partitioning
            #   define UNITY_outputtopology         outputtopology
            #   define UNITY_patchconstantfunc      patchconstantfunc
            #   define UNITY_outputcontrolpoints    outputcontrolpoints
            #endif
            
            
            // Apply Tessellation vertex
            #pragma vertex TessellationVertexProgram
            // #pragma vertex LitPassVertex

            #pragma fragment LitPassFragment
            // This line defines the name of the hull shader. 
            #pragma hull hull
            // This line defines the name of the domain shader. 
            #pragma domain domain

            // due to using Tessellation
            // https://docs.unity3d.com/2019.3/Documentation/Manual/SL-ShaderCompileTargets.html
            #pragma require tessellation
            #pragma target 4.6
            #pragma multi_compile _PARTITIONING_FRACTIONAL_ODD _PARTITIONING_FRACTIONAL_EVEN _PARTITIONING_POW2 _PARTITIONING_INTEGER


            // -------------------------------------
            // Material Keywords
            // unused shader_feature variants are stripped from build automatically
            // #pragma shader_feature _NormalMap
            
            // -------------------------------------
            // Universal Render Pipeline keywords
            // When doing custom shaders you most often want to copy and past these #pragmas
            // These multi_compile variants are stripped from the build depending on:
            // 1) Settings in the LWRP Asset assigned in the GraphicsSettings at build time
            // e.g If you disable AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
            // will be stripped from build
            // 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
            // but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // Optional Parameters
            #pragma shader_feature_local _DebugLod
            #pragma shader_feature_local _DebugNormal
            #pragma shader_feature_local _TSEnable
            #pragma shader_feature_local _TSDebug

            // Including the following two function is enought for shading with Universal Pipeline. Everything is included in them.
            // Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
            // It also includes matrix/space conversion functions and fog.
            // Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
            // that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

            // Required by all Universal Render Pipeline shaders.
            // It will include Unity built-in shader variables (except the lighting variables)
            // (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
            // It will also include many utilitary functions. 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Include this if you are doing a lit shader. This includes lighting shader variables,
            // lighting and shadow functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Material shader variables are not defined in SRP or LWRP shader library.
            // This means _BaseColor, _BaseMap, _BaseMap_ST, and all variables in the Properties section of a shader
            // must be defined by the shader itself. If you define all those properties in CBUFFER named
            // UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
            // of each drawcall.
            // In this case, for sinmplicity LitInput.hlsl is included. This contains the CBUFFER for the material
            // properties defined above. As one can see this is not part of the ShaderLibrary, it specific to the
            // LWRP Lit shader.
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 uvLM         : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Pre Tessellation - Extra vertex struct
            // should remain the same as struct - "Attributes"
            // Also copy on related to "TessellationVertexProgram"
            struct ControlPoint
            {
                float4 positionOS   : INTERNALTESSPOS;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 uvLM         : TEXCOORD1;
            };
            // tessellation data
            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            struct Varyings
            {
                half4 positionCS                : SV_POSITION;
                half4 positionWSAndFogFactor    : TEXCOORD0; // xyz: positionWS, w: vertex fog factor
                half3 normalWS                  : TEXCOORD1;
                half3 tangentWS                 : TEXCOORD2;
                half3 bitangentWS               : TEXCOORD3;
                half3 vertexLight               : TEXCOORD4;
                half4 uvLM                      : TEXCOORD5; // xy: UV, zw: uvLightMap
#ifdef _MAIN_LIGHT_SHADOWS
                half4 shadowCoord              : TEXCOORD6; // compute shadow coord per-vertex for the main light
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            struct ShareParams
            {
                half3 positionWS;
                half3 normalTS;
                half3 normalWS;
                half3 viewVectorWS;
                half3 viewDirectionWS;
                half fogCoord;
                half2 uv;
                half2 uvLM;
                half lod;
                half lodDistance;
            };

            //TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
            TEXTURE2D(_DetailMap);          SAMPLER(sampler_DetailMap);
            TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
            TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
            //TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
#if _TSEnable
            // SSS
            TEXTURE2D(_LightAmbient);       SAMPLER(sampler_LightAmbient);
            TEXTURE2D(_LightThickness);     SAMPLER(sampler_LightThickness);
#endif

            // In order to support VR & GPU Instancing
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseMap_ST);
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor);
                UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor);
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetailMap_ST);
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetailNormalMap_ST);
                UNITY_DEFINE_INSTANCED_PROP(half4, _LightAmbient_ST);
                UNITY_DEFINE_INSTANCED_PROP(half4, _LightThickness_ST);
#if _TSEnable
                UNITY_DEFINE_INSTANCED_PROP(half4, _TSAlbedo);
                UNITY_DEFINE_INSTANCED_PROP(half, _LightDistortion);
                UNITY_DEFINE_INSTANCED_PROP(half, _LightPower);
                UNITY_DEFINE_INSTANCED_PROP(half, _LightScale);
#endif
                UNITY_DEFINE_INSTANCED_PROP(half, _NormalScale);
                UNITY_DEFINE_INSTANCED_PROP(half, _DetailFader);
                UNITY_DEFINE_INSTANCED_PROP(half, _DetailGrayScale);
                UNITY_DEFINE_INSTANCED_PROP(half, _DetailNormalScale);
                UNITY_DEFINE_INSTANCED_PROP(half, _LODDistance);
                UNITY_DEFINE_INSTANCED_PROP(half, _LODBias);
                UNITY_DEFINE_INSTANCED_PROP(half, _Cutoff);
                UNITY_DEFINE_INSTANCED_PROP(half, _Metallic);
                UNITY_DEFINE_INSTANCED_PROP(half, _Smoothness);
                UNITY_DEFINE_INSTANCED_PROP(half, _OcclusionStrength);
                UNITY_DEFINE_INSTANCED_PROP(half, _Partitioning);
                UNITY_DEFINE_INSTANCED_PROP(half, _Tessellation);
                UNITY_DEFINE_INSTANCED_PROP(half, _MinTessDistance);
                UNITY_DEFINE_INSTANCED_PROP(half, _MaxTessDistance);
                
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            
            SurfaceData GetSurfaceData(Varyings IN, inout ShareParams share)
            {
                half4 i_BaseColor           = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                half4 i_BaseMap_ST          = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
                half4 i_EmissionColor       = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
                half i_Cutoff               = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
                
                // LOD
                half i_LODDistance          = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LODDistance);
                half i_LODBias              = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LODBias);
                half lod                    = ceil(smoothstep(0, i_LODDistance, length(share.viewVectorWS) * i_LODBias) * i_LODDistance);
                share.lod                   = lod;
                share.lodDistance           = i_LODDistance;
                //half debugLod = 1.0 - lod / _maxLodDistance;
                //return half4(debugLod, 0, 0, 1);

                // texture
                half4 albedo                = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, share.uv * i_BaseMap_ST.xy + i_BaseMap_ST.zw, share.lod);
                albedo.rgb                  *= i_BaseColor.rgb;

                // Detail texture
                half4 i_DetailMap_ST        = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailMap_ST);
                half i_DetailFader          = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailFader));
                half i_DetailGrayScale      = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailGrayScale));
                
                half4 detailMap             = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, share.uv * i_DetailMap_ST.xy + i_DetailMap_ST.zw, share.lod);
                half lumen                  = detailMap.a * (detailMap.r + detailMap.g + detailMap.b) / 3.0;
                detailMap.rgb               = lerp(detailMap.rgb, (half3)lumen, i_DetailGrayScale);
                albedo.rgb                  *= LerpWhiteTo(detailMap.rgb, i_DetailFader);

                // Surface Data
                SurfaceData surfaceData;
                surfaceData.albedo          = albedo.rgb;
                surfaceData.alpha           = Alpha(albedo.a, _BaseColor, i_Cutoff);
                
                // Specular/Metallic
                half i_Smoothness           = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness));
                surfaceData.metallic        = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic));
                surfaceData.specular        = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, share.uv).rgb;
                surfaceData.smoothness      = (surfaceData.specular.r + surfaceData.specular.g + surfaceData.specular.b)/3.0 * i_Smoothness; // (R+G+B)/3 = Lumen
                
                // Occlusion
                real i_OcclusionStrength    = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _OcclusionStrength);
#if defined(SHADER_API_GLES)
                surfaceData.occlusion       = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, share.uv).g;
#else
                real occ                    = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, share.uv).g;
                surfaceData.occlusion       = LerpWhiteTo(occ, i_OcclusionStrength);
#endif
                surfaceData.emission        = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, share.uv).rgb * i_EmissionColor.rgb;
                
                // Normal & detail normal
                // Unpack normal from zipped vector(U3D)
                half i_NormalScale          = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale));
                half i_DetailNormalScale    = saturate(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailNormalScale));
                half4 i_DetailNormalMap_ST  = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailNormalMap_ST);
                real3 normalTS              = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, share.uv), i_NormalScale); //UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), i_NormalScale);
                real3 dNormalTS             = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_DetailNormalMap, sampler_DetailNormalMap, share.uv * i_DetailNormalMap_ST.xy + i_DetailNormalMap_ST.zw, lod), i_DetailNormalScale);
                surfaceData.normalTS        = real3(normalTS.x + dNormalTS.x, normalTS.y + dNormalTS.y, normalTS.z);
                // convert normal - local to world space
                real3x3 tbn                 = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                share.normalWS              = normalize(mul(surfaceData.normalTS, tbn)); // TransformTangentToWorld(surfaceData.normalTS, tbn)


                surfaceData.clearCoatMask           = 0.0;
                surfaceData.clearCoatSmoothness     = 1.0;
                return surfaceData;
            }

            InputData GetInputData(Varyings IN, ShareParams share)
            {
                InputData inputData;
                inputData.positionWS        = share.positionWS;
                inputData.normalWS          = share.normalWS;
                inputData.viewDirectionWS   = share.viewDirectionWS;
                inputData.fogCoord          = share.fogCoord;
                inputData.vertexLighting    = IN.vertexLight;

#ifdef LIGHTMAP_ON
                // Normal is required in case Directional lightmaps are baked
                half3 bakedGI = SampleLightmap(uvLightMap, normalWS);
#else
                // Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
                // are also defined in case you want to sample some terms per-vertex.
                half3 bakedGI = SampleSH(share.normalWS);
#endif
                inputData.bakedGI           = bakedGI;
                
                // Apply environment light
#if _MAIN_LIGHT_SHADOWS
                inputData.shadowCoord       = IN.shadowCoord;
#else
                inputData.shadowCoord       = 0;
#endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                half4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
                half4 shadowMask = unity_ProbesOcclusion;
#else
                half4 shadowMask = half4(1, 1, 1, 1);
#endif
                inputData.shadowMask        = shadowMask;
                return inputData;
            }

            // Step 2) Triangle Indices
            // info so the GPU knows what to do (triangles) and how to set it up , clockwise, fractional division
            // hull takes the original vertices and outputs more
            [UNITY_domain("tri")]
            [UNITY_outputcontrolpoints(3)]
            [UNITY_outputtopology("triangle_cw")]
#if _PARTITIONING_FRACTIONAL_ODD 
            [UNITY_partitioning("fractional_odd")]
#endif
#if _PARTITIONING_FRACTIONAL_EVEN 
            [UNITY_partitioning("fractional_even")]
#endif
#if _PARTITIONING_POW2
            [UNITY_partitioning("pow2")]
#endif
#if _PARTITIONING_INTEGER
            [UNITY_partitioning("integer")]
#endif
            [UNITY_patchconstantfunc("patchConstantFunction")] // send data to here
            ControlPoint hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            // Step 3.1) optimization,
            // fade tessellation at a distance
            float CalcDistanceTessFactor(float4 vertex, float minDist, float maxDist, float tess)
            {
                float3 worldPosition = TransformObjectToWorld(vertex.xyz);
                float dist = distance(worldPosition, _WorldSpaceCameraPos);
                // Calculate the factor we need to apply tessellation 0.01 ~ 1;
                float f = clamp(1.0 - ((dist - minDist) / (maxDist - minDist)), 0.01, 1.0) * tess;
                return f;
            }

            // Step 3)
            // Tessellation, receive info from Hull, and patching data.
            TessellationFactors patchConstantFunction(InputPatch<ControlPoint, 3> patch)
            {
                // values for distance fading the tessellation
                // since wrong order will calculate in flipped result.
                // ensure the min/max values.
                float minDist = min(_MinTessDistance, _MaxTessDistance);
                float maxDist = max(_MinTessDistance, _MaxTessDistance);

                TessellationFactors f;
                // half _t = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Tessellation);
                float edge0 = CalcDistanceTessFactor(patch[0].positionOS, minDist, maxDist, _Tessellation);
                float edge1 = CalcDistanceTessFactor(patch[1].positionOS, minDist, maxDist, _Tessellation);
                float edge2 = CalcDistanceTessFactor(patch[2].positionOS, minDist, maxDist, _Tessellation);

                // make sure there are no gaps between different tessellated distances, by averaging the edges out.
                f.edge[0] = (edge1 + edge2) / 2;
                f.edge[1] = (edge2 + edge0) / 2;
                f.edge[2] = (edge0 + edge1) / 2;
                f.inside = (edge0 + edge1 + edge2) / 3;
                return f;
            }
            
            // Step 1) prepare Vertices data to Hull program
            // Pre tesselation vertex program
            ControlPoint TessellationVertexProgram(Attributes IN)
            {
                ControlPoint p;
                p.positionOS    = IN.positionOS;
                p.normalOS      = IN.normalOS;
                p.tangentOS     = IN.tangentOS;
                p.uv            = IN.uv;
                p.uvLM          = IN.uvLM;

                return p;
            }

#if _TSEnable
            struct TranslucencyConfig
            {
                half4   albedo;
                half    ambient;
                half    thickness;
                half    lightDistortion;
                half    lightPower;
                half    lightScale;
            };

            // https://www.slideshare.net/colinbb/colin-barrebrisebois-gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurfacescattering-look-7170855
            half3 TranslucencyScattering(half3 viewDir, half3 normal, Light light, TranslucencyConfig config)
            {
                // TranslucencyScattering
                half3 L = light.direction + normal * (1.0 - config.lightDistortion);
                half power = saturate(1.00001 - config.lightPower) * 10;
                half fLTDot = pow(saturate(dot(viewDir, -L)), power) * config.lightScale;
                
                // I think above was wrong implementation, instead of add "fLTDot" should be multiply
                // half fLT = light.distanceAttenuation * (config.ambient + fLTDot) * config.thickness;
                half fLT = light.distanceAttenuation * config.ambient * fLTDot * config.thickness; // 20220327 - Canis edition.

                return config.albedo.rgb * light.color * fLT;
            }

            TranslucencyConfig GetTransluencyConfig(half2 uv)
            {
                TranslucencyConfig tsConfig;
                half4 t0                    = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LightAmbient_ST);
                half4 t1                    = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LightThickness_ST);
                tsConfig.ambient            = SAMPLE_TEXTURE2D(_LightAmbient, sampler_LightAmbient, uv * t0.xy + t0.zw).r;
                tsConfig.thickness          = SAMPLE_TEXTURE2D(_LightThickness, sampler_LightThickness, uv * t1.xy + t1.zw).r;
                tsConfig.albedo             = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _TSAlbedo);
                tsConfig.lightDistortion    = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LightDistortion);
                tsConfig.lightPower         = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LightPower);
                tsConfig.lightScale         = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _LightScale);
                return tsConfig;
            }
#endif
            
            // Ref : Lighting.hlsl > LightingPhysicallyBased(...)
            half3 PerBRDFLight(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light,
                half3 normalWS, half3 viewDirectionWS,
                half clearCoatMask, bool specularHighlightsOff)
            {
                half NdotL = saturate(dot(normalWS, light.direction));
                half3 radiance = light.color * (light.distanceAttenuation * NdotL);

                half3 brdf = brdfData.diffuse;
            #ifndef _SPECULARHIGHLIGHTS_OFF
                [branch] if (!specularHighlightsOff)
                {
                    brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, light.direction, viewDirectionWS);
            #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
                    // Clear coat evaluates the specular a second timw and has some common terms with the base specular.
                    // We rely on the compiler to merge these and compute them only once.
                    half brdfCoat = kDielectricSpec.r * DirectBRDFSpecular(brdfDataClearCoat, normalWS, light.direction, viewDirectionWS);

                        // Mix clear coat and base layer using khronos glTF recommended formula
                        // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
                        // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
                        half NoV = saturate(dot(normalWS, viewDirectionWS));
                        // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
                        // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
                        half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);

                    brdf = brdf * (1.0 - clearCoatMask * coatFresnel) + brdfCoat * clearCoatMask;
            #endif // _CLEARCOAT
                }
                
            #endif // _SPECULARHIGHLIGHTS_OFF

                return brdf * radiance;
            }

            // Study Unity3D's URP Lit shader here:
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            half3 CalBRDFLight(SurfaceData surfaceData, InputData inputData, ShareParams share)
            {
            #ifdef _SPECULARHIGHLIGHTS_OFF
                bool specularHighlightsOff = true;
            #else
                bool specularHighlightsOff = false;
            #endif
            
                // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
                // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
                // below with your own.
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
                
                BRDFData brdfDataClearCoat = (BRDFData)0;
            #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
                // base brdfData is modified here, rely on the compiler to eliminate dead computation by InitializeBRDFData()
                InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData, brdfDataClearCoat);
            #endif

            // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
            #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                half4 shadowMask = inputData.shadowMask;
            #elif !defined (LIGHTMAP_ON)
                half4 shadowMask = unity_ProbesOcclusion;
            #else
                half4 shadowMask = half4(1, 1, 1, 1);
            #endif
                // Light struct is provide by LWRP to abstract light shader variables.
                // It contains light direction, color, distanceAttenuation and shadowAttenuation.
                // LWRP take different shading approaches depending on light and platform.
                // You should never reference light shader variables in your shader, instead use the GetLight
                // funcitons to fill this Light struct.
            #ifdef _MAIN_LIGHT_SHADOWS
                // Main light is the brightest directional light.
                // It is shaded outside the light loop and it has a specific set of variables and shading path
                // so we can be as fast as possible in the case when there's only a single directional light
                // You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
                // computed.
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
            #else
                Light mainLight = GetMainLight();
            #endif

            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                mainLight.color *= aoFactor.directAmbientOcclusion;
                surfaceData.occlusion = min(surfaceData.occlusion, aoFactor.indirectAmbientOcclusion);
            #endif

                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

                // Mix diffuse GI with environment reflections.
                // half3 gi = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, share.normalWS, share.viewDirectionWS);
                half3 gi = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                            inputData.bakedGI, surfaceData.occlusion,
                                            inputData.normalWS, inputData.viewDirectionWS);

                // LightingPhysicallyBased computes direct light contribution.
                half3 lightSrc = PerBRDFLight(brdfData, brdfDataClearCoat, mainLight,
                                     inputData.normalWS, inputData.viewDirectionWS,
                                     surfaceData.clearCoatMask, specularHighlightsOff);
#if _TSEnable
                // https://www.slideshare.net/colinbb/colin-barrebrisebois-gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurfacescattering-look-7170855
                TranslucencyConfig tsConfig = GetTransluencyConfig(share.uv);
    #if !_TSDebug
                lightSrc.rgb        += TranslucencyScattering(inputData.viewDirectionWS, inputData.normalWS, mainLight, tsConfig);
    #else
                lightSrc.rgb        = TranslucencyScattering(inputData.viewDirectionWS, inputData.normalWS, mainLight, tsConfig);
    #endif // TSDebug
#endif

                // Additional lights loop
#ifdef _ADDITIONAL_LIGHTS
                // Returns the amount of lights affecting the object being renderer.
                // These lights are culled per-object in the forward renderer
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightsCount; ++i)
                {
                    // Similar to GetMainLight, but it takes a for-loop index. This figures out the
                    // per-object light index and samples the light buffer accordingly to initialized the
                    // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
                    Light light = GetAdditionalLight(i, inputData.positionWS);

#if !_TSEnable
                    lightSrc += PerBRDFLight(brdfData, brdfDataClearCoat, light,
                                         inputData.normalWS, inputData.viewDirectionWS,
                                         surfaceData.clearCoatMask, specularHighlightsOff);
#else
    #if !_TSDebug
                    lightSrc += PerBRDFLight(brdfData, brdfDataClearCoat, light,
                                         inputData.normalWS, inputData.viewDirectionWS,
                                         surfaceData.clearCoatMask, specularHighlightsOff);
                    lightSrc += TranslucencyScattering(inputData.viewDirectionWS, inputData.normalWS, light, tsConfig);
    #else
                    lightSrc += TranslucencyScattering(inputData.viewDirectionWS, inputData.normalWS, light, tsConfig);
    #endif // TSDebug
#endif // _TSEnable
                }
#endif // _ADDITIONAL_LIGHTS

            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                lightSrc += inputData.vertexLighting * brdfData.diffuse;
            #endif


                return gi + lightSrc + surfaceData.emission;
            }

            Varyings LitPassVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                
                // https://docs.unity3d.com/Manual/SinglePassInstancing.html // Support in 2020.3
                UNITY_SETUP_INSTANCE_ID(IN);
                //UNITY_INITIALIZE_OUTPUT(Varyings, OUT); // Support in 2020.3
                // https://docs.unity3d.com/Manual/SinglePassStereoRendering.html
                // https://docs.unity3d.com/Manual/Android-SinglePassStereoRendering.html
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT); // necessary only if you want to access instanced properties in the fragment Shader.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); // VR support - Single-Pass Stereo Rendering for Android

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                // TRANSFORM_TEX is the same as the old shader library.
                OUT.uvLM.xy = IN.uv; // TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvLM.zw = IN.uv * unity_LightmapST.xy + unity_LightmapST.zw;

                // We just use the homogeneous clip position from the vertex input
                OUT.positionCS = vertexInput.positionCS;
                // Computes fog factor per-vertex.
                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                OUT.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);

                // Here comes the flexibility of the input structs.
                // In the variants that don't have normal map defined
                // tangentWS and bitangentWS will not be referenced and
                // GetnormalInputs is only converting normal
                // from object to world space
                OUT.normalWS = normalInput.normalWS;
                OUT.tangentWS = normalInput.tangentWS;
                OUT.bitangentWS = normalInput.bitangentWS;

                OUT.vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

#ifdef _MAIN_LIGHT_SHADOWS
                // shadow coord for the main light is computed in vertex.
                // If cascades are enabled, LWRP will resolve shadows in screen space
                // and this coord will be the uv coord of the screen space shadow texture.
                // Otherwise LWRP will resolve shadows in light space (no depth pre-pass and shadow collect pass)
                // In this case shadowCoord will be the position in light space.
                OUT.shadowCoord = GetShadowCoord(vertexInput);
#endif
                return OUT;
            }

            // Step 4)
            // prepare vertices & triangles data for Geomertry Program
            // In order work, send data to org vertex program.
            [UNITY_domain("tri")]
            Varyings domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
            {
                Attributes v;

                #define DomainPos(fieldName) v.fieldName = \
				patch[0].fieldName * barycentricCoordinates.x + \
				patch[1].fieldName * barycentricCoordinates.y + \
				patch[2].fieldName * barycentricCoordinates.z;

                DomainPos(positionOS)
                DomainPos(normalOS)
                DomainPos(tangentOS)
                DomainPos(uv)
                DomainPos(uvLM)

                return LitPassVertex(v);
            }
            
            half4 LitPassFragment(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); // VR support - Single-Pass Stereo Rendering for Android

                // Surface data contains albedo, metallic, specular, smoothness, occlusion, emission and alpha
                // InitializeStandarLitSurfaceData initializes based on the rules for standard shader.
                // You can write your own function to initialize the surface data of your shader.
                ShareParams share = (ShareParams)0;
                share.positionWS            = IN.positionWSAndFogFactor.xyz;
                // share.normalWS              = half3(0,0,1); // not ready yet.
                share.viewVectorWS          = GetCameraPositionWS() - share.positionWS;
                share.viewDirectionWS       = SafeNormalize(share.viewVectorWS);
                share.fogCoord              = IN.positionWSAndFogFactor.w;
                share.uv                    = IN.uvLM.xy;
                share.uvLM                  = IN.uvLM.zw;

                // Surface Data
                SurfaceData surfaceData     = GetSurfaceData(IN, share);
                InputData inputData         = GetInputData(IN, share);

#if _DebugNormal 
                return half4(surfaceData.normalTS, 1);
#endif


                half3 brdf = CalBRDFLight(surfaceData, inputData, share);
                
                // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
                // with a custom one.
                half3 color = MixFog(brdf, inputData.fogCoord);

#if _DebugLod
                half debugLod               = share.lod / max(1.0,share.lodDistance);
                color.gb *= debugLod;
#endif
                return half4(color, surfaceData.alpha);
            }
            ENDHLSL
        }

        // Used for rendering shadowmaps
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        // Used for depth prepass
        // If shadows cascade are enabled we need to perform a depth prepass. 
        // We also need to use a depth prepass in some cases camera require depth texture
        // (e.g, MSAA is enabled and we can't resolve with Texture2DMS
        UsePass "Universal Render Pipeline/Lit/DepthOnly"

        UsePass "Universal Render Pipeline/Lit/DepthNormals"

        // Used for Baking GI. This pass is stripped from build.
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    // Uses a custom shader GUI to display settings. Re-use the same from Lit shader as they have the
    // same properties.
    // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}