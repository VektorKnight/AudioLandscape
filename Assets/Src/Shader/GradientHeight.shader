﻿Shader "Custom/GradientHeight"
{
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Gradient (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MaxHeight("Maximum Height", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input {
            // We need the local position for gradient sampling.
            float3 localPos;
        };

        fixed4 _Color;
        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        half _MaxHeight;
        
        void vert (inout appdata_full v, out Input o) {
            o.localPos = v.vertex.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Here we assume MaxHeight is an extent from zero.
            // We shift the coordinate up by the max height then divide by 2 * MaxHeight to get a 0-1 coordinate.
            // This then becomes our X-Coord for sampling the gradient texture.
            // The Y-Coord is always zero as the gradient texture is 1D.
            float2 uv = float2((IN.localPos.y + _MaxHeight) / (_MaxHeight * 2), 1.0);
            fixed4 c = tex2D (_MainTex, uv) * _Color;
            o.Albedo = c.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
