using UnityEngine;
using UnityEngine.UI;

namespace WorkshopMatch
{
    // Native overlay UI avoids rotating inside nested IMGUI clip matrices. Only rotor
    // transforms rotate; their pixel-sized rectangles and pivots never change with angle.
    public sealed class OpeningScreenView
    {
        public GameObject Root { get; }
        public Canvas Canvas { get; }
        private readonly RectTransform viewport, wall;
        private readonly RawImage background, logo, cluster, topRail, bottomRail;
        private readonly RawImage[] gears = new RawImage[3];
        private bool hasLayout;
        private Rect previousPortrait, previousSafe;

        public OpeningScreenView(Transform parent, Texture backgroundArt, Texture logoArt, Texture clusterArt, Texture gearArt, Texture woodArt = null)
        {
            Root = new GameObject("Matchworks Opening Canvas", typeof(RectTransform), typeof(Canvas));
            if (parent != null) Root.transform.SetParent(parent, false);
            Canvas = Root.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 32767;
            Canvas.pixelPerfect = true;
            var outside = Image("Outside portrait", Root.transform, Texture2D.whiteTexture);
            outside.color = new Color(.035f, .12f, .15f, 1);
            Stretch(outside.rectTransform);
            viewport = Panel("Portrait", Root.transform);
            viewport.gameObject.AddComponent<RectMask2D>();
            background = Image("Turquoise wall", viewport, backgroundArt);
            background.uvRect = new Rect(0, .04f, 1, .92f);
            Stretch(background.rectTransform);
            wall = Panel("Rotor clipping at wood frame", viewport);
            wall.gameObject.AddComponent<RectMask2D>();
            for (int i = 0; i < gears.Length; i++)
            {
                gears[i] = Image("Independent rotor " + i, wall, gearArt);
                gears[i].color = new Color(1, 1, 1, .62f);
                gears[i].rectTransform.pivot = new Vector2(627.5f / 1254f, 1f - 623f / 1254f);
            }
            // Sample the clean horizontal plank from the actual main-menu frame.
            // Reuse its grain rather than tinting the unrelated orange opening artwork.
            Rect woodUv = new Rect(190f / 941f, 1f - 110f / 1672f, 560f / 941f, 38f / 1672f);
            topRail = Image("Top wood", viewport, woodArt != null ? woodArt : backgroundArt);
            topRail.uvRect = woodArt != null ? woodUv : new Rect(0, .962f, 1, .038f);
            bottomRail = Image("Bottom wood", viewport, woodArt != null ? woodArt : backgroundArt);
            bottomRail.uvRect = woodArt != null ? woodUv : new Rect(0, 0, 1, .038f);
            logo = Image("Matchworks HD", viewport, logoArt);
            cluster = Image("Tools and candies HD", viewport, clusterArt);
        }

        public void Layout(Rect screenPortrait, Rect safe)
        {
            if (hasLayout && previousPortrait == screenPortrait && previousSafe == safe) return;
            hasLayout = true;
            previousPortrait = screenPortrait; previousSafe = safe;
            var layout = new OpeningScreenLayout(screenPortrait, safe);
            Place(viewport, screenPortrait);
            Vector2 origin = screenPortrait.position;
            Place(wall, Relative(layout.Wall, origin));
            Place(topRail.rectTransform, Relative(layout.TopRail, origin));
            Place(bottomRail.rectTransform, Relative(layout.BottomRail, origin));
            Place(logo.rectTransform, Relative(OpeningScreenLayout.Fit(layout.Logo,
                (float)logo.texture.width / logo.texture.height), origin));
            Place(cluster.rectTransform, Relative(OpeningScreenLayout.Fit(layout.Cluster,
                (float)cluster.texture.width / cluster.texture.height), origin));
            PlaceGear(gears[0].rectTransform, layout.LeftGearCenter - layout.Wall.position, layout.LeftGearSize);
            PlaceGear(gears[1].rectTransform, layout.RightGearCenter - layout.Wall.position, layout.LeftGearSize);
            PlaceGear(gears[2].rectTransform, layout.BottomGearCenter - layout.Wall.position, layout.BottomGearSize);
        }

        public void Rotate(float elapsed)
        {
            gears[0].rectTransform.localRotation = Quaternion.Euler(0, 0, -elapsed * 6);
            gears[1].rectTransform.localRotation = Quaternion.Euler(0, 0, elapsed * 6 - 15);
            gears[2].rectTransform.localRotation = Quaternion.Euler(0, 0, -elapsed * 6 - 30);
        }

        public void SetVisible(bool visible) => Root.SetActive(visible);

        private static void PlaceGear(RectTransform rect, Vector2 center, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            // Correct the small source silhouette's width/height difference once; the
            // complete circle then rotates as one rigid rectangle without changing shape.
            rect.sizeDelta = new Vector2(size, size * (1151f / 1124f));
            rect.anchoredPosition = new Vector2(center.x, -center.y);
        }

        private static Rect Relative(Rect rect, Vector2 origin)
        { rect.position -= origin; return rect; }

        private static RectTransform Panel(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }

        private static RawImage Image(string name, Transform parent, Texture texture)
        {
            var rect = Panel(name, parent);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Rect area)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(area.x, -area.y);
            rect.sizeDelta = area.size;
        }
    }
}
