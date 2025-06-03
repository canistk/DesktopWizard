// https://developer.amd.com/wordpress/media/2012/10/Scheuermann_HairSketchSlides.pdf
// https://zhuanlan.zhihu.com/p/363829203
// https://blog.csdn.net/noahzuo/article/details/51162472
// Important !!!!
// require MultiRenderPassFeature.cs to run this shader.
// add renderer feature to unlock Transparent-2, -1, +1, +2
Shader "Kit/Universal Render Pipeline/Kajiya Kay Hair"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (0.5,0.5,0.5,1)
        _MidColor("Mid Color", Color) = (0.5,0.5,0.5,1)
        _DarkColor("Dark Color", Color) = (0.5,0.5,0.5,1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _AOTex ("Ambient Occlusion", 2D) = "white" {}

        [header(Rim Light)]
        [HDR]_RimColor("Rim Color", Color) = (1.0,1.0,1.0,1.0)
        _RimLightPower("Rim Light Power", Range(0, 5)) = 1.0
        _RimLightShadow("Rim Light Shadow", Range(0, 1)) = 1.0
        [Slider]_RimRangeX("Rim Range Start", Range(0.0,1.0)) = 0.3
        [Slider]_RimRangeY("Rim Range End", Range(0.0,1.0)) = 0.4

        [Space(20)]
        [header(Hair Strand)]
        _SpecularShift("Hard strand specular shift Sample", 2D) = "white" {}
        [HDR]_PrimaryColor("Primary Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _PrimaryV40("shift,power,sharp,weight", Vector) = (0.5, 1.0, 8.0, 1.0)
        _PrimaryV41("width,feather,dissolve,none", Vector) = (0.0, 1.0, 1.0, 1.0)
        [Space]
        [HDR]_SecondaryColor("Secondary Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SecondaryV40("shift,power,sharp,weight", Vector) = (0.5, 1.0, 8.0, 1.0)
        _SecondaryV41("width,feather,dissolve,none", Vector) = (0.0, 1.0, 1.0, 1.0)

        [Space]
        _ParallaxDepthMap("Parallax Depth Map", 2D) = "white" {}
        _ParallaxV40("layer,fading,u,v", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ParallaxV41("shift,height,", Vector) = (0.0, 0.0, 0.0, 0.0)

        [header(Misc)]
        [Toggle(_KIT_ADDITIONAL_LIGHTS)] _KIT_ADDITIONAL_LIGHTS("Additional Light", Float) = 1.0
        
        //[Header(Pass Debug)]
        //[Toggle(ZWrite)] _ZWrite("_ZWrite (default = off)", float) = 0
        //// https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
        //// 4 = LessEqual = Default
        //// 0 = Disable, to improve GPU performance
        //[Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("_ZTest (default = LessEqual)", float) = 4
        //// https://docs.unity3d.com/ScriptReference/Rendering.CullMode.html
        //// 0 = Off, 1 = Front, 2 = Back
        //[Enum(UnityEngine.Rendering.CullMode)]_Cull("_Cull (default = Off)", float) = 0
        //// https://docs.unity3d.com/ScriptReference/Rendering.BlendMode.html
        //[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("_SrcBlend (default = SrcAlpha)", float) = 5 // 5 = SrcAlpha
        //[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("_DstBlend (default = OneMinusSrcAlpha)", float) = 10 // 10 = OneMinusSrcAlpha
    }

    SubShader
    {
        // https://docs.unity3d.com/Manual/SL-SubShaderTags.html
        Tags {
            //"RenderType" = "TransparentCutout"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Transparent"
            //"Queue" = "Transparent" // Queue : { Background, Geometry, AlphaTest, Transparent, Overlay }
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector"   = "True"
            // "ShaderModel"="4.5"
        }
        LOD 300

        Cull Off
        // AlphaToMask On

        Pass
        {
            // pass 1
            Name "Alpha Mask"
            Tags
            {
                //"LightMode" = "Transparent-2"
                //"LightMode" = "UniversalForward"
                "LightMode" = "SRPDefaultUnlit"
            }
            Cull Back
            ZWrite On
            ZTest Less
            //Blend SrcAlpha OneMinusSrcAlpha
            //BlendOp Max
            //AlphaToMask On
            //AlphaTest Less 0.5
            //AlphaTest Greater 0.5
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex HairVertexShaderAlpha
            #pragma fragment HairFragmentShaderAlpha
            #pragma target 3.0
            #pragma multi_compile_instancing

            // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/Kit_KajiyaKay.hlsl"
            ENDHLSL
        }

        Pass
        {
            // Pass 2
            Name "ForwardLit"
            Tags {
                //"LightMode" = "Transparent-1"
                "LightMode" = "UniversalForward"
                //"LightMode" = "SRPDefaultUnlit"
            }

            // https://docs.unity3d.com/Manual/shader-shaderlab-commands.html
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add
            //AlphaToMask On
            //AlphaTest Less 0.1
            // ColorMask 0

            HLSLPROGRAM
            #pragma vertex HairVertexShader
            //#pragma fragment HairFragmentShaderRed
            #pragma fragment HairFragmentShader
            #pragma target 3.0
            #pragma shader_feature_local _KIT_ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing
            //#pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #include "../ShaderLibrary/Kit_KajiyaKay.hlsl"
            ENDHLSL
        }

        Pass
        {
            // Pass 3
            Tags {
                "LightMode" = "Transparent+3"
            }

            // https://docs.unity3d.com/Manual/shader-shaderlab-commands.html
            Cull Front
            ZWrite Off
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add
            //AlphaToMask On
            //AlphaTest Less 0.1
            //ColorMask BG

            HLSLPROGRAM
            #pragma vertex HairVertexShader
            //#pragma fragment HairFragmentShaderBlue
            #pragma fragment HairFragmentShader
            #pragma target 3.0
            #pragma shader_feature_local _KIT_ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/Kit_KajiyaKay.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            // Pass 4
            Tags {
                "LightMode" = "Transparent+4"
            }

            // https://docs.unity3d.com/Manual/shader-shaderlab-commands.html
            Cull Back
            ZWrite On
            ZTest Less    
            Blend SrcAlpha OneMinusSrcAlpha
            //AlphaToMask On
            //AlphaTest Less 0.1
            //ColorMask 0

            HLSLPROGRAM
            #pragma vertex HairVertexShader
            //#pragma fragment HairFragmentShaderGreen
            #pragma fragment HairFragmentShader
            #pragma target 3.0
            #pragma shader_feature_local _KIT_ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/Kit_KajiyaKay.hlsl"
            ENDHLSL
        }

        // Used for rendering shadowmaps
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    
    // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
    CustomEditor "KajiyaKayHairShaderGUI"
}