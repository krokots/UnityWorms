using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class WeaponPanel : MonoBehaviour
	{


		public float Speed = 1.0f;
		public AnimationCurve Curve;
		public bool IsActive { get; private set; }
		public bool IsMoving { get; private set; }

		private RectTransform rectTransform;
		private float widthRatio;


		private void Start()
		{
			IsActive = false;
			rectTransform = GetComponent<RectTransform>();
			widthRatio = rectTransform.sizeDelta.x + 10;
		}


		public void GetOnScreen()
		{
			IsMoving = true;
			StartCoroutine(Move(Speed, false, () => { IsActive = true; IsMoving = false; }));
		}

		public void GetOffScreen()
		{
			IsMoving = true;
			StartCoroutine(Move(Speed, true, () => IsMoving = false));
			IsActive = false;
		}

		private IEnumerator Move(float t, bool offScreen, System.Action finishedAction = null)
		{
			for (float i = 0; i <= t; i += Time.deltaTime)
			{
				float dx = Curve.Evaluate(i / t) * widthRatio;
				if (!offScreen)
					dx = widthRatio - dx;
				rectTransform.anchoredPosition = new Vector3(dx, rectTransform.position.y, rectTransform.position.z);
				if (i + Time.deltaTime > t)
				{
					//Coroutine is finishing
					if (finishedAction != null)
						finishedAction.Invoke();
				}
				yield return null;
			}
		}
	}
}