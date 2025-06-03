#ifndef KIT_SHADER_VECTOR
#define KIT_SHADER_VECTOR

real3 Projection(real3 v, real3 normalWS)
{
    normalWS = normalize(normalWS);
    return dot(v, normalWS) * normalWS;
}

real3 ProjectOnPlane(real3 v, real3 normalWS)
{
    normalWS = normalize(normalWS);
    real d = dot(v, normalWS);
    real3 scaledPn = normalWS * d;
    return v - scaledPn * d;
}

#endif