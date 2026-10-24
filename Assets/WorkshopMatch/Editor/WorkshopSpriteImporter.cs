using UnityEditor;
using UnityEngine;
namespace WorkshopMatch.Editor {
 public sealed class WorkshopSpriteImporter : AssetPostprocessor {
  [InitializeOnLoadMethod]
  static void RefreshExistingSprites() {
   EditorApplication.delayCall += () => {
    foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/WorkshopMatch/Resources/WorkshopMatch/Sprites2D"})) {
     var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
     if(importer!=null && (!importer.isReadable || (importer.mipmapEnabled && !importer.assetPath.EndsWith("/frozen.png")) || !importer.alphaIsTransparency || importer.textureCompression!=TextureImporterCompression.Uncompressed || (importer.assetPath.EndsWith("/frozen.png") && (importer.filterMode!=FilterMode.Bilinear || !importer.mipmapEnabled || importer.maxTextureSize!=2048)) || (importer.assetPath.Contains("map_reference_exact") && importer.maxTextureSize!=2048)))
      importer.SaveAndReimport();
    }
   };
  }
  void OnPreprocessTexture() {
   if(!assetPath.Contains("/Sprites2D/")) return;
   var importer=(TextureImporter)assetImporter;
   importer.textureType=TextureImporterType.Default;
   importer.alphaIsTransparency=true;importer.isReadable=true;
   bool frozen=assetPath.EndsWith("/frozen.png");
   importer.mipmapEnabled=frozen;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=frozen || assetPath.Contains("map_reference_exact")?2048:1024;
   importer.textureCompression=TextureImporterCompression.Uncompressed;
   importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
  }
 }
}
