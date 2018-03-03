// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

// Based on Transparent/Cutout/Diffuse. Adds lighting.
Shader "Custom/TransparentBlock" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
}

SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 200
     
CGPROGRAM
#pragma surface surf Lambert alphatest:_Cutoff

sampler2D _MainTex;

struct Input {
    float2 uv_MainTex;
    float4 color: Color;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
    o.Albedo = c.rgb;
    o.Alpha = c.a;
}
ENDCG
}

Fallback "Transparent/Cutout/VertexLit"
}
