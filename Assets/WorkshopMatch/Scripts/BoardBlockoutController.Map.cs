using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly Texture2D[] flatNodes=new Texture2D[3];
        private Texture2D mapTerrainTexture;
        private Texture2D illustratedMap;
        private Texture2D mapDash;
        private float mapScrollPitch;
        private Texture2D IllustratedMap()
        {
            if(illustratedMap!=null) return illustratedMap;
            Texture2D source=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/map_reference_exact");
            if(source==null) return null;
            // Keep source art immutable. Separate its route from its reusable paper.
            Color32[] pixels=source.GetPixels32();
            for(int y=0;y<source.height;y++)
            {
                float topY=1536f-y*1536f/source.height;
                if(topY<384 || topY>1012)continue;
                for(int x=0;x<source.width;x++)
                {
                    float sourceX=x*1024f/source.width;
                    if(sourceX<220 || sourceX>822)continue;
                    Color c=pixels[y*source.width+x];
                    if(c.r<.87f || c.g<.58f || c.r-c.b<.19f)continue;
                    // Dashes are the only saturated gold in this corridor; neighboring
                    // paper is sampled well outside their antialiased edges.
                    int a=Mathf.Max(0,x-42*source.width/1024),b=Mathf.Min(source.width-1,x+42*source.width/1024);
                    Color paper=source.GetPixel(a,y);
                    Color other=source.GetPixel(b,y);
                    pixels[y*source.width+x]=paper.b>other.b?paper:other;
                }
            }
            // Lift the three baked buttons into interactive layers. Reconstruct only the
            // small paper patches underneath from their neighboring paper pixels.
            foreach(Rect originalCut in new[]{new Rect(145,999,176,132),new Rect(405,601,238,158),new Rect(713,265,186,120),
                new Rect(640,20,384,225),new Rect(0,1180,315,305),
                new Rect(0,86,185,134),new Rect(63,359,100,62),new Rect(634,686,105,82),
                new Rect(20,772,125,88),new Rect(278,1080,125,76),new Rect(869,1100,155,116)})
            {
                Rect cut=Rect.MinMaxRect(Mathf.Max(0,originalCut.x-60),Mathf.Max(0,originalCut.y-60),
                    Mathf.Min(1024,originalCut.xMax+60),Mathf.Min(1536,originalCut.yMax+60));
                int x0=Mathf.RoundToInt(cut.x*source.width/1024f),x1=Mathf.RoundToInt(cut.xMax*source.width/1024f);
                int y0=Mathf.RoundToInt((1536-cut.yMax)*source.height/1536f),y1=Mathf.RoundToInt((1536-cut.y)*source.height/1536f);
                for(int y=y0;y<y1;y++)
                {
                    // Never stretch a sampled boundary row: its rock shadow or contour
                    // becomes a long horizontal stripe across the repaired patch.
                    Color paper = new Color(.995f, .982f, .953f, 1f);
                    for(int x=x0;x<x1;x++)
                    {
                        float feather=32f*source.width/1024f;
                        float edge=Mathf.Min(y-y0,y1-1-y);
                        if(x0>0)edge=Mathf.Min(edge,x-x0);
                        if(x1<source.width)edge=Mathf.Min(edge,x1-1-x);
                        float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/feather));
                        pixels[y*source.width+x]=Color.Lerp(pixels[y*source.width+x],
                            paper,blend);
                    }
                }
            }
            illustratedMap=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            // Neutral grey shadows belonged to the lifted baked disks/rocks, not
            // the paper. Remove their remaining horizontal ghosts from the terrain.
            for(int i=0;i<pixels.Length;i++)
            {
                Color c=pixels[i];
                float neutral=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.08f,.16f,c.r-c.b));
                float dark=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.94f,.985f,c.r));
                pixels[i]=Color.Lerp(c,new Color(.995f,.982f,.953f,1),neutral*dark);
            }
            illustratedMap.SetPixels32(pixels);
            illustratedMap.Apply(false,false);
            illustratedMap.filterMode=FilterMode.Bilinear;
            illustratedMap.wrapMode=TextureWrapMode.Clamp;
            return illustratedMap;
        }
        // About three levels per viewport, independent of the source illustration's aspect ratio.
        private static float MapLevelPitch(Rect area) => Mathf.Max(area.height*.34f,area.width*.52f);
        private float MapHeight(Rect area)
        {
            Texture2D map=IllustratedMap();
            return area.width*(map!=null?(float)map.height/map.width:4f);
        }
        private Vector2 IllustratedMapPoint(Rect area,float level,float focus)
        {
            Vector2 p=ReferenceWorldPoint(level),camera=ReferenceWorldPoint(focus);
            return new Vector2(area.width*p.x,area.height*.54f+(camera.y-p.y)*area.width);
        }
        private static Vector2 ReferenceWorldPoint(float level)
        {
            // Shared turning points: 1/2/3 rightward, 3/4/5 leftward, etc.
            // No duplicated endpoint and no straight panel-joining segment.
            return new Vector2(.5f-.24f*Mathf.Cos(level*Mathf.PI*.5f),level*.64f);
        }
        private Texture2D MapTerrainTexture()
        {
            if(mapTerrainTexture!=null)return mapTerrainTexture;
            mapTerrainTexture=new Texture2D(192,128,TextureFormat.RGBA32,false);
            var pixels=new Color[192*128];
            for(int y=0;y<128;y++)for(int x=0;x<192;x++)
            {
                Vector2 p=new((x-95.5f)/91f,(y-63.5f)/59f);
                float a=Mathf.Atan2(p.y,p.x),edge=.87f+.07f*Mathf.Sin(a*3)+.035f*Mathf.Cos(a*5);
                float r=p.magnitude;
                float opacity=Mathf.SmoothStep(0,.55f,Mathf.Clamp01((edge-r)/.16f));
                pixels[y*192+x]=new Color(.89f,.86f,.78f,opacity);
            }
            mapTerrainTexture.SetPixels(pixels);mapTerrainTexture.Apply();return mapTerrainTexture;
        }
        private Texture2D FlatMapNode(bool unlocked,bool current)
        {
            int index=!unlocked?0:current?2:1;
            if(flatNodes[index]!=null)return flatNodes[index];
            Texture2D reference=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/map_reference_exact");
            if(reference!=null)
            {
                // Reuse the actual blue disk in the approved artwork, including its golden base.
                float sx=reference.width/1024f,sy=reference.height/1536f;
                int left=Mathf.RoundToInt(150*sx),bottom=Mathf.RoundToInt((1536-1125)*sy);
                int w=Mathf.RoundToInt(166*sx),h=Mathf.RoundToInt(125*sy);
                Color[] crop=reference.GetPixels(left,bottom,w,h);
                // Source rows 1000..1010 contain the painted road entering the disk.
                // The disk itself starts at row 1011; retain its full shape and draw only the live route.
                int roadRows=Mathf.CeilToInt(11f*sy);
                for(int i=0;i<crop.Length;i++)
                {
                    if(i/w>=h-roadRows){crop[i]=Color.clear;continue;}
                    Color c=crop[i];
                    // The crop includes the reference's cream paper; it is not part of
                    // the interactive disk. Key only neutral warm paper, not gold/blue.
                    float chroma = Mathf.Max(c.r,c.g,c.b)-Mathf.Min(c.r,c.g,c.b);
                    float opacity = Mathf.Clamp01((chroma-.10f)/.12f);
                    c.a *= opacity;
                    if(c.b>c.r*1.2f && c.g>c.r*1.1f)
                    {
                        if(index==0){float gray=c.grayscale; c=new Color(gray*.92f,gray*.98f,gray,c.a);}
                        else if(index==2)c=new Color(c.r*.65f,Mathf.Min(1,c.g*1.28f),Mathf.Min(1,c.b*1.04f),c.a);
                    }
                    crop[i]=c;
                }
                var disk=new Texture2D(w,h,TextureFormat.RGBA32,false);
                disk.SetPixels(crop);disk.Apply();flatNodes[index]=disk;return disk;
            }
            var texture=new Texture2D(192,128,TextureFormat.RGBA32,false);
            var pixels=new Color[192*128];
            Color fill=index==0?new Color(.63f,.66f,.69f):index==2?new Color(.20f,.84f,.95f):new Color(.27f,.66f,.94f);
            for(int y=0;y<128;y++)for(int x=0;x<192;x++)
            {
                // Shallow golden cylinder: contact shadow, exposed front wall, then raised face.
                float shadow=new Vector2((x-97f)/94f,(y-48f)/39f).magnitude;
                Color pixel=new Color(.36f,.25f,.12f,MapEdgeOpacity(shadow,.78f)*.20f);
                float baseRadius=new Vector2((x-95.5f)/87f,(y-59f)/42f).magnitude;
                if(baseRadius<1)
                    pixel=new Color(y<39?.66f:.88f,y<39?.37f:.56f,.16f,MapEdgeOpacity(baseRadius,.98f));
                float radius=new Vector2((x-95.5f)/87f,(y-74f)/42f).magnitude;
                if(radius<1)
                {
                    Color face=Color.Lerp(fill,new Color(fill.r*.78f,fill.g*.78f,fill.b*.78f,1),y<65?.20f:0);
                    bool shine=new Vector2((x-70f)/15f,(y-86f)/7f).sqrMagnitude<1;
                    pixel=radius>.96f?new Color(.78f,.47f,.14f):radius>.83f?new Color(1f,.80f,.36f):radius>.77f?new Color(1f,.93f,.64f):shine?Color.Lerp(face,Color.white,.35f):face;
                    pixel.a=MapEdgeOpacity(radius,.98f);
                }
                pixels[y*192+x]=pixel;
            }
            texture.SetPixels(pixels);texture.Apply();flatNodes[index]=texture;return texture;
        }
        private static float MapEdgeOpacity(float radius,float fadeStart)
        {
            // Mathf.SmoothStep interpolates OUTPUT values; normalize the radius first.
            // The entire disk interior must remain opaque, with only the edge feathered.
            return 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(fadeStart,1f,radius));
        }
        private static void DrawMapPaper(Rect rect)
        {
            Color old=GUI.color;GUI.color=new Color(.94f,.87f,.72f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
        }
        private void DrawWoodPanel(Rect rect)
        {
            Color old=GUI.color;GUI.color=new Color(.61f,.32f,.12f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=new Color(.88f,.57f,.25f);GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,Mathf.Max(2,rect.width*.008f)),Texture2D.whiteTexture);
            GUI.color=new Color(.34f,.17f,.065f);GUI.DrawTexture(new Rect(rect.x,rect.yMax-rect.width*.006f,rect.width,rect.width*.006f),Texture2D.whiteTexture);
            GUI.color=old;
            float tile=rect.width*.26f;
            float tileHeight=tile*28f/120f;
            for(float y=rect.y+rect.width*.012f;y<rect.yMax-rect.width*.012f;y+=tileHeight)
                for(float x=rect.x;x<rect.xMax;x+=tile)
                {
                    float w=Mathf.Min(tile,rect.xMax-x),h=Mathf.Min(tileHeight,rect.yMax-rect.width*.012f-y);
                    DrawSource(new Rect(x,y,w,h),uiCatalog?.HomeTopFrame,new Rect(190,105,120*w/tile,28*h/tileHeight));
                }
        }
        // World coordinates are X/Z, never an XY ribbon. Camera looks down 35 degrees.
        private Vector2 ProjectMapGround(Rect area, Vector2 ground, float focus)
        {
            // Shallow ground camera; perspective is bounded so distant levels never pile at a horizon.
            // No yaw term proportional to depth: the route cannot drift continuously left.
            float depth=ground.y-focus*2.6f;
            float scale=Mathf.Clamp(1f/(1f+Mathf.Max(-6f,depth)*.035f),.72f,1.18f);
            float unit=area.width*.27f;
            return new Vector2(area.center.x+ground.x*unit*scale,
                area.y+area.height*.54f-(depth+ground.x*.32f)*unit*.62f*scale);
        }

        private void DrawGroundPatch(Rect area,Vector2 ground,float focus)
        {
            Vector2 p=ProjectMapGround(area,ground+new Vector2(-.72f,-.48f),focus);
            Vector2 right=ProjectMapGround(area,ground+new Vector2(.72f,-.48f),focus)-p;
            Vector2 down=ProjectMapGround(area,ground+new Vector2(-.72f,.48f),focus)-p;
            Matrix4x4 old=GUI.matrix,plane=Matrix4x4.identity;
            plane.m00=right.x;plane.m10=right.y;plane.m01=down.x;plane.m11=down.y;
            plane.m03=p.x;plane.m13=p.y;GUI.matrix=old*plane;
            GUI.DrawTexture(new Rect(0,0,1,1),MapTerrainTexture());GUI.matrix=old;
        }

        private static void MapLine(Vector2 a, Vector2 b, float width, Color color)
        {
            Matrix4x4 matrix=GUI.matrix; Color old=GUI.color;
            GUI.color=color; GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);
            GUI.matrix=matrix;GUI.color=old;
        }

        private void DrawMapDash(Vector2 a,Vector2 b,float width)
        {
            if(mapDash==null)
            {
                mapDash=new Texture2D(64,32,TextureFormat.RGBA32,false);
                var pixels=new Color[64*32];
                for(int y=0;y<32;y++)for(int x=0;x<64;x++)
                {
                    float dx=x-Mathf.Clamp(x,16f,47f),dy=y-15.5f;
                    pixels[y*64+x]=new Color(1f,.78f,.43f,Mathf.Clamp01(16f-Mathf.Sqrt(dx*dx+dy*dy)));
                }
                mapDash.SetPixels(pixels);mapDash.Apply();
            }
            Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;bool enabled=GUI.enabled;
            GUI.enabled=true;GUI.color=Color.white;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b),width),mapDash);
            GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabled;
        }

        private void DrawMapGroundDraft(Rect area, float focus)
        {
            Texture2D map=IllustratedMap();
            if(map!=null)
            {
                Color savedColor=GUI.color;
                bool savedEnabled=GUI.enabled;
                GUI.color=new Color(.96f,.90f,.78f);
                GUI.enabled=true;
                // Shallow compression of the ground plane; disks use the same angle.
                float height=area.width*1.5f*.88f;
                float camera=area.height*.54f+ReferenceWorldPoint(focus).y*area.width;
                // Cover the complete viewport even at either clamped endpoint. Only
                // quiet terrain repeats, never the big gear/log or painted route.
                int first=Mathf.FloorToInt((camera-area.height)/height)-1;
                int last=Mathf.CeilToInt(camera/height)+1;
                for(int section=first;section<=last;section++)
                {
                    Rect tile=new Rect(0,camera-(section+1)*height,area.width,height+1);
                    if(!area.Overlaps(tile))continue;
                    bool flip=(section&1)!=0;
                    GUI.DrawTextureWithTexCoords(tile,map,new Rect(flip?1:0,0,flip?-1:1,1));
                }
                // Two landmarks over the whole ten-level map, placed on the opposite
                // bank from the route. Reuse the original painting, not new models.
                Texture2D reference=Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/map_reference_exact");
                DrawReferenceLandmark(area,focus,reference,new Rect(0,1180,315,305),.16f,1.65f,.27f);
                DrawReferenceLandmark(area,focus,reference,new Rect(640,20,384,225),.81f,4.25f,.30f);
                DrawReferenceLandmark(area,focus,reference,new Rect(30,85,155,135),.81f,.25f,.14f);
                DrawReferenceLandmark(area,focus,reference,new Rect(63,359,100,62),.15f,3f,.12f);
                DrawReferenceLandmark(area,focus,reference,new Rect(63,359,100,62),.17f,6.2f,.15f);
                DrawReferenceLandmark(area,focus,reference,new Rect(869,1100,155,116),.82f,7.9f,.15f);
                // A small rock finishing landmark, beyond level 10 but no further path.
                DrawReferenceLandmark(area,focus,reference,new Rect(30,85,155,135),.72f,9.38f,.13f);
                GUI.color=savedColor;
                GUI.enabled=savedEnabled;
                return;
            }
            // Sparse paper terrain, shared by home and full map.
            Color previous=GUI.color;
            GUI.color=new Color(.985f,.976f,.945f);
            GUI.DrawTexture(area,Texture2D.whiteTexture); GUI.color=previous;
            // Contours have absolute map coordinates: scrolling does not regenerate them.
            for(int marker=0;marker<LevelRepository.PrototypeLevelIds.Length;marker++)
            {
                float z=marker*2.6f+0.8f;
                float x=marker%2==0 ? -1.02f : 1.02f;
                DrawGroundPatch(area,new Vector2(x,z),focus);
                // Small open contour strokes, not repeated concentric target rings.
                for(int segment=0;segment<14;segment++)
                {
                    float a=segment*.12f+.2f,b=(segment+1)*.12f+.2f;
                    Vector2 p=ProjectMapGround(area,new Vector2(x+Mathf.Cos(a)*.6f,z+Mathf.Sin(a)*.34f),focus);
                    Vector2 q=ProjectMapGround(area,new Vector2(x+Mathf.Cos(b)*.6f,z+Mathf.Sin(b)*.34f),focus);
                    MapLine(p,q,Mathf.Max(1f,area.width*.0017f),new Color(.70f,.63f,.49f,.3f));
                }
            }
            // Painted landmarks are anchored to world coordinates, not screen space.
            foreach(int milestone in new[]{0,2,5,8})
            {
                bool log=milestone==0||milestone==5;
                Vector2 point=ProjectMapGround(area,new Vector2(log?-1.1f:1.1f,milestone*2.6f+(log?-1f:.6f)),focus);
                float width=area.width*.25f;
                Rect landmark=new(point.x-width/2,point.y-width*.35f,width,width*.7f);
                if(area.Overlaps(landmark)) DrawPieceIcon(landmark,log?"map_log":"map_gear");
            }
        }

        private void DrawReferenceLandmark(Rect area,float focus,Texture2D source,Rect crop,float x,float level,float width)
        {
            if(source==null)return;
            float y=IllustratedMapPoint(area,level,focus).y;
            float w=area.width*width,h=w*crop.height/crop.width*.88f;
            Rect target=new Rect(area.width*x-w/2,y-h/2,w,h);
            if(!area.Overlaps(target))return;
            crop=new Rect(crop.x*source.width/1024f,crop.y*source.height/1536f,
                crop.width*source.width/1024f,crop.height*source.height/1536f);
            DrawSource(target,source,crop);
        }
    }
}
