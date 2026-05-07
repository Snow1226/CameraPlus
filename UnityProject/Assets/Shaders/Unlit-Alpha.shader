//Spout Receiver display shader
Shader "BeatSaber/Unlit/Transparent" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Threshold ("Threshold", Range(0,1)) = 0.9
    _Offset ("Offset", Vector) = (0,0,0,0)
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
        float4 _Offset;
        static const float _FloorPriorityY = 0.01;
        fixed _Threshold;

        inline bool UsesTextureAlpha()
        {
            return _Threshold < 1;
        }

        inline bool IsZeroPixel(fixed4 col)
        {
            return col.r == 0 && col.g == 0 && col.b == 0 && col.a == 0;
        }

        inline float2 GetScrolledUv(float2 uv)
        {
            return uv + _Offset.xy;
        }

        inline void ClipScrolledUv(float2 uv)
        {
            clip(uv.x);
            clip(uv.y);
            clip(1 - uv.x);
            clip(1 - uv.y);
        }

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

            fixed4 fragDepthFloorPriority (v2f i) : SV_Target
            {
                clip(_FloorPriorityY - i.worldY);
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                if (UsesTextureAlpha())
                {
                    clip(col.a - _Threshold);
                }
                else if (IsZeroPixel(col))
                {
                    clip(-1);
                }
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

            fixed4 fragDepth (v2f i) : SV_Target
            {
                clip(i.worldY - _FloorPriorityY);
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                if (UsesTextureAlpha())
                {
                    clip(col.a - _Threshold);
                }
                else if (IsZeroPixel(col))
                {
                    clip(-1);
                }
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

            fixed4 fragAlphaClearFloorPriority (v2f i) : SV_Target
            {
                clip(_FloorPriorityY - i.worldY);
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                if (UsesTextureAlpha())
                {
                    clip(col.a - _Threshold);
                }
                else if (IsZeroPixel(col))
                {
                    clip(-1);
                }
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

            fixed4 fragAlphaClear (v2f i) : SV_Target
            {
                clip(i.worldY - _FloorPriorityY);
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                if (UsesTextureAlpha())
                {
                    clip(col.a - _Threshold);
                }
                else if (IsZeroPixel(col))
                {
                    clip(-1);
                }
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
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                col.a = UsesTextureAlpha() ? col.a : (IsZeroPixel(col) ? 0 : 1);
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
                float2 uv = GetScrolledUv(i.texcoord);
                ClipScrolledUv(uv);
                fixed4 col = tex2D(_MainTex, uv);
                col.a = UsesTextureAlpha() ? col.a : (IsZeroPixel(col) ? 0 : 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
        ENDCG
    }
}

}
