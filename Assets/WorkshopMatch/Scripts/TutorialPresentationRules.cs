using System;
namespace WorkshopMatch
{
    internal static class TutorialPresentationRules
    {
        // Ratios are in portrait-width units. The frame lip is at most .054 wide.
        internal const float TextPitch=.058f, TopPadding=.10f, BottomPadding=.095f;
        internal const float PromptHeight=.040f, PromptGap=.027f;
        internal static float PanelHeight(int lines,bool prompt)=>TopPadding+BottomPadding+
            Math.Max(1,lines)*TextPitch+(prompt?PromptGap+PromptHeight:0);
        internal static bool IsMeaningfulMapSwipe(float verticalPixels,float portraitHeight,float heldSeconds)
            =>Math.Abs(verticalPixels)>=portraitHeight*.18f&&heldSeconds>=.30f;
        internal const float IntroDelay=.85f, IntroFade=.50f;
    }
}
