Shader "Effect/DepthOfFieldDownSample"
{
	Properties{
		_MainTex("Texture", 2D) = "" {}
		_BlurTex("Blur", 2D) = ""{}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct v2f_blur
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
	};

	struct v2f_dof
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	};

	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	sampler2D _BlurTex;
	sampler2D _CameraDepthTexture;
	float4 _offsets;
	float _focalDistance;
	float _nearBlurScale;
	float _farBlurScale;

	v2f_blur vert_blur(appdata_img v)
	{
		v2f_blur o;
		_offsets *= _MainTex_TexelSize.xyxy;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;

		o.uv01 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1);
		o.uv23 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1) * 2.0;
		o.uv45 = v.texcoord.xyxy + _offsets.xyxy * float4(1, 1, -1, -1) * 3.0;

		return o;
	}

	fixed4 frag_blur(v2f_blur i) : SV_Target
	{
		fixed4 color = fixed4(0,0,0,0);
		color += 0.40 * tex2D(_MainTex, i.uv);
		color += 0.15 * tex2D(_MainTex, i.uv01.xy);
		color += 0.15 * tex2D(_MainTex, i.uv01.zw);
		color += 0.10 * tex2D(_MainTex, i.uv23.xy);
		color += 0.10 * tex2D(_MainTex, i.uv23.zw);
		color += 0.05 * tex2D(_MainTex, i.uv45.xy);
		color += 0.05 * tex2D(_MainTex, i.uv45.zw);
		return color;
	}

	v2f_dof vert_dof(appdata_img v)
	{
		v2f_dof o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv.xy = v.texcoord.xy;
		o.uv1.xy = o.uv.xy;
#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1 - o.uv.y;
#endif	
		return o;
	}

	fixed4 frag_dof(v2f_dof i) : SV_Target
	{
		fixed4 ori = tex2D(_MainTex, i.uv1);
		fixed4 blur = tex2D(_BlurTex, i.uv);
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
		depth = Linear01Depth(depth);

		fixed4 final = (depth <= _focalDistance) ? ori : lerp(ori, blur, clamp((depth - _focalDistance) * _farBlurScale, 0, 1));
		final = (depth > _focalDistance) ? final : lerp(ori, blur, clamp((_focalDistance - depth) * _nearBlurScale, 0, 1));

		return final;
	}

	ENDCG

	SubShader
	{
		Pass
		{
			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }

			CGPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur
			ENDCG
		}

		Pass
		{

			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }
			ColorMask RGBA

			CGPROGRAM
			#pragma vertex vert_dof
			#pragma fragment frag_dof
			ENDCG
		}

	}
}