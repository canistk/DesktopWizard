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


#ifndef KitTessellation
#define KitTessellation
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float3 GetViewDirectionFromPosition(float3 positionWS) {
    return normalize(GetCameraPositionWS() - positionWS);
}

float4 GetShadowCoord(float3 positionWS, float4 positionCS) {
    // Calculate the shadow coordinate depending on the type of shadows currently in use
#if SHADOWS_SCREEN
    return ComputeScreenPos(positionCS);
#else
    return TransformWorldToShadowCoord(positionWS);
#endif
}

// Returns true if the point is outside the bounds set by lower and higher
bool IsOutOfBounds(float3 p, float3 lower, float3 higher) {
    return p.x < lower.x || p.x > higher.x || p.y < lower.y || p.y > higher.y || p.z < lower.z || p.z > higher.z;
}

// Returns true if the given vertex is outside the camera fustum and should be culled
bool IsPointOutOfFrustum(float4 positionCS, float tolerance) {
    float3 culling = positionCS.xyz;
    float w = positionCS.w;
    // UNITY_RAW_FAR_CLIP_VALUE is either 0 or 1, depending on graphics API
    // Most use 0, however OpenGL uses 1
    float3 lowerBounds = float3(-w - tolerance, -w - tolerance, -w * UNITY_RAW_FAR_CLIP_VALUE - tolerance);
    float3 higherBounds = float3(w + tolerance, w + tolerance, w + tolerance);
    return IsOutOfBounds(culling, lowerBounds, higherBounds);
}

// Returns true if the points in this triangle are wound counter-clockwise
bool ShouldBackFaceCull(float4 p0PositionCS, float4 p1PositionCS, float4 p2PositionCS, float tolerance) {
    float3 point0 = p0PositionCS.xyz / p0PositionCS.w;
    float3 point1 = p1PositionCS.xyz / p1PositionCS.w;
    float3 point2 = p2PositionCS.xyz / p2PositionCS.w;
    // In clip space, the view direction is float3(0, 0, 1), so we can just test the z coord
#if UNITY_REVERSED_Z
    return cross(point1 - point0, point2 - point0).z < -tolerance;
#else // In OpenGL, the test is reversed
    return cross(point1 - point0, point2 - point0).z > tolerance;
#endif
}

// Returns true if it should be clipped due to frustum or winding culling
bool ShouldClipPatch(float4 p0PositionCS, float4 p1PositionCS, float4 p2PositionCS, float frustumTolerance, float windingTolerance) {
    bool allOutside = IsPointOutOfFrustum(p0PositionCS, frustumTolerance) &&
        IsPointOutOfFrustum(p1PositionCS, frustumTolerance) &&
        IsPointOutOfFrustum(p2PositionCS, frustumTolerance);
    return allOutside || ShouldBackFaceCull(p0PositionCS, p1PositionCS, p2PositionCS, windingTolerance);
}

// Calculate the tessellation factor for an edge
// This function needs the world and clip space positions of the connected vertices
float EdgeTessellationFactor(float scale, float bias, float multiplier, float3 p0PositionWS, float4 p0PositionCS, float3 p1PositionWS, float4 p1PositionCS) {
    float factor = 1;
#if defined(_TESSELLATION_FACTOR_CONSTANT)
    factor = scale;
#elif defined(_TESSELLATION_FACTOR_WORLD)
    factor = distance(p0PositionWS, p1PositionWS) / scale;
#elif defined(_TESSELLATION_FACTOR_WORLD_WITH_DEPTH)
    float length = distance(p0PositionWS, p1PositionWS);
    float distanceToCamera = distance(GetCameraPositionWS(), (p0PositionWS + p1PositionWS) * 0.5);
    factor = length / (scale * distanceToCamera * distanceToCamera);
#elif defined(_TESSELLATION_FACTOR_SCREEN)
    factor = distance(p0PositionCS.xyz / p0PositionCS.w, p1PositionCS.xyz / p1PositionCS.w) * _ScreenParams.y / scale;
#endif
    return max(1, (factor + bias) * multiplier);
}

#if NUM_BEZIER_CONTROL_POINTS > 0
//Bezier control point calculations. See https://alex.vlachos.com/graphics/CurvedPNTriangles.pdf for explanation
float3 CalculateBezierControlPoint(float3 p0PositionWS, float3 aNormalWS, float3 p1PositionWS, float3 bNormalWS) {
    float w = dot(p1PositionWS - p0PositionWS, aNormalWS);
    return (p0PositionWS * 2 + p1PositionWS - w * aNormalWS) / 3.0;
}

void CalculateBezierControlPoints(inout float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0PositionWS, float3 p0NormalWS, float3 p1PositionWS, float3 p1NormalWS, float3 p2PositionWS, float3 p2NormalWS) {
    bezierPoints[0] = CalculateBezierControlPoint(p0PositionWS, p0NormalWS, p1PositionWS, p1NormalWS);
    bezierPoints[1] = CalculateBezierControlPoint(p1PositionWS, p1NormalWS, p0PositionWS, p0NormalWS);
    bezierPoints[2] = CalculateBezierControlPoint(p1PositionWS, p1NormalWS, p2PositionWS, p2NormalWS);
    bezierPoints[3] = CalculateBezierControlPoint(p2PositionWS, p2NormalWS, p1PositionWS, p1NormalWS);
    bezierPoints[4] = CalculateBezierControlPoint(p2PositionWS, p2NormalWS, p0PositionWS, p0NormalWS);
    bezierPoints[5] = CalculateBezierControlPoint(p0PositionWS, p0NormalWS, p2PositionWS, p2NormalWS);
    float3 avgBezier = 0;
    [unroll] for (int i = 0; i < 6; i++) {
        avgBezier += bezierPoints[i];
    }
    avgBezier /= 6.0;
    float3 avgControl = (p0PositionWS + p1PositionWS + p2PositionWS) / 3.0;
    bezierPoints[6] = avgBezier + (avgBezier - avgControl) / 2.0;
}

float3 CalculateBezierControlNormal(float3 p0PositionWS, float3 aNormalWS, float3 p1PositionWS, float3 bNormalWS) {
    float3 d = p1PositionWS - p0PositionWS;
    float v = 2 * dot(d, aNormalWS + bNormalWS) / dot(d, d);
    return normalize(aNormalWS + bNormalWS - v * d);
}

void CalculateBezierNormalPoints(inout float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0PositionWS, float3 p0NormalWS, float3 p1PositionWS, float3 p1NormalWS, float3 p2PositionWS, float3 p2NormalWS) {
    bezierPoints[7] = CalculateBezierControlNormal(p0PositionWS, p0NormalWS, p1PositionWS, p1NormalWS);
    bezierPoints[8] = CalculateBezierControlNormal(p1PositionWS, p1NormalWS, p2PositionWS, p2NormalWS);
    bezierPoints[9] = CalculateBezierControlNormal(p2PositionWS, p2NormalWS, p0PositionWS, p0NormalWS);
}
#endif // NUM_BEZIER_CONTROL_POINTS > 0

// Barycentric interpolation as a function
float3 BarycentricInterpolate(float3 bary, float3 a, float3 b, float3 c) {
    return bary.x * a + bary.y * b + bary.z * c;
}

// Call this macro to interpolate between a triangle patch, passing the field name
#define BARYCENTRIC_INTERPOLATE(fieldName) \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z

// Calculate Phong projection offset
float3 PhongProjectedPosition(float3 flatPositionWS, float3 cornerPositionWS, float3 normalWS) {
    return flatPositionWS - dot(flatPositionWS - cornerPositionWS, normalWS) * normalWS;
}

// Apply Phong smoothing
float3 CalculatePhongPosition(float3 bary, float smoothing, float3 p0PositionWS, float3 p0NormalWS,
    float3 p1PositionWS, float3 p1NormalWS, float3 p2PositionWS, float3 p2NormalWS) {
    float3 flatPositionWS = BarycentricInterpolate(bary, p0PositionWS, p1PositionWS, p2PositionWS);
    float3 smoothedPositionWS =
        bary.x * PhongProjectedPosition(flatPositionWS, p0PositionWS, p0NormalWS) +
        bary.y * PhongProjectedPosition(flatPositionWS, p1PositionWS, p1NormalWS) +
        bary.z * PhongProjectedPosition(flatPositionWS, p2PositionWS, p2NormalWS);
    return lerp(flatPositionWS, smoothedPositionWS, smoothing);
}

#if NUM_BEZIER_CONTROL_POINTS > 0
// Calculate bezier-based smoothing for position and normal

float3 CalculateBezierPosition(float3 bary, float smoothing, float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0PositionWS, float3 p1PositionWS, float3 p2PositionWS) {
    float3 flatPositionWS = BarycentricInterpolate(bary, p0PositionWS, p1PositionWS, p2PositionWS);
    float3 smoothedPositionWS =
        p0PositionWS * (bary.x * bary.x * bary.x) +
        p1PositionWS * (bary.y * bary.y * bary.y) +
        p2PositionWS * (bary.z * bary.z * bary.z) +
        bezierPoints[0] * (3 * bary.x * bary.x * bary.y) +
        bezierPoints[1] * (3 * bary.y * bary.y * bary.x) +
        bezierPoints[2] * (3 * bary.y * bary.y * bary.z) +
        bezierPoints[3] * (3 * bary.z * bary.z * bary.y) +
        bezierPoints[4] * (3 * bary.z * bary.z * bary.x) +
        bezierPoints[5] * (3 * bary.x * bary.x * bary.z) +
        bezierPoints[6] * (6 * bary.x * bary.y * bary.z);
    return lerp(flatPositionWS, smoothedPositionWS, smoothing);
}

float3 CalculateBezierNormal(float3 bary, float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0NormalWS, float3 p1NormalWS, float3 p2NormalWS) {
    return p0NormalWS * (bary.x * bary.x) +
        p1NormalWS * (bary.y * bary.y) +
        p2NormalWS * (bary.z * bary.z) +
        bezierPoints[7] * (2 * bary.x * bary.y) +
        bezierPoints[8] * (2 * bary.y * bary.z) +
        bezierPoints[9] * (2 * bary.z * bary.x);
}

float3 CalculateBezierNormalWithSmoothFactor(float3 bary, float smoothing, float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0NormalWS, float3 p1NormalWS, float3 p2NormalWS) {
    float3 flatNormalWS = BarycentricInterpolate(bary, p0NormalWS, p1NormalWS, p2NormalWS);
    float3 smoothedNormalWS = CalculateBezierNormal(bary, bezierPoints, p0NormalWS, p1NormalWS, p2NormalWS);
    return normalize(lerp(flatNormalWS, smoothedNormalWS, smoothing));
}

void CalculateBezierNormalAndTangent(float3 bary, float smoothing, float3 bezierPoints[NUM_BEZIER_CONTROL_POINTS],
    float3 p0NormalWS, float3 p0TangentWS, float3 p1NormalWS, float3 p1TangentWS, float3 p2NormalWS, float3 p2TangentWS,
    out float3 normalWS, out float3 tangentWS) {

    float3 flatNormalWS = BarycentricInterpolate(bary, p0NormalWS, p1NormalWS, p2NormalWS);
    float3 smoothedNormalWS = CalculateBezierNormal(bary, bezierPoints, p0NormalWS, p1NormalWS, p2NormalWS);
    normalWS = normalize(lerp(flatNormalWS, smoothedNormalWS, smoothing));

    float3 flatTangentWS = BarycentricInterpolate(bary, p0TangentWS, p1TangentWS, p2TangentWS);
    float3 flatBitangentWS = cross(flatNormalWS, flatTangentWS);
    tangentWS = normalize(cross(flatBitangentWS, normalWS));
}
#endif // NUM_BEZIER_CONTROL_POINTS > 0





#endif