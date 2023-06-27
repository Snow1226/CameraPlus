Shader "Effect/DepthOfFieldGauss"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct VertexInput
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct VertexOutput
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    sampler2D _CameraDepthTexture;
    float _FocusDistance;
    float _FocusRange;
    float _BlurRadius;

    VertexOutput vert (VertexInput v)
    {
        VertexOutput o = (VertexOutput)0;

        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);

        return o;
    }

    fixed4 fragBlurVertical(VertexOutput i) : SV_Target
    {
        fixed4 finalColor = tex2D(_MainTex, i.uv);

        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
        depth = LinearEyeDepth(depth);

        float blurCoefficient = (depth - (_FocusDistance * 3)) / _FocusRange;
        blurCoefficient = clamp(blurCoefficient, -1, 1);
        blurCoefficient *= lerp(1, -1, step(blurCoefficient, 0));

        fixed3 blurColor = 0;
        blurColor += tex2D(_MainTex, i.uv + float2(0.0,  3.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(0.0,  2.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(0.0,  1.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += finalColor.rgb;
        blurColor += tex2D(_MainTex, i.uv + float2(0.0, -1.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(0.0, -2.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(0.0, -3.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor *= 0.142857;

        finalColor.rgb = lerp(finalColor, blurColor, blurCoefficient);

        return finalColor;
    }

    fixed4 fragBlurHorizontal(VertexOutput i) : SV_Target
    {
        fixed4 finalColor = tex2D(_MainTex, i.uv);

        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
        depth = LinearEyeDepth(depth);

        float blurCoefficient = (depth - (_FocusDistance * 3)) / _FocusRange;
        blurCoefficient = clamp(blurCoefficient, -1, 1);
        blurCoefficient *= lerp(1, -1, step(blurCoefficient, 0));

        fixed3 blurColor = 0;
        blurColor += tex2D(_MainTex, i.uv + float2(3.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(2.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(1.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += finalColor.rgb;
        blurColor += tex2D(_MainTex, i.uv + float2(-1.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(-2.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor += tex2D(_MainTex, i.uv + float2(-3.0, 0.0) * _MainTex_TexelSize.xy * blurCoefficient * _BlurRadius);
        blurColor *= 0.142857;

        finalColor.rgb = lerp(finalColor, blurColor, blurCoefficient);

        return finalColor;
    }

    ENDCG

    SubShader
    {
        Cull Off
            ZTest Always
            ZWrite Off

            Tags{ "RenderType" = "Opaque" }

            Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment fragBlurVertical
            ENDCG
        }

            Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment fragBlurHorizontal
            ENDCG
        }
    }
}