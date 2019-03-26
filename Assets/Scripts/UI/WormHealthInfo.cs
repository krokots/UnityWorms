using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class WormHealthInfo : WormInfo {


		public float YMove;
		public float YMax;

		//private void Start()
		//{
		//	yOffset = new Vector3(0, YOffset, 0);
		//	rt = GetComponent<RectTransform>();
		//}

		//void Update()
		//{
		//	Tick(Time.deltaTime);
		//}

		protected override void Tick(float delta)
		{
			YOffset += YMove * delta;
			yOffset.y = YOffset;

			if(YOffset > YMax)
			{
				YOffset = YMax;
				YMove = 0;
				Destroy(gameObject, 1.0f);
			}
			GameManager.GM.Delay();
			base.Tick(delta);
		}


		public void SetHealth(int health)
		{
			tmp = GetComponent<TMPro.TextMeshProUGUI>();
			tmp.text = health.ToString();
		}
	}
}
