using UnityEditor;
using UnityEngine;
namespace WorkshopMatch.Editor {
 public sealed class GearAtlasImportSettings : AssetPostprocessor {
  void OnPreprocessTexture() {
   if(!assetPath.EndsWith("gear_machine_staggered_v2.png"))return;
   var importer=(TextureImporter)assetImporter;
   importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
   importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
   importer.textureCompression=TextureImporterCompression.Uncompressed;
   importer.maxTextureSize=4096;
  }
 }
}
