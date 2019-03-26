using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class CameraManager : MonoBehaviour
	{
		[Header("Camera properties")]
		public float MouseSpeed = 25.0f;
		public float MouseDrag = 0.85f;
		[Range(0.25f, 1.0f)]
		public float VerticalDec = 0.75f;
		public bool MoveCameraOn = true;

		private float maxX;
		private float maxY;

		public float MouseX;
		public float MouseY;
		public Vector3 Target;
		private float oldX;
		private float oldY;

		private float delta;

		[Header("Map info")]

		public MapGenerator MapGen;
		//public Transform CameraLimiter;
		public Vector3 TopRight;
		public Vector3 BottomLeft;

		public GameObject[] BackgroundLayers = new GameObject[2];
		GameObject[] WaterLayers = new GameObject[6];

		public bool initialized;

		void Start()
		{
			if(MapGen)
			{
				maxX = MapGen.Width;
				maxY = MapGen.Height;
				//VerticalDec = maxY / maxX;
			}
		}



		void Update()
		{
			if (!initialized)
				return;

			delta = Time.deltaTime;

			if (Target != Vector3.zero)
				CenterOnTarget();
			else if (MoveCameraOn)
				MoveCamera();

			if (MouseX != 0 && MouseY != 0)
			{
				//Debug.Log("Clearing camera target");
				Target = Vector3.zero;
			}
		}

		public void TurnOffCamera()
		{
			MoveCameraOn = false;
			oldX = 0;
			oldY = 0;
			MouseX = 0;
			MouseY = 0;
		}

		public void CenterOnWorm(WormObject worm)
		{
			Target = worm.transform.position;
		}

		public void CenterOnTarget()
		{
			float dx = Target.x - Camera.main.transform.position.x;
			float dy = Target.y - Camera.main.transform.position.y;

			Vector3 mouseMove = new Vector3(dx, dy, 0);
			float dist = mouseMove.magnitude;
			if (dist > 1.0f)
				dist = 1.0f;
			mouseMove = mouseMove.normalized * delta * dist * MouseSpeed;
			mouseMove = SmoothenCameraMovement(mouseMove);
			ApplyCameraMove(mouseMove);
		}

		private void MoveCamera()
		{
			MouseY *= VerticalDec;
			float nx = Mathf.Abs(MouseX) > Mathf.Abs(oldX) ? MouseX : oldX;
			float ny = Mathf.Abs(MouseY) > Mathf.Abs(oldY) ? MouseY : oldY;
			Vector3 mouseMove = new Vector3(nx, ny, 0) * delta * MouseSpeed;
			oldX = nx * MouseDrag;
			oldY = ny * MouseDrag;
			//
			//oldMouseMove = mouseMove;
			if (Mathf.Abs(oldX) < 0.01f) oldX = 0;
			if (Mathf.Abs(oldY) < 0.01f) oldY = 0;

			mouseMove = SmoothenCameraMovement(mouseMove);
			ApplyCameraMove(mouseMove);
		}

		private Vector3 SmoothenCameraMovement(Vector3 mouseMove)
		{
			Vector3 distToLB = Camera.main.WorldToViewportPoint(BottomLeft - mouseMove);
			Vector3 distToRU = Camera.main.WorldToViewportPoint(TopRight - mouseMove);

			//Debug.LogFormat("LB {0}, RU {1}", distToLB, distToRU);


			if (distToLB.y > -1.0f && mouseMove.y < 0.0f)
				mouseMove.y *= (distToLB.y * -1.0f);
			if (distToRU.y < 2.0f && mouseMove.y > 0.0f)
				mouseMove.y *= (distToRU.y - 1.0f);
			if (distToLB.x > -1.0f && mouseMove.x < 0.0f)
				mouseMove.x *= (distToLB.x * -1.0f);
			if (distToRU.x < 2.0f && mouseMove.x > 0.0f)
				mouseMove.x *= (distToRU.x - 1.0f);
			if (distToLB.x > 0.0f || distToRU.x < 1.0f)
				mouseMove.x = 0;
			if (distToRU.y < 1.0f || distToLB.y > 0.0f)
				mouseMove.y = 0;

			return mouseMove;
		}

		private void ApplyCameraMove(Vector3 mouseMove)
		{
			if (mouseMove != Vector3.zero)
			{
				Camera.main.transform.Translate(mouseMove);
				MoveBackground(mouseMove);
				MoveWater(mouseMove);
			}
		}

		public void SetBackgroundLayer(GameObject bck, int layer)
		{
			BackgroundLayers[layer] = bck;
		}

		public void SetWaterLayer(GameObject water, int layer)
		{
			WaterLayers[layer] = water;
		}

		void MoveBackground(Vector3 move)
		{
			MoveBackgroundLayer(0, move, 1.2f);
			MoveBackgroundLayer(1, move, 1.15f);
		}

		void MoveBackgroundLayer(int layer, Vector3 move, float decrease)
		{
			for (int i = 0; i < BackgroundLayers[layer].transform.childCount; i++)
			{
				Transform tra = BackgroundLayers[layer].transform.GetChild(i);
				tra.Translate(move / decrease);
			}
		}

		void MoveWater(Vector3 move)
		{
			MoveWaterLayer(0, -move, 4.0f);
			MoveWaterLayer(1, -move, 6.0f);
			MoveWaterLayer(3, move, 2.4f);
			MoveWaterLayer(4, move, 2.0f);
			MoveWaterLayer(5, move, 1.6f);
		}

		void MoveWaterLayer(int layer, Vector3 move, float decrease)
		{
			for (int i = 0; i < WaterLayers[layer].transform.childCount; i++)
			{
				Transform tra = WaterLayers[layer].transform.GetChild(i);
				move.y = 0;
				tra.Translate(move / decrease);
			}
		}

		public void MoveCamera(Vector3 move)
		{
			Camera.main.transform.Translate(move);
			MoveBackground(move);
		}
	}


}