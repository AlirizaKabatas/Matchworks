using System.Collections.Generic;
using UnityEngine;
namespace WorkshopMatch {
 public sealed partial class BoardBlockoutController {
  private readonly Dictionary<string,Texture2D> pieceTextures=new();
  private readonly Dictionary<Texture2D,Rect> pieceRects=new();
  private readonly Dictionary<string,Material> pieceMaterials=new();
  // Preserve source PNG colors; brightness is not a substitute for disabling
  // the template camera's tone mapping/vignette.
  private const float BoardSpriteBrightness=1f;
  private Texture2D PartTexture(int variant,int rust) {
   string key=(variant>=2?"screw":"nut")+"_"+((variant&1)==0?"yellow":"blue")+"_rust"+Mathf.Clamp(rust,0,2);
   if(!pieceTextures.TryGetValue("part_"+key,out var texture)) {
    texture=Resources.Load<Texture2D>("WorkshopMatch/Parts/"+key);pieceTextures["part_"+key]=texture;
   }
   return texture;
  }
  private bool TrySetProductionBoltSprite(CellView view,string key,int rust) {
   bool screw=key=="bolt_screw";
   Texture2D texture=PartTexture(view.PartVariant,rust)??(screw?(rust>=2?uiCatalog?.ScrewRust2:rust==1?uiCatalog?.ScrewRust1:uiCatalog?.ScrewClean)
    :(rust>=2?uiCatalog?.NutRust2:rust==1?uiCatalog?.NutRust1:uiCatalog?.NutClean));
   if(texture==null)return false;
   string stateKey="production_bolt_"+key+"_"+view.PartVariant+"_"+rust;
   if(view.ModelKey==stateKey && view.Model!=null)return true;
   if(view.Model!=null){view.Model.SetActive(false);Destroy(view.Model);}
   string materialKey="production_"+texture.name;
   if(!pieceMaterials.TryGetValue(materialKey,out Material material)) {
    material=new Material(Shader.Find("WorkshopMatch/FlatPiece")){name=materialKey,mainTexture=texture};
    material.SetColor("_BaseColor",Color.white);material.SetFloat("_Brightness",BoardSpriteBrightness);
    material.SetFloat("_ShineStrength",.38f);
    material.SetFloat("_ShinePhase",screw?.37f:.08f);
    Rect crop=PieceRect(texture);
    material.mainTextureScale=new Vector2(crop.width/texture.width,crop.height/texture.height);
    material.mainTextureOffset=new Vector2(crop.x/texture.width,crop.y/texture.height);
    pieceMaterials[materialKey]=material;generatedMaterials.Add(material);
   }
   GameObject visual=GameObject.CreatePrimitive(PrimitiveType.Quad);
   visual.SetActive(true);
   visual.name=stateKey;visual.GetComponent<Collider>().enabled=false;
   visual.transform.SetParent(view.Root,false);
   visual.transform.localPosition=new Vector3(0,0,-.85f);
   Rect visible=PieceRect(texture);
   float fit=CellPitch*.88f/Mathf.Max(visible.width,visible.height);
   visual.transform.localScale=new Vector3(visible.width*fit/CellSize,visible.height*fit/CellSize,1);
   Renderer renderer=visual.GetComponent<Renderer>();renderer.sharedMaterial=material;
   renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
   view.Model=visual;view.ModelKey=stateKey;view.ModelRenderers=new[]{renderer};return true;
  }
  private void UpdateMotorIndicators(CellView view,int hits,int required) {
   if(view.Model==null || !view.ModelKey.StartsWith("sprite_motor"))return;
   int total=Mathf.Max(0,required);
   // A single inset bar fills from left to right, anchored to the drawn control strip.
   for(int i=0;i<2;i++) {
    string lampName=i==0?"Progress Track":"Progress Fill";
    var child=view.Model.transform.Find(lampName);
    if(child==null) {
     var lamp=GameObject.CreatePrimitive(PrimitiveType.Quad);lamp.name=lampName;lamp.GetComponent<Collider>().enabled=false;
     child=lamp.transform;child.SetParent(view.Model.transform,false);
     var r=lamp.GetComponent<Renderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
    }
    float fraction=total>0?Mathf.Clamp01((float)hits/total):0;
    float width=i==0?.37f:.35f*fraction;
    child.localPosition=new Vector3(i==0?0:-.175f+width*.5f,.17f,i==0?-.02f:-.025f);
    child.localScale=new Vector3(width,i==0?.053f:.035f,1);
    child.gameObject.SetActive(i==0 || fraction>0);
    string key=i==1?"motor_bar_on":"motor_bar_off";
    if(!pieceMaterials.TryGetValue(key,out var material)) {
     material=new Material(Shader.Find("WorkshopMatch/FlatPiece")){mainTexture=Texture2D.whiteTexture,name=key};
     material.SetColor("_BaseColor",i==1?new Color(1f,.73f,.16f):new Color(.25f,.17f,.09f));
     material.SetFloat("_CornerRadius",.12f);
     pieceMaterials[key]=material;generatedMaterials.Add(material);
    }
    child.GetComponent<Renderer>().sharedMaterial=material;
   }
  }
  private Texture2D PieceTexture(string name) {
   if(name=="bolt_nut") return PartTexture(0,0)??uiCatalog?.NutClean;
   if(name=="bolt_screw") return PartTexture(2,0)??uiCatalog?.ScrewClean;
   if(!pieceTextures.TryGetValue(name,out var texture)) {
    if(name=="candy_0") texture=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/candy_0_hex");
    if(name=="candy_2") texture=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/candy_2_full");
    if(texture==null) texture=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/"+name);
    pieceTextures[name]=texture;
   } return texture;
  }
  private Rect PieceRect(Texture2D texture) {
   if(pieceRects.TryGetValue(texture,out Rect rect))return rect;
   rect=new Rect(0,0,texture.width,texture.height);
   if(texture.isReadable) {
    var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
    for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
     if(pixels[y*texture.width+x].a>32){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
    if(maxX>=minX)rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
   } pieceRects[texture]=rect;return rect;
  }
  private void DrawPieceIcon(Rect area,string key) {
   if(key=="oil") {Color oldOil=GUI.color;GUI.color=new Color(.72f,.56f,.37f,oldOil.a);DrawArt(area,uiCatalog?.OilSplash);GUI.color=oldOil;return;}
   var texture=PieceTexture(key);if(texture==null)return;
   Rect crop=PieceRect(texture);float fit=Mathf.Min(area.width/crop.width,area.height/crop.height);
   Vector2 size=crop.size*fit;
   if(key=="motor"){float h=Mathf.Min(area.height,area.width/1.5f);size=new Vector2(h*1.5f,h);}
   Color oldIcon=GUI.color;
   GUI.DrawTextureWithTexCoords(new Rect(area.center-size/2,size),texture,
    new Rect(crop.x/texture.width,crop.y/texture.height,crop.width/texture.width,crop.height/texture.height),true);
   GUI.color=oldIcon;
  }
  private bool TrySetPieceSprite(CellView view,string key,CellContent content,SpecialKind special) {
   string file=key.StartsWith("ray_")?"rocket":key.StartsWith("motor_")?"motor":key;
   var texture=PieceTexture(file);if(texture==null)return false;
   if(view.ModelKey=="sprite_"+key && view.Model!=null)return true;
   bool reuse=view.Model!=null&&view.ModelKey.StartsWith("sprite_");
   if(view.Model!=null&&!reuse){view.Model.SetActive(false);Destroy(view.Model);}
   Rect crop=PieceRect(texture);
   if(!pieceMaterials.TryGetValue(file,out var material)) {
    material=new Material(Shader.Find("WorkshopMatch/FlatPiece")){name="2D "+file,mainTexture=texture};
    material.SetColor("_BaseColor",Color.white);material.SetFloat("_Brightness",BoardSpriteBrightness);
    if(file=="frozen") {
     material.SetColor("_BaseColor",Color.white);material.SetFloat("_Brightness",BoardSpriteBrightness);
     material.SetFloat("_FrostGlow",.80f);
     material.SetFloat("_EdgeAccent",.08f);
     material.SetFloat("_CenterOpacity",1f);
     material.SetFloat("_CornerRadius",.095f);
    }
    material.mainTextureScale=new Vector2(crop.width/texture.width,crop.height/texture.height);
    material.mainTextureOffset=new Vector2(crop.x/texture.width,crop.y/texture.height);
    pieceMaterials[file]=material;generatedMaterials.Add(material);
   }
   GameObject visual=reuse?view.Model:GameObject.CreatePrimitive(PrimitiveType.Quad);
   visual.SetActive(true);
   visual.name="2D "+key;visual.GetComponent<Collider>().enabled=false;
   visual.transform.SetParent(view.Root,false);visual.transform.localPosition=new Vector3(0,0,-0.85f);
   float width=CellPitch*0.94f,height=width;
   bool turn=special==SpecialKind.RayHorizontal||key=="motor_v";
   if(content==CellContent.Motor){width=CellPitch*3-0.05f;height=CellPitch*2-0.05f;}
   else {float fit=width/Mathf.Max(crop.width,crop.height);width=crop.width*fit;height=crop.height*fit;}
   // Candy silhouettes keep their source aspect ratio; independent axis growth
   // stretched the horizontal red and vertical green pieces.
   if(key=="frozen") width=height=CellPitch*.96f;
   visual.transform.localScale=new Vector3(width/CellSize,height/CellSize,1);
   visual.transform.localRotation=Quaternion.Euler(0,0,special==SpecialKind.RayHorizontal?-90:turn?90:0);
   var renderer=visual.GetComponent<Renderer>();
   renderer.SetPropertyBlock(null);renderer.sharedMaterial=material;
   renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
   view.Model=visual;view.ModelKey="sprite_"+key;if(!reuse)view.ModelRenderers=new[]{renderer};return true;
  }
 }
}
