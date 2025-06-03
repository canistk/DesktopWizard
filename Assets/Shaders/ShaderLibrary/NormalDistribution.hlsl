#ifndef KIT_NORMAL_DISTRIBUTION
#define KIT_NORMAL_DISTRIBUTION

//-----------------------------------------------
//Normal Distribution Functions
float sqr(float x){
	return x*x; 
}

float BlinnPhongNormalDistribution(float NdotH, float specularpower, float speculargloss){
    float Distribution = pow(NdotH,speculargloss) * specularpower;
    Distribution *= (2+specularpower) / (2*3.1415926535);
    return Distribution;
}
float PhongNormalDistribution(float RdotV, float specularpower, float speculargloss){
    float Distribution = pow(RdotV,speculargloss) * specularpower;
    Distribution *= (2+specularpower) / (2*3.1415926535);
    return Distribution;
}

float BeckmannNormalDistribution(float roughness, float NdotH)
{
    float roughnessSqr = roughness*roughness;
    float NdotHSqr = NdotH*NdotH;
    return max(0.000001,(1.0 / (3.1415926535*roughnessSqr*NdotHSqr*NdotHSqr))* exp((NdotHSqr-1)/(roughnessSqr*NdotHSqr)));
}

float GaussianNormalDistribution(float roughness, float NdotH)
{
    float roughnessSqr = roughness*roughness;
	float thetaH = acos(NdotH);
    return exp(-thetaH*thetaH/roughnessSqr);
}

float GGXNormalDistribution(float roughness, float NdotH)
{
    float roughnessSqr = roughness*roughness;
    float NdotHSqr = NdotH*NdotH;
    float TanNdotHSqr = (1-NdotHSqr)/NdotHSqr;
    return (1.0/3.1415926535) * sqr(roughness/(NdotHSqr * (roughnessSqr + TanNdotHSqr)));
//    float denom = NdotHSqr * (roughnessSqr-1)

}

float TrowbridgeReitzNormalDistribution(float NdotH, float roughness){
    float roughnessSqr = roughness*roughness;
    float Distribution = NdotH*NdotH * (roughnessSqr-1.0) + 1.0;
    return roughnessSqr / (3.1415926535 * Distribution*Distribution);
}

float TrowbridgeReitzAnisotropicNormalDistribution(float anisotropic, float NdotH, float HdotX, float HdotY){
	float aspect = sqrt(1.0h-anisotropic * 0.9h);
	float X = max(.001, sqr(1.0-_Glossiness)/aspect) * 5;
 	float Y = max(.001, sqr(1.0-_Glossiness)*aspect) * 5;
    return 1.0 / (3.1415926535 * X*Y * sqr(sqr(HdotX/X) + sqr(HdotY/Y) + NdotH*NdotH));
}

float WardAnisotropicNormalDistribution(float anisotropic, float NdotL, float NdotV, float NdotH, float HdotX, float HdotY){
    float aspect = sqrt(1.0h-anisotropic * 0.9h);
    float X = max(.001, sqr(1.0-_Glossiness)/aspect) * 5;
 	float Y = max(.001, sqr(1.0-_Glossiness)*aspect) * 5;
    float exponent = -(sqr(HdotX/X) + sqr(HdotY/Y)) / sqr(NdotH);
    float Distribution = 1.0 / ( 3.14159265 * X * Y * sqrt(NdotL * NdotV));
    Distribution *= exp(exponent);
    return Distribution;
}


#endif