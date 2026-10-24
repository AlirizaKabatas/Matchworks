Shader "WorkshopMatch/WorkshopBackdrop" {
 Properties {
  _MainTex("Unused source",2D)="white"{}
  _BaseColor("Theme",Color)=(0.15,0.64,0.7,1)
  _PatternSeed("Level pattern seed",Float)=31
  _MotionTime("Unscaled animation time",Float)=0
  _GearEtching("Integrated animated gear wallpaper",Float)=0
  _GearAtlas("Original gear artwork",2D)="white"{}
  _GearSupportArt("Flowing mechanical wood support",2D)="white"{}
  _GearAspect("Portrait aspect",Float)=1
 }
 SubShader { Tags { "Queue"="Background" "RenderType"="Opaque" } ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 ringA:TEXCOORD1; float3 ringB:TEXCOORD2; float3 ringC:TEXCOORD3; float2 patternInfo:TEXCOORD4; float4 screen:TEXCOORD5; };
 float4 _BaseColor;
 float _PatternSeed, _MotionTime;
 float _GearEtching,_GearAspect;
 float4 _GearA,_GearB,_GearC,_GearD,_GearE;
 sampler2D _GearAtlas,_GearSupportArt;
 float4 _GearUvA,_GearUvB,_GearUvC,_GearUvD,_GearUvE;
 float4 _GearSupportRect,_GearSupportUv;
 float randomValue(float n) { return frac(sin(n*127.1+_PatternSeed*31.7)*43758.5453); }
 // Seed and motion calculations run only at the quad's four vertices.
 float3 ringData(float n,float phase) {
  float k=n*5+2;
  float t=_MotionTime*.28;
  float2 center=float2(.08+.84*randomValue(k),.10+.80*randomValue(k+1));
  center+=float2(sin(t+phase+n*2),cos(t*.8+phase+n))*float2(.026,.018);
  return float3(center,.065+.10*randomValue(k+2));
 }
 v2f vert(appdata v) {
  v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.screen=ComputeScreenPos(o.pos);
  float phase=randomValue(1)*6.283;
  o.ringA=ringData(0,phase);o.ringB=ringData(1,phase);o.ringC=ringData(2,phase);
  o.patternInfo=float2(phase,.12+.76*randomValue(20));return o;
 }
 float ringAmount(float2 p,float3 ring) {
  float2 q=p-ring.xy;q.x*=.58;
  return 1-smoothstep(.004,.012,abs(length(q)-ring.z));
 }
 // Use the same artwork and tooth phases as other levels, embedded in the backdrop.
 float3 applyGearArt(float3 background,float4 art) {
  // Reuse texture detail as relief, but remove all gold/peach/cyan source hues.
  float detail=dot(art.rgb,float3(.299,.587,.114));
  float3 lineColor=lerp(_BaseColor.rgb,float3(1,1,1),.36);
  float3 ink=lerp(_BaseColor.rgb*.92,lineColor,saturate(detail));
  return lerp(background,ink,art.a);
 }
 float4 gearArt(float2 p,float4 gear,float4 crop) {
  if(gear.z<=0)return float4(0,0,0,0);
  float2 q=(p-gear.xy)*float2(_GearAspect,1)/gear.z;
  // Rotation-independent rejection keeps atlas sampling local to the upper area.
  if(dot(q,q)>2)return float4(0,0,0,0);
  float c=cos(gear.w),s=sin(gear.w);
  float2 local=float2(q.x*c+q.y*s,-q.x*s+q.y*c)*.5+.5;
  if(any(local<0)||any(local>1))return float4(0,0,0,0);
  return tex2D(_GearAtlas,crop.xy+local*crop.zw);
 }
 fixed4 frag(v2f i):SV_Target {
  float t=_MotionTime*.28;
  float2 p=i.uv;
  float phase=i.patternInfo.x;
  // A gentle diagonal wash drifts without moving objects or texture swaps.
  float wave=_GearEtching>.5?.5:.5+.5*sin(p.y*7+p.x*4+t+phase);
  float3 color=_BaseColor.rgb*(.93+.07*p.y+.035*wave);
  float pattern=ringAmount(p,i.ringA)+ringAmount(p,i.ringB)+ringAmount(p,i.ringC);
  float x=i.patternInfo.y;
  float curve=x+.065*sin(p.y*8+t*.7+phase);
  float band=1-smoothstep(.009,.027,abs(p.x-curve));
  if(_GearEtching>.5) {
   // The gears form the animated wallpaper motif in every level's own palette.
   float2 screen=i.screen.xy/i.screen.w;
   if(_GearSupportRect.z>0&&_GearSupportRect.w>0) {
    float2 local=(screen-_GearSupportRect.xy)/_GearSupportRect.zw;
    if(all(local>=0)&&all(local<=1))
     color=applyGearArt(color,tex2D(_GearSupportArt,_GearSupportUv.xy+local*_GearSupportUv.zw));
   }
   color=applyGearArt(color,gearArt(screen,_GearA,_GearUvA));
   color=applyGearArt(color,gearArt(screen,_GearB,_GearUvB));
   color=applyGearArt(color,gearArt(screen,_GearC,_GearUvC));
   color=applyGearArt(color,gearArt(screen,_GearD,_GearUvD));
   color=applyGearArt(color,gearArt(screen,_GearE,_GearUvE));
  } else {
   color=lerp(color,lerp(_BaseColor.rgb,float3(1,1,1),.45),saturate(pattern*.15+band*.10));
  }
  return float4(color,1);
 }
 ENDCG
 } } }
