/*
// required properties

_NormalScale("Normal Map Scale", Range(0.0, 1.0)) = 1.0
[Normal][noscaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}

[Header(Detail Texture)]
_DetailFader                        ("Detail Texture Fader", Range(0.0,1.0)) = 1.0
_DetailGrayScale                    ("Detail Texture Grayscale", Range(0.0, 1.0)) = 1.0
_DetailMap                          ("Detail Texture", 2D) = "white" {}
[Header(Detail Normal)]
_DetailNormalMapScale               ("Detail Normal Scale", Range(0.0, 1.0)) = 1.0
[Normal] _DetailNormalMap           ("Detail Normal", 2D) = "bump" {}
[Toggle(_DebugNormal)] _DebugNormal ("Debug Normal (default = false)", Float) = 0

*/

#ifndef KitTexture
#define KitTexture
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// #include_with_pragmas "Kit_DetailTexture.hlsl"
// https://docs.unity3d.com/Manual/shader-include-directives.html
#pragma shader_feature_local _NORMALMAP

#pragma shader_feature_local _DebugNormal

TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
TEXTURE2D(_DetailMap);          SAMPLER(sampler_DetailMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);

UNITY_INSTANCING_BUFFER_START(DetailTexture)

    UNITY_DEFINE_INSTANCED_PROP(real, _NormalScale);

    UNITY_DEFINE_INSTANCED_PROP(real, _DetailFader);
    UNITY_DEFINE_INSTANCED_PROP(real, _DetailGrayScale);
    UNITY_DEFINE_INSTANCED_PROP(real4, _DetailMap_ST);
    UNITY_DEFINE_INSTANCED_PROP(real, _DetailNormalMapScale);
    UNITY_DEFINE_INSTANCED_PROP(real4, _DetailNormalMap_ST);
UNITY_INSTANCING_BUFFER_END(DetailTexture)

struct DetailNormalPack
{
    real3 albedo;
    real3 normalTS;
    real3 normalWS;
};

half3 LerpWhiteTo(half3 b, half t)
{
    half oneMinusT = 1 - t;
    return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}

/* CommonMaterial.hlsl > BlendNormal, BlendNormalRNM, BlendNormalWorldspaceRNM
// ref http://blog.selfshadow.com/publications/blending-in-detail/
// ref https://gist.github.com/selfshadow/8048308
// Reoriented Normal Mapping
// Blending when n1 and n2 are already 'unpacked' and normalised
// assume compositing in tangent space
real3 BlendNormalRNM(real3 n1, real3 n2)
{
    real3 t = n1.xyz + real3(0.0, 0.0, 1.0);
    real3 u = n2.xyz * real3(-1.0, -1.0, 1.0);
    real3 r = (t / t.z) * dot(t, u) - u;
    return r;
}
*/

real3 KitGetNormal(real2 uv, real lod, real scale)
{
    // output normalTS
    return UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uv, lod), scale);
}

// We got another LitInput.hlsl > half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
DetailNormalPack KitGetDetailWithNormal(real4 albedo, real2 uv, real lod, real3x3 tbn)
{
    // Detail texture
    real4 i_DetailMap_ST        = UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _DetailMap_ST);
    real i_DetailFader          = saturate(UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _DetailFader));
    real i_DetailGrayScale      = saturate(UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _DetailGrayScale));
                
    real4 detailMap             = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, uv * i_DetailMap_ST.xy + i_DetailMap_ST.zw, lod);
    real lumen                  = detailMap.a * (detailMap.r + detailMap.g + detailMap.b) / 3.0;
    albedo.rgb                  *= LerpWhiteTo( lerp(detailMap.rgb, (real3)lumen, i_DetailGrayScale), i_DetailFader);

    // Detail Normal
    real i_DetailNormalMapScale = saturate(UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _DetailNormalMapScale));
    real4 i_DetailNormalMap_ST  = UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _DetailNormalMap_ST);
    real3 dNormalTS             = UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_DetailNormalMap, sampler_DetailNormalMap, uv * i_DetailNormalMap_ST.xy + i_DetailNormalMap_ST.zw, lod), i_DetailNormalMapScale);
    
    real i_NormalScale          = saturate(UNITY_ACCESS_INSTANCED_PROP(DetailTexture, _NormalScale));
    real3 orgNormalTS           = KitGetNormal(uv, lod, i_NormalScale);

    real3 normalTS              = real3(orgNormalTS.x + dNormalTS.x, orgNormalTS.y + dNormalTS.y, orgNormalTS.z);
    real3 normalWS              = normalize(mul(normalTS, tbn)); // TransformTangentToWorld(surfaceData.normalTS, tbn)

    DetailNormalPack dn;
#if _DebugNormal
    dn.albedo = normalTS;
#else
    dn.albedo = albedo.rgb;
#endif

    dn.normalTS = normalTS;
    dn.normalWS = normalWS;
    return dn;
}

real3 KitNormalToWorld(real3 normalTS, real3x3 tbn)
{
    return normalize(mul(normalTS, tbn));
}

real3 KitNormalToWorld(real3 normalTS, real3 tangentWS, real3 bitangentWS, real3 normalWS)
{
    return KitNormalToWorld(normalTS, real3x3(tangentWS, bitangentWS, normalWS));
}

DetailNormalPack KitGetDetailWithNormal(real4 albedo, real2 uv, real lod, real3 tangentWS, real3 bitangentWS, real3 normalWS)
{
    // convert normal - local to world space
    return KitGetDetailWithNormal(albedo, uv, lod, real3x3(tangentWS, bitangentWS, normalWS));
}

#endif