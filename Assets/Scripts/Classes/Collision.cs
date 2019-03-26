using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WormsClone
{
	public class Collision 
	{
		public Node CollisionNode;
		public RectObject CollisionObject;
		public Vector2Int CollisionPos;
		public Vector2Int HitPos;
		public Vector2Int HitPart;
		public Vector2Int Direction;
		public Vector2 Slide;
		public int HCollision;
		public int VCollision;

		public Collision(Node collisionNode, RectObject collisionObject, Vector2Int collisionPos, Vector2Int hitPos, Vector2Int hitPart, Vector2 slide)
		{
			CollisionNode = collisionNode;
			CollisionObject = collisionObject;
			CollisionPos = collisionPos;
			HitPos = hitPos;
			HitPart = hitPart;
			HCollision = -1;
			VCollision = -1;
			Direction = Vector2Int.zero;
			Slide = slide;
		
		}

		//Does not work perfectly, when you are in integer-coordinates, and go down-left, with vel.x faster than vel.y (returns still y as first jump, should return x) no worries tho.
		public static Vector2Int[] GetCollisionNodes(Vector2 s, Vector2 e, ref string info)
		{			
			
			float dx = e.x - s.x;
			float dy = e.y - s.y;
			//Debug.LogFormat("Get col nodes, s : {0}, e : {1}", s, e);
			//info += string.Format("Get col nodes, s : {0}, e : {1}\n", s, e);
			if (dx == 0 && dy == 0)
			{
				Vector2Int[] path = new Vector2Int[1];
				path[0] = new Vector2Int((int)s.x, (int)s.y);
				return path;
			}
			if(dx == 0)
			{
				//int count = Mathf.Abs(Mathf.FloorToInt(dy)) + 1;
				int count = s.y > e.y ? 
					Mathf.FloorToInt(e.y) - ((Mathf.CeilToInt(s.y) == (int)s.y) ? (int)s.y + 1 : Mathf.CeilToInt(s.y)) : 
					((Mathf.CeilToInt(e.y) == (int)e.y) ? (int)e.y + 1 : Mathf.CeilToInt(e.y)) - Mathf.FloorToInt(s.y);
				count = Mathf.Abs(count);
				Vector2Int[] path = new Vector2Int[count];
				for (int y = 0; y < count; y++)
				{
					int ny = y;
					if (dy < 0)
						ny *= -1;
					Vector2Int pos = new Vector2Int((int)s.x, (int)s.y + ny);
					path[y] = pos;
				}
				return path;
			}
			float a = dy / dx;
			float b = s.y - a * s.x;
			int count_x = s.x > e.x ? 
				((Mathf.CeilToInt(s.x) == (int)s.x) ? Mathf.FloorToInt(e.x) - ((int)s.x + 1) : Mathf.FloorToInt(e.x) - Mathf.CeilToInt(s.x)) : 
				((Mathf.CeilToInt(e.x) == (int)e.x) ? ((int)e.x + 1) - Mathf.FloorToInt(s.x) : Mathf.CeilToInt(e.x) - Mathf.FloorToInt(s.x));
			count_x = Mathf.Abs(count_x);
			List<Vector2Int> pathList = new List<Vector2Int>();
			//Debug.LogFormat("Count x : {0}, ceiltoint(sx) : {1}, floortiint(ex) : {2}, eq 2 : {3}, (int)s.x + 1 : {4},result:{5}", count_x, Mathf.CeilToInt(s.x), Mathf.FloorToInt(e.x), (Mathf.CeilToInt(s.x) == (int)s.x), (int)s.x + 1, Mathf.FloorToInt(e.x) - ((int)s.x + 1));
			//info += string.Format("Count x : {0}, ceiltoint(sx) : {1}, floortiint(ex) : {2}, eq 2 : {3}, (int)s.x + 1 : {4},result:{5}\n", count_x, Mathf.CeilToInt(s.x), Mathf.FloorToInt(e.x), (Mathf.CeilToInt(s.x) == (int)s.x), (int)s.x + 1, Mathf.FloorToInt(e.x) - ((int)s.x + 1));

			float current_y = s.y;
			//if (s.y > e.y)  //going down
			//{
			//	current_y = Mathf.CeilToInt(s.y) == (int)s.y ? s.y + 1 : s.y;
			//}


			for (int x = 0; x < count_x; x++)
			{
				int nx = x;
				if (dx < 0)
					nx *= -1;

				//s.x % 1 == 0 to to samo co Mathf.CeilToInt(s.x) == (int)s.x ?
				int next_x = s.x > e.x ? 
					s.x % 1 == 0 ? (int)s.x - x - 1 : Mathf.FloorToInt(s.x - x) : 
					s.x % 1 == 0 ? (int)s.x + x + 1 : Mathf.CeilToInt(s.x + x);

				if (Mathf.CeilToInt(s.x) == (int)s.x)
					next_x++;

				float next_y;

				if (x < count_x - 1)
				{
					next_y = a * next_x + b;
				}
				else
				{
					next_y = Mathf.CeilToInt(e.y) == (int)e.y ? e.y + 1 : e.y;
				}
				int start_y = s.y >= e.y ?
					Mathf.CeilToInt(current_y) == (int)current_y ? (int)current_y + 1 : Mathf.CeilToInt(current_y) : Mathf.FloorToInt(current_y);
					//Mathf.CeilToInt(current_y) : Mathf.FloorToInt(current_y);
				int end_y = s.y > e.y ? Mathf.FloorToInt(next_y) : Mathf.CeilToInt(next_y);
				//int count_y = s.y != e.y ? Mathf.Abs(end_y - start_y) : 1; 
				int count_y = start_y != end_y ? Mathf.Abs(end_y - start_y) : 1;
				//Debug.LogFormat("x : {0}, next_x : {1}, current_y : {2}, next_y : {3}, start_y : {4}, end_y : {5}, count_y : {6}, a : {7}, b :{8}", x, next_x, current_y, next_y, start_y, end_y, count_y, a, b);
				//info += string.Format("x : {0}, next_x : {1}, current_y : {2}, next_y : {3}, start_y : {4}, end_y : {5}, count_y : {6}, a : {7}, b :{8}\n", x, next_x, current_y, next_y, start_y, end_y, count_y, a, b);

				for (int y = 0; y < count_y; y++)
				{
					int ny = y;
					if (dy < 0)
						ny *= -1;
					
					Vector2Int pos = new Vector2Int((int)s.x + nx, (int)current_y + ny);
					pathList.Add(pos);
				}
				current_y = next_y;
			}

			return pathList.ToArray();
		} 

		public static Vector2Int[] ConvertToPath(Vector2Int[] colNodes, bool favorY = true)
		{
			if (colNodes == null || colNodes.Length == 0)
				return null;
			if(colNodes.Length == 1)
			{
				Vector2Int[] single = new Vector2Int[1];
				single[0] = Vector2Int.zero;
				return single;
			}
			//Vector2Int[] path = new Vector2Int[colNodes.Length - 1];
			List<Vector2Int> path = new List<Vector2Int>();
			for (int i = 1; i < colNodes.Length; i++)
			{
				Vector2Int current = colNodes[i - 1];
				Vector2Int next = colNodes[i];

				Vector2Int dir = next - current;
				if(dir.x != 0 && dir.y != 0)
				{
					Vector2Int dirY = new Vector2Int(0, dir.y);
					Vector2Int dirX = new Vector2Int(dir.x, 0);
					if (favorY)
					{
						path.Add(dirY);
						path.Add(dirX);
					}
					else
					{
						path.Add(dirX);
						path.Add(dirY);
					}
				}
				else
					path.Add(dir);
			}
			return path.ToArray();
		}

		public static Vector2Int[] GetCollisionPath(Vector2Int vel)	//Niewystarczajaco dokladne
		{
			int count_x = Mathf.Abs(vel.x);
			int count_y = Mathf.Abs(vel.y);
			int count = count_x + count_y;
			//Debug.LogFormat("Get col path, vel : {0}, count_x : {1}, count_y : {2}", vel, count_x, count_y);
			List<Vector2Int> pList = new List<Vector2Int>();
			if (count == 0)
				return pList.ToArray();
			if (count_x == 0)
			{
				for (int i = 0; i < count_y; i++)
				{
					pList.Add(new Vector2Int(0, vel.y / Mathf.Abs(vel.y)));
				}
				return pList.ToArray();
			}
			if(count_y == 0)
			{
				for (int i = 0; i < count_x; i++)
				{
					pList.Add(new Vector2Int(vel.x / Mathf.Abs(vel.x), 0));
				}
				return pList.ToArray();
			}
			int x = vel.x / Mathf.Abs(vel.x);
			int y = vel.y / Mathf.Abs(vel.y);
			float add = 0;
			float div = 0;
			if(count_x > count_y)
			{
				div = (float)count_y / count_x;
				while (count > 0)
				{
					add += div;
					if(add >= 1)
					{
						pList.Add(new Vector2Int(0, y));
						add -= 1.0f;
						count--;
					}
					pList.Add(new Vector2Int(x, 0));
					count--;
				}
			}
			else if(count_y > count_x)
			{
				div = (float)count_x / count_y;
				while (count > 0)
				{
					add += div;
					if(add >= 1)
					{
						pList.Add(new Vector2Int(x, 0));
						add -= 1.0f;
						count--;
					}
					pList.Add(new Vector2Int(0, y));
					count--;
				}
			}
			else if(count_x == count_y)
			{
				while(count > 0)
				{
					pList.Add(new Vector2Int(0, y));
					pList.Add(new Vector2Int(x, 0));
					count--;
					count--;
				}
			}

			return pList.ToArray();
		}

		public static Vector2Int[] GetCollisionPath(Vector2 vel)
		{
			int x = 0;
			int y = 0;
			if (vel.x >= 0)
				x = Mathf.CeilToInt(vel.x);
			else if (vel.x < 0)
				x = Mathf.FloorToInt(vel.x);
			if (vel.y >= 0)
				y = Mathf.CeilToInt(vel.y);
			else if (vel.y < 0)
				y = Mathf.FloorToInt(vel.y);
			return GetCollisionPath(new Vector2Int(x, y));
		}

		public override string ToString()
		{
			string col = "";
			if (HCollision != -1)
				col += "HCollision " + HCollision;
			if (VCollision != -1)
				col += "VCollision " + VCollision;

			return "Collision, node : " + CollisionNode.ToString() + ", obj : " + CollisionObject + ", hit pos : " + HitPos.ToString() + ", hit part : " + HitPart.ToString() + ", " + col + ", Direction " + Direction;
		}

		public static implicit operator bool(Collision c)
		{
			if (c == null)
				return false;
			return true;
		}
	}
}
