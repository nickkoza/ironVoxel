// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

// Based on Transparent/Specular. Adds frame animation and lighting.
Shader "Custom/WaterBlock" {
Properties {
 _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
 _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
 _MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
}

SubShader {
 Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
 LOD 300

CGPROGRAM
#pragma surface surf BlinnPhong alpha

sampler2D _MainTex;
half _Shininess;

struct Input {
 float2 uv_MainTex;
 float4 color: Color;
};

float NUMBER_OF_FRAMES = 15.0;

void surf (Input IN, inout SurfaceOutput o) {
 // If you update the size of the textures or the number of water animation frames, you need to update these values:
 // 15.0 = number of frames in the water animation
 // 0.0625 = [width of the individual texture] / [width of the overall texture]
 IN.uv_MainTex[0] = IN.uv_MainTex[0] + floor(fmod(_Time[3] * 2.0, 15.0)) * 0.0625;
 
 fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
 o.Albedo = tex.rgb * IN.color.rgb;
 o.Gloss = tex.a;
 o.Alpha = tex.a * 0.7f;
 o.Specular = _Shininess;
}
ENDCG
}

Fallback "Transparent/VertexLit"
}
