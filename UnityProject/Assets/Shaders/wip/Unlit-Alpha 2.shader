//Spout Receiver display shader
Shader "BeatSaber/Unlit/Transparent" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _DepthWriteThreshold ("Depth Write Threshold", Range(0,1)) = 0.5
    _AlphaClearThreshold ("Alpha Clear Threshold", Range(0,1)) = 0.5
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100

    //Common Section.
    CGINCLUDE
        #pragma vertex vert
        #pragma target 2.0
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        static const float _FloorPriorityY = 0.01;

        struct appdata_t {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct v2f {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float worldY : TEXCOORD1;
            UNITY_FOG_COORDS(2)
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            float4 worldVertex = mul(unity_ObjectToWorld, v.vertex);
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            o.worldY = worldVertex.y;
            UNITY_TRANSFER_FOG(o,o.vertex);
            return o;
        }
    ENDCG

    // Depth-only pass for the floor-priority area.
    Pass {
        Name "DepthWriteFloorPriority"
        ZWrite On
        ZTest Always
        ColorMask 0

        CGPROGRAM
            #pragma fragment fragDepthFloorPriority

            fixed _DepthWriteThreshold;

            fixed4 fragDepthFloorPriority (v2f i) : SV_Target
            {
                clip(_FloorPriorityY - i.worldY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                clip(col.a - _DepthWriteThreshold);
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Depth-only pass for the normal area.
    Pass {
        Name "DepthWrite"
        ZWrite On
        ZTest LEqual
        ColorMask 0

        CGPROGRAM
            #pragma fragment fragDepth

            fixed _DepthWriteThreshold;

            fixed4 fragDepth (v2f i) : SV_Target
            {
                clip(i.worldY - _FloorPriorityY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                // Keep only nearly-opaque fragments so they write depth.
                // Threshold slightly below 1.0 to account for filtering.
                clip(col.a - _DepthWriteThreshold);
                // ColorMask 0 ensures no color write; depth will be written because ZWrite On.
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Alpha clear pass for the floor-priority area.
    Pass {
        Name "AlphaClearFloorPriority"
        ZWrite On
        ZTest Always
        ColorMask A
        Blend Off

        CGPROGRAM
            #pragma fragment fragAlphaClearFloorPriority

            fixed _AlphaClearThreshold;

            fixed4 fragAlphaClearFloorPriority (v2f i) : SV_Target
            {
                clip(_FloorPriorityY - i.worldY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                clip(col.a - _AlphaClearThreshold);
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Alpha clear pass: If the main texture is fully opaque (alpha >= threshold), 
    // the destination alpha is overwritten to 0 before the main color pass 
    // for bloom removal on the background (walls, sabers, etc.).
    Pass {
        Name "AlphaClear"
        ZWrite On
        ZTest LEqual
        ColorMask A
        Blend Off

        CGPROGRAM
            #pragma fragment fragAlphaClear

            fixed _AlphaClearThreshold;

            fixed4 fragAlphaClear (v2f i) : SV_Target
            {
                clip(i.worldY - _FloorPriorityY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                // If alpha is above threshold, write alpha = 0 to the render target.
                clip(col.a - _AlphaClearThreshold);
                // Only alpha channel is written due to ColorMask A; return alpha 0.
                return fixed4(0,0,0,0);
            }
        ENDCG
    }

    // Main transparent pass for the floor-priority area.
    Pass {
        Name "ColorFloorPriority"
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

        CGPROGRAM
            #pragma fragment fragFloorPriority
            #pragma multi_compile_fog

            fixed4 fragFloorPriority (v2f i) : SV_Target
            {
                clip(_FloorPriorityY - i.worldY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
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
            #pragma fragment frag
            #pragma multi_compile_fog

            fixed4 frag (v2f i) : SV_Target
            {
                clip(i.worldY - _FloorPriorityY);
                fixed4 col = tex2D(_MainTex, i.texcoord);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
        ENDCG
    }
}

}
