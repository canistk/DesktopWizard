/*
// required properties

[Header(LOD)]
[IntRange] _LODMapLevel ("Max detail level", Range(0, 20)) = 6
_LODMapDistance ("LOD Map to distance", Range(0,20)) = 10.0
_LODBias ("LOD Mapping bias", Range(0.1, 3)) = 1.0
[Toggle(_DebugLod)] _DebugLod("Debug Lod (default = false)", Float) = 0

*/

#ifndef KitLevelOfDetail
#define KitLevelOfDetail
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// #include_with_pragmas 
// https://docs.unity3d.com/Manual/shader-include-directives.html
#pragma shader_feature_local _DebugLod

UNITY_INSTANCING_BUFFER_START(LevelOfDetail)
    UNITY_DEFINE_INSTANCED_PROP(half, _LODMapDistance);
    UNITY_DEFINE_INSTANCED_PROP(half, _LODMapLevel);
    UNITY_DEFINE_INSTANCED_PROP(half, _LODBias);
UNITY_INSTANCING_BUFFER_END(LevelOfDetail)

struct LODPack
{
    /// range between 0 ~ Map-level, e.g. mapLevel3 = 0.0 ~ 0.5 ~ 1.0 ~ 1.5 ~ 2.0,
    /// include decimal places
    real lodLinear;

    /// range between 0 ~ Map-level in integer, e.g. mapLevel3 = 0,1,2
    /// usually used for texture sampling.
    real lod;

    /// Remap into giving level & distance, can be used as debug color.
    real lod01;

    /// _Debug mode off = real3(1,1,1)
    /// _Debug mode on = Lod0 ~= red, Lod(far) ~= green,
    /// used for shader debug, can multiple on final result.
    real3 Debug;

    /// for reference mapped distance for LOD
    real MapDistance;
    /// for reference mapped level for LOD
    real MapLevel;
};

real3 JetColor(real f01)
{
    real fourValue = 4.0 * f01;
    real red    = min(fourValue - 1.5, -fourValue + 4.5);
    real green  = min(fourValue - 0.5, -fourValue + 3.5);
    real blue   = min(fourValue + 0.5, -fourValue + 2.5);
    return saturate(real3(red, green, blue));
}

LODPack GetLOD(in real3 viewVectorWS)
{
    real mapDistance            = _LODMapDistance;  //UNITY_ACCESS_INSTANCED_PROP(LevelOfDetail, _LODMapDistance);
    real mapLevel               = _LODMapLevel;     //UNITY_ACCESS_INSTANCED_PROP(LevelOfDetail, _LODMapLevel);
    real i_LODBias              = _LODBias;         //UNITY_ACCESS_INSTANCED_PROP(LevelOfDetail, _LODBias);
    real viewLengthPt           = saturate(min(pow(length(viewVectorWS),i_LODBias), mapDistance) / max(0.01, mapDistance));

    LODPack pack;
    pack.MapLevel               = mapLevel;
    pack.MapDistance            = mapDistance;
    // remap vector from map distance & normalized.
    pack.lod01                  = viewLengthPt;

    // lod based on map level
    pack.lodLinear              = viewLengthPt * mapLevel;

    // lod in interger
    pack.lod                    = floor(pack.lodLinear);

#if _DebugLod

    // debug Lod01, flip result, red == closest, blue = far, black = out range.
    //pack.Debug                  = JetColor(1 - pack.lod01);
    //pack.Debug                  = JetColor(1 - pack.lodLinear / mapLevel); // normalize to visualize the lod change by jet color
    pack.Debug                  = JetColor(1 - pack.lod / mapLevel);
#else
    pack.Debug                  = 1;
#endif
    return pack;
}



#endif