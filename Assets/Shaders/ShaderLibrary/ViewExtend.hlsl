#ifndef ViewExtend
#define ViewExtend

#define ViewToObjectMatrix              mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)

// [important note] pack ViewRayOS in vertex shader, and unpack in frangment shader
half4 PackViewRayOS(half3 positionVS)
{
    // Cal viewRay
    half3 viewRay = positionVS;
    half viewDepth = viewRay.z;
    // unity's camera space is right hand coord(negativeZ pointing into screen), we want positive z ray in fragment shader, so negate it
    viewRay *= -1;
    // it is ok to write very expensive code in decal's vertex shader, 
    // it is just a unity cube(4*6 vertices) per decal only, won't affect GPU performance at all.
    // half4x4 ViewToObjectMatrix = mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V);
    // transform everything to object space(decal space) in vertex shader first, so we can skip all matrix mul() in fragment shader
    half4 viewRayOS = half4(mul((half3x3)ViewToObjectMatrix, viewRay), viewDepth);
    return viewRayOS;
}

// [important note] pack ViewRayOS in vertex shader, and unpack in frangment shader
half3 UnpackViewRayOS(half4 viewRayOS)
{
    // now do "viewRay z division" that we skipped in vertex shader earlier.
    half3 _viewRayOS = viewRayOS.xyz /= viewRayOS.w;
    return _viewRayOS;
}

#endif