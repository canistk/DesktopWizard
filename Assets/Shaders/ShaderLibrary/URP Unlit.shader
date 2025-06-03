Shader "Kit/Universal Render Pipeline/URP Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("_Color (default = 1,1,1,1)", color) = (1,1,1,1)

        [Header(Unity Fog)]
        [Toggle(_UnityFogEnable)] _UnityFogEnable("_UnityFogEnable (default = on)", float) = 1

        [Header(ZWrite)]
        [Toggle(ZWrite)] _ZWrite("_ZWrite (default = on)", float) = 1

        [Header(ZTest)]
        // https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        // 4 = LessEqual = Default
        // 0 = Disable, to improve GPU performance
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("_ZTest (default = LessEqual)", float) = 4

        [Header(Cull)]
        // https://docs.unity3d.com/ScriptReference/Rendering.CullMode.html
        // 0 = Off, 1 = Front, 2 = Back
        [Enum(UnityEngine.Rendering.CullMode)]_Cull("_Cull (default = Back)", float) = 2

        [Header(Blending)]
        // https://docs.unity3d.com/ScriptReference/Rendering.BlendMode.html
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("_SrcBlend (default = SrcAlpha)", float) = 5 // 5 = SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("_DstBlend (default = OneMinusSrcAlpha)", float) = 10 // 10 = OneMinusSrcAlpha
        

        //====================================== below = usually can ignore in normal use case =====================================================================
        [Header(Stencil Masking)]
        // https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        _StencilRef("_StencilRef", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("_StencilComp (default = Disable)", float) = 0 //0 = disable

    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        // https://docs.unity3d.com/Manual/SL-SubShaderTags.html
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent" // Queue : { Background, Geometry, AlphaTest, Transparent, Overlay }
            "RenderPipeline" = "UniversalPipeline"
            // "DisableBatching" = "True"
        }
        LOD 100
		
        Pass
        {
            // https://docs.unity3d.com/Manual/shader-shaderlab-commands.html
            Cull[_Cull]
            ZTest[_ZTest]
            ZWrite[_ZWrite]
            Blend[_SrcBlend][_DstBlend]

            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex UnlitVertexShader
            // This line defines the name of the fragment shader. 
            #pragma fragment UnlitFragmentShader
            // make fog work
            #pragma multi_compile_fog
            // GPU instancing
            #pragma multi_compile_instancing

            // due to using ddx() & ddy()
            #pragma target 3.0

            #pragma shader_feature_local _UnityFogEnable

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // In order to support VR & GPU Instancing
            SAMPLER(_MainTex);
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color);
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                half4   positionOS  : POSITION;
                half2   uv          : TEXCOORD0;
                half3   normalOS    : NORMAL;
                half4   tangentOS   : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                half4   positionCS  : SV_POSITION;
                half3   uv_fog      : TEXCOORD0; // uv = xy, fog = z
                half3   positionWS  : TEXCOORD1;
                half3   normalWS    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings UnlitVertexShader(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;

                // https://docs.unity3d.com/Manual/SinglePassInstancing.html // Support in 2020.3
                UNITY_SETUP_INSTANCE_ID(IN);
                //UNITY_INITIALIZE_OUTPUT(Varyings, OUT); // Support in 2020.3
                // https://docs.unity3d.com/Manual/SinglePassStereoRendering.html
                // https://docs.unity3d.com/Manual/Android-SinglePassStereoRendering.html
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); // VR support - Single-Pass Stereo Rendering for Android
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT); // necessary only if you want to access instanced properties in the fragment Shader.


                // VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space, ndc)
                // Unity compiler will strip all unused references (say you don't use view space).
                // Therefore there is more flexibility at no additional cost with this struct.
                VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInput.normalWS;

                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionCS = vertexPositionInput.positionCS;
                // OUT.positionNDC = vertexPositionInput.positionNDC;
                // OUT.positionVS = vertexPositionInput.positionVS;
                OUT.positionWS = vertexPositionInput.positionWS;

                // regular unity fog
#if _UnityFogEnable
                OUT.uv_fog = half3(IN.uv, ComputeFogFactor(OUT.positionCS.z));
#else
                OUT.uv_fog = half3(IN.uv, 0);
#endif
                return OUT;
            }

            // The fragment shader definition.            
            half4 UnlitFragmentShader(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); // VR support - Single-Pass Stereo Rendering for Android
                // Read GPU instancing data
                float4 i_color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);

                // Defining the color variable and returning it.
                half4 col = i_color * tex2D(_MainTex, IN.uv_fog.xy);

#if _UnityFogEnable
                // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
                // with a custom one.
                col.rgb = MixFog(col.rgb, IN.uv_fog.z);
#endif
                return col;
            }
            ENDHLSL
        }
    }
}