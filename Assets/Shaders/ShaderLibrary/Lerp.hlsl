#ifndef KIT_SHADER_MATH
#define KIT_SHADER_MATH

// https://www.ronja-tutorials.com/post/047-invlerp_remap/#sources
/* //hlsl supports linear interpolation intrinsically so this isn't needed
real lerp(real from, real to, real rel){
  return ((1 - rel) * from) + (rel * to);
}
*/
real invLerp(real from, real to, real value)
{
	return (value - from) / (to - from);
}

real4 invLerp(real4 from, real4 to, real4 value)
{
	return (value - from) / (to - from);
}

real remap(real origFrom, real origTo, real targetFrom, real targetTo, real value)
{
	real rel = invLerp(origFrom, origTo, value);
	return lerp(targetFrom, targetTo, rel);
}

real4 remap(real4 origFrom, real4 origTo, real4 targetFrom, real4 targetTo, real4 value)
{
	real4 rel = invLerp(origFrom, origTo, value);
	return lerp(targetFrom, targetTo, rel);
}
#endif