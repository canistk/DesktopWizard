Shader "Heroine/Eyebrows"
{
    Properties
    {
        [MainColor]_Color("Base Color", Color) = (1,1,1,1)
        [MainTexture]_MainTex ("Main Texture", 2D) = "white" {}
        _ViewBias ("View Bias", Float) = 0.05
        
    }

    SubShader
    {
        // https://docs.unity3d.com/Manual/SL-SubShaderTags.html
        Tags {
            "RenderType"        = "Opaque"
            "Queue"             = "Geometry" // Queue : { Background, Geometry, AlphaTest, Transparent, Overlay }
            "RenderPipeline"    = "UniversalPipeline"
            "DisableBatching"   = "True"
        }
        LOD 100
        Cull Off
        ZWrite On
		
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Include this if you are doing a lit shader. This includes lighting shader variables,
            // lighting and shadow functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "../ShaderLibrary/KitShader.hlsl"
            
            #define MY_TEXTURE(name, uv) SAMPLE_TEXTURE2D(name, sampler##name, uv.xy * name##_ST.xy + name##_ST.zw)

            //TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            
            
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(real4,  _MainTex_ST);
                UNITY_DEFINE_INSTANCED_PROP(real4,  _Color);
                UNITY_DEFINE_INSTANCED_PROP(real,   _ViewBias);
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                real4 positionOS    : POSITION;
                real3 normalOS      : NORMAL;
                real2 uv            : TEXCOORD0;
                real4 color         : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                real4 positionCS    : SV_POSITION;
                real4 color         : COLOR;
                real3 normalWS      : NORMAL;
                real2 uv            : TEXCOORD0;
                real3 positionWS    : TEXCOORD1;
                real3 vertexLight   : TEXCOORD2;
#ifdef _MAIN_LIGHT_SHADOWS
                real4 shadowCoord   : TEXCOORD3; // compute shadow coord per-vertex for the main light
#endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            InputData GetInputData(Varyings IN)
            {
                InputData inputData;
                inputData.positionWS        = IN.positionWS;
                inputData.normalWS          = IN.normalWS;
                inputData.viewDirectionWS   = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                inputData.fogCoord          = ComputeFogFactor(IN.positionCS.z);
                inputData.vertexLighting    = IN.vertexLight;
#if _MAIN_LIGHT_SHADOWS
                inputData.shadowCoord       = IN.shadowCoord;
#else
                inputData.shadowCoord       = 0;
#endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                real4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
                real4 shadowMask = unity_ProbesOcclusion;
#else
                real4 shadowMask = real4(1, 1, 1, 1);
#endif
                inputData.shadowMask        = shadowMask;
                return inputData;
            }
            
            Varyings LitPassVertex(Attributes IN)
            {
                // https://docs.unity3d.com/Manual/SinglePassInstancing.html // Support in 2020.3
                UNITY_SETUP_INSTANCE_ID(IN);
                //UNITY_INITIALIZE_OUTPUT(Varyings, OUT); // Support in 2020.3
                // https://docs.unity3d.com/Manual/SinglePassStereoRendering.html
                // https://docs.unity3d.com/Manual/Android-SinglePassStereoRendering.html
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT); // necessary only if you want to access instanced properties in the fragment Shader.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); // VR support - Single-Pass Stereo Rendering for Android

                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
     
                /// https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl
                //real3 vPos = TransformWorldToView(vertexInput.positionWS.xyz);
                //vPos.z += _ViewBias;
                //real3 wPos = TransformViewToWorld(vPos.xyz);
                //real4 cPos = TransformWorldToHClip(wPos.xyz);

                real3 wPos  = vertexInput.positionWS.xyz;
                real3 dir   = normalize(_WorldSpaceCameraPos - wPos);
                real3 wPos2 = wPos + dir * _ViewBias;
                real4 cPos2 = TransformWorldToHClip(wPos2);

                OUT.positionCS = cPos2;
                OUT.positionWS = wPos2;

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                OUT.uv   = IN.uv;
                OUT.vertexLight = VertexLighting(OUT.positionWS, OUT.normalWS);
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

            // The fragment shader definition.            
            real4 LitPassFragment(Varyings IN) : SV_Target
            {
                InputData inputData = GetInputData(IN);

                
                // Albedo
                real3 diffuse   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                
                
            #ifdef _MAIN_LIGHT_SHADOWS
                // Main light is the brightest directional light.
                // It is shaded outside the light loop and it has a specific set of variables and shading path
                // so we can be as fast as possible in the case when there's only a single directional light
                // You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
                // computed.
                Light mainlight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
            #else
                Light mainlight = GetMainLight();
            #endif

                real3 baseColor = lerp(diffuse, diffuse * _Color.rgb, _Color.a);
                real NdotL01 = dot(normalize(IN.normalWS), mainlight.direction) * 0.5 + 0.5;
                real sampleU    = saturate(NdotL01 * mainlight.distanceAttenuation);
                return real4(baseColor * sampleU, 1);
            }
            ENDHLSL
        }
        
        // Used for rendering shadowmaps
        // UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        
        // Used for depth prepass
        // If shadows cascade are enabled we need to perform a depth prepass. 
        // We also need to use a depth prepass in some cases camera require depth texture
        // (e.g, MSAA is enabled and we can't resolve with Texture2DMS
        //UsePass "Universal Render Pipeline/Lit/DepthOnly"
        
        //UsePass "Universal Render Pipeline/Lit/DepthNormals"

        // Used for Baking GI. This pass is stripped from build.
        // UsePass "Universal Render Pipeline/Lit/Meta"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}