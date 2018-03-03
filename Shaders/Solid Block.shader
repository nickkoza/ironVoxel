// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

Shader "Custom/SolidBlock"

{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
                #include "UnityCG.cginc"
                #pragma vertex vert
                #pragma fragment frag

                struct v2f
                {
                    fixed4 color : COLOR;
                    fixed4 pos : SV_POSITION;
                    fixed2 lmap : TEXCOORD1;
                    fixed2 pack0 : TEXCOORD0;
                };

                sampler2D _MainTex;
                fixed4 _MainTex_ST;

                v2f vert(appdata_full v)
                {
                    v2f o;
                    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                    o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color;
                    return o;
                }

                fixed4 frag(v2f i) : COLOR
                {
                    fixed4 c = tex2D(_MainTex, i.pack0)* i.color;
                    return c;
                }
            ENDCG
        }
    }
}