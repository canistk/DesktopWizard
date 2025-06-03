Shader "DesktopWizard/Shaders/ReadBuffer"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			
			CGPROGRAM

				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest 
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 frag(v2f_img i):COLOR
				{
					return tex2D(_MainTex, i.uv);
				}
			ENDCG
		}
	}
	FallBack off
}
