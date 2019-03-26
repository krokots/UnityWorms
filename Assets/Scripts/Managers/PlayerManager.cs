using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public enum GameState
	{
		Movement,
		Weapon,
		Observing,
		Damage,
		Killing,
		Drop
	}

	public class PlayerManager : MonoBehaviour
	{
		[Header("Properties")]
		public float PowerPerTick;
		public float MaxPower;

		[Header("Pointers")]
		public WeaponPanel WeaponPanel;
		public CameraManager CameraManager;
		public WormObject CurrentWorm { get; private set; }

		[Header("Debug")]
		public Weapon DebugWeapon;
		public float ShootForce;

		private float power;
		private bool playerShot;

		void Start()
		{
			
		}

		void Update()
		{
			ControlCamera();
			if(GameManager.GM.State == GameState.Movement)
				Move();
		}

		public void ControlCamera()
		{
			CameraManager.MouseX = Input.GetAxis("Mouse X");
			CameraManager.MouseY = Input.GetAxis("Mouse Y");
		}

		public void Move()
		{
			if (CurrentWorm && !WeaponPanel.IsActive && !WeaponPanel.IsMoving)		//Worm control
			{
				//Debug.Log("Power : " + power);
				if (power == 0)		//Things that can't be done when powering up shot
				{
					//Debug.Log("Can move ! ");
					CurrentWorm.Horizontal = Input.GetAxis("Horizontal");               //Moving
					CurrentWorm.Vertical = Input.GetAxis("Vertical");                   //Aiming
					if (Input.GetKeyDown(KeyCode.Return))                               //Jumping
						CurrentWorm.SetJump();
				}
				if (!playerShot && GameManager.GM.State == GameState.Movement)
				{
					if (Input.GetKeyDown(KeyCode.Space) && CurrentWorm.CurrentWeapon)       //Shooting
					{
						if (!CurrentWorm.CurrentWeapon.Chargable)
						{
							CurrentWorm.Shoot(ShootForce);
							GameManager.GM.Delay();
							StartCoroutine(GameManager.GM.RetreatWorm(CurrentWorm.CurrentWeapon.AfterTime, CurrentWorm));
						}
					}
					if (Input.GetKey(KeyCode.Space) && CurrentWorm.CurrentWeapon)
					{
						if (CurrentWorm.CurrentWeapon.Chargable)        //charge power
						{
							if (power < MaxPower)
							{
								power += PowerPerTick;
								if (power > MaxPower)
									power = MaxPower;
								if (CurrentWorm.PowerIndicator)
								{
									Vector3 start = CurrentWorm.transform.position + new Vector3(0, 0.035f, 0);
									Vector3 end = CurrentWorm.Crosshair.position;
									Vector3 vec = Vector3.Lerp(start, end, power / MaxPower);
									CurrentWorm.PowerIndicator.SetPosition(0, start);
									CurrentWorm.PowerIndicator.SetPosition(1, vec);
								}
								//Debug.Log("Power " + power);
								return;
							}
							else if (power == MaxPower)
								Shoot(power);
						}
					}
					if (Input.GetKeyUp(KeyCode.Space) && CurrentWorm.CurrentWeapon)
					{
						if (CurrentWorm.CurrentWeapon.Chargable)            //release shot
							Shoot(power);
					}
				}
			}

			if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
			{
				//CurrentWorm = GameManager.GM.SpawnWorm("Debug worm", Color.grey);
				//if (CurrentWorm)
				//	CameraManager.CenterOnWorm(CurrentWorm);
				Vector2Int mousePos = GetMousePosition();
				GameManager.GM.SpawnFluidCircle(mousePos, 4);
				//Debug.Break();
			}
			if (Input.GetMouseButtonDown(1) && CurrentWorm)							//Weapon selection and info
			{
				if (WeaponPanel.IsActive && !WeaponPanel.IsMoving)
				{
					WeaponPanel.GetOffScreen();
					CameraManager.MoveCameraOn = true;
				}
				else if (!WeaponPanel.IsActive && !WeaponPanel.IsMoving)
				{
					WeaponPanel.GetOnScreen();
					CameraManager.TurnOffCamera();
					CurrentWorm.Horizontal = 0;
					CurrentWorm.Vertical = 0;
				}
			}
			if(Input.GetKeyDown(KeyCode.C) && CurrentWorm)
			{
				CameraManager.CenterOnWorm(CurrentWorm);
			}
		}

		private void Shoot(float power)
		{
			//Debug.Log("SHOOT");
			CurrentWorm.Shoot(power);
			GameManager.GM.Delay();
			StartCoroutine(GameManager.GM.RetreatWorm(CurrentWorm.CurrentWeapon.AfterTime, CurrentWorm));
			this.power = 0;
			playerShot = true;
			CurrentWorm.PowerIndicator.SetPosition(1, CurrentWorm.PowerIndicator.GetPosition(0));
		}

		public Vector2Int GetMousePosition()
		{
			RaycastHit hitInfo = new RaycastHit();

			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			bool hit = Physics.Raycast(ray, out hitInfo);
			if (hit)
			{
				int mx = (int)(hitInfo.point.x * 100);
				int my = (int)(hitInfo.point.y * 100);
				return new Vector2Int(mx, my);
			}
			return (Vector2Int.one * -1);
		}

		public void ChooseWeapon(Weapon weapon)
		{
			if (WeaponPanel.IsActive && CurrentWorm && weapon)
			{
				CurrentWorm.SetWeapon(weapon);
				WeaponPanel.GetOffScreen();
				CameraManager.MoveCameraOn = true;
			}
		}

		public void ActivateWorm(WormObject worm)
		{
			CurrentWorm = worm;
			playerShot = false;
		}
	}
}