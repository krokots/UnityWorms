using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace WormsClone
{
    //[ExecuteInEditMode]
    public class MapGenerator : MonoBehaviour
    {
        public bool Generate;
		[Header("Map generation values")]
        public int Width;
        public int Height;
        private int width;
        private int height;
        public int Detail;
        public int Octaves;
        public float Persistance;
        [Range(0.0f, 1.0f)]
        public float Flatness;
        public int WaterHeight;
		public int DeleteMassThreshold;
        public int Seed;
        public TerrainType TerrainType;

		[Header("Containters")]
		public GameObject TerrainObject;
		public GameObject BackgroundObject;
		public GameObject WaterObject;
		[Header("Prefabs")]
		public GameObject WaterPrefab;
		[Header("Pointers")]
		public CameraManager CameraManager;
		public GameManager GameManager;

		public int Layers { get; private set; }
		public Node[,] Grid { get; private set; }
		public List<Node> TopNodes { get; private set; }
		public List<Node> BottomNodes { get; private set; }
		public List<NodeMass> Masses { get; private set; }

		private Sprite mapSprite;

		#region Main and init functions

		private void Start()
        {
			GameManager.SetMapGrid(GenerateMap(), Width, Height);
			GameManager.SetSprite(mapSprite);
			GameManager.InitGame();
		}

        private void Update()
        {
    //        if (Generate)
    //        {
    //            Generate = false;
				//GenerateMap(false);
    //        }
        }

		Node[,] GenerateMap(bool spawnCameraLimiter = true)
		{
			Init();
			SetDefaults();
			Grid = CreateMap();
			mapSprite = SetSprite(Grid);
			SetBackground();
			SetWater(-0.080f);
			if(spawnCameraLimiter)
				SpawnTopRightObject();

			SetCameraStartingPos();
			SetBoxCollider(width, height);
			return Grid;
		}

        void Init()
        {
            TopNodes = new List<Node>();
            BottomNodes = new List<Node>();
			Masses = new List<NodeMass>();
            SetSeed();
        }

        void SetDefaults()
        {
            height = Height;
            width = Width;
        }

        public void SetSeed(bool r = false)
        {
            if (Seed == 0 || r)
                Seed = (int)System.DateTime.Now.Ticks;
            GRandom.Rnd = new System.Random(Seed);
        }

        #endregion

        #region Creating map grid

        Node[,] CreateMap()
        {
            Node[,] grid = new Node[width, height];
            Layers = 7;
            NoiseData[] data = GetNoiseLayers(Layers, 100, GRandom.GetFloat(0.4f, 0.6f));
            SetGrid(grid, data);
            //grid = TrimGrid(grid);
            SetNodeTypes(grid);
            return grid;

        }

        Node[,] SetGrid(Node[,] oldGrid, NoiseData[] noiseData)
        {
            //Profiler.BeginSample("SetGrid");
            for (int x = 0; x < width; x++)
            {
                bool lastSolid = true;
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    bool added = false;
                    for (int i = Layers - 1; i > -1; i--)
                    {
                        if (y < noiseData[i].Data[x])
                        {
                            if (noiseData[i].Solid)
                            {
                                oldGrid[x, y] = new Node(true, pos);
                                if (y == 0)
                                    lastSolid = true;
                                else if(!lastSolid)
                                    BottomNodes.Add(oldGrid[x, y]);
                                lastSolid = true;
                                added = true;
                                break;
                            }
                            else
                            {
                                oldGrid[x, y] = new Node(false, pos);
                                if (y == 0)
                                    lastSolid = false;
                                else if(lastSolid && y - 1 >= 0)
                                {
                                    Node top = oldGrid[x, y - 1];
                                    if (top && top.Solid)
                                        TopNodes.Add(top);
                                }
                                lastSolid = false;
                                added = true;
                                break;
                            }
                        }
                    }
                    if (oldGrid[x, y] == null && !added)
                    {
                        oldGrid[x, y] = new Node(false, pos);
                        if (lastSolid && y - 1 >= 0)
                        {
                            Node top = oldGrid[x, y - 1];
                            if (top && top.Solid)
                                TopNodes.Add(top);
                        }
                        lastSolid = false;
                        added = true;
                    }
                }
            }
            // Profiler.EndSample();
            return oldGrid;
        }

		void SpawnTopRightObject(int additionalHeight = 300)
		{
			Destroy(GameObject.FindGameObjectWithTag("CameraLimiter"));
			GameObject obj = new GameObject("CameraLimiter");
			obj.transform.SetPositionAndRotation(new Vector3(Width / 100, (Height + additionalHeight)/ 100, 0), Quaternion.identity);
			obj.transform.tag = "CameraLimiter";
			CameraManager.TopRight = obj.transform.position;
			CameraManager.BottomLeft = transform.position - new Vector3(0, 0.25f, 0);
			CameraManager.initialized = true;
		}

		void SetCameraStartingPos()
		{
			Vector3 oldPos = Camera.main.transform.position;
			Vector3 newPos = new Vector3(Width / 200, Height / 200, -2.0f);
			CameraManager.MoveCamera(oldPos - newPos);
		}

        //Node[,] TrimGrid(Node[,] oldGrid, int amount = 0)
        //{
        //    if(amount == 0)
        //    {
        //        amount = (int)(height * (1 - Flatness) + 1);
        //    }
        //    int newHeight = height - amount + 10;
        //    Node[,] newGrid = new Node[width, newHeight];

        //    for (int x = 0; x < width; x++)
        //    {
        //        for (int y = 0; y < height; y++)
        //        {
        //            if (y < newHeight)
        //                newGrid[x, y] = oldGrid[x, y];
        //        }
        //    }
        //    height = newHeight;
        //    return newGrid;
        //}
        #endregion

        #region Node regions

        Node[,] SetNodeTypes(Node[,] grid)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Node node = grid[x, y];
                    if(node.Type == NodeType.Null)
                    {
                        //Debug.LogFormat("Node to fill {0}", node);
                        Queue<Node> q = new Queue<Node>();
                        q.Enqueue(node);
						//Node[,] mass = FillNodeType(q, grid, node);
						NodeMass mass = FillNodeType(q, grid, node);
						Masses.Add(mass);
                        //if (GetMassType(mass) == NodeType.Floating)
                        //    if (EraseFloatingAndCaves) SetNodeType(mass, grid, NodeType.Air);
                        //    else SetNodeType(mass, grid, NodeType.Land);
                        //else if (GetMassType(mass) == NodeType.Cave)
                        //    if (EraseFloatingAndCaves) SetNodeType(mass, grid, NodeType.Land);
                        //    else SetNodeType(mass, grid, NodeType.Air);

                    }
                }
            }
			List<NodeMass> toDelete = new List<NodeMass>();
			foreach (var mass in Masses)
			{
				//Debug.Log(mass);
				if(mass.Solid == true && mass.Type == NodeType.Floating)
				{
					if (mass.Size < DeleteMassThreshold)
					{
						foreach (var node in mass.Mass)
						{
							node.Solid = false;
							node.Type = NodeType.Air;
						}
						toDelete.Add(mass);
					}
					else
					{
						foreach (var node in mass.Mass)
						{
							node.Type = NodeType.Land;
						}
						mass.Type = NodeType.Land;
					}
				}
				//if(mass.Solid == false && mass.Type == NodeType.Cave)
				//{
				//	foreach (var node in mass.Mass)
				//	{
				//		node.Type = NodeType.Air;
				//	}
				//	mass.Type = NodeType.Air;
				//}
			}
			foreach (var mass in toDelete)
			{
				Masses.Remove(mass);
			}
            return null;
        }

        NodeType GetMassType(Node[,] mass)
        {
            foreach (var node in mass)
            {
                if (node)
                    return node.Type;
            }
            return NodeType.Null;
        }

		NodeType GetMassType(NodeMass mass)
		{
			return mass.Type;
		}

        void SetNodeType(Node[,] mass, Node[,] grid, NodeType newType)
        {
            foreach (var node in mass)
            {
                if(node)
                {
                    grid[node.Position.x, node.Position.y].Type = newType;
                    if (newType == NodeType.Air)
                        node.Solid = false;
                    else if (newType == NodeType.Land)
                        node.Solid = true;

                }
            }
        }

		void SetNodeType(NodeMass mass, Node[,] grid, NodeType newType)
		{
			foreach (var node in mass.Mass)
			{
				node.Type = newType;
				if (newType == NodeType.Air)
					node.Solid = false;
				else if (newType == NodeType.Land)
					node.Solid = true;
			}
		}

        NodeMass FillNodeType(Queue<Node> q, Node[,] grid, Node first)
        {
            NodeType massType = NodeType.Null;
            if (first.Solid)
                massType = NodeType.Floating;
            else if (!first.Solid && first.Position.y == 0)
                massType = NodeType.WaterCave;
            else if (!first.Solid)
                massType = NodeType.Cave;

			List<Node> massList = new List<Node>();
			first.InRegion = true;
            while(q.Count > 0)
            {
                Node C = q.Dequeue();
                Node N = GetNeighbourNode(C, grid, Vector2Int.up);
                Node S = GetNeighbourNode(C, grid, Vector2Int.down);
                Node E = GetNeighbourNode(C, grid, Vector2Int.right);
                Node W = GetNeighbourNode(C, grid, Vector2Int.left);
				if(N && N.Solid == C.Solid && !N.InRegion)
                {
                    N.Type = C.Type;
                    q.Enqueue(N);
					N.InRegion = true;
					massList.Add(N);
                }
				if(S && S.Solid == C.Solid && !S.InRegion)
                {
                    S.Type = C.Type;
                    q.Enqueue(S);
					S.InRegion = true;
					massList.Add(S);
                }
				if(E && E.Solid == C.Solid && !E.InRegion)
                {
                    E.Type = C.Type;
                    q.Enqueue(E);
					E.InRegion = true;
					massList.Add(E);
                }
				if(W && W.Solid == C.Solid && !W.InRegion)
                {
                    W.Type = C.Type;
                    q.Enqueue(W);
					W.InRegion = true;
					massList.Add(W);
                }
                if(!N || !S || !E || !W)
                {
                    if (C.Solid && IsTouchingGround(C))
                        massType = NodeType.Land;
                    else if (!C.Solid && IsTouchingAir(C))
                        massType = NodeType.Air;
                }
            }

            foreach (var node in massList)
            {
                if(node != null)
                    node.Type = massType;
            }
			NodeMass nodeMass = new NodeMass(massList, first.Solid, massType, massList.Count);
			return nodeMass;
        }

        bool IsOnBorder(Node node)
        {
            if (node.Position.x == 0 || node.Position.x == width - 1 || node.Position.y == 0 || node.Position.y == height - 1)
                return true;
            return false;
        }

        bool IsTouchingAir(Node node)
        {
            if (node.Position.x == 0 || node.Position.x == width - 1 || node.Position.y == height - 1)
                return true;
            return false;
        }

        bool IsTouchingGround(Node node)
        {
            if (node.Position.y == 0 || node.Position.x == 0 || node.Position.x == width - 1)
                return true;
            return false;
        }

        bool IsUnderAir(Node node, Node[,] grid)
        {
            int x = node.Position.x;
            for (int y = node.Position.y; y < height; y++)
            {
                Node upper = grid[x, y];
                if (upper.Solid)
                    return false;
            }
            return true;
        }

        Node GetNeighbourNode(Node node, Node[,] grid, Vector2Int dir)
        {
            if(dir.x == 0 && dir.y == 1 && node.Position.y +1 < height)    //N
                return grid[node.Position.x, node.Position.y + 1];
            if(dir.x == 0 && dir.y == -1 && node.Position.y - 1 >= 0)       //S
                return grid[node.Position.x, node.Position.y - 1];
            if(dir.x == -1 && dir.y == 0 && node.Position.x - 1 >= 0)      //W
                return grid[node.Position.x - 1, node.Position.y];
            if(dir.x == 1 && dir.y == 0 && node.Position.x + 1 < width) //E
                return grid[node.Position.x + 1, node.Position.y];
            return null;
        }

        #endregion

        #region Texture creation

        Sprite SetSprite(Node[,] grid)
        {
            SpriteRenderer sr = TerrainObject.GetComponent<SpriteRenderer>();
            if (sr)
            {
                Texture2D tex = CreateTexture(grid);
				tex.name = "Map texture";
                Rect rect = new Rect(0, 0, width, height);
                Sprite spr = Sprite.Create(tex, rect, Vector2.zero, 100.0f, 0, SpriteMeshType.FullRect);
                sr.sprite = spr;
				sr.sortingOrder = 6;
				return spr;
            }
			return null;
        }

        Texture2D CreateTexture(Node[,] grid)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(SetPixels(grid));
			AddLowerGrowth(grid, tex);
			AddUpperGrowth(grid, tex);
            tex.Apply();
            return tex;
        }

		void SetBoxCollider(int w, int h)
		{
			BoxCollider box = TerrainObject.AddComponent<BoxCollider>();
			box.size = new Vector3(w / 100.0f, h / 100.0f, 0);
		}

        Color[] SetPixels(Node[,] grid)
        {
            Color[] pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y].Type == NodeType.Land)
                        pixels[x + y * width] = GetGroundPixel(x, y);
                    else if (grid[x, y].Type == NodeType.Air)
                        pixels[x + y * width] = new Color(0, 0, 0, 0);
                    else if (grid[x, y].Type == NodeType.Cave || grid[x, y].Type == NodeType.WaterCave)
                        pixels[x + y * width] = new Color(0, 0, 0, 0);
					else if (grid[x, y].Type == NodeType.Floating)
                        pixels[x + y * width] = GetGroundPixel(x, y);

					if (grid[x, y].Type == NodeType.Null && grid[x, y].Solid)
                        pixels[x + y * width] = GetGroundPixel(x, y);

                }
            }
            return pixels;
        }

        Color GetGroundPixel(int x, int y)
        {
            if (TerrainType == null || TerrainType.GroundTexture == null)
                return Color.black;

            int w = TerrainType.GroundTexture.width;
            int h = TerrainType.GroundTexture.height;

            return TerrainType.GroundTexture.GetPixel(x % w, y % h);
        }

        void Debug_ShowNodeList(List<Node> nodes, Texture2D tex, Color col)
        {
            foreach (var node in nodes)
            {
                tex.SetPixel(node.Position.x, node.Position.y, col);
            }
        }

		#endregion

		#region Backgrounds

		void SetBackground()
		{
			CameraManager.SetBackgroundLayer(SpawnBackgroundLayer(10.0f, 1.2f, TerrainType.Background1, 2, 1), 0);
			CameraManager.SetBackgroundLayer(SpawnBackgroundLayer(15.0f, 1.3f, TerrainType.Background2, 1, 2), 1);
		}

		GameObject SpawnBackgroundLayer(float depth, float height, Sprite spr, int order, int layerNumber)
		{
			GameObject bckLayer = new GameObject("Layer" + layerNumber.ToString());
			bckLayer.transform.SetParent(BackgroundObject.transform);
			float xoffs = GRandom.GetFloat(0, 3.2f);
			int bckObjects = Width / 320 + 2;
			for (int i = 0; i < bckObjects; i++)
			{
				float spawnx = -3.2f + i * 3.2f + xoffs - 1.6f;
				GameObject bckObj = new GameObject("BackgroundLayer" + layerNumber + "_" + i);
				bckObj.transform.SetParent(bckLayer.transform);
				bckObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
				bckObj.transform.localPosition = new Vector3(spawnx, height, 0);

				SpriteRenderer sr = bckObj.AddComponent<SpriteRenderer>();
				sr.sprite = spr;
				sr.sortingOrder = order;
			}
			return bckLayer;
		}

		#endregion

		#region Water

		void SetWater(float baseHeight = 0)
		{
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight - 0.2f,	9,	1,	0.7f), 0);
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight - 0.1f,	8,	2,	0.6f), 1);
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight,			7,	3,	0.5f), 2);
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight + 0.1f,	5,	4,	0.4f), 3);
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight + 0.2f,	4,	5,	0.3f), 4);
			CameraManager.SetWaterLayer(SpawnWaterLayer(baseHeight + 0.3f,	3,	6,	0.2f), 5);
		}

		GameObject SpawnWaterLayer(float height, int order, int layerNumber, float brightness)
		{
			GameObject waterLayer = new GameObject("Layer" + layerNumber.ToString());
			waterLayer.transform.SetParent(WaterObject.transform);
			float xoffs = GRandom.GetFloat(0, 0.64f);
			int waterObjects = Width / 64 * 2;
			for (int i = 0; i < waterObjects; i++)
			{
				float spawnx = -(Width / 200) + i * 0.64f + xoffs;
				GameObject watObj = Instantiate(WaterPrefab, waterLayer.transform);
				watObj.name = "WaterObject" + layerNumber.ToString() + "_" + i.ToString();
				watObj.transform.localPosition = new Vector3(spawnx, height, 0);
				SpriteRenderer sr = watObj.GetComponent<SpriteRenderer>();
				sr.sortingOrder = order;
				Animator anim = watObj.GetComponent<Animator>();
				anim.Play("WaterAnimation", 0, GRandom.GetFloat(0.0f, 1.0f));
				Material mat = sr.material;
				mat.SetFloat("_Bright", brightness);
			}
			return waterLayer;
		}

		#endregion

		#region Adding upper and lower layers


		void AddUpperGrowth(Node[,] grid, Texture2D tex)
        {
            for (int i = TopNodes.Count - 1; i > -1; i--)
            {
                Node top = TopNodes[i];
                if(!top.Solid)
                {
                    TopNodes.RemoveAt(i);
                    continue;
                }
                if(top.Position.y < height - 1)
                {
                    Node upper = grid[top.Position.x, top.Position.y + 1];
                    if(upper.Solid)
                    {
                        TopNodes.RemoveAt(i);
                        continue;
                    }
                }
                int growth = GetUpperGrowthSize(top, grid, 1);
                AddUpperGrowth(top, grid, growth, tex);
            }
        }

		void AddLowerGrowth(Node[,] grid, Texture2D tex)
		{
			for (int i = BottomNodes.Count - 1; i > -1; i--)
			{
				Node bottom = BottomNodes[i];
				if(!bottom.Solid)
				{
					BottomNodes.RemoveAt(i);
					continue;
				}
				if(bottom.Position.y > 0)
				{
					Node lower = grid[bottom.Position.x, bottom.Position.y - 1];
					if(lower.Solid)
					{
						BottomNodes.RemoveAt(i);
						continue;
					}
				}
				int growth = GetLowerGrowthSize(bottom, grid, 1);
				AddLowerGrowth(bottom, grid, growth, tex);
			}
		}

        int GetUpperGrowthSize(Node node, Node[,] grid, int jaggedness)
        {
            if (node == null || grid == null)
                return -1;

            int nodesDown = 0;
            int nodesUp = 0;

            for (int y = node.Position.y + 1; y < node.Position.y + 12; y++)
            {
                if (y >= height)
                    break;
                Node up = grid[node.Position.x, y];
                if (!up.Solid)
                    nodesUp++;
                else
                    break;
            }
            for(int y = node.Position.y; y > node.Position.y - 6; y--)
            {
                if (y < 0)
                    break;
                Node down = grid[node.Position.x, y];
                if (down.Solid)
                    nodesDown++;
                else
                    break;
            }
            int count = (nodesUp > nodesDown) ? nodesDown : nodesUp / 2;
            count += GRandom.GetInt(0, jaggedness);
            return count;
        }

		int GetLowerGrowthSize(Node node, Node[,] grid, int jagedness)
		{
			if (node == null || grid == null)
				return -1;

			int count = 1;

			for (int y = node.Position.y - 1; y > node.Position.y - 6; y--)
			{
				if (y < 0)
					break;
				Node down = grid[node.Position.x, y];
				if (!down.Solid)
					count++;
				else
					break;
			}
			count -= GRandom.GetInt(0, jagedness);
			return count;
		}

        void AddUpperGrowth(Node node, Node[,] grid, int growthHeight, Texture2D tex)
        {
            ColorGradient gradient = TerrainType.GetTopGradient();
            if (growthHeight > 6)   growthHeight = 6;
            for (int y = 1; y < growthHeight + 1; y++)
            {
                int curY = node.Position.y + y;
                if (curY >= height)
                    return;

                grid[node.Position.x, curY].Solid = true;
                grid[node.Position.x, curY].Type = NodeType.Land;		//Overrides floating, etc

                Color col = gradient.Colors[growthHeight - y];
                col.a = 1.0f;
                tex.SetPixel(node.Position.x, curY, col);
            }
        }

		void AddLowerGrowth(Node node, Node[,] grid, int growthHeight, Texture2D tex)
		{
			ColorGradient gradient = TerrainType.GetBottomGradient();
			if (growthHeight > 5) growthHeight = 5;
			for (int y = 0; y < growthHeight + 1; y++)
			{
				int curY = node.Position.y - y;
				if (curY < 0)
					return;

				grid[node.Position.x, curY].Solid = true;
				grid[node.Position.y, curY].Type = NodeType.Land;       //Overrides floating, etc

				Color col = gradient.Colors[y];
				col.a = 1.0f;
				tex.SetPixel(node.Position.x, curY, col);
			}
		}

        #endregion

        #region Raw data creation from noise

        NoiseData GetRawData(int[] ints, int layer, bool solid, float flatness, int heightDrop, bool convert)
        {
            NoiseData nd = new NoiseData(width, layer, solid);
            for (int x = 0; x < width; x++)
            {
                float noise = OctaveNoise(x, ints, Octaves, Persistance);
                nd.Data[x] = noise;
                if (noise < nd.Min)
                    nd.Min = noise;
                else if (noise > nd.Max)
                    nd.Max = noise;
            }
			if(solid)
				nd.Data = SetDataToStartFromMin(nd.Data, nd.Min);
            if(convert)
            {
                for (int x = 0; x < width; x++)
                {
                    nd.Data[x] = ScaleTo01(nd.Data[x], nd.Min, nd.Max) * (height - 10) * flatness - heightDrop;
                }
            }
            return nd;
        }

        NoiseData[] GetNoiseLayers(int count, int dropAmount, float flatnessFactor)
        {
            NoiseData[] layers = new NoiseData[count];
            bool solid = true;
            for (int i = 0; i < count; i++)
            {
                int drop = i *dropAmount;
                float flatness = Flatness;
                if(!solid)
                {
                    drop -= dropAmount;
                    flatness *= flatnessFactor;
                }
                NoiseData layer = GetRawData(GetIntArray(width), i, solid, flatness, drop, true);
                solid = !solid;
                layers[i] = layer;
            }

            return layers;
        }

        float[] MoveRawData(float[] data, float factor = 0.0f)
        {
            if (factor == 0.0f)
            {
                factor = GRandom.GetFloat(0.0f, 100.0f);// random.Next(0, 101) * 0.01f;
            }
            int start = Mathf.FloorToInt(width * factor);

            float[] newdata = new float[width];
            for (int x = 0; x < width; x++)
            {
                if (start >= width)
                    start = 0;
                newdata[x] = data[start];
                start++;
            }
            return newdata;
        }

        float[] SetDataToStartFromMin(float[] data, float min)
        {
            int start = 0;
            for (int x = 0; x < data.Length; x++)
            {
                if (data[x] == min)
                {
                    start = x;
                    break;
                }
            }
            float[] newdata = new float[width];
            for (int x = 0; x < width; x++)
            {
                if (start >= width)
                    start = 0;
                newdata[x] = data[start];
                start++;
            }
            return newdata;
        }

        #endregion

        #region Input for noise

        int[] GetIntArray(int size)
        {
            if (size == 0 || size < 0)
                return null;

            int[] ints = new int[size * 2];

            List<int> list = CreateIntList(size);
            for (int i = 0; i < size; i++)
            {
                int num = GetRandomInt(list);
                ints[i] = num;
            }
            for (int i = 0; i < size; i++)
            {
                ints[size + i] = ints[i];
            }
            return ints;
        }

        List<int> CreateIntList(int size)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < size; i++)
                list.Add(i);
            return list;
        }

        int GetRandomInt(List<int> list)
        {
            if (list == null || list.Count == 0)
                return -1;

            int rnd = GRandom.GetIntRaw(0, list.Count);// random.Next(0, list.Count);
            int num = list[rnd];
            list.RemoveAt(rnd);
            return num;
        }

        #endregion

        #region Scaling data

        float[] GetMinMax(float[] floats)
        {
            float[] minAndMax = new float[2];
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int x = 0; x < width; x++)
            {
                float f = floats[x];
                if (f < min)
                    min = f;
                else if (f > max)
                    max = f;
            }
            minAndMax[0] = min;
            minAndMax[1] = max;
            return minAndMax;
        }

        float ScaleTo01(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        #endregion

        #region Noise

        public float Noise2(float rx, int[] perm)
        {
            float x = rx / width * Detail;
            var X = Mathf.FloorToInt(x) & 0xff;
            x -= Mathf.Floor(x);
            var u = Fade(x);
            return Lerp(u, Grad(perm[X], x), Grad(perm[X + 1], x - 1)) * 2;
        }

        public float Noise(float rx, int[] perm)
        {
            float x = rx / width * Detail;

            int X = Mathf.FloorToInt(x) % width;
            int X2 = (X + (Width / Detail));
            if (X2 > width)
                X2 = 0;
            //if (X2 == Detail) X2 = 0;

            x -= Mathf.Floor(x);
            var u = Fade(x);
            var g1 = Grad(perm[X], x);
            var g2 = Grad(perm[X + 1], x - 1);

            var value = Lerp(u, g1, g2) * 2;
            return value;
        }

        public float OctaveNoise(float x, int[] perm, int octave, float persistance)
        {
            if (octave <= 0)
                octave = 1;
            if (persistance < 0.0f)
                persistance = 0.0f;
            float value = 0.0f;
            float amplitude = 1.0f;
            float frequency = 1.0f;
            for (var i = 0; i < octave; i++)
            {
                value += Noise(x * frequency, perm) * amplitude;
                frequency *= 2.0f;
                amplitude *= persistance;
            }
            return value;
        }


        static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        static float Grad(int hash, float x)
        {
            return (hash & 1) == 0 ? x : -x;
        }

        #endregion
    }

}