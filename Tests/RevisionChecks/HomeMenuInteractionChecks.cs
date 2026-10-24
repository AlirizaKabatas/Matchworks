using WorkshopMatch;
using UnityEngine;

partial class Program
{
    static void RunHomeMenuInteractionChecks()
    {
        foreach (var viewport in new[] { new Rect(0,0,1080,1920), new Rect(200,0,750,1334),
            new Rect(0,0,1170,2532), new Rect(0,0,1536,2048) })
        {
            foreach (float safeTop in new[] { 0f, viewport.width*.28f })
            {
                Rect safe=new(viewport.x,safeTop,viewport.width,viewport.height-safeTop);
                var safeGame=WorkshopMatchScreenLayout.Calculate(viewport,
                    new Rect(safe.x,safe.y,safe.width,safe.height-viewport.width*.09f),8,8,.92f);
                float wellSize=WorkshopMatchScreenLayout.ToolbarWellSize(safeGame.Toolbar);
                float progressBottom=safeGame.Toolbar.y+safeGame.Toolbar.height*.02f+wellSize*1.405f;
                Check(wellSize>0 && progressBottom<=safeGame.Toolbar.yMax-viewport.width*.024f,
                    "toolbar stock and progress boxes fit with bottom safe inset: "+viewport+" safeTop="+safeTop);
                Rect button=HomeMenuLayout.MenuButton(viewport,safe);
                Check(button.y>=viewport.y+viewport.width*.14f && button.y>safe.y &&
                    button.xMax<=viewport.xMax-viewport.width*.12f,
                    "home menu clears upper wood cap, notch and right rail: "+viewport+" safeTop="+safeTop);
            }
            Rect menuSafe=new(viewport.x,viewport.y,viewport.width,viewport.height);
            Rect route=HomeMenuLayout.Route(viewport,menuSafe,viewport.yMax-viewport.width*.65f);
            Rect toggle=HomeMenuLayout.MenuButton(viewport,menuSafe);
            for(int selected=0;selected<20;selected++)
                for(int level=System.Math.Max(0,selected-1);level<=System.Math.Min(19,selected+1);level++)
                {
                    Vector2 center=HomeMenuLayout.RoutePoint(viewport,route,level,selected)+viewport.position;
                    float width=viewport.width*(selected==level?.245f:.18f);
                    Rect node=new(center.x-width*.5f,center.y-width*125f/166f*.44f,width,width*125f/166f*.88f);
                    Check(route.Contains(node.min)&&route.Contains(node.max)&&!toggle.Overlaps(node),
                        "selected and adjacent home nodes stay visible and clickable: "+selected+"/"+level+" "+viewport);
                }
            var game=WorkshopMatchScreenLayout.Calculate(viewport,menuSafe,8,8,.92f);
            Check(System.Math.Abs(game.Frame.yMax-game.Toolbar.y)<.01f,
                "board frame snaps to toolbar without lower wallpaper gap "+viewport);
            Check(System.Math.Abs(System.Math.Min(game.Aperture.width/8,game.Aperture.height/8)*8-game.Aperture.height)<.01f,
                "square-cell upper spawn edge meets frame aperture "+viewport);
            Rect area=new(viewport.x+viewport.width*.1f,viewport.y+viewport.width*.4f,
                viewport.width*.8f,viewport.height*.45f);
            float slop=viewport.width*.02f;
            var tap=new MenuMapTapGesture();
            Vector2 inside=area.center, outside=new(area.x-30,area.center.y);
            Check(!tap.Sample(area,inside,false,false,true,slop),"map lesson ignores orphan mouse release "+viewport);
            Check(!tap.Sample(area,inside,true,true,false,slop) &&
                tap.Sample(area,inside+new Vector2(1,1),false,false,true,slop) &&
                !tap.Sample(area,inside,false,false,true,slop),
                "map lesson accepts one screen-space click without an IMGUI event "+viewport);
            tap.Sample(area,inside,true,true,false,slop);
            tap.Sample(area,inside+new Vector2(slop*2,0),false,true,false,slop);
            Check(!tap.Sample(area,inside,false,false,true,slop),"drag back to origin cannot count as map tap "+viewport);
            tap.Sample(area,outside,true,true,false,slop);
            Check(!tap.Sample(area,inside,false,false,true,slop),"outside press cannot open map lesson "+viewport);
            tap.Sample(area,inside,true,true,false,slop);
            tap.Reset();
            Check(!tap.Sample(area,inside,false,false,true,slop),"lesson transition clears pending map tap "+viewport);
        }
    }
}
