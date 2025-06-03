#ifndef KIT_FRESNELLERP
#define KIT_FRESNELLERP

//------------------------------------------------
//schlick functions
float SchlickFresnel(float i){
    float x = clamp(1.0-i, 0.0, 1.0);
    float x2 = x*x;
    return x2*x2*x;
}
float3 FresnelLerp (float3 x, float3 y, float d)
{
	float t = SchlickFresnel(d);	
	return lerp (x, y, t);
}

float3 SchlickFresnelFunction(float3 SpecularColor,float LdotH){
    return SpecularColor + (1 - SpecularColor)* SchlickFresnel(LdotH);
}

float SchlickIORFresnelFunction(float ior,float LdotH){
    float f0 = pow((ior-1)/(ior+1),2);
    return f0 +  (1 - f0) * SchlickFresnel(LdotH);
}
float SphericalGaussianFresnelFunction(float LdotH,float SpecularColor)
{	
	float power = ((-5.55473 * LdotH) - 6.98316) * LdotH;
    return SpecularColor + (1 - SpecularColor)  * pow(2,power);
}

//---------------------------
//helper functions
float MixFunction(float i, float j, float x) {
	    return  j * x + i * (1.0 - x);
} 
float2 MixFunction(float2 i, float2 j, float x){
	    return  j * x + i * (1.0h - x);
}   
float3 MixFunction(float3 i, float3 j, float x){
	    return  j * x + i * (1.0h - x);
}   
float MixFunction(float4 i, float4 j, float x){
	    return  j * x + i * (1.0h - x);
}
//------------------------------

//-----------------------------------------------
//normal incidence reflection calculation
float F0 (float NdotL, float NdotV, float LdotH, float roughness){
// Diffuse fresnel
    float FresnelLight = SchlickFresnel(NdotL); 
    float FresnelView = SchlickFresnel(NdotV);
    float FresnelDiffuse90 = 0.5 + 2.0 * LdotH*LdotH * roughness;
    return  MixFunction(1, FresnelDiffuse90, FresnelLight) * MixFunction(1, FresnelDiffuse90, FresnelView);
}

//-----------------------------------------------
#endif