using UnityEngine;
namespace WorkshopMatch {
 public sealed partial class BoardBlockoutController {
  private bool languageOpen;
  private int languagePending=-1;
  private void DrawLanguagePopup(Rect portrait,GUIStyle title) {
   bool enabled=GUI.enabled;GUI.enabled=true;
   Rect panel=UtilityPopup(portrait,portrait.width*.76f,true);
   var style=new GUIStyle(title){fontSize=Mathf.RoundToInt(panel.width*.05f),wordWrap=true,
    normal={textColor=new Color(.31f,.16f,.065f)}};
   bool confirm=languagePending>=0;
   DrawUiLabel(LayoutRect(panel,.12f,.16f,.76f,.24f),WrapTutorialText(confirm?"Change language?":"Choose your language"),style);
   if(confirm) {
    DrawUiLabel(LayoutRect(panel,.12f,.40f,.76f,.10f),languagePending==1?"ENGLISH":"TURKCE",style);
    if(DrawTextureButton(LayoutRect(panel,.13f,.58f,.34f,.15f),new GUIContent("EVET"),uiCatalog.ActionButton,null,null,style)) {
     GameLanguage.Select(languagePending==1);languageOpen=false;languagePending=-1;
    }
    if(DrawTextureButton(LayoutRect(panel,.53f,.58f,.34f,.15f),new GUIContent("HAYIR"),uiCatalog.ActionButton,null,null,style))languagePending=-1;
   } else {
    if(DrawTextureButton(LayoutRect(panel,.17f,.46f,.66f,.15f),new GUIContent("TURKCE"),uiCatalog.ActionButton,null,null,style))languagePending=0;
    if(DrawTextureButton(LayoutRect(panel,.17f,.66f,.66f,.15f),new GUIContent("ENGLISH"),uiCatalog.ActionButton,null,null,style))languagePending=1;
   }
   if(DrawTextureButton(PopupCloseRect(panel),GUIContent.none,uiCatalog.CloseButton,null,null,style)){languageOpen=false;languagePending=-1;}
   GUI.enabled=enabled;
  }
 }
}
