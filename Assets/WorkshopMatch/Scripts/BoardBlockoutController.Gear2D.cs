using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private Texture2D gearMachineAtlas;
        private Texture2D gearVineSupport;
        private float gear2DAngle;
        private bool UseGear2DMachine()
        {
            if(activeLevel == null) return false;
            gearMachineAtlas ??= Resources.Load<Texture2D>("WorkshopMatch/gear_machine_staggered_v2");
            return gearMachineAtlas != null;
        }

        private void DrawGear2DMachine(Rect portrait)
        {
            if(!UseGear2DMachine()) return;
            Rect area = GameplayLayout(portrait).Gears;
            Color previous=GUI.color;
            try {
            // Oversized, cropped gears read as a moving wallpaper rather than a separate machine.
            float width = portrait.width*Mathf.Clamp(area.height/portrait.width*3.4f,1.10f,1.60f);
            float x = area.center.x-width*.5f;
            float cy = area.center.y+area.height*.18f;
            // Approved five-gear design: static smooth oak frame and five isolated
            // rotor regions. UV coordinates use the 2100x749 transparent atlas.
            Rect support = new Rect(x,cy-width*.11f,width,width*345f/1640f);
            if(gameplayThemeMaterial!=null)
            {
                // Let both support ends extend past the display instead of ending like a floating plaque.
                support.width=width*1.10f;
                support.x=portrait.center.x-support.width*.5f;
                gameplayThemeMaterial.SetTexture("_GearAtlas",gearMachineAtlas);
                gearVineSupport ??= Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/gear_support_vines_source");
                gameplayThemeMaterial.SetTexture("_GearSupportArt",gearVineSupport!=null?gearVineSupport:gearMachineAtlas);
                Vector4 supportUv=GearAtlasUv(new Rect(225,8,1650,355));
                if(gearVineSupport!=null)
                {
                    Rect pixels=TexturePixels(gearVineSupport);
                    support.height=support.width*pixels.height/pixels.width;
                    support.y=cy-support.height*.5f;
                    supportUv=new Vector4(pixels.x/gearVineSupport.width,1f-pixels.yMax/gearVineSupport.height,
                        pixels.width/gearVineSupport.width,pixels.height/gearVineSupport.height);
                }
                gameplayThemeMaterial.SetVector("_GearSupportRect",new Vector4(
                    (support.x-portrait.x)/portrait.width,1f-(support.yMax-portrait.y)/portrait.height,
                    support.width/portrait.width,support.height/portrait.height));
                gameplayThemeMaterial.SetVector("_GearSupportUv",supportUv);
            }
            float angle = Time.unscaledTime*6f + gear2DAngle;
            int[] teeth={8,12,10,12,8};
            // Tooth phases measured from each atlas rotor's alpha silhouette.
            float[] toothPhase={-.137f,14.184f,17.185f,14.744f,-.276f};
            Rect[] sources={new Rect(80,408,310,310),new Rect(430,363,400,400),new Rect(875,387,350,350),new Rect(1270,363,400,400),new Rect(1712,408,310,310)};
            float[] cropScale={310f/290f,400f/370f,350f/322f,400f/370f,310f/290f};
            float[] contact={35f,-22f,20f,-35f};
            Vector2 center=new Vector2(x+width*.13f,cy-width*.07f);
            for(int i=0;i<5;i++)
            {
                if(i>0)
                {
                    float phi=contact[i-1];
                    float distance=(teeth[i-1]+teeth[i])*width*.01045f;
                    center+=new Vector2(Mathf.Cos(phi*Mathf.Deg2Rad),Mathf.Sin(phi*Mathf.Deg2Rad))*distance;
                    angle=((teeth[i-1]+teeth[i])*phi+teeth[i]*180f-teeth[i-1]*(angle+toothPhase[i-1])-teeth[i]*toothPhase[i]-180f)/teeth[i];
                }
                float diameter=(teeth[i]*.02f+.025f)*width*cropScale[i];
                // All levels sample the same animated artwork in their own wallpaper palette.
                gameplayThemeMaterial?.SetVector("_Gear"+(char)('A'+i),new Vector4(
                    (center.x-portrait.x)/portrait.width,1f-(center.y-portrait.y)/portrait.height,
                    diameter*.5f/portrait.height,-angle*Mathf.Deg2Rad));
                gameplayThemeMaterial?.SetVector("_GearUv"+(char)('A'+i),GearAtlasUv(sources[i]));
            }
            } finally { GUI.color=previous; }
        }

        private Vector4 GearAtlasUv(Rect source)=>new Vector4(source.x/gearMachineAtlas.width,
            1f-source.yMax/gearMachineAtlas.height,source.width/gearMachineAtlas.width,source.height/gearMachineAtlas.height);

        private void DrawGearRotor(Rect source,float x,float y,float diameter,float angle)
        {
            Rect rect = new Rect(x-diameter*.5f,y-diameter*.5f,diameter,diameter);
            Matrix4x4 original = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle,rect.center);
            DrawSource(rect,gearMachineAtlas,source);
            GUI.matrix = original;
        }
    }
}
