using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class RectObject : MonoBehaviour
	{
		[Header("Physics")]
		public Vector2 Position;
		public Vector2 LastPosition;
		public Vector2 Velocity;
		public Vector2 LastVelocity;

		[Header("Pointers")]
		public Transform ObjectSprite;

		[Header("Properties")]
		public int Width;
		public int Height;
		public float Mass = 1.0f;
		public float Elasticity = 0.5f;
		public float Friction = 1.0f;
		public float ReboundThreshold;


		[Header("Flags")]
		public bool DisableGravity = false;
		public bool DisableHorizontalMovement = false;
		public bool DisableUpwardsMovement = false;
		public bool DisableCollisions = false;
		public bool DisableObjectCollisions = false;
		public bool AffectedByWind = false;
		public bool RotateToVelocity = false;
		public bool IgnoredByObjects = false;

		public System.Func<RectObject, bool> CollisionIgnoringCondition;

		public event System.Action<RectObject> OnStopEvent;
		public event System.Action<RectObject> OnDestroyEvent;
		public event System.Action<RectObject> OnTickEvent;
		public event System.Action<RectObject> OnTerrainChangedEvent;

		public float Delta { get; protected set; }

		[Header("Debug stuff")]
		public bool AccuratePosition;
		public bool DebugSprite;
		public bool DebugLastPosition;
		public bool DebugLastCollision;
		public bool PauseOnCollisions;
		public string LastCollision;
		public GameObject DebugObject;

		private GameObject debugLastPos;
		private GameObject debugLastCol;
		private GameObject debugHitPart;
		private GameObject debugHitNode;
		private GameObject debugNormal;
		private GameObject debugSlide;
		private GameObject debugStuckPos;
		private GameObject debugStuckLastPos;
		private string debugStuckInfo;
		private bool DebugIsStuck;

		private void Start()
		{
			ObjectSprite = GetComponentInChildren<SpriteRenderer>().transform;
			if(DebugSprite)
			{
				SetDebugSprite(gameObject, Color.yellow, Width, Height);
			}
			if (DebugLastPosition)
			{
				debugLastPos = SpawnDebugObject("Last position", new Vector2(-1000, -1000), Color.blue);
				SetDebugSprite(debugLastPos, Color.red, Width, Height, 0.75f);
			}
			if (DebugLastCollision)
			{
				debugLastCol = SpawnDebugObject("Last collision", new Vector2(-1000, -1000));
				debugHitPart = SpawnDebugObject("Hit object part", new Vector2(-1000, -1000));
				debugHitNode = SpawnDebugObject("Hit node", new Vector2(-1000, -1000));
				debugNormal = SpawnDebugObject("Collision normal", new Vector2(-1000, -1000), Color.green);
				debugSlide = SpawnDebugObject("Collision slide", new Vector2(-1000, -1000), Color.magenta);
				SetDebugSprite(debugLastCol, Color.cyan, Width, Height, 0.5f);
				SetDebugSprite(debugHitPart, Color.blue, 1, 1);
				SetDebugSprite(debugHitNode, Color.white, 1, 1);
				SetDebugSprite(debugNormal, Color.white, 1, 1, 0);
				SetDebugSprite(debugSlide, Color.white, 1, 1, 0);
			}
			SetPosition(transform.localPosition);
		}

		private void Update()
		{
			Delta = GameManager.GM.Delta;
			StoreLastTick();
			Tick(Delta);
			if (debugLastPos)
			{
				debugLastPos.transform.position = (LastPosition / 100.0f);
				Debug_DrawLine(LastPosition, LastPosition + LastVelocity * Delta, debugLastPos.GetComponent<LineRenderer>());
			}
		}

		protected void ApplyGravity()
		{
			Velocity.y -= 9.81f * Mass;
		}

		public void AddForce(Vector2 vel)
		{
			Velocity += vel;
		}

		public void SetSize(int size)
		{
			SetSize(size, size);
		}

		public void SetSize(int w, int h)
		{
			Width = w;
			Height = h;
		}

		virtual protected void SetPosition(Vector2 gridPos)
		{
			if (AccuratePosition)
			{
				transform.localPosition = gridPos / 100.0f;
				return;
			}
			float x = ((int)(gridPos.x)) / 100.0f;
			float y = ((int)(gridPos.y)) / 100.0f;
			transform.localPosition = new Vector3(x, y, 0);
		}

		protected void SetPosition(Vector3 worldPos)
		{

			float x = transform.localPosition.x * 100;
			float y = transform.localPosition.y * 100;
			Position = new Vector2(x, y);
		}

		protected Vector3 GridToWorldPos(Vector2 pos)
		{
			return new Vector3(pos.x / 100, pos.y / 100);
		}

		public Vector2Int GetCurrentPos()
		{
			return new Vector2Int((int)Position.x, (int)Position.y);
		}

		public Vector2Int GetLastPos()
		{
			return new Vector2Int((int)LastPosition.x, (int)LastPosition.y);
		}

		public Vector2Int GetPos(Vector2 pos)
		{
			return new Vector2Int((int)pos.x, (int)pos.y);
		}

		public Node GetCurrentNode()
		{
			return GameManager.GM.MapGrid[(int)Position.x, (int)Position.y];
		}

		public static Node GetCurrentNode(Vector2 pos)
		{
			return GameManager.GM.MapGrid[(int)pos.x, (int)pos.y];
		}

		public Vector2 GetCenter()
		{
			float cx = Position.x + (Width / 2);
			float cy = Position.y + (Height / 2);
			return new Vector2(cx, cy);
		}

		protected void StoreLastTick()
		{
			LastPosition = Position;
			LastVelocity = Velocity;
		}

		public virtual void Tick(float dt)
		{
			//if (GameManager.GM.IsNodeSolid(Position, this, true))
			//{
			//	Velocity = Vector2.zero;
			//	OnStuck(Position);
			//}
			//if(!DisableObjectCollisions)
			//	GameManager.GM.ClearNodeObject(LastPosition, Width, Height);
			if (RotateToVelocity)
				AutoRotate();
			if (!GameManager.GM.IsNodeSolid(new Vector2(Position.x, Position.y - 0.06f), Vector2Int.right, Width, this, DisableObjectCollisions))	//Object is on floor
			{
				if (!DisableGravity)
					ApplyGravity();
			}
			else if(Velocity.y > -40.0f && Velocity.y < 40.0f)
			{
				Velocity.y = 0;
				Velocity.x *= GameManager.GM.GroundFriction * Friction;
			}
			if(DisableHorizontalMovement || (Mathf.Abs(Velocity.x) < 4.0f && Velocity.x != 0))
				Velocity.x = 0;
			Move(dt);
			SetPosition(Position);
			if (Position != LastPosition)
			{
				OnMove(LastPosition, Position);
				if(!DisableObjectCollisions)
				{
					GameManager.GM.ClearNodeObject(LastPosition, Width, Height);
					GameManager.GM.SetNodeObject(this);
				}
			}
			if (Velocity == Vector2.zero && LastVelocity != Vector2.zero)
				OnStop(Position);
			OnTick(LastPosition, Position);
			//if(!DisableObjectCollisions)
			//	GameManager.GM.SetNodeObject(this);
			//if (GameManager.GM.IsNodeSolid(Position, this, true))
			//{

			//	if (!DebugIsStuck)
			//	{
			//		debugStuckInfo = LastCollision;
			//		//Debug.LogWarningFormat("Object is in a solid node : {0}, {2}\n{1}", transform.parent.name, debugStuckInfo, GameManager.GM.MapGrid[(int)Position.x, (int)Position.y]);
			//		//SpawnStuckPosObjects();
			//		DebugIsStuck = true;
			//	}
			//	//Velocity = Vector2.zero;
			//	//Debug.Break();
			//	//SetDebugSprite(gameObject, Color.blue, Width, Height);
			//}
			//else
			//{
			//	//SetDebugSprite(gameObject, Color.yellow, Width, Height);
			//}

		}

		protected void DestroyObject()
		{
			OnDestroyObject();
			Destroy(gameObject);
		}

		public void Move(float dt)
		{
			if (Velocity == Vector2.zero)
				return;
			Vector2 dest = Position + (Velocity * dt);
			float toMove = (Velocity * dt).magnitude;
			if (dest.y < 0)
				DestroyObject();
			if (dest.y >= GameManager.GM.Height)    //Not checking lines in that case
			{
				Position = dest;
				return;
			}

			Collision col = null;
			string info = "";
			Vector2 vel = Velocity;
			int collisions = 0;
			while (col = CheckCollision(Position, (vel * dt), ref info))
			{
				if (DisableCollisions)
					break;
				if (collisions == 0)
				{
					//info += string.Format("initial Posision {0},{1}, Velocity {2}, toMove : {3} \n", Position.x, Position.y, Velocity, toMove);
				}
				collisions++;
				Vector2 posFromCol = GetPositionFromCollision(Position, ref vel, dt, col, ref info);
				//info += string.Format("posFromCol : {0}\n", posFromCol);
				//SetDebugPos(debugLastCol, posFromCol);
				//SetDebugPos(debugHitPart, col.HitPos);
				//SetDebugPos(debugHitNode, col.CollisionPos);

				Vector2 normal = GameManager.GM.GetNormalVector(col.HitPos, col.CollisionPos);	//Normal to the ground
				Vector2 rebound = (vel - 2 * (Vector2.Dot(vel, normal)) * normal);				//This way the object will go if elasticity is full
				float ex = Mathf.Lerp(col.Slide.x / dt, rebound.x, Elasticity);					//x part of the vector between just sliding (0 elasticity) and bouncing (1 elasticity)
				float ey = Mathf.Lerp(col.Slide.y / dt, rebound.y, Elasticity);					//y part...
				vel = new Vector2(ex, ey);


				//info += string.Format("normal : {0}, slide : {1}, rebound : {2}, ex/ey:{3}/{4}", normal, col.Slide, rebound, ex, ey);
				float moved = Vector2.Distance(Position, posFromCol);							//How much distance was made between start of frame position and position at the moment of collision

				//float left = 1 / (toMove * (toMove - moved));                                 //Just for checking
				//if (!DisableObjectCollisions && col.CollisionObject)                            //If collided with an object, not node, share the collision with it
				//	OnCollideWithObject(col.CollisionObject, ref vel);
				if (Velocity.magnitude > ReboundThreshold)
				{
					OnBounce(col, posFromCol, Velocity, ref vel, normal);
					//Velocity = vel - vel * normal * (1.0f - Elasticity);                      //Applying current velocity (after collision) to velocity after collisions					
				}
				else
				{
					OnStop(posFromCol);
					Velocity = Vector2.zero;
				}
				//info += string.Format("vel: {0}, left : {1}, toMove : {2}, moved : {3}, Velocity : {4}", vel, left, toMove, moved, Velocity);
				//SetDebugPosAndLine(debugNormal, col.CollisionPos, col.CollisionPos + (normal * 3.0f));
				//SetDebugPosAndLine(debugSlide, col.CollisionPos, col.CollisionPos + (col.Slide * 3.0f));

				if (moved < toMove && moved > 0)												//If moved less than should move (standard case - moved from position at frame start, to position at moment of collision
																								//so there is some movement left from this frame (also modified by elasticity)
				{
					vel = vel / toMove * (toMove - moved);										//Next velocity - the rest that was "cut"
					//info += string.Format(", vel*left: {0}, toMove(1): {1}, ", vel, toMove);
					toMove = (vel * dt).magnitude;
					Position = posFromCol;
					//info += string.Format("n:{0}, hitPart:{1}, normal:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, posFromCol:{8} \n", col.CollisionNode, col.HitPart, normal, moved, vel, Velocity, toMove, Position, posFromCol);

				}
				else if(moved >= toMove)														//If moved more than should (special case - only if the "newPos" "jumped" the object more than should because of the
																								//1-pixel differance between actual position and width/height.
				{
					//Debug.LogWarning("Weird calculations");
					toMove = 0;
					Position = posFromCol;
					//info += string.Format("n:{0}, hitPart:{1}, normal:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, posFromCol:{8} \n", col.CollisionNode, col.HitPart, normal, moved, vel, Velocity, toMove, Position, posFromCol);

					break;
				}
				else if (moved == 0)    //Should not happen
				{
					//Debug.LogWarning("Moved == 0");
					Position = posFromCol;
					//info += string.Format("n:{0}, hitPart:{1}, normal:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, posFromCol:{8} \n", col.CollisionNode, col.HitPart, normal, moved, vel, Velocity, toMove, Position, posFromCol);
				}

				if (Position == posFromCol)		//WHY????? sprawdzic to
					break;

				if (collisions > 5)
				{
					Debug.LogErrorFormat("Failed collision : {0} : {1}", transform.parent.name, null);
					break;
				}
			}
			if (DisableHorizontalMovement)
				Velocity.x = 0;
			if (DisableUpwardsMovement && Velocity.y > 0)
				Velocity.y = 0;
			if (collisions > 0)
			{
				dest = Position + (vel * dt);
				//info += string.Format("New velocity : {0}\n", Velocity);
				if(!GameManager.GM.IsNodeSolid(dest, Velocity, Width, Height, this, DisableObjectCollisions))
				{
					Position = dest;
					//info += string.Format("New position : {0}\n", Position);
				}
					
			}
			else
				Position += Velocity * dt;
			//SetPosition(Position);	//This is not a place for this, is it..............................
			if (col)
			{
				//Debug.Log(info);
				if(PauseOnCollisions)
					Debug.Break();
			}
			//LastCollision = info;
			//Debug_DrawLine(Position, Position + Velocity * dt);
		}


		public Collision CheckCollision(Vector2 pos, Vector2 vel, ref string info)
		{
			//info += string.Format("===== new collision ===== \n");
			Vector2 dst = pos + vel;
			int pos_x = (int)pos.x;
			int pos_y = (int)pos.y;
			int dst_x = (int)dst.x;
			int dst_y = (int)dst.y;
			//Debug.LogFormat("New collision check, from {0} to {1}", pos, dst);

			float dx = dst.x - pos.x;   //dx > 0 => right
										//dx < 0 => left
			float dy = dst.y - pos.y;   //dy > 0 => up
										//dy < 0 => down

			Vector2Int curPos = GetPos(pos);
			//Debug.LogFormat("Check collision, curPos : {0}", curPos);
			Vector2Int[] collisionNodes = Collision.GetCollisionNodes(pos, dst, ref info);
			Vector2Int[] collisionPath = Collision.ConvertToPath(collisionNodes);
			if (collisionPath == null || collisionPath.Length == 0)
				return null;
			foreach (var vec in collisionPath)
			{
				curPos += vec;
				//Debug.LogFormat("curPos : {0}, vec : {1}", curPos, vec);
				//info += string.Format("curPos : {0}, vec : {1}\n", curPos, vec);
				if(vec.x == 1)	//Going right
				{
					int nx = curPos.x + Width - 1;  //Column to check
					if (dy <= 0)					//Going right-down or just to right
					{
						for (int y = 0; y < Height; y++)    //Rows to check
						{
							int ny = curPos.y + y;
							if (GameManager.GM.IsInBounds(nx, ny))  //Map bounds
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx - 1, ny, Width - 1, y, n, n.Object, -1, n.Position.x, Vector2Int.right, Vector2.down * vel.y);
							}
						}
					}
					else if(dy > 0)				//Going right-up
					{
						for (int y = Height - 1; y > -1; y--)    //Rows to check
						{
							int ny = curPos.y + y;
							if (GameManager.GM.IsInBounds(nx, ny))  //Map bounds
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx - 1, ny, Width - 1, y, n, n.Object, -1, n.Position.x, Vector2Int.right, Vector2.up * Mathf.Abs(vel.y));
							}
						}
					}
				}
				else if(vec.x == -1)	//Going left
				{
					int nx = curPos.x;
					if (dy <= 0)
					{
						for (int y = 0; y < Height; y++)
						{
							int ny = curPos.y + y;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx + 1, ny, 0, y, n, n.Object, -1, n.Position.x + 1, Vector2Int.left, Vector2.down * Mathf.Abs(vel.y));
							}
						}
					}
					else if(dy > 0)
					{
						for (int y = Height - 1; y > -1; y--)
						{
							int ny = curPos.y + y;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx + 1, ny, 0, y, n, n.Object, -1, n.Position.x + 1, Vector2Int.left, Vector2.up * Mathf.Abs(vel.y));
							}
						}
					}
				}
				else if(vec.y == 1)	//Going up
				{
					int ny = curPos.y + Height - 1;
					if (dx <= 0)
					{
						for (int x = 0; x < Width; x++)
						{
							int nx = curPos.x + x;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx, ny - 1, x, Height - 1, n, n.Object, n.Position.y, -1, Vector2Int.up, Vector2.left * Mathf.Abs(vel.x));
							}
						}
					}
					else if(dx > 0)
					{
						for (int x = Width - 1; x > -1; x--)
						{
							int nx = curPos.x + x;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx, ny - 1, x, Height - 1, n, n.Object, n.Position.y, -1, Vector2Int.up, Vector2.right * Mathf.Abs(vel.x));
							}
						}
					}
				}
				else if(vec.y == -1)	//Going down
				{
					int ny = curPos.y;
					if (dx <= 0)
					{
						for (int x = 0; x < Width; x++)
						{
							int nx = curPos.x + x;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx, ny + 1, x, 0, n, n.Object, n.Position.y + 1, -1, Vector2Int.down, Vector2.left * Mathf.Abs(vel.x));
							}
						}
					}
					else if(dx > 0)
					{
						for (int x = Width - 1; x > -1; x--)
						{
							int nx = curPos.x + x;
							if (GameManager.GM.IsInBounds(nx, ny))
							{
								Node n = GameManager.GM.MapGrid[nx, ny];
								if (n.IsSolid(this, DisableObjectCollisions, CollisionIgnoringCondition))
									return CreateCollision(nx, ny + 1, x, 0, n, n.Object, n.Position.y + 1, -1, Vector2Int.down, Vector2.right * Mathf.Abs(vel.x));
							}
						}
					}
				}
			}

			return null;
		}

		public Collision CreateCollision(int hitPosX, int hitPosY, int hitPartX, int hitPartY, Node n, RectObject colobj, int hcol, int vcol, Vector2Int dir, Vector2 slide)
		{
			Vector2Int hitPos = new Vector2Int(hitPosX, hitPosY);
			Vector2Int hitPart = new Vector2Int(hitPartX, hitPartY);
			Collision col = new Collision(n, colobj, n.Position, hitPos, hitPart, slide);
			col.VCollision = vcol;
			col.HCollision = hcol;
			col.Direction = dir;
			return col;
		}



		public Vector2 GetPositionFromCollision(Vector2 pos, ref Vector2 vel, float dt, Collision col, ref string info)
		{
			Vector2 posFromCol = Vector2.zero;
			pos.x += col.HitPart.x;
			pos.y += col.HitPart.y;
			Vector2 dst = pos + (vel * dt);
			float dx = dst.x - pos.x;
			float dy = dst.y - pos.y;
			if (Mathf.Abs(dy) < 0.0001f)
				dy = 0;

			if(dx == 0)
			{
				
			}
			if(dy == 0)
			{

			}

			float a = dy / dx;
			float b = pos.y - a * pos.x;

			if(col.Direction == Vector2Int.right)
			{
				float nx = col.VCollision - Width - 0.01f;
				float ny = a * col.VCollision + b - col.HitPart.y;
				if (!GameManager.GM.IsNodeSolid(nx, ny, this))
					posFromCol = new Vector2(nx, ny);
				else
				{   //Stuck in between two nodes
					posFromCol = new Vector2(nx + 0.01f, ny);
					vel.x = 0;
					Velocity.x = 0;
				}
			}
			else if(col.Direction == Vector2Int.left)
			{
				float nx = col.VCollision + 0.01f;
				float ny = a * col.VCollision + b - col.HitPart.y;
				if (!GameManager.GM.IsNodeSolid(nx, ny, this))
					posFromCol = new Vector2(nx, ny);
				else
				{	//Stuck in between two nodes
					posFromCol = new Vector2(nx - 0.01f, ny);
					vel.x = 0;
					Velocity.x = 0;
				}
			}
			else if(col.Direction == Vector2Int.up)
			{
				float nx;
				if (Mathf.Abs(dx) <= 0.1f)
					nx = pos.x - col.HitPart.x;
				else
					nx = ((col.HCollision - b) / a) - col.HitPart.x;
				float ny = col.HCollision - Height - 0.01f;
				if(!GameManager.GM.IsNodeSolid(nx, ny, this))
					posFromCol = new Vector2(nx, ny);
				else
				{
					posFromCol = new Vector2(nx, ny + 0.01f);
					vel.y = 0;
					Velocity.y = 0;
				}
			}
			else if(col.Direction == Vector2Int.down)
			{
				float nx;
				if (Mathf.Abs(dx) <= 0.1f)
					nx = pos.x - col.HitPart.x;
				else
					nx = ((col.HCollision - b) / a) - col.HitPart.x;
				float ny = col.HCollision + 0.01f;
				if(!GameManager.GM.IsNodeSolid(nx, ny, this))
					posFromCol = new Vector2(nx, ny);
				else
				{
					posFromCol = new Vector2(nx, ny - 0.01f);
					vel.y = 0;
					Velocity.y = 0;
				}
			}
			//info += string.Format("Get new pos, pos {0}, vel {1}, dx {2}, dy {3}, a {4}, b {5}, newPos {6}, col {7}", pos, vel, dx, dy, a, b, posFromCol, col);
			//Debug.LogFormat("Get new pos, pos {0}, vel {1}, dx {2}, dy {3}, a {4}, b {5}, newPos {6}, col {7}", pos, vel, dx, dy, a, b, newPos, col);
			return posFromCol;
		}

		protected void AutoRotate()
		{
			if (!ObjectSprite)
				return;
			float ang = Mathf.Atan2(Velocity.y, Velocity.x);
			ang = ang * 180.0f / Mathf.PI;

			ObjectSprite.localEulerAngles = new Vector3(0, 0, 180 + ang);
		}

		protected virtual void OnBounce(Collision col, Vector2 pos, Vector2 oldvel, ref Vector2 vel, Vector2 collisionNormal)
		{
			if (!DisableObjectCollisions && col.CollisionObject)
			{
				if (!OnCollideWithObject(col.CollisionObject, ref Velocity))
				{
					Velocity = vel;	//Don't bounce if returned false
					return;
				}
			}
			Velocity = vel - vel * collisionNormal * (1.0f - Elasticity);
		}
		protected virtual void OnMove(Vector2 oldpos, Vector2 newpos)
		{
			GameManager.GM.Delay();
		}
		protected virtual void OnStop(Vector2 pos)
		{
			if (OnStopEvent != null)
				OnStopEvent(this);
		}
		protected virtual void OnStuck(Vector2 pos) { }
		protected virtual void OnDestroyObject()
		{
			if (OnDestroyEvent != null)
				OnDestroyEvent(this);
		}
		protected virtual void OnTick(Vector2 oldpos, Vector2 newpos)
		{
			if (OnTickEvent != null)
				OnTickEvent(this);
		}
		/// <summary>
		/// If returns false, it won't bounce of the object.
		/// </summary>
		protected virtual bool OnCollideWithObject(RectObject other, ref Vector2 vel)	
		{
			vel = vel / 2.0f;
			other.AddForce(-vel);
			return true;
		}



		////Not needed really
		//public Vector2 ModifyReboundVector(Vector2 oldRebound, Vector2Int hitPart)
		//{
		//	Vector2 newRebound = oldRebound;



		//	return newRebound;
		//}

		#region Debug Stuff

		protected void SpawnStuckPosObjects()
		{
			debugStuckPos = SpawnDebugObject("Stuck position", Position);
			debugStuckLastPos = SpawnDebugObject("Stuck last position", LastPosition);

			SetDebugSprite(debugStuckPos, Color.blue, Width, Height);
			SetDebugSprite(debugStuckLastPos, Color.cyan, Width, Height);

			if(debugStuckPos)
				Debug_DrawLine(Position, (Velocity * Delta), debugStuckPos.GetComponent<LineRenderer>());
			if(debugStuckLastPos)
				Debug_DrawLine(LastPosition, (LastVelocity * Delta), debugStuckLastPos.GetComponent<LineRenderer>());
		}

		protected GameObject SpawnDebugObject(string name, Vector2 pos, Color? lineStart = null)
		{
			if (!DebugObject)
				return null;
			GameObject go = new GameObject(name);
			go.transform.parent = DebugObject.transform;
			go.transform.position = pos / 100.0f;
			SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
			sr.sortingOrder = 100;
			if (lineStart != null)
			{
				LineRenderer lr = go.AddComponent<LineRenderer>();
				LineRenderer orig = GetComponent<LineRenderer>();
				CopyLineRenderer(lr, orig, (Color)lineStart);
			}
			return go;
		}

		protected void CopyLineRenderer(LineRenderer lr, LineRenderer orig, Color lineStart)
		{
			lr.sortingLayerID = orig.sortingLayerID;
			lr.sortingOrder = orig.sortingOrder;
			lr.startWidth = orig.startWidth;
			lr.endWidth = orig.endWidth;
			lr.startColor = lineStart;
			lr.endColor = orig.endColor;
			lr.material = orig.material;
		}

		protected void SetDebugSprite(GameObject go, Color col, int w, int h, float alpha = 1.0f)
		{
			if (!go)
				return;

			SpriteRenderer sr;
			sr = go.GetComponent<SpriteRenderer>();
			if (!sr)
				sr = go.GetComponentInChildren<SpriteRenderer>();
			Texture2D tex = new Texture2D(w, h);
			col.a = alpha;
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					tex.SetPixel(x, y, col);
				}
			}

			tex.Apply();
			Sprite spr = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero);
			sr.sprite = spr;
		}

		protected void SetDebugPos(GameObject go, Vector2 pos)
		{
			if (!go)
				return;
			go.transform.position = pos / 100.0f;
		}

		protected void SetDebugLine(GameObject go, Vector2 dest)
		{
			if (!go)
				return;
			Debug_DrawLine(go.transform.position, go.transform.position + new Vector3(dest.x, dest.y, 0), go.GetComponent<LineRenderer>());
		}

		protected void SetDebugPosAndLine(GameObject go, Vector2 pos, Vector2 dest)
		{
			SetDebugPos(go, pos);
			SetDebugLine(go, dest);
		}

		protected void Debug_DrawLine(Vector2 p1, Vector2 p2, LineRenderer lr = null)
		{
			Vector3 v1 = GridToWorldPos(p1);
			Vector3 v2 = GridToWorldPos(p2);
			if (lr == null)
				lr = GetComponent<LineRenderer>();
			if (!lr)
				return;
			lr.SetPosition(0, v1);
			lr.SetPosition(1, v2);
		}

		protected void Debug_DrawText(float value)
		{

		}

		#endregion
	}

}

#region Dump

//public void Move2(float dt)
//{
//	Vector2 dest = Position + (Velocity * dt);
//	float toMove = (Velocity * dt).magnitude;
//	if (dest.y < 0)
//		DestroyObject();
//	if (dest.y >= GameManager.GM.Height)    //Not checking lines in that case
//		return;

//	Node n = null;
//	string collision = "";
//	Vector2 vel = Velocity;
//	int collisions = 0;
//	while (n = CheckCollision(Position, Position + (vel * dt)))
//	{
//		if (collisions == 0)
//		{
//			collision += string.Format("===== new collision ===== \n");
//			collision += string.Format("initial Posision {0},{1}, Velocity {2} \n", Position.x, Position.y, Velocity);
//		}
//		collisions++;
//		Vector2 slope = GetSlope(Position, Position + (vel * dt));
//		Vector2Int hitPart = GetHitRectPart(n.Position, slope.x, slope.y, ref collision);
//		if(hitPart == n.Position)
//		{
//			Debug.LogErrorFormat("Failed to get hit part : \n{0}", collision);
//			return;
//		}
//		Vector2Int newPos = GetNewPosition(n.Position, hitPart);
//		//Node hit = GetHitNode(GetCurrentNode(), n);
//		//if (hit.Solid)
//		//	Debug.LogError("Hit node is solid");
//		Vector2 reb = GameManager.GM.GetReboundVector(Position + hitPart, n.Position);
//		float moved = Vector2.Distance(Position, (Vector2)newPos);
//		//float moved = GetMovedDistance(Position, n.Position, slope, GetCurrentPos() + hitPart);
//		vel = (vel - 2 * (Vector2.Dot(vel, reb)) * reb);
//		collision += string.Format("vel(1):{0}", vel);
//		vel *= Elasticity;
//		if (moved < toMove && moved > 0)
//		{
//			vel = vel / toMove * (toMove - moved);

//			collision += string.Format(", vel(2):{0}, toMove(1):{1}, ", vel, toMove);
//			toMove = (vel * dt).magnitude;
//			Position = newPos;
//			collision += string.Format("n:{0}, hitPart:{1}, reb:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, slope:{8}, newPos:{9} \n", n, hitPart, reb, moved, vel, Velocity, toMove, Position, slope, newPos);

//		}
//		else if(moved == 0)
//		{
//			Position = newPos;
//			collision += string.Format("n:{0}, hitPart:{1}, reb:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, slope:{8}, newPos:{9} \n", n, hitPart, reb, moved, vel, Velocity, toMove, Position, slope, newPos);
//		}
//		else
//		{
//			toMove = 0;
//			Position = newPos;
//			collision += string.Format("n:{0}, hitPart:{1}, reb:{2}, moved:{3}, vel(2):{4}, Velocity:{5}, toMove(2):{6}, Position:{7}, slope:{8}, newPos:{9} \n", n, hitPart, reb, moved, vel, Velocity, toMove, Position, slope, newPos);

//			break;
//		}
//		if (Position == newPos)
//			break;
//		//if (hitPart.x == 0 && hitPart.y == 0)
//		//	break;
//		//if (Position == hit.Position)
//		//	break;
//		//Position = hit.Position;
//		//Position = newPos;

//		if (collisions > 5)
//		{
//			Debug.LogError("Failed collision : " + collision);
//			break;
//		}
//	}
//	if (!string.IsNullOrEmpty(collision))
//	{

//		Debug.Log(collision);

//	}
//	if (collisions > 0)
//	{
//		dest = Position + (vel * dt);
//		Velocity = vel.normalized * Velocity.magnitude * Mathf.Pow(Elasticity, collisions);
//		if (!GameManager.GM.MapGrid[(int)dest.x, (int)dest.y].Solid)
//			Position = dest;
//	}
//	else
//		Position += Velocity * dt;
//	SetPosition(Position);

//	Debug_DrawLine(Position, Position + Velocity * dt);


//}

//public override Node CheckCollision(Vector2 pos, Vector2 dest)
//{
//	//Debug.Log("Rect collision");
//	int AX = (int)pos.x;
//	int AY = (int)pos.y;
//	int BX = (int)dest.x;
//	int BY = (int)dest.y;
//	if (AX == BX && AY == BY)
//		return null;
//	float dx = BX - AX; //dx < 0 : going left
//						//dx > 0 : going right
//	float dy = BY - AY; //dy < 0 : going down
//						//dy > 0 : going up

//	//Debug.LogFormat("Checking rect col. AX {0}, AY {1}, BX {2}, BY {3}, dx {4}, dy {5}", AX, AY, BX, BY, dx, dy);
//	if (dx == 0)
//	{
//		if(dy > 0)
//			for (int y = 0; y < dy + Height; y++)
//				for (int x = 0; x < Width; x++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		if(dy < 0)
//			for (int y = 0; y > dy - 1; y--)
//				for (int x = 0; x < Width; x++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//	}
//	if(dy == 0)
//	{
//		if(dx < 0)
//			for(int x = 0; x > dx - 1; x--)
//				for(int y = 0; y < Height; y++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		if(dx > 0)
//			for(int x = 0; x < dx + Width; x++)
//				for (int y = 0; y < Height; y++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//	}

//	if (dx > 0 && dy > 0)
//	{
//		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
//		{
//			for (int x = 0; x < dx + Width; x++)
//				for (int y = 0; y < dy + Height; y++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//		else if (Mathf.Abs(dy) >= Mathf.Abs(dx))
//		{
//			for (int y = 0; y < dy + Width; y++)
//				for (int x = 0; x < dx + Height; x++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//	}
//	else if (dx > 0 && dy < 0)
//	{
//		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
//		{
//			for (int x = 0; x < dx + Width; x++)
//				for (int y = Height - 1; y > dy - 1; y--)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//		else if (Mathf.Abs(dy) >= Mathf.Abs(dx))
//		{
//			for (int y = Height - 1; y > dy - 1; y--)
//				for (int x = 0; x < dx + Width; x++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//	}
//	else if (dx < 0 && dy > 0)
//	{
//		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
//		{
//			for (int x = Width - 1; x > dx - 1; x--)
//				for (int y = 0; y < dy + Height; y++)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//		else if (Mathf.Abs(dy) >= Mathf.Abs(dx))
//		{
//			for (int y = 0; y < dy + Height; y++)
//				for (int x = Width - 1; x > dx - 1; x--)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//	}
//	else if (dx < 0 && dy < 0)
//	{
//		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
//		{
//			for (int x = Width - 1; x > dx - 1; x--)
//				for (int y = Height - 1; y > dy - 1; y--)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];

//		}
//		else if (Mathf.Abs(dy) >= Mathf.Abs(dx))
//		{
//			for (int y = Height - 1; y > dy - 1; y--)
//				for (int x = Width - 1; x > dx - 1; x--)
//					if (CheckIfNodeInLineSolid(x, y, dx, dy, AX, AY))
//						return GameManager.GM.MapGrid[AX + x, AY + y];
//		}
//	}

//	return null;
//}

//public bool CheckIfNodeInLineSolid(int x, int y, float dx, float dy, int AX, int AY)
//{

//	if (AX + x >= GameManager.GM.Width || AY + y >= GameManager.GM.Height || AX + x < 0 || AY + y < 0)
//		return false;
//	if (x < Width && y < Height && x > -1 && y > -1)
//		return false;




//	float ax = (dy * x / dx);
//	float aw = Mathf.Abs(dy * Width / dx);
//	//Debug.LogFormat("Checking {0}/{1}, dx {2}, dy {3}, AX {4}, AY {5}, ax {6}, aw {7}", x, y, dx, dy, AX, AY, ax, aw);
//	if (dx == 0 || dy == 0)
//	{
//		return GameManager.GM.MapGrid[AX + x, AY + y].Solid;
//	}

//	if (dy / dx > 0)
//	{
//		if (y > (ax - aw - 1) && y < (ax + Height + 1))
//		{
//			return GameManager.GM.MapGrid[AX + x, AY + y].Solid;
//		}
//	}
//	else
//	{
//		if(y > (ax - 1) && y < (ax + Height + aw + 1))
//		{
//			return GameManager.GM.MapGrid[AX + x, AY + y].Solid;
//		}
//	}
//	return false;
//}

//public Vector2Int GetHitRectPart(Vector2Int col, float dx, float dy, ref string info)	//Nie dziala - nie znajduje pozycji dla skrajow
//{
//	if (dx == 0 && dy == 0)
//		return col;

//	Vector2Int curPos = GetCurrentPos();

//	if(dx == 0)
//	{
//		if(dy > 0)
//		{
//			return new Vector2Int(col.x - curPos.x, Height - 1);
//		}
//		if(dy < 0)
//		{
//			return new Vector2Int(col.x - curPos.x, 0);
//		}
//	}
//	if(dy == 0)
//	{
//		if(dx > 0)
//		{
//			return new Vector2Int(Width - 1, col.y - curPos.y);
//		}
//		if(dx < 0)
//		{
//			return new Vector2Int(0, col.y - curPos.y);
//		}
//	}

//	int colX = 0;
//	int colY = 0;

//	colX = col.x - curPos.x;
//	colY = col.y - curPos.y;

//	float ax = (dy * colX / dx);
//	float b = colY - ax;	

//	if(dx > 0 && dy > 0)
//	{
//		for (int x = Width - 1; x > -1; x--)
//		{
//			for (int y = Height - 1; y > -1; y--)
//			{
//				//Debug.LogFormat("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}", x, y, x, y, b, colX, colY, ax, dx, dy);
//				info += string.Format("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}\n", x, y, x, y, b, colX, colY, ax, dx, dy);

//				float f = (dy * x / dx) + b;
//				if(y >= f - Mathf.Abs(dy / dx) && y <= f + Mathf.Abs(dy / dx) + 1)
//				{
//					return new Vector2Int(x, y);
//				}
//			}
//		}
//	}
//	else if(dx > 0 && dy < 0)
//	{
//		for (int x = Width - 1; x > -1; x--)
//		{
//			for (int y = 0; y < Height; y++)
//			{
//				//Debug.LogFormat("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}", x, y, x, y, b, colX, colY, ax, dx, dy);
//				info += string.Format("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}\n", x, y, x, y, b, colX, colY, ax, dx, dy);
//				float f = (dy * x / dx) + b;
//				if (y >= f - Mathf.Abs(dy / dx) && y <= f + Mathf.Abs(dy / dx) + 1)
//				{
//					return new Vector2Int(x, y);
//				}
//			}
//		}
//	}
//	else if (dx < 0 && dy > 0)
//	{
//		for (int x = 0; x < Width; x++)
//		{
//			for (int y = Height - 1; y > -1; y--)
//			{
//				//Debug.LogFormat("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}", x, y, x, y, b, colX, colY, ax, dx, dy);
//				info += string.Format("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}\n", x, y, x, y, b, colX, colY, ax, dx, dy);
//				float f = (dy * x / dx) + b;
//				if (y >= f - Mathf.Abs(dy / dx) && y <= f + Mathf.Abs(dy / dx) + 1)
//				{
//					return new Vector2Int(x, y);
//				}
//			}
//		}
//	}
//	else if (dx < 0 && dy < 0)
//	{
//		for (int x = 0; x < Width; x++)
//		{
//			for (int y = 0; y < Height; y++)
//			{
//				//Debug.LogFormat("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}", x, y, x, y, b, colX, colY, ax, dx, dy);
//				info += string.Format("Checking hit, x{0} y{1} dx {8} dy {9} x {2} y {3} ax {7} b {4} colX {5} colY {6}\n", x, y, x, y, b, colX, colY, ax, dx, dy);
//				float f = (dy * x / dx) + b;
//				if (y >= f - Mathf.Abs(dy / dx) && y <= f + Mathf.Abs(dy / dx) + 1)
//				{
//					return new Vector2Int(x, y);
//				}
//			}
//		}
//	}

//	return col;
//}

//public Vector2Int GetSlope(Vector2 pos, Vector2 dest)
//{
//	int AX = (int)pos.x;
//	int AY = (int)pos.y;
//	int BX = (int)dest.x;
//	int BY = (int)dest.y;

//	if (AX == BX && AY == BY)
//		return Vector2Int.zero;
//	int dx = BX - AX; //dx < 0 : going left
//						//dx > 0 : going right
//	int dy = BY - AY; //dy < 0 : going down
//						//dy > 0 : going up
//	Vector2Int slope = new Vector2Int(dx, dy);
//	Debug.Log("Slope " + slope);
//	return slope;								
//}

//public Vector2 GetSlope(Vector2 pos, Vector2 dest)
//{
//	return new Vector2(dest.x - pos.x, dest.y - pos.y);
//}
#endregion