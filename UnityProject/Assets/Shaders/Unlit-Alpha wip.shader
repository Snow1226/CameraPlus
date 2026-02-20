Shader "Unlit/Transparent wip" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _DepthWriteThreshold ("Depth Write Threshold", Range(0,1)) = 0.5
    _AlphaClearThreshold ("Alpha Clear Threshold", Range(0,1)) = 0.5
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100

    // Depth-only pass: write depth only when _MainTex alpha is (nearly) 1.0.
    Pass {
        Name "DepthWrite"
        ZWrite On
        ZTest LEqual
        ColorMask 0

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _DepthWriteThreshold;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 fragDepth (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                // Keep only nearly-opaque fragments so they write depth.
                // Threshold slightly below 1.0 to account for filtering.
                clip(col.a - _DepthWriteThreshold);
                // ColorMask 0 ensures no color write; depth will be written because ZWrite On.
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Alpha-clear pass: where the main texture is fully opaque (alpha >= threshold),
    // overwrite the destination alpha to 0 before the main color pass.
    Pass {
        Name "AlphaClear"
        ZWrite Off
        ZTest Always
        ColorMask A
        Blend Off

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAlphaClear
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _AlphaClearThreshold;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 fragAlphaClear (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                // If alpha is above threshold, write alpha = 0 to the render target.
                clip(col.a - _AlphaClearThreshold);
                // Only alpha channel is written due to ColorMask A; return alpha 0.
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Main transparent pass: blended color, no depth write.
    Pass {
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
        ENDCG
    }
}

}
