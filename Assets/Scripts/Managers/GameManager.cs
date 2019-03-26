using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WormsClone
{
	public class GameManager : MonoBehaviour
	{
		public static GameManager GM;

		public int Width { get; private set; }
		public int Height { get; private set; }
		private List<Team> Teams = new List<Team>();
		public List<Team> GetTeams { get { return new List<Team>(Teams); } }
		private List<WormObject> WormsToDie = new List<WormObject>();
		public List<WormObject> GetWormsToDie { get { return new List<WormObject>(WormsToDie); } }

		public Node[,] MapGrid { get; private set; }
		public Sprite MapSprite { get; private set; }

		public float Delta { get; private set; }
		public int CurrentTeam { get; private set; }
		public GameState State { get; private set; }

		//public bool ActionComplete;
		public int ObjectActive;

		private bool mapChanged = false;
		private int objectCount = 0;
		private bool[,] wormSpawned;
		private int debrisCount;
		private Texture2D debrisTex;

		public static Color NoColor = new Color(0, 0, 0, 0);

		[Header("Game properties")]
		public int WormHealth = 100;
		public float GroundFriction = 0.95f;
		public float GameSpeed = 1.0f;
		public int MinWormSpawnRange = 16;
		public float DamageFactor = 4.0f;
		public float FallDamageFactor = 250.0f;
		public int ActionTics = 10;

		[Header("Settings")]
		public Color[] DefTeamColors;

		[Header("Prefabs")]
		public GameObject WormPrefab;
		public GameObject WormInfoPrefab;
		public GameObject WormHealthInfoPrefab;

		public GameObject DefaultExplosionObject;
		public GameObject DefaultExplosionPuffObject;

		[Header("Containers")]
		public GameObject ObjectContainer;
		public GameObject WormCanvas;
		public GameObject DebrisContainer;
		public GameObject FluidsContainer;

		[Header("Pointers")]
		public PlayerManager PlayerManager;
		public CameraManager CameraManager;

		[Header("Debug")]
		public GameObject DebugObjectPrefab;
		public int DebugObjectSize = 5;

		#region Init

		private void Awake()
		{
			GM = this;
		}


		void Start()
		{
			debrisTex = new Texture2D(1, 1);
			State = GameState.Movement;
			
		}

		public void SetMapGrid(Node[,] grid, int w, int h)
		{
			MapGrid = grid;
			Width = w;
			Height = h;
			wormSpawned = new bool[w, h];
		}

		public void SetSprite(Sprite spr)
		{
			MapSprite = spr;
		}

		public void InitGame()
		{
			CreateDebugTeams(); //
			CurrentTeam = GRandom.Rnd.Next(0, Teams.Count);
			SpawnWorms();
			ActivateWorm(Teams[CurrentTeam].Worms[0]);
		}

		#endregion

		void Update()
		{
			Delta = Time.deltaTime * GameSpeed;
			if (mapChanged)
				MapSprite.texture.Apply();
			mapChanged = false;
			ObjectActive--;
			if (ObjectActive <= 0)
				ObjectActive = 0;

			if (State == GameState.Observing)
			{
				if (ObjectActive == 0)
				{
					bool damageTaken = UpdateWormHealth();
					if (damageTaken)
					{
						State = GameState.Damage;
						Delay();
					}
					else
					{
						State = GameState.Movement;
						NextTurn(PlayerManager.CurrentWorm);
					}
				}
			}
			else if (State == GameState.Damage)
			{
				if (ObjectActive == 0)
				{
					bool killedWorm = KillNextWorm();
					if (killedWorm)
					{
						State = GameState.Killing;
						Delay();
					}
					else
					{
						State = GameState.Movement;
						NextTurn(PlayerManager.CurrentWorm);
					}
				}
			}
		}

		#region Grid

		public Vector3 GridPosToWorldPos(Vector2Int gridPos)
		{
			return new Vector3(gridPos.x / 100.0f, gridPos.y / 100.0f, 0);
		}

		public Vector3 GridPosToWorldPos(Vector2 gridPos)
		{
			return GridPosToWorldPos(gridPos.x, gridPos.y);
		}

		public Vector3 GridPosToWorldPos(float x, float y)
		{
			return new Vector3(x / 100.0f, y / 100.0f, 0);
		}

		#endregion

		#region Spawning

		public RectObject SpawnObject(GameObject prefab, Vector2 pos)
		{
			GameObject gameObj = Instantiate(prefab, ObjectContainer.transform);
			gameObj.transform.position = GridPosToWorldPos(pos);
			RectObject obj = gameObj.GetComponent<RectObject>();
			obj.Position = new Vector2(pos.x, pos.y);
			if(!obj.DisableObjectCollisions)
				SetNodeObject(obj);
			//Debug.LogFormat("Spawned {0} at {1}", obj, pos);
			return obj;
		}

		public RectObject SpawnObject(GameObject prefab, Vector2Int pos)
		{
			return SpawnObject(prefab, new Vector2(pos.x, pos.y));
		}

		#endregion

		#region Game cycle

		public void ActivateWorm(WormObject worm)
		{
			PlayerManager.ActivateWorm(worm);
			CameraManager.CenterOnWorm(worm);
			SetWormCrosshair(worm, true);
		}

		private void SetWormCrosshair(WormObject worm, bool activate)
		{
			worm.Crosshair.gameObject.SetActive(activate);
		}

		public bool UpdateWormHealth()
		{
			bool damageTaken = false;
			for (int t = 0; t < Teams.Count; t++)
			{
				Team team = Teams[t];
				foreach (var worm in team.Worms)
				{
					if(worm.Damage > 0)
					{
						damageTaken = true;
						GameObject obj = Instantiate(WormHealthInfoPrefab, WormCanvas.transform);
						WormHealthInfo whInfo = obj.GetComponent<WormHealthInfo>();
						if (whInfo)
						{
							whInfo.Target = worm.transform;
							whInfo.SetHealth(worm.Damage);
							whInfo.SetColor(team.TeamColor);
						}
						worm.Health -= worm.Damage;
						worm.Damage = 0;
						worm.WormInfo.UpdateHealth(worm.Health);
						if (worm.Health <= 0)
							WormsToDie.Add(worm);
					}
				}
			}
			return damageTaken;
		}

		public bool KillNextWorm()
		{
			if(WormsToDie.Count > 0)
			{
				WormObject worm = WormsToDie[0];
				worm.SetToDie();
				return true;
			}
			return false;
		}

		public bool KillWorm(WormObject worm)
		{
			if(WormsToDie.Contains(worm))
			{
				WormsToDie.Remove(worm);
				DeleteWorm(worm);
				
				State = GameState.Damage;
				return true;
			}
			return false;
		}

		public void HurtWorm(WormObject worm, int damage)
		{
			if (worm == PlayerManager.CurrentWorm && State == GameState.Movement)
				State = GameState.Observing;
		}

		public void DeleteWorm(WormObject worm)
		{
			if (worm == PlayerManager.CurrentWorm && State == GameState.Movement)
				State = GameState.Observing;
			Teams[worm.TeamIndex].Worms.Remove(worm);
		}

		public IEnumerator RetreatWorm(float time, WormObject worm)
		{
			yield return new WaitForSeconds(time);
			if (worm)
			{
				worm.Horizontal = 0;
				worm.Vertical = 0;
			}
			State = GameState.Observing;
		}
		public void Delay()
		{
			ObjectActive = ActionTics;
		}

		public void NextTurn(WormObject lastWorm)
		{
			if (lastWorm)
				SetWormCrosshair(lastWorm, false);
			for (int i = 0; i < DebrisContainer.transform.childCount; i++)
				Destroy(DebrisContainer.transform.GetChild(i).gameObject, GRandom.GetFloat(0, 1.0f));
			CycleTeams();
			WormObject worm = Teams[CurrentTeam].GetNextWorm();
			ActivateWorm(worm);
		}

		public void CycleTeams()
		{
			CurrentTeam++;
			if (CurrentTeam >= Teams.Count)
				CurrentTeam = 0;
		}

		#endregion

		#region Worm spawning

		public void SpawnWorms()
		{
			for (int t = 0; t < Teams.Count; t++)
			{
				Team team = Teams[t];
				Color col = Color.grey;
				if (t < DefTeamColors.Length)
					col = DefTeamColors[t];
				team.TeamColor = col;
				foreach (var name in team.WormNames)
				{
					int safe = 100;
					while (true)
					{
						WormObject worm = SpawnWorm(name, col);
						if (worm)
						{
							team.Worms.Add(worm);
							worm.Crosshair.gameObject.SetActive(false);
							worm.TeamIndex = t;
							break;
						}
						safe--;
						if(safe == 0)
						{
							Debug.LogError("Failed to spawn a worm");
							break;
						}
					}
				}
			}
		}


		public WormObject SpawnWorm(string name, Color col)
		{
			WormObject wormDef = WormPrefab.GetComponent<WormObject>();
			int w = wormDef.Width;
			int h = wormDef.Height;

			int random_x = GRandom.GetInt(0, Width - w);
			int start_y = Height;

			//Debug.LogFormat("Trying to spawn worm at {0}", random_x);

			while(start_y > 1)
			{
				start_y--;
				Node n = MapGrid[random_x, start_y];
				Node floor = MapGrid[random_x, start_y - 1];
				if(wormSpawned[random_x, start_y])
					continue;
				if(!floor.Solid || n.Solid)
					continue;
				Vector2Int pos = new Vector2Int(n.Position.x, n.Position.y);
				WormObject worm = SpawnWormAt(pos, w, h);
				if (worm)
				{
					//Debug.LogFormat("Spawned at node {0}", MapGrid[pos.x, pos.y]);
					worm.name = name;
					worm.Health = WormHealth;
					RadiusAction(pos, MinWormSpawnRange, wormSpawned, null, (x) => x = true);
					GameObject wormInfoObj = Instantiate(WormInfoPrefab, WormCanvas.transform);
					WormInfo wormInfo = wormInfoObj.GetComponent<WormInfo>();
					if (wormInfo)
					{
						wormInfo.Target = worm.transform;
						wormInfo.SetName(name);
						wormInfo.SetColor(col);
					}
					worm.WormInfo = wormInfo;
					return worm;
				}
			}
			return null;
		}

		public WormObject SpawnWormAt(Vector2Int pos, int wormWidth, int wormHeight)
		{
			int start_y = pos.y;
			int safe = 10000;
			while (true)
			{
				if (start_y >= Height)
				{
					Debug.Log("Not enough space in Y to spawn new worm");
					return null;
				}
				bool found = true;
				for (int x = 0; x < wormWidth; x++)
				{
					if (pos.x + x >= Width)
						return null;

					Node n = MapGrid[pos.x + x, start_y];
					if(n.Solid)
					{
						start_y++;
						found = false;
						break;
					}
					for (int y = 0; y < wormHeight; y++)
					{
						if (start_y + y >= Height)
							continue;
						n = MapGrid[pos.x + x, start_y + y];
						if(n.Solid)
						{
							return null;
						}
					}
				}
				if (found)
				{
					Vector2Int wormPos = new Vector2Int(pos.x, start_y);
					return (WormObject)SpawnObject(WormPrefab, wormPos);
				}

				safe--;
				if(safe == 0)
				{
					Debug.LogErrorFormat("Failed to spawn worm, start_y : {0}", start_y);
					return null;
				}
			}
		}


		public RectObject SpawnSimpleObject(Vector2Int pos, Vector2 vel, string name, Color col, GameObject container)
		{
			GameObject obj = new GameObject(name);
			obj.transform.parent = container.transform;
			obj.transform.position = GridPosToWorldPos(new Vector2(pos.x, pos.y));

			RectObject robj = obj.AddComponent<RectObject>();
			robj.Position = pos;
			robj.Velocity = vel;
			robj.Width = 1;
			robj.Height = 1;

			SpriteRenderer sr = obj.gameObject.AddComponent<SpriteRenderer>();
			Texture2D tex = new Texture2D(1, 1);
			Color[] pixels = new Color[1];
			pixels[0] = col;
			tex.SetPixels(pixels);
			Sprite spr = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
			sr.sprite = spr;
			sr.sortingOrder = 10;


			return robj;
		}

		public RectObject SpawnDebris(Vector2Int pos, Color col, Vector2 vel, string name, float lifetime, bool disableCollisions = true, bool merge = false)
		{
			RectObject debris = SpawnSimpleObject(pos, vel, name, col, DebrisContainer);

			debris.DisableObjectCollisions = disableCollisions;
			debris.IgnoredByObjects = true;
			debris.CollisionIgnoringCondition = (w) => w is WormObject;
			debris.Elasticity = 0.6f;
			debris.Mass = 0.9f;
			if (lifetime > 0)
				Destroy(debris.gameObject, GRandom.GetFloat(lifetime, lifetime + 2.0f));
			else if (merge)
				debris.OnStopEvent += MergeDebris;
			debrisCount++;
			debris.OnDestroyEvent += (d) => debrisCount--;
			return debris;
		}

		public RectObject SpawnFluid(Vector2Int pos, Color col, Vector2 vel, string name)
		{
			RectObject fluid = SpawnSimpleObject(pos, vel, name, col, FluidsContainer);

			fluid.CollisionIgnoringCondition = (w) => w is WormObject;
			fluid.Elasticity = 0;
			fluid.Mass = 0.9f;
			fluid.OnTickEvent += FluidCheck;
			fluid.DisableHorizontalMovement = true;
			fluid.DisableUpwardsMovement = true;

			return fluid;
		}

		public void MergeDebris(RectObject debris)
		{
			Color col = debris.GetComponent<SpriteRenderer>().sprite.texture.GetPixel(0, 0);
			Vector2Int pos = debris.GetCurrentPos();
			Vector2Int bot = pos;	bot.y -= 1;
			if(IsInBounds(bot) && IsInBounds(pos))			
			{
				Node b = MapGrid[bot.x, bot.y];
				Node n = MapGrid[pos.x, pos.y];
				if (b.Solid && !b.Object && !n.Object)  //Checking if bottom node (the node debris is on) is solid but NOT an object. Also checking current node if not occupied by any object.
				{
					Destroy(debris.gameObject);
					n.Solid = true;
					MapSprite.texture.SetPixel(pos.x, pos.y, col);
					mapChanged = true;
				}
			}
		}

		public void FluidCheck(RectObject fluid)
		{
			float flowSpeed = 0.25f;

			if (fluid.Velocity.y < 0)// && Mathf.Abs(fluid.Velocity.x) > flowSpeed)
				return;

			Vector2Int pos = fluid.GetCurrentPos();
			Vector2Int bot = pos; bot.y--;
			for (int i = 1; i < 4; i++)
			{
				Vector2Int left = bot;	left.x -= i;
				Vector2Int righ = bot;	righ.x += i;

				Node lNode = null;
				Node rNode = null;
				if (IsInBounds(left))
					lNode = MapGrid[left.x, left.y];
				if (IsInBounds(righ))
					rNode = MapGrid[righ.x, righ.y];

				Node flow = null;
				float dir = 0;
				if (lNode && rNode)
				{
					if (!lNode.Solid && !rNode.Solid)
					{
						if (GRandom.GetBool())
						{ flow = lNode; dir = -flowSpeed; }
						else
						{ flow = rNode; dir = flowSpeed; }
					}
					else if (!lNode.Solid)
					{ flow = lNode; dir = -flowSpeed; }
					else if (!rNode.Solid)
					{ flow = rNode; dir = flowSpeed; }
				}
				else if (lNode && !lNode.Solid)
				{ flow = lNode; dir = -flowSpeed; }
				else if (rNode && !rNode.Solid)
				{ flow = rNode; dir = flowSpeed; }

				if (flow && !flow.IsSolid(fluid))
				{
					Node u = MapGrid[flow.Position.x, flow.Position.y + 1];
					if (u.IsSolid(fluid))
						continue;
					//	fluid.AddForce(new Vector2(dir, 0));
					ClearNodeObject(fluid.Position, 1, 1);
					fluid.LastPosition = fluid.Position;
					fluid.Position = new Vector2(fluid.Position.x + dir, fluid.Position.y);
					//fluid.Position.x += 0.5f;
					//fluid.Position.y += 0.5f;
					SetNodeObject(fluid);

				}
			}
		}

		#endregion

		#region Node checks

		public bool IsNodeSolid(int x, int y, RectObject self, bool ignoreObjects = false)
		{
			if (!IsInBounds(x, y))
				return false;
			return MapGrid[x, y].IsSolid(self, ignoreObjects);
		}

		public bool IsNodeSolid(float fx, float fy, RectObject self, bool ignoreObjects = false)
		{
			int x = (int)fx;
			int y = (int)fy;

			return IsNodeSolid(x, y, self, ignoreObjects);
		}

		public bool IsNodeSolid(Vector2 pos, RectObject self, bool ignoreObjects = false)
		{
			int x = (int)pos.x;
			int y = (int)pos.y;

			return IsNodeSolid(x, y, self, ignoreObjects);
		}

		public bool IsNodeSolid(Vector2 pos, Vector2Int dir, int count, RectObject self, bool ignoreObjects = false)
		{
			for (int i = 0; i < count; i++)
			{
				int x = (int)pos.x + i * dir.x;
				int y = (int)pos.y + i * dir.y;

				if (IsNodeSolid(x, y, self, ignoreObjects))
					return true;
			}
			return false;
		}

		public bool IsNodeSolid(Vector2 pos, Vector2 vel, int count_x, int count_y, RectObject self, bool ignoreObjects = false)
		{
			Vector2 newpos = pos;
			
			if(vel.x > 0)
			{
				newpos.x += count_x - 1;
				if (IsNodeSolid(newpos, Vector2Int.up, count_y, self, ignoreObjects))
					return true;
			}
			else if(vel.x < 0)
			{
				if (IsNodeSolid(pos, Vector2Int.up, count_y, self, ignoreObjects))
					return true;
			}
			if(vel.y > 0)
			{
				newpos.y += count_y - 1;
				if (IsNodeSolid(newpos, Vector2Int.right, count_x, self, ignoreObjects))
					return true;
			}
			else if(vel.y < 0)
			{
				if (IsNodeSolid(pos, Vector2Int.right, count_x, self, ignoreObjects))
					return true;
			}
			return false;
		}

		#endregion

		#region Node objects

		public void SetNodeObject(RectObject obj)
		{
			Vector2Int pos = obj.GetCurrentPos();
			for (int x = 0; x < obj.Width; x++)
			{
				for (int y = 0; y < obj.Height; y++)
				{
					int nx = pos.x + x;
					int ny = pos.y + y;
					if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
						continue;
					MapGrid[pos.x + x, pos.y + y].Object = obj;
				}
			}
		}

		public void ClearNodeObject(Vector2 pos, int w, int h)
		{
			ClearNodeObject((int)pos.x, (int)pos.y, w, h);
		}

		public void ClearNodeObject(int sx, int sy, int w, int h)
		{
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					int nx = sx + x;
					int ny = sy + y;
					if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
						continue;
					MapGrid[sx + x, sy + y].Object = null;
				}
			}
		}

		#endregion

		#region Map modification

		public HashSet<RectObject> Explode(Vector2Int pos, int radius, int forceRadius, float maxForce, int debrisChance = 16)
		{
			HashSet<RectObject> hitObjects = new HashSet<RectObject>();
			System.Action<Node> act = (x) =>
			{
				if (x.Solid == true && GRandom.GetInt(1, 100) <= debrisChance)
				{
					Color col = MapSprite.texture.GetPixel(x.Position.x, x.Position.y);
					string name = "Debris_" + debrisCount;
					//RectObject debris = SpawnDebris(x.Position, col, Vector2.zero, name, GRandom.GetFloat(2.0f, 4.0f));
					RectObject debris = SpawnDebris(x.Position, col, Vector2.zero, name, 0, false, true);
					hitObjects.Add(debris);
				}
				x.Solid = false;

				if (x.Object)
				{
					if (!hitObjects.Contains(x.Object))
						hitObjects.Add(x.Object);
				}

			};
			RadiusAction(pos, radius, MapGrid, act, null, NoColor);
			foreach (var hitObject in hitObjects)
			{
				Vector2 objectCenter = hitObject.GetCenter();
				float dist = Vector2.Distance(pos, objectCenter);
				float dist_b = dist / radius;
				float force = Mathf.Lerp(maxForce, 0, dist_b);
				Vector2 vel = new Vector2(objectCenter.x - pos.x, objectCenter.y - pos.y).normalized;
				hitObject.AddForce(vel * force);
				if(hitObject is WormObject)
				{
					WormObject worm = (WormObject)hitObject;
					int damage = (int)(force / DamageFactor);
					worm.GetHurt(damage, DamageType.Explosion);
				}
			}
			mapChanged = true;
			return hitObjects;
		}

		public void SetNodeCircle(Vector2Int pos, int radius, bool add, Color? col = null)
		{
			RadiusAction(pos, radius, MapGrid, (x) => x.Solid = add, null, col);
			mapChanged = true;
				 
		}
		public void RadiusAction<T>(Vector2Int pos, int radius, T[,] array, System.Action<T> act = null, System.Func<T, T> func = null, Color? col = null)
		{
			for (int x = pos.x - radius; x < pos.x + radius; x++)
			{
				for (int y = pos.y - radius; y < pos.y + radius; y++)
				{
					if (Mathf.Pow(x - pos.x, 2) + Mathf.Pow(y - pos.y, 2) <= Mathf.Pow(radius, 2))
					{
						if (x >= 0 && x < Width && y >= 0 && y < Height)
						{
							if(act != null)
								act(array[x, y]);
							else if(func != null)
								array[x, y] = func(array[x, y]);
							if (col != null)
								MapSprite.texture.SetPixel(x, y, (Color)col);
						}
					}
				}
			}
		}

		#endregion

		#region Map info

		public Vector2 GetNormalVector(Vector2Int pos, Vector2Int dest)
		{
			int dx = dest.x - pos.x;
			int dy = dest.y - pos.y;
			//Debug.LogFormat("Pos : {2}, dest : {3}, Rebound, dx : {0}, dy : {1}", dx, dy, pos, dest);
			Vector2 normal = Vector2.zero;
			Node C = MapGrid[dest.x, dest.y];
			Node NW = null, W = null, SW = null, S = null, SE = null, E = null, NE = null, N = null;

			//Get the 3x3 grid

			normal = new Vector2(-dx, -dy);
			if(dx > 0 && pos.x + 1 < Width)
			{
				E = MapGrid[pos.x + 1, pos.y];
				if(dy == 0)
				{
					if(pos.y + 1 < Height)
						NE = MapGrid[pos.x + 1, pos.y + 1];
					if(pos.y - 1 >= 0)
						SE = MapGrid[pos.x + 1, pos.y - 1];
				}
			}
			else if(dx < 0 && pos.x - 1 >= 0)
			{
				W = MapGrid[pos.x - 1, pos.y];
				if(dy == 0)
				{
					if(pos.y + 1 < Height)
						NW = MapGrid[pos.x - 1, pos.y + 1];
					if(pos.y - 1 >= 0)
						SW = MapGrid[pos.x - 1, pos.y - 1];
				}
			}
			else
			{
				if(pos.x + 1 < Width)
					E = MapGrid[pos.x + 1, pos.y];
				if(pos.x - 1 >= 0)
					W = MapGrid[pos.x - 1, pos.y];
				if(dy > 0 && pos.y + 1 < Height)
				{
					if(pos.x + 1 < Width)
						NE = MapGrid[pos.x + 1, pos.y + 1];
					if(pos.x - 1 >= 0)
						NW = MapGrid[pos.x - 1, pos.y + 1];
				}
				else if(dy < 0 && pos.y - 1 >= 0)
				{
					if(pos.x + 1 < Width)
						SE = MapGrid[pos.x + 1, pos.y - 1];
					if(pos.x - 1 >= 0)
						SW = MapGrid[pos.x - 1, pos.y - 1];
				}
			}
			if(dy > 0 && pos.y + 1 < Height)
			{
				N = MapGrid[pos.x, pos.y + 1];
			}
			else if(dy < 0 && pos.y - 1 >= 0)
			{
				S = MapGrid[pos.x, pos.y - 1];
			}
			else
			{
				if(pos.y + 1 < Height)
					N = MapGrid[pos.x, pos.y + 1];
				if(pos.y - 1 >= 0)
					S = MapGrid[pos.x, pos.y - 1];
			}

			//Set the rebound vector
			if(E && E.Solid)
			{
				normal.x -= 0.5f;
			}
			if(W && W.Solid)
			{
				normal.x += 0.5f;
			}
			if(N && N.Solid)
			{
				normal.y -= 0.5f;
			}
			if(S && S.Solid)
			{
				normal.y += 0.5f;
			}
			if(NE && NE.Solid)
			{
				if (dx != 0)
					normal.y -= 0.5f;
				else if (dy != 0)
					normal.x -= 0.5f;
			}
			if(NW && NW.Solid)
			{
				if (dx != 0)
					normal.y -= 0.5f;
				else if (dy != 0)
					normal.x += 0.5f;
			}
			if(SE && SE.Solid)
			{
				if (dx != 0)
					normal.y += 0.5f;
				else if (dy != 0)
					normal.x -= 0.5f;
			}
			if(SW && SW.Solid)
			{
				if (dx != 0)
					normal.y += 0.5f;
				else if (dy != 0)
					normal.x += 0.5f;
			}
			return normal.normalized;
		}

		public bool IsInBounds(Vector2Int pos)
		{
			if (pos.x > -1 && pos.x < Width && pos.y > -1 && pos.y < Height)
				return true;
			return false;
		}

		public bool IsInBounds(int x, int y)
		{
			if (x > -1 && x < Width && y > -1 && y < Height)
				return true;
			return false;
		}

		#endregion

		#region Debug stuff

		public void SpawnDebugObject(Vector2Int pos)
		{
			objectCount++;
			Vector3 npos = new Vector3(pos.x / 100.0f, pos.y / 100.0f, 0);
			GameObject obj = Instantiate(DebugObjectPrefab, Vector3.zero, Quaternion.identity, ObjectContainer.transform);
			obj.name = "Debug_object_" + objectCount;
			RectObject rectObj = obj.GetComponentInChildren<RectObject>();
			rectObj.SetSize(DebugObjectSize);
			Transform t = rectObj.transform;
			t.position = npos;
		}
		public void SpawnDebrisCircle(Vector2Int pos, int radius)
		{
			System.Action<Node> act = (x) =>
			{
				Vector2Int np = new Vector2Int(x.Position.x, x.Position.y);
				debrisCount++;
				SpawnDebris(np, Color.white, Vector2.zero, "Debris_" + debrisCount, 0, false);
			};
			RadiusAction(pos, radius, MapGrid, act);
		}

		public void SpawnFluidCircle(Vector2Int pos, int radius)
		{
			System.Action<Node> act = (x) =>
			{
				Vector2Int np = new Vector2Int(x.Position.x, x.Position.y);
				debrisCount++;
				SpawnFluid(np, Color.blue, Vector2.zero, "Fluid_" + np);
			};
			RadiusAction(pos, radius, MapGrid, act);
		}

		public void DebugRedrawMap()
		{
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					Color pix = MapSprite.texture.GetPixel(x, y);
					Node n = MapGrid[x, y];
					if(n.Solid)
					{
						if (pix.a == 1.0f)
							pix = Color.black;
						MapSprite.texture.SetPixel(x, y, pix);
					}
				}
			}
			MapSprite.texture.Apply();
		}

		public void CreateDebugTeams()
		{
			Team t1 = new Team();
			t1.WormNames.Add("Worm 1");
			t1.WormNames.Add("Worm 2");
			t1.WormNames.Add("Worm 3");
			t1.WormNames.Add("Worm 4");
			t1.WormNames.Add("Worm 5");
			t1.WormNames.Add("Worm 6");
			Team t2 = new Team();
			t2.WormNames.Add("Worm A");
			t2.WormNames.Add("Worm B");
			t2.WormNames.Add("Worm C");
			t2.WormNames.Add("Worm D");
			t2.WormNames.Add("Worm E");
			t2.WormNames.Add("Worm F");
			Teams.Add(t1);
			Teams.Add(t2);
		}



		#endregion

	}
}
