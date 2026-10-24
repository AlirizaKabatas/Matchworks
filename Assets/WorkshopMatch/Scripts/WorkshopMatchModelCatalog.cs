using UnityEngine;

namespace WorkshopMatch
{
    public sealed class WorkshopMatchModelCatalog : ScriptableObject
    {
        public GameObject[] Candies = new GameObject[5];
        public GameObject Bolt;
        public GameObject BoltScrew;
        public GameObject Motor;
        public GameObject FrozenGrease;
        public GameObject BoardCell;
        public GameObject OilJoinedSurface;
        public GameObject Ray;
        public GameObject Solvent;
        public GameObject Magnet;
        public GameObject FactoryTop;
        public GameObject Hammer;
        public Texture2D[] ToolIcons = new Texture2D[4];
    }
}
