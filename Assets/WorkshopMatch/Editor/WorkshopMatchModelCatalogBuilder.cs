using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    [InitializeOnLoad]
    public static class WorkshopMatchModelCatalogBuilder
    {
        private const string CatalogPath = "Assets/WorkshopMatch/Resources/WorkshopMatch/WorkshopMatchModelCatalog.asset";
        private const string ModelRoot = "Assets/WorkshopMatch/Models/ToonV4/";

        static WorkshopMatchModelCatalogBuilder()
        {
            EditorApplication.delayCall += EnsureCatalog;
        }

        [MenuItem("Window/Workshop Match/Rebuild Model Catalog")]
        public static void EnsureCatalog()
        {
            WorkshopMatchModelCatalog catalog = AssetDatabase.LoadAssetAtPath<WorkshopMatchModelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorkshopMatchModelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Candies = new[]
            {
                Load("Candy_01"), Load("Candy_02"), Load("Candy_04"), Load("Candy_03"), Load("Candy_05")
            };
            catalog.Bolt = Load("Bolt_Nut_M6");
            catalog.BoltScrew = Load("Bolt_Screw_M6");
            catalog.Motor = Load("Motor_Core");
            catalog.FrozenGrease = Load("Grease_Frozen");
            catalog.BoardCell = Load("Board_Cell");
            catalog.OilJoinedSurface = Load("Oil_Joined_Surface");
            catalog.Ray = Load("Power_Rail");
            catalog.Solvent = Load("Power_Solvent");
            catalog.Magnet = Load("Power_Magnet");
            catalog.FactoryTop = Load("Factory_Top");
            catalog.Hammer = Load("Hammer");
            catalog.ToolIcons = new[]
            {
                LoadIcon("Icon_Rail"), LoadIcon("Icon_Solvent"),
                LoadIcon("Icon_Magnet"), LoadIcon("Icon_Hammer")
            };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelRoot}{name}.fbx");
        }

        private static Texture2D LoadIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{ModelRoot}Icons/{name}.png");
        }
    }
}
