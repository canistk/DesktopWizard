/*
// required properties


        [Header(Translucency Scattering)]
        [Toggle(_TSEnable)] _TSEnable ("Translucency", Float) = 1
        [Toggle(_TSDebug)] _TSDebug("Debug Translucency", Float) = 0
        [KeywordEnum(Multiply,Screen,Overlay,Lighten,Inner,FakeSSS)]
        _TSBlend("Light Blending (default = Multiply)", Float) = 0
        _TSAlbedo ("Subcutaneous Color", color) = (0.2,0.14,0.08,1)
        // translucency that is always present, both front and back.
        _LightAmbient("Translucency Ambient", 2D) = "white" {}
        [Toggle(_TSFlip)] _TSFlip("Invert Texture", Float) = 0
        // Local thickness map, used for both direct and indirect translucency
        _LightThickness("Local Thickness Map", 2D) = "white" {}
        // Subsurface distortion, Breaks continuity, view-dependent, allows for more organic, Fresnel-like
        _LightDistortion("Subsurface Distortion", Range(0,1)) = 0.5
        // power value for direct translucency
        _LightPower("Translucency Power", Range(0,1)) = 1
        // Direct/Back translucency, View-oriented, Should be defined per-light to control the central point.
        _LightScale("Translucency Scale(view)", Range(0.01,10)) = 1
        _LightOffset("Edge Offset", Range(0.0, 1.0)) = 0
*/


#ifndef KitTranslucencyScattering
#define KitTranslucencyScattering
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// #include_with_pragmas 
// https://docs.unity3d.com/Manual/shader-include-directives.html
#pragma shader_feature_local _TSEnable
#pragma shader_feature_local _TSDebug
#pragma multi_compile _TSBLEND_MULTIPLY _TSBLEND_SCREEN _TSBLEND_OVERLAY _TSBLEND_LIGHTEN _TSBLEND_INNER _TSBLEND_FAKESSS
#pragma shader_feature_local _TSFlip

#if _TSEnable
            // SSS
            TEXTURE2D(_LightAmbient);       SAMPLER(sampler_LightAmbient);
            TEXTURE2D(_LightThickness);     SAMPLER(sampler_LightThickness);
UNITY_INSTANCING_BUFFER_START(TranslucencyScattering)
                UNITY_DEFINE_INSTANCED_PROP(real4, _LightAmbient_ST);
                UNITY_DEFINE_INSTANCED_PROP(real4, _LightThickness_ST);
                UNITY_DEFINE_INSTANCED_PROP(real4, _TSBlend);
                UNITY_DEFINE_INSTANCED_PROP(real4, _TSAlbedo);
                UNITY_DEFINE_INSTANCED_PROP(real, _LightDistortion);
                UNITY_DEFINE_INSTANCED_PROP(real, _LightPower);
                UNITY_DEFINE_INSTANCED_PROP(real, _LightScale);
                UNITY_DEFINE_INSTANCED_PROP(real, _LightOffset);
UNITY_INSTANCING_BUFFER_END(TranslucencyScattering)
#endif

struct TranslucencyConfig
{
    real4   albedo;
    real    ambient;
    real    thickness;
    real    lightDistortion;
    real    lightPower;
    real    lightScale;
    real    lightOffset;
};

// https://www.slideshare.net/colinbb/colin-barrebrisebois-gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurfacescattering-look-7170855
real3 TranslucencyScattering(real3 viewDir, real3 normal, Light light, TranslucencyConfig config)
{
    // TranslucencyScattering
    real3 L = light.direction + normal * (1.0 - saturate(config.lightDistortion) + config.lightOffset);
    real power = saturate(1.00001 - config.lightPower); // 0.0001 ~ 1.0
    real VDotL = saturate(dot(viewDir, L));
    real VDotNL = saturate(dot(viewDir, -L));
    // real fresnel = saturate(pow(1.0 - dot(normal, viewDir), config.lightOffset));
    real fLTDot = pow(VDotNL, power) * config.lightScale;
                
    // I think above was wrong implementation, instead of add "fLTDot" should be multiply
    // real fLT = light.distanceAttenuation * (config.ambient + fLTDot) * config.thickness;
    real fLT = light.distanceAttenuation * config.ambient * fLTDot * config.thickness; // 20220327 - Canis edition.
                
#if _TSBLEND_MULTIPLY
    return config.albedo.rgb * light.color * fLT; // Org
#elif _TSBLEND_SCREEN
    return (1 - (1 - light.color) * (1 - config.albedo.rgb)) * fLT;
#elif _TSBLEND_OVERLAY
    real3 multiply = light.color * config.albedo.rgb * 2.0;
    real3 screen = (1 - (1 - light.color) * (1 - config.albedo.rgb));
    real lumen = (light.color.r + light.color.g + light.color.b) / 3.0;
    return lerp(multiply,screen,lumen) * fLT;
#elif _TSBLEND_LIGHTEN
    return real3(max(light.color.r, config.albedo.r),
                max(light.color.g, config.albedo.g),
                max(light.color.b, config.albedo.b)) * fLT;
#elif _TSBLEND_INNER
    real blendLight = saturate(VDotNL * 0.5 + 0.5); // -1 ~ 1 => 0 ~ 1
    real fresnel0 = saturate(pow(saturate(1.0 - dot(normal, viewDir)), blendLight));
    return lerp(light.color, config.albedo.rgb * light.color, fresnel0) * fLT;
#elif _TSBLEND_FAKESSS
    real blendLight = saturate(VDotNL * 0.5 + 0.5); // -1 ~ 1 => 0 ~ 1
    return lerp(config.albedo.rgb, config.albedo.rgb * light.color, blendLight) * fLT;
#endif
}

TranslucencyConfig GetTransluencyConfig(real2 uv)
{
    TranslucencyConfig tsConfig;
#if _TSEnable
    tsConfig.albedo             = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _TSAlbedo);
    tsConfig.lightDistortion    = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightDistortion);
    tsConfig.lightPower         = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightPower);
    tsConfig.lightScale         = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightScale);
    tsConfig.lightOffset        = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightOffset);
    real4 t0                    = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightAmbient_ST);
    real4 t1                    = UNITY_ACCESS_INSTANCED_PROP(TranslucencyScattering, _LightThickness_ST);
    tsConfig.ambient            = SAMPLE_TEXTURE2D(_LightAmbient, sampler_LightAmbient, uv * t0.xy + t0.zw).r;
#if _TSFlip
    tsConfig.ambient            = 1 - tsConfig.ambient;
#endif
    tsConfig.thickness          = SAMPLE_TEXTURE2D(_LightThickness, sampler_LightThickness, uv * t1.xy + t1.zw).r;
#endif
    return tsConfig;
}
#endif