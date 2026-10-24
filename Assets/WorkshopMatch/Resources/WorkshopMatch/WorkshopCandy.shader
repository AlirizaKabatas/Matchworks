Shader "WorkshopMatch/CandyToon"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) _Gloss("Glaze", Range(0,1)) = 0.5 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            float4 _BaseColor;
            float _Gloss;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal), l=normalize(float3(-0.45,0.7,-0.9));
                float3 eye=normalize(_WorldSpaceCameraPos-i.world);
                float shade=0.68+0.20*smoothstep(-0.3,0.2,dot(n,l))+0.12*smoothstep(0.35,0.65,dot(n,l));
                float shine=pow(saturate(dot(n,normalize(l+eye))),24)*_Gloss;
                float rim=pow(1-saturate(dot(n,eye)),3)*0.10;
                return float4(_BaseColor.rgb*shade+shine*float3(1,0.94,0.78)+rim*_BaseColor.rgb,1);
            }
            ENDCG
        }
    }
}
