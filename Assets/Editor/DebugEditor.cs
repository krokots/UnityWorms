using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WormsClone
{
	[CustomEditor(typeof(DebugManager))]
	public class DebugEditor : Editor
	{
		DebugState debugState;

		Vector2Int posA;
		Vector2Int posB;

		Vector2 force;

		Vector3 lineA;
		Vector3 lineB;

		Vector2Int colPath;

		List<Vector2Int> points = new List<Vector2Int>();
		List<Vector2Int> collisions = new List<Vector2Int>();
		List<Vector3[]> lines = new List<Vector3[]>();
		List<Vector3[]> rebounds = new List<Vector3[]>();

		DebugEditor()
		{
			SceneView.onSceneGUIDelegate += UpdateView;
			
		}

		

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			if (GUILayout.Button("Reset"))
				ResetDebugEditor();
			if (GUILayout.Button("Get mouse pos A"))
				debugState = DebugState.GetMousePosA;
			posA = EditorGUILayout.Vector2IntField("Pos A", posA);
			if (GUILayout.Button("Get mouse pos B"))
				debugState = DebugState.GetMousePosB;
			posB = EditorGUILayout.Vector2IntField("Pos B", posB);
			if (GUILayout.Button("Get rebound vector"))
				GetReboundVector();
			//if (GUILayout.Button("Check collision"))
			//	CheckCollision();
			if (GUILayout.Button("Get rect collision"))
				CheckRectCollision();
			//if (GUILayout.Button("Move"))
			//	Move();
			if (GUILayout.Button("Get destination"))
				GetDestination();
			force = EditorGUILayout.Vector2Field("Force", force);
			if (GUILayout.Button("Add force"))
				AddForce();
			if (GUILayout.Button("Draw"))
				debugState = DebugState.Draw;
			colPath = EditorGUILayout.Vector2IntField("Collision path", colPath);
			if (GUILayout.Button("Get collision path"))
				GetCollisionPath();
			if (GUILayout.Button("Get collision path 2"))
				GetCollisionPath2();
			if (GUILayout.Button("Get rect collision path"))
				GetRectCollisionPath();
			if (GUILayout.Button("Get collision nodes from rect pos and vel"))
				GetCollisionNodes();
			if (GUILayout.Button("Check node objects"))
				CheckNodeForObjects();
		}

		private void UpdateView(SceneView sceneView)
		{
			if (Event.current != null)
			{
				if (Event.current.type == EventType.MouseDown)
				{
					SetFunction(debugState);
				}
			}
			DrawPoint(posA, Color.white);
			DrawPoint(posB, Color.white);
			foreach (var point in points)
				DrawPoint(point, Color.red);
			foreach (var col in collisions)
				DrawPoint(col, Color.yellow);
			foreach (var line in lines)
				DrawLine(line, Color.magenta);
			foreach (var reb in rebounds)
				DrawLine(reb, Color.magenta, true);
			DrawLine(lineA, lineB);
		}

		private void DrawLine(Vector3 a, Vector3 b)
		{
			if (a == Vector3.zero || b == Vector3.zero)
				return;
			Handles.color = Color.green;
			Handles.DrawLine(a, b);
		}

		private void DrawLine(Vector3[] line, Color col)
		{
			
			
			Vector3 a = new Vector3(line[0].x / 100.0f + 0.005f, line[0].y / 100.0f + 0.005f);
			Vector3 b = new Vector3(line[1].x / 100.0f + 0.005f, line[1].y / 100.0f + 0.005f);
			Handles.DrawLine(line[0], line[1]);
		}

		private void DrawLine(Vector3[] line, Color col, bool reb)
		{
			Handles.color = col;
			Handles.DrawLine(line[0], line[1]);
		}

		private void DrawPoint(Vector2Int p, Color col)
		{
			Handles.color = col;
			if (p == Vector2Int.zero)
				return;

			Vector3 a = new Vector3(p.x / 100.0f + 0.001f, p.y / 100.0f + 0.001f, 0);
			Vector3 b = new Vector3(p.x / 100.0f + 0.001f, p.y / 100.0f + 0.009f, 0);
			Vector3 c = new Vector3(p.x / 100.0f + 0.009f, p.y / 100.0f + 0.009f, 0);
			Vector3 d = new Vector3(p.x / 100.0f + 0.009f, p.y / 100.0f + 0.001f, 0);
			Handles.DrawLine(a, b);
			Handles.DrawLine(b, c);
			Handles.DrawLine(c, d);
			Handles.DrawLine(d, a);
		}

		private void SetFunction(DebugState st)
		{
			switch (st)
			{
				case DebugState.Null:
					break;
				case DebugState.GetMousePosA:
					posA = GetMousePos();
					break;
				case DebugState.GetMousePosB:
					posB = GetMousePos();
					break;
				//case DebugState.Draw:
				//	GameManager.GM.FillNode(GetMousePos(), Color.blue);
				//	break;
				default:
					break;
			}
			//ResetState();
		}

		private void ResetDebugEditor()
		{
			ResetState();

			posA = Vector2Int.zero;
			posB = Vector2Int.zero;

			lineA = Vector3.zero;
			lineB = Vector3.zero;

			points.Clear();
			points = new List<Vector2Int>();
			collisions.Clear();
			collisions = new List<Vector2Int>();
		}

		private Vector2Int GetMousePos()
		{
			Vector3 mousePos = Event.current.mousePosition;

			mousePos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - mousePos.y;
			mousePos = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(mousePos);
			mousePos *= 100.0f;
			return new Vector2Int((int)mousePos.x, (int)mousePos.y);
		}

		private void GetReboundVector()
		{
			if (Vector2Int.Distance(posA, posB) > 1.5f)
			{
				Debug.Log("Points are not touching");
				return;
			}
			GameManager gm = ((DebugManager)target).GM;
			Vector2 reb = gm.GetNormalVector(posA, posB);
			Debug.Log(reb);
			lineA = new Vector3((posA.x / 100.0f + 0.005f), (posA.y / 100.0f + 0.005f), 0);
			lineB = new Vector3((posA.x / 100.0f + 0.005f) + (reb.x / 100.0f), (posA.y / 100.0f + 0.005f) + (reb.y / 100.0f), 0);
		}

		private void AddReboundVector(Vector2Int pos, Vector2 reb)
		{
			Vector3 v1 = new Vector3((pos.x / 100.0f + 0.005f), (pos.y / 100.0f + 0.005f), 0);
			Vector3 v2 = new Vector3((pos.x / 100.0f + 0.005f) + (reb.x / 100.0f), (pos.y / 100.0f + 0.005f) + (reb.y / 100.0f), 0);
			rebounds.Add(new Vector3[2] { v1, v2 });
		}

		private void GetCollisionPath()
		{
			Vector2Int[] collisionPath = Collision.GetCollisionPath(colPath);
			string colstr = "";
			foreach (var vec in collisionPath)
			{
				if (vec.x != 0)
					colstr += "x";
				else if (vec.y != 0)
					colstr += "y";
			}
			Debug.Log(colstr);
		}

		private void GetCollisionPath2()
		{
			if (posA == Vector2Int.zero || posB == Vector2Int.zero)
				return;
			string colstr = "";
			Vector2Int[] collisionNodes = Collision.GetCollisionNodes(posA, posB, ref colstr);
			Vector2Int[] collisionPath = Collision.ConvertToPath(collisionNodes);
			Vector2Int[] collisionPathOld = Collision.GetCollisionPath(posB - posA);
			collisions.Clear();
			foreach (var col in collisionNodes)
			{
				//Debug.Log(col);
				collisions.Add(col);
			}

			
			foreach (var vec in collisionPath)
			{
				if (vec.x != 0)
					colstr += "x";
				else if (vec.y != 0)
					colstr += "y";
			}
			string oldcol = "";
			foreach (var vec in collisionPathOld)
			{
				if (vec.x != 0)
					oldcol += "x";
				else if (vec.y != 0)
					oldcol += "y";
			}
			Debug.Log(colstr);
			Debug.Log(oldcol);
		}

		private void GetCollisionNodes()
		{
			DebugManager dm = (DebugManager)target;
			if (!dm.RectObj)
				return;

			points.Clear();
			collisions.Clear();

			Vector2 pos = dm.RectObj.Position;
			Vector2 vel = dm.RectObj.Velocity * dm.RectObj.Delta;
			Vector2 dest = dm.RectObj.Position + vel;
			string info = "";
			Vector2Int[] colNodes = Collision.GetCollisionNodes(pos, dest, ref info);
			foreach (var colN in colNodes)
			{
				Debug.Log(colN);
				collisions.Add(colN);
			}
		}

		private void GetRectCollisionPath()
		{
			DebugManager dm = (DebugManager)target;
			if (!dm.RectObj)
				return;

			points.Clear();
			collisions.Clear();

			Vector2 pos = dm.RectObj.Position;
			Vector2 vel = dm.RectObj.Velocity * dm.RectObj.Delta;
			Vector2 dst = dm.RectObj.Position + force;

			Debug.LogFormat("Pos : {0}, vel : {1}, dst : {2}", pos, force, dst);
			string info = "";
			Vector2Int[] colNodes = Collision.GetCollisionNodes(pos, dst, ref info);
			Vector2Int[] colPath = Collision.ConvertToPath(colNodes);

			foreach (var path in colPath)
			{
				Debug.Log(path);
			}
		}

		private void CheckRectCollision()
		{
			DebugManager dm = (DebugManager)target;
			if (!dm.RectObj)
				return;

			points.Clear();
			collisions.Clear();

			Vector2 pos = dm.RectObj.Position;
			Vector2 vel = dm.RectObj.Velocity;
			float dt = dm.RectObj.Delta;
			Vector2 dest = dm.RectObj.Position + vel;

			string info = "";

			Collision col = dm.RectObj.CheckCollision(pos, vel * dt, ref info);
			Debug.Log(col);
			if(col)
			{
				collisions.Add(col.CollisionPos);
				points.Add(col.HitPos);
				Vector2 newPos = dm.RectObj.GetPositionFromCollision(pos, ref vel, dt, col, ref info);
				Debug.LogFormat("newPos : {0}", newPos);
				//points.Add(newPos);
			}
		}
		private void CheckNodeForObjects()
		{
			points.Clear();
			for (int x = 0; x < GameManager.GM.Width; x++)
			{
				for (int y = 0; y < GameManager.GM.Height; y++)
				{
					if (GameManager.GM.MapGrid[x, y].Object)
					{
						Debug.Log(GameManager.GM.MapGrid[x, y]);
						points.Add(GameManager.GM.MapGrid[x, y].Position);
					}
				}
			}
		}
		private void AddForce()
		{
			DebugManager dm = (DebugManager)target;
			if (!dm.RectObj)
				return;

			dm.RectObj.AddForce(force);
		}


		private void GetDestination()
		{
			points.Clear();
			DebugManager dm = (DebugManager)target;
			if (dm.RectObj == null)
				return;
			int dx = 0, dy = 0;

			if(dm.RectObj != null)
			{
				dx = (int)(dm.RectObj.Position.x + dm.RectObj.Velocity.x * dm.RectObj.Delta);
				dy = (int)(dm.RectObj.Position.y + dm.RectObj.Velocity.y * dm.RectObj.Delta);

			}
			Debug.Log(dm.RectObj.Delta);
			Debug.Log(dm.RectObj.Velocity.y * dm.RectObj.Delta);
			Debug.Log(dm.RectObj);
			points.Add(new Vector2Int(dx, dy));
			Debug.Log(dx + ", " + dy);
		}

		private void ResetState()
		{
			debugState = DebugState.Null;
		}
	}

	public enum DebugState
	{
		Null,
		GetMousePosA,
		GetMousePosB,
		Draw,
		Erase
	}
}