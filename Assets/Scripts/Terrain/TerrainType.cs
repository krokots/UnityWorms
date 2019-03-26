using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone {
    [CreateAssetMenu(fileName = "new_terrain_type", menuName = "WormsClone/TerrainType", order = 1)]
    public class TerrainType : ScriptableObject {

        public Texture2D GroundTexture;
		public Sprite Background1;
		public Sprite Background2;
        public List<ColorGradient> TopGradients;
		public List<ColorGradient> BottomGradients;


        public ColorGradient GetTopGradient()
        {
            int max = TopGradients.Count;
            return TopGradients[GRandom.GetIntRaw(0, max)];
        }

		public ColorGradient GetBottomGradient()
		{
			int max = BottomGradients.Count;
			return BottomGradients[GRandom.GetIntRaw(0, max)];
		}

    }

    [System.Serializable]
    public class ColorList
    {
        public List<Color> Colors;
    }
    [System.Serializable]
    public class ColorGradient
    {
        public Color[] Colors;
    }

}