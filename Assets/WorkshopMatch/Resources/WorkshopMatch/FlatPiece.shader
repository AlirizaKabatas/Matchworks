Shader "WorkshopMatch/FlatPiece" {
 Properties { _Brightness("Sprite brightness",Range(1,1.5))=1  _ShineStrength("Periodic shine",Range(0,1))=0 _ShinePhase("Shine phase",Float)=0 _EdgeAccent("Edge accent",Range(0,1))=0 _FrostGlow("Frozen oil sparkle",Range(0,1))=0  _FallClipTop("Fall ceiling",Float)=100000 _FallClipLeft("Fall left edge",Float)=-100000 _FallClipRight("Fall right edge",Float)=100000 [MainTexture] _MainTex("Sprite",2D)="white"{} [MainColor] _BaseColor("Tint",Color)=(1,1,1,1) _CornerRadius("Rounded tile radius",Range(0,0.3))=0 _CenterOpacity("Center opacity",Range(0,1))=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
 struct output { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 localUV:TEXCOORD1; float2 worldXY:TEXCOORD2; };
 sampler2D _MainTex; float4 _MainTex_ST; float4 _BaseColor; float _CornerRadius; float _CenterOpacity;
 float _Brightness; float _FallClipTop; float _FallClipLeft; float _FallClipRight; float _ShineStrength; float _ShinePhase; float _EdgeAccent; float _FrostGlow;
 output vert(input v) { output o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.localUV=v.uv;o.worldXY=mul(unity_ObjectToWorld,v.vertex).xy;return o; }
 fixed4 frag(output i):SV_Target {
  clip(_FallClipTop-i.worldXY.y);
  clip(i.worldXY.x-_FallClipLeft);clip(_FallClipRight-i.worldXY.x);
  fixed4 color=tex2D(_MainTex,i.uv)*_BaseColor;
  if(_CornerRadius>0) {
   float2 q=abs(i.localUV-.5)-(.5-_CornerRadius);
   float distance=length(max(q,0))+min(max(q.x,q.y),0)-_CornerRadius;
   color.a*=1-smoothstep(-.004,0,distance);
  }
  float edge=smoothstep(.12,.48,max(abs(i.localUV.x-.5),abs(i.localUV.y-.5)));
  color.a*=lerp(_CenterOpacity,1,edge);
  // One short diagonal glint every 2.8 seconds, clipped to the sprite alpha.
  float cycle=frac(_Time.y/2.8+_ShinePhase);
  float window=smoothstep(0,.035,cycle)*(1-smoothstep(.16,.21,cycle));
  float sweep=cycle/.21*2.6-.5;
  float glint=1-smoothstep(.025,.12,abs(i.localUV.x+i.localUV.y-sweep));
  color.rgb=lerp(color.rgb,float3(1,1,.86),glint*window*_ShineStrength);
  float rim=smoothstep(.40,.48,max(abs(i.localUV.x-.5),abs(i.localUV.y-.5)));
  color.rgb=lerp(color.rgb,float3(1,.82,.42),rim*_EdgeAccent);
  // Frozen oil uses breathing rim/corner sparkles, never the bolt's moving strip.
  float breath=pow(.5+.5*sin(_Time.y*2.1),5);
  float2 cornerUV=abs(i.localUV-.5);
  float corner=1-smoothstep(.028,.075,length(cornerUV-float2(.36,.36)));
  float cornerPulse=pow(.5+.5*sin(_Time.y*3.6+i.localUV.x*11+i.localUV.y*7),12);
  color.rgb=lerp(color.rgb,float3(1,.97,.77),_FrostGlow*(rim*breath*.38+corner*cornerPulse));
  color.rgb=pow(saturate(color.rgb),1.0/max(1.0,_Brightness));
  return color;
 }
 ENDCG
 } } }
