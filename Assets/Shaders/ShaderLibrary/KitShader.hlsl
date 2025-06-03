//avoid multiple imports
#ifndef KIT_SHADER_COMMON
#define KIT_SHADER_COMMON
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Lerp.hlsl"
#include "VectorExtend.hlsl"

struct LightInfo
{
    real3 viewDirection;
    real3 normalDirection;
    real3 lightDirection;
    real3 lightReflectDirection;
    real3 viewReflectDirection;
    real3 halfDirection;
    real NdotL;
    real NdotH;
    real NdotV;
    real VdotH;
    real LdotH;
    real LdotV;
    real RdotV;
};

LightInfo CalcLightData(real3 positionWS, real3 normalDirection, real3 lightDirection)
{
    LightInfo rst = (LightInfo)0;

    rst.viewDirection               = normalize(GetCameraPositionWS() - positionWS);

    float shiftAmount               = dot(normalDirection, rst.viewDirection);
	rst.normalDirection             = shiftAmount < 0.0f ?
                                        normalDirection + rst.viewDirection * (-shiftAmount + 1e-5f) :
                                        normalDirection;
    rst.normalDirection             = normalize(rst.normalDirection);

    rst.lightDirection              = normalize(lightDirection);
    rst.lightReflectDirection       = reflect( -rst.lightDirection,             rst.normalDirection);
    rst.viewReflectDirection        = normalize(reflect( -rst.viewDirection,    rst.normalDirection ));
    rst.halfDirection               = normalize(rst.viewDirection + rst.lightDirection); 
    rst.NdotL                       = max(0.0,dot( rst.normalDirection,         rst.lightDirection ));
    rst.NdotH                       = max(0.0,dot( rst.normalDirection,         rst.halfDirection));
    rst.NdotV                       = max(0.0,dot( rst.normalDirection,         rst.viewDirection));
    rst.VdotH                       = max(0.0,dot( rst.viewDirection,           rst.halfDirection));
    rst.LdotH                       = max(0.0,dot( rst.lightDirection,          rst.halfDirection)); 
    rst.LdotV                       = max(0.0,dot( rst.lightDirection,          rst.viewDirection)); 
    rst.RdotV                       = max(0.0,dot( rst.lightReflectDirection,   rst.viewDirection ));
    return rst;
}


// https://learnopengl.com/Advanced-Lighting/Normal-Mapping
// http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/
// Normal map
// 'TBN' transforms the world space into a tangent space
// we need its inverse matrix
// Tip : An inverse matrix of orthogonal matrix is its transpose matrix
real3x3 GetTBN(real3 tangentWS, real3 bitangentWS, real3 normalWS)
{
    return transpose(real3x3(
		normalize(tangentWS),
		normalize(bitangentWS),
		normalize(normalWS)));
}


InputData CreateInputData(half3 positionWS, half3 normalWS, half fogFactgor)
{
    InputData OUT;
    OUT.positionWS = positionWS;
    OUT.normalWS = normalWS;
    OUT.viewDirectionWS = GetCameraPositionWS() - positionWS;

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    OUT.shadowCoord = TransformWorldToShadowCoord(positionWS);
#else
    OUT.shadowCoord = real4(0,0,0,0);
#endif

    OUT.fogCoord = fogFactgor; // ComputeFogFactor(vertexInput.positionCS.z);

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    OUT.vertexLighting = VertexLighting(positionWS, normalWS);
#else
    OUT.vertexLighting = half3(0.0, 0.0, 0.0);
#endif

    OUT.bakedGI = half3 (0,0,0);

    //TODO: how to fix this marco.
    //#if defined(LightMap_ON)
    //    OUT.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, normalWS);
    //#endif
    return OUT;
}

// Lighting.hlsl > UniversalFragmentBlinnPhong() > LightingLambert()
half3 BlinnPhongLite(Light light, half3 normalWS)
{
    half NdotL = saturate(dot(normalWS, normalize(light.direction)));
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightColor = attenuatedLightColor;
    //#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    //half smoothness = exp2(10 * surfaceData.smoothness + 1);
    //lightColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);
    //#endif
    return lightColor * NdotL;
}
// Lighting.hlsl > GetAdditionalLight(uint i, half3 positionWS, half4 shadowMask)
half CalcAdditionalShadow(int lightIndex, half3 positionWS)
{
    int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[perObjectLightIndex].occlusionProbeChannels;
#else
    half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[perObjectLightIndex];
#endif
    //half4 shadowMask = SAMPLE_SHADOWMASK(uv);
    //half shadowAttenuation = AdditionalLightShadow(perObjectLightIndex, positionWS, shadowMask, occlusionProbeChannels);
    half shadowAttenuation = AdditionalLightRealtimeShadow(perObjectLightIndex, positionWS);
    return shadowAttenuation;
}

half3 CalcBlinnPhongLite(real3 positionWS, real3 normalWS)
{
	half4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    half3 lightColor = BlinnPhongLite(GetMainLight(shadowCoord), normalWS);
    int cnt = GetAdditionalLightsCount();
    for (int i = 0; i < cnt; i++)
    {
        // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
        // This way the following code will work for both directional and punctual lights.
        Light light = GetAdditionalPerObjectLight(i, positionWS);
        light.shadowAttenuation = CalcAdditionalShadow(i, positionWS);
        lightColor.rgb += BlinnPhongLite(light, normalWS);
    }
    return lightColor;
}

#endif