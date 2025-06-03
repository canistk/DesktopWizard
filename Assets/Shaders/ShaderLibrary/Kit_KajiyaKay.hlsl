#ifndef KAJIYAKAY_INCLUDED
#define KAJIYAKAY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "KitShader.hlsl"

// --- Define Properties
TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_SpecularShift);      SAMPLER(sampler_SpecularShift);
TEXTURE2D(_AOTex);              SAMPLER(sampler_AOTex);
TEXTURE2D(_ParallaxDepthMap);   SAMPLER(sampler_ParallaxDepthMap);
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(real4, _BaseMap_ST);
    UNITY_DEFINE_INSTANCED_PROP(real4, _BumpMap_ST);
    UNITY_DEFINE_INSTANCED_PROP(real4, _SpecularShift_ST);
    UNITY_DEFINE_INSTANCED_PROP(real4, _ParallaxDepthMap_ST);
    UNITY_DEFINE_INSTANCED_PROP(real4, _BaseColor);
    UNITY_DEFINE_INSTANCED_PROP(real4, _RimColor);
    
    UNITY_DEFINE_INSTANCED_PROP(real4, _PrimaryColor);
    UNITY_DEFINE_INSTANCED_PROP(real4, _PrimaryV40);
    UNITY_DEFINE_INSTANCED_PROP(real4, _PrimaryV41);

    UNITY_DEFINE_INSTANCED_PROP(real4, _SecondaryColor);
    UNITY_DEFINE_INSTANCED_PROP(real4, _SecondaryV40);
    UNITY_DEFINE_INSTANCED_PROP(real4, _SecondaryV41);

    UNITY_DEFINE_INSTANCED_PROP(real4, _ParallaxV40);
    UNITY_DEFINE_INSTANCED_PROP(real4, _ParallaxV41);
    UNITY_DEFINE_INSTANCED_PROP(real, _BumpScale);
    UNITY_DEFINE_INSTANCED_PROP(real, _RimLightPower);
    UNITY_DEFINE_INSTANCED_PROP(real, _RimLightShadow);
    UNITY_DEFINE_INSTANCED_PROP(real, _RimRangeX);
    UNITY_DEFINE_INSTANCED_PROP(real, _RimRangeY);
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// In case U3D remove it in library.
//real3 ShiftTangent(real3 T, real3 N, real shift) { return normalize(T + N * shift); }

struct HighLight
{
    real4 color;
    real shift;
    real strength;
    real oil;
    real weight;
    real width;
    real feather;
    real dissolve;
};

// https://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf
real3 HairHighLight(HighLight config, Light light, real shiftTex,
    real3 T, real3 B, real3 N, real3 V)
{
    real3 L = normalize(light.direction);
    real3 H = normalize(V + L);
    real NdotL = dot(N, L);
    real SoftNdotL = lerp(0.25, 1.0, NdotL);
    real3 KayT = ShiftTangent(B, N, shiftTex + config.shift);
    real TdotH = dot(KayT, H);
    real sinTH = sqrt(1.0 - TdotH * TdotH);
    real dirAtten = smoothstep(-1, 0.0, TdotH);
    const static float epsilon = 1E-4;
    real strandSpecular = dirAtten * pow(sinTH + (config.width * 0.05), max(1.0, config.oil * 50));
    
    real hardEdge = (1.0 - config.strength) * 0.5;
    real softEdge = hardEdge + config.feather + epsilon;
    if (hardEdge || softEdge) 
    {
        strandSpecular = smoothstep(hardEdge, softEdge, strandSpecular);
    }
    // real lumen = saturate((light.color.r + light.color.g + light.color.b) / 3.0);
    real3 shadowBias = light.color.rgb * SoftNdotL * config.weight * light.distanceAttenuation * light.shadowAttenuation;
    real3 hairColor = config.color.rgb * config.color.a;
    return hairColor * strandSpecular * shadowBias;
}

// Simple subsurface scattering approximation
// https://developer.amd.com/wordpress/media/2012/10/Scheuermann_HairSketchSlides.pdf
real3 KajiyaKayLightTerm(Light light, real3 N)
{
    return light.color * light.shadowAttenuation * light.distanceAttenuation * 
        max(0.0, 0.75 * dot(N, light.direction) + 0.25);
}

// --- VertexShader
// The structure definition defines which variables it contains.
// This example uses the Attributes structure as an input structure in
// the vertex shader.
struct Attributes
{
    // The positionOS variable contains the vertex positions in object
    // space.
    real4   positionOS  : POSITION;
    real3   normalOS    : NORMAL;
    real4   tangentOS   : TANGENT;
    real2   uv          : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    // The positions in this struct must have the SV_POSITION semantic.
    real4   positionCS  : SV_POSITION;
    real2   uv          : TEXCOORD0;
    real3   normalWS    : TEXCOORD2;
    real3   tangentWS   : TEXCOORD3;
    real3   bitangentWS : TEXCOORD4;
    real3   positionWS  : TEXCOORD5;
    real4   positionOS  : TEXCOORD6;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct Parallax
{
    int     layer;
    real    height;
    real2   uv;
    real    shift;
    real    fading;
};

Parallax GetParallax()
{
    Parallax p;
    p.layer     = _ParallaxV40.x;
    p.fading    = _ParallaxV40.y;
    p.uv        = _ParallaxV40.zw;
    p.shift     = _ParallaxV41.x;
    p.height    = _ParallaxV41.y;
    return p;
}

real4 SampleTex(real2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _BaseMap_ST.xy + _BaseMap_ST.zw);
}

real SampleDepth(real2 uv)
{
    return SAMPLE_TEXTURE2D(_ParallaxDepthMap, sampler_ParallaxDepthMap, uv * _ParallaxDepthMap_ST.xy + _ParallaxDepthMap_ST.zw).r;
}

real4 ParallaxMapping(real2 uv, real3 positionOS)
{
    real3 cameraPosOS = TransformWorldToObject(GetCameraPositionWS());
    real3 localViewDir = normalize(positionOS - cameraPosOS);
    Parallax parallax = GetParallax();
    real4 tex = SampleTex(uv);
    if (parallax.layer < 1)
        return tex;
    
    // Shift tail
    localViewDir.x += (parallax.shift * (localViewDir.y * 0.5 - 0.5));

    // parallax
    real3 diffuse = tex.rgb * tex.a;
    real alpha = tex.a;
    real2 offset = uv;
    for (int i = 0; i < parallax.layer; i++)
    {
        if (offset.x >= 0.0 && offset.x <= 1.0 &&
            offset.y >= 0.0 && offset.y <= 1.0)
        {
            real gap        = SampleDepth(uv).r * parallax.height;
            real2 stepUV    = (localViewDir.xz + parallax.uv) * gap;
            offset          += stepUV;
            real4 layer     = SampleTex(offset);
            diffuse         += layer.rgb * layer.a;
            alpha           = max(alpha, layer.a * saturate(1.0 - parallax.fading - i / parallax.layer));
        }
    }
    
    return real4(diffuse, alpha);
}

Varyings HairVertexShader(Attributes IN)
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
    OUT.positionCS = vertexPositionInput.positionCS;
    // OUT.positionNDC = vertexPositionInput.positionNDC;
    // OUT.positionVS = vertexPositionInput.positionVS;
    OUT.positionWS = vertexPositionInput.positionWS;
    OUT.positionOS = IN.positionOS;
    OUT.uv = IN.uv;

    VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
    OUT.normalWS = normalInput.normalWS;
    OUT.tangentWS = normalInput.tangentWS;
    OUT.bitangentWS = normalInput.bitangentWS;
    return OUT;
}

real4 HairFragmentShader(Varyings IN, real face : VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); // VR support - Single-Pass Stereo Rendering for Android
    // Read GPU instancing data
    
    real4 diffuse = ParallaxMapping(IN.uv, IN.positionOS.xyz);
    diffuse.rgb *= _BaseColor.rgb;
    if (diffuse.a < 0.01)
        discard;
    //clip(saturate(diffuse.a) - 0.01);

    HighLight i_Primary;
    i_Primary.color     = _PrimaryColor;
    i_Primary.shift     = _PrimaryV40.x;
    i_Primary.strength  = _PrimaryV40.y;
    i_Primary.oil       = _PrimaryV40.z;
    i_Primary.weight    = _PrimaryV40.w;
    i_Primary.width     = _PrimaryV41.x;
    i_Primary.feather   = _PrimaryV41.y;
    i_Primary.dissolve  = _PrimaryV41.z;

    HighLight i_Secondary;
    i_Secondary.color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryColor);
    real4 sv40      = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryV40);
    real4 sv41      = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryV41);
    i_Secondary.shift   = sv40.x;
    i_Secondary.strength= sv40.y;
    i_Secondary.oil     = sv40.z;
    i_Secondary.weight  = sv40.w;
    i_Secondary.width   = sv41.x;
    i_Secondary.feather = sv41.y;
    i_Secondary.dissolve= sv41.z;

    // Since hair usually render 2 side, in order to render back face lighing correctly
    // the back face normal should also flipped
    real3 normalWS0 = IN.normalWS;
    real3 normalWS1 = IN.normalWS * face;
    real3x3 TBN = GetTBN(IN.tangentWS, IN.bitangentWS, normalWS1);
    real4 rawNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv * _BumpMap_ST.xy + _BumpMap_ST.zw);
    real i_BumpScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BumpScale);
    real3 normalTS = UnpackNormalScale(rawNormal, i_BumpScale);
    real3 normalWS = mul(TBN, normalTS);
    real3 positionWS = IN.positionWS;

    Light mainLight = GetMainLight();
    
    real3 N = normalize(normalWS);
    real3 T = normalize(IN.tangentWS);
    real3 B = normalize(IN.bitangentWS);
    real3 V = normalize(GetCameraPositionWS() - positionWS);
    real3 L = normalize(mainLight.direction);
    real3 H = normalize(L + V);
    

    real ambientOcclusion = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, IN.uv).r;
    diffuse.rgb = diffuse.rgb  * ambientOcclusion;

    // Hair strand
    real shiftTex = SAMPLE_TEXTURE2D(_SpecularShift, sampler_SpecularShift, IN.uv * _SpecularShift_ST.xy + _SpecularShift_ST.zw).r - 0.5;
    real3 hairStrand = 0;
    hairStrand      += HairHighLight(i_Primary,      mainLight, shiftTex, T, B, N, V);
    hairStrand      += HairHighLight(i_Secondary,    mainLight, shiftTex, T, B, N, V);

    // Lighting
    real3 lightTerm = KajiyaKayLightTerm(mainLight, N);
#ifdef _KIT_ADDITIONAL_LIGHTS
    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; i++)
    {
        Light subLight = GetAdditionalPerObjectLight(i, positionWS);
        lightTerm   += KajiyaKayLightTerm(subLight, N);
        hairStrand  += HairHighLight(i_Primary,      subLight, shiftTex, T, B, N, V);
        hairStrand  += HairHighLight(i_Secondary,    subLight, shiftTex, T, B, N, V);
    }
#endif
    diffuse.rgb *= lightTerm;

    // Rim light
    real4 i_RimColor        = _RimColor;
    real i_RimLightPower    = _RimLightPower;
    real i_RimLightShadow   = _RimLightShadow;
    real2 i_RimRange        = real2(_RimRangeX, _RimRangeY);
    real NdotV = dot(V, N);
    real rim = smoothstep(i_RimRange.x, i_RimRange.y, (1.0 - NdotV));
    rim = pow(rim, max(0.0001, i_RimLightPower));
    // hack to Calculate the main light intensity, fall off and stuff
    // to provide the hotfix for any other Non-real lighting to act nature.
    // E.g. No light, or Weak main light in the scene.
    real softNdotL = lerp(0.25, 1.0, dot(N, L));
    real shadowHotfix = softNdotL * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    real rimLightShadow = (1 - shadowHotfix) * i_RimLightShadow;
    real3 rimLight = saturate(i_RimColor.rgb * rim) * (1 - rimLightShadow) * i_RimColor.a;

    
    real3 hairColor = saturate(diffuse.rgb + hairStrand + rimLight);
    return real4(hairColor, diffuse.a);
}

Varyings HairVertexShaderAlpha(Attributes IN)
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

    OUT.uv = IN.uv;
    

    // VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space, ndc)
    // Unity compiler will strip all unused references (say you don't use view space).
    // Therefore there is more flexibility at no additional cost with this struct.
    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(IN.positionOS.xyz);
    OUT.positionCS = vertexPositionInput.positionCS;
    // OUT.positionNDC = vertexPositionInput.positionNDC;
    // OUT.positionVS = vertexPositionInput.positionVS;
    OUT.positionWS = vertexPositionInput.positionWS;
    OUT.positionOS = IN.positionOS;

    VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
    OUT.normalWS = normalInput.normalWS;
    OUT.tangentWS = normalInput.tangentWS;
    OUT.bitangentWS = normalInput.bitangentWS;
    return OUT;
}

real4 HairFragmentShaderAlpha(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); // VR support - Single-Pass Stereo Rendering for Android
    // Read GPU instancing data

    //real3 cameraPosOS = TransformWorldToObject(GetCameraPositionWS());
    //real3 localViewDir = normalize(IN.positionOS.xyz - cameraPosOS);
    real4 diffuse = ParallaxMapping(IN.uv, IN.positionOS.xyz);


    //if (diffuse.a < 0.01) 
    //    discard;
    clip(diffuse.a - 0.99);
    return diffuse;
}

real4 HairFragmentShaderRed(Varyings IN) : SV_TARGET
{
    return real4(1,0,0,1);
}

real4 HairFragmentShaderGreen(Varyings IN) : SV_TARGET
{
    return real4(0,1,0,1);
}
real4 HairFragmentShaderBlue(Varyings IN) : SV_TARGET
{
    return real4(0,0,1,1);
}
#endif