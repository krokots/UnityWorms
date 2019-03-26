using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class WormInfo : MonoBehaviour
	{

		public Transform Target;

		public float YOffset = 0.26f;

		protected string wormName;
		protected RectTransform rt;
		protected Vector3 yOffset;
		protected TMPro.TextMeshProUGUI tmp;

		private void Start()
		{
			yOffset = new Vector3(0, YOffset, 0);
			rt = GetComponent<RectTransform>();
		}

		void Update()
		{
			Tick(Time.deltaTime);
		}

		protected virtual void Tick(float delta)
		{
			if (!Target)
			{
				Destroy(gameObject);
				return;
			}
			Vector3 pos = Target.transform.localPosition + yOffset;

			Vector3 scrPos = Camera.main.WorldToScreenPoint(pos);
			rt.position = scrPos;
		}

		public void SetName(string name, int health = 100)
		{
			gameObject.name = name;
			wormName = name;
			tmp = GetComponent<TMPro.TextMeshProUGUI>();
			tmp.text = name + "\n" + health;
		}

		public void SetColor(Color col_a)
		{
			Color col_b = col_a / 2.0f;
			col_a.a = col_b.a = 0.8f;

			TMPro.TMP_ColorGradient grad = ScriptableObject.CreateInstance<TMPro.TMP_ColorGradient>();
			grad.topLeft = col_a;
			grad.topRight = col_a;
			grad.bottomLeft = col_b;
			grad.bottomRight = col_b;

			tmp.colorGradientPreset = grad;
		}

		public void UpdateHealth(int health)
		{
			tmp = GetComponent<TMPro.TextMeshProUGUI>();
			tmp.text = wormName + "\n" + health;
		}
	
	}
}
