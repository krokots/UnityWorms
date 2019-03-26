using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public enum WormState
	{
		Idle,
		Jump,
		Hurt,
		Stuck,
		Busy
	}

	public enum DamageType
	{
		Explosion,
		FallDamage
	}

	public class WormObject : RectObject
	{
		[Header("Worm attributes")]
		public int Health;
		public int Damage;
		public int TeamIndex;
		[Header("Worm parameters")]
		public float WalkSpeed;
		public int MaxStepUp;
		public int MaxStepDown;
		public float Angle;
		public float MinAngle = -90;
		public float MaxAngle = 90;
		public bool IsFacingLeft;
		public WormState State;
		public Weapon CurrentWeapon { get; protected set; }
		

		public float CrosshairDistance = 0.2f;
		public float CrosshairSpeed = 0.4f;
		public float KnockOutVelocity = 100.0f;

		public float JumpDistance;

		[Header("Input")]
		public float Horizontal;
		public float Vertical;

		[Header("Pointers")]
		public Transform Crosshair;
		public WormInfo WormInfo;
		public LineRenderer PowerIndicator;

		private Animator animator;
		private AnimatorOverrideController animatorOverride;
		private int animDefaultId;
		private int animWalkingId;
		private int animWalkingDirId;
		private int animJumpId;
		private int animIsJumpingId;
		private int animAngleId;
		private SpriteRenderer spriteRenderer;

		private float reboundThresholdDef;
		private float frictionDef;

		private AnimationClip defaultIdleStr;
		private AnimationClip defaultIdleUp;
		private AnimationClip defaultIdleDown;

		private AnimationClip defaultMoveStr;
		private AnimationClip defaultMoveUp;
		private AnimationClip defaultMoveDown;

		private AnimationClip defaultSingleAction;
		private AnimationClip defaultSingleActionUp;
		private AnimationClip defaultSingleActionDown;

		List<KeyValuePair<AnimationClip, AnimationClip>> defaultIdleAnims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		List<KeyValuePair<AnimationClip, AnimationClip>> defaultMoveAnims = new List<KeyValuePair<AnimationClip, AnimationClip>>();



		private void Start()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			animator = GetComponent<Animator>();
			animWalkingId = Animator.StringToHash("walking");
			animDefaultId = Animator.StringToHash("WormDefaultAngled");
			animWalkingDirId = Animator.StringToHash("moveDir");
			animJumpId = Animator.StringToHash("jump");
			animIsJumpingId = Animator.StringToHash("isJumping");
			animAngleId = Animator.StringToHash("angle");
			if (DebugSprite)
			{
				SetDebugSprite(gameObject, Color.yellow, Width, Height);
			}
			//SetPosition(transform.localPosition);
			IsFacingLeft = true;
			reboundThresholdDef = ReboundThreshold;
			frictionDef = Friction;

			animatorOverride = new AnimatorOverrideController(animator.runtimeAnimatorController);
			animator.runtimeAnimatorController = animatorOverride;

			defaultIdleStr = animatorOverride["WormDefault"];
			defaultIdleUp = animatorOverride["WormDefaultUp"];
			defaultIdleDown = animatorOverride["WormDefaultDown"];

			defaultMoveStr = animatorOverride["WormMove"];
			defaultMoveUp = animatorOverride["WormMoveUp"];
			defaultMoveDown = animatorOverride["WormMoveDown"];

			KeyValuePair<AnimationClip, AnimationClip> idleStr = new KeyValuePair<AnimationClip, AnimationClip>(defaultIdleStr, defaultIdleStr);
			KeyValuePair<AnimationClip, AnimationClip> idleUp = new KeyValuePair<AnimationClip, AnimationClip>(defaultIdleUp, defaultIdleUp);
			KeyValuePair<AnimationClip, AnimationClip> idleDown = new KeyValuePair<AnimationClip, AnimationClip>(defaultIdleDown, defaultIdleDown);
			defaultIdleAnims.Add(idleStr);
			defaultIdleAnims.Add(idleUp);
			defaultIdleAnims.Add(idleDown);

			KeyValuePair<AnimationClip, AnimationClip> moveStr = new KeyValuePair<AnimationClip, AnimationClip>(defaultMoveStr, defaultMoveStr);
			KeyValuePair<AnimationClip, AnimationClip> moveUp = new KeyValuePair<AnimationClip, AnimationClip>(defaultMoveUp, defaultMoveUp);
			KeyValuePair<AnimationClip, AnimationClip> moveDown = new KeyValuePair<AnimationClip, AnimationClip>(defaultMoveDown, defaultMoveDown);
			defaultMoveAnims.Add(moveStr);
			defaultMoveAnims.Add(moveUp);
			defaultMoveAnims.Add(moveDown);

			defaultSingleAction = animatorOverride["WormActionDefault"];
			defaultSingleActionUp = animatorOverride["WormActionDefaultUp"];
			defaultSingleActionDown = animatorOverride["WormActionDefaultDown"];

			CollisionIgnoringCondition = (d) => d.IgnoredByObjects;

		}

		private void Update()
		{
			Delta = GameManager.GM.Delta;
			StoreLastTick();
			Tick(Delta);
		}

		public override void Tick(float dt)
		{
			SetCrosshairPosition(Angle, (Height / 100.0f));
			if(State == WormState.Idle)
			{
				if (Velocity == Vector2.zero)
				{
					bool walking = Walk(Horizontal, Delta);
					SetSpriteWalking(walking);
				}
				if (Vertical != 0)
					ModifyAngle(Vertical > 0 ? CrosshairSpeed : -CrosshairSpeed);
			}
			if (Velocity.magnitude > reboundThresholdDef)
				SetFalling();
			if (Velocity.x != 0)
				IsFacingLeft = (Velocity.x > 0) ? false : true;
			SetSpriteFacing(IsFacingLeft);
			base.Tick(dt);
		}

		protected override void OnBounce(Collision col, Vector2 pos, Vector2 oldvel, ref Vector2 newvel, Vector2 collisionNormal)
		{
			if (oldvel.magnitude > reboundThresholdDef && !col.CollisionObject)	//If falling a long distance
			{
				if (oldvel.x == 0)                      //If falling straight down
				{
					GetStuck(oldvel.magnitude);
					newvel = Vector2.zero;
				}
				else
				{
					int fallDamage = (int)(oldvel.magnitude / GameManager.GM.FallDamageFactor);
					GetHurt(fallDamage, DamageType.FallDamage);
				}
			}
			base.OnBounce(col, pos, oldvel, ref newvel, collisionNormal);
		}

		protected override bool OnCollideWithObject(RectObject other, ref Vector2 vel)
		{
			if(other is WormObject && vel.magnitude > KnockOutVelocity)
			{
				//Debug.LogFormat("Hit a worm, vel {0}", vel);
				WormObject worm = (WormObject)other;
				vel *= 0.8f;
				//Debug.Break();
				worm.AddForce(vel);
				worm.SetFalling();
				return false;
			}
			return base.OnCollideWithObject(other, ref vel);
		}

		protected override void OnStop(Vector2 pos)
		{
			//Debug.Log("OnStop");
			ReboundThreshold = reboundThresholdDef;
			Friction = frictionDef;
			if (State == WormState.Jump)
				State = WormState.Idle;
			if (State != WormState.Stuck)
				SetSpriteIdle();

			//if (State == WormState.Jumping)
			//	State = WormState.Idle;
		}

		protected override void OnDestroyObject()
		{
			//GameManager.GM.ActionComplete = true;
			//GameManager.GM.State = GameState.Observing;
			GameManager.GM.DeleteWorm(this);		
			
		}

		override protected void SetPosition(Vector2 gridPos)
		{
			if (AccuratePosition)
			{
				transform.localPosition = gridPos / 100.0f;
				return;
			}
			float x = ((int)(gridPos.x) + 1) / 100.0f;		//+1 is for fixing flipping sprites
			float y = ((int)(gridPos.y)) / 100.0f;
			transform.localPosition = new Vector3(x, y, 0);
		}

		public void ModifyAngle(float val)
		{
			Angle += val;
			if (val > 0)
			{
				if (Angle > MaxAngle)
					Angle = MaxAngle;
			}
			else if (val < 0)
			{
				if (Angle < MinAngle)
					Angle = MinAngle;
			}
			SetAnimationAngle();
		}

		public void SetCrosshairPosition(float angle, float height)
		{
			Vector2 aim = GetAim(angle, CrosshairDistance);
			Crosshair.localPosition = new Vector3(aim.x, aim.y + height, 0);
		}

		public bool Walk(float hz, float dt)
		{
			if (hz == 0)
				return false;
			IsFacingLeft = hz > 0 ? false : true;
			Vector2 wlk = new Vector2(hz > 0 ? 1 : -1, 0);
			Vector2 dst = Position + (wlk * WalkSpeed * dt);
			Vector2 pos_a = GetCurrentPos();
			Vector2 pos_b = GetPos(dst);
			//Debug.LogFormat("Walk : hz {0}, wlk {1}, dst {2}", hz, wlk, dst);
			if (pos_a != pos_b)
			{
				Vector2Int step = GetWalkingStep(hz);
				if (step == Vector2Int.zero)
					return false;
				dst.y += step.y;
				//Debug.LogFormat("Step : {0}, dst : {1}", step, dst);
			}
			Position = dst;

			int slope = GetSlope(hz);

			SetPosition(Position);
			SetSpriteSlope(slope);

			return true;
		}


		private float GetAngle()
		{
			return GetAngle(Angle);
		}

		private float GetAngle(float angle)
		{
			if (IsFacingLeft)
			{
				if (angle >= 0)
					angle = 180 - angle;
				else if (angle < 0)
					angle = -180 - angle;
			}
			return angle;
		}

		public Vector2 GetAim(float angle, float dist = 1.0f)
		{
			angle = GetAngle(angle);
			float rad = angle * Mathf.PI / 180;
			float x = dist * Mathf.Cos(rad);
			float y = dist * Mathf.Sin(rad);
			return new Vector2(x, y);
		}

		public Vector2 GetAim()
		{
			return Crosshair.localPosition.normalized;
		}

		#region Weapons

		public void Shoot(float power)
		{
			//Debug.Log("Shoot");
			State = WormState.Busy;
			Vector2 spawnPos = GetSpawnPosition(CurrentWeapon.GetWidth(), CurrentWeapon.GetHeight(), Angle, 1.0f);
			//Debug.LogFormat("Spawn position to position : {0} / {1}", spawnPos, Position);
			RectObject proj = GameManager.GM.SpawnObject(CurrentWeapon.Projectile, spawnPos);
			proj.AddForce(GetAim() * power);
			//CurrentWeapon = null;
			SetSpriteShooting(Angle);
		}

		public Vector2 GetSpawnPosition(int proj_w, int proj_h, float angle, float x_off)
		{
			Vector2 pos = Vector2.zero;
			if(IsFacingLeft)
				pos.x = Position.x - proj_w - x_off;
			else
				pos.x = Position.x + Width + x_off;

			angle = (angle + 90.0f) / 180.0f;
			float add_h = Mathf.Lerp(0, 1.0f, angle) * Height;
			pos.y = Position.y + add_h;

			return pos;
		}

		public void SetWeapon(Weapon weapon)
		{
			CurrentWeapon = weapon;
			MinAngle = weapon.MinAngle;
			MaxAngle = weapon.MaxAngle;

			if (weapon.OverrideIdleAnims)
				SetWeaponAnimations(defaultIdleStr, defaultIdleUp, defaultIdleDown, weapon.IdleStraight, weapon.IdleUp, weapon.IdleDown);
			else
				SetDefaultIdleAnimations(defaultIdleAnims);

			if (weapon.OverrideMoveAnims)
				SetWeaponAnimations(defaultMoveStr, defaultMoveUp, defaultMoveDown, weapon.MoveStraight, weapon.MoveUp, weapon.MoveDown);
			else
				SetDefaultIdleAnimations(defaultMoveAnims);


			SetSingleActionAnimation(weapon.ShootAnimation, weapon.ShootAnimationUp, weapon.ShootAnimationDown);
		}

		private void SetWeaponAnimations(AnimationClip cur_str, AnimationClip cur_up, AnimationClip cur_down, AnimationClip weap_str, AnimationClip weap_up, AnimationClip weap_down)
		{
			List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
			KeyValuePair<AnimationClip, AnimationClip> animStr = new KeyValuePair<AnimationClip, AnimationClip>(cur_str, weap_str);
			KeyValuePair<AnimationClip, AnimationClip> animUp = new KeyValuePair<AnimationClip, AnimationClip>(cur_up, weap_up);
			KeyValuePair<AnimationClip, AnimationClip> animDown = new KeyValuePair<AnimationClip, AnimationClip>(cur_down, weap_down);

			overrides.Add(animStr);
			overrides.Add(animUp);
			overrides.Add(animDown);
			animatorOverride.ApplyOverrides(overrides);
		}

		private void SetDefaultIdleAnimations(List<KeyValuePair<AnimationClip, AnimationClip>> defaults)
		{
			animatorOverride.ApplyOverrides(defaults);
		}

		private void SetSingleActionAnimation(AnimationClip weap_str, AnimationClip weap_up, AnimationClip weap_down)
		{
			List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
			KeyValuePair<AnimationClip, AnimationClip> str = new KeyValuePair<AnimationClip, AnimationClip>(defaultSingleAction, weap_str);
			KeyValuePair<AnimationClip, AnimationClip> up = new KeyValuePair<AnimationClip, AnimationClip>(defaultSingleActionUp, weap_up);
			KeyValuePair<AnimationClip, AnimationClip> down = new KeyValuePair<AnimationClip, AnimationClip>(defaultSingleActionDown, weap_down);
			overrides.Add(str);
			overrides.Add(up);
			overrides.Add(down);
			animatorOverride.ApplyOverrides(overrides);
		}

		#endregion

		#region Jumping

		public void SetJump()
		{
			State = WormState.Jump;
			SetSpriteBeginJump();
		}

		public void SetFalling()
		{
			if (State == WormState.Hurt)
				return;
			ReboundThreshold = 0;
			if (Velocity != Vector2.zero)
			{
				SetSpriteFalling();
				State = WormState.Hurt;
			}
		}

		public void Jump()
		{
			float angle = Angle;
			if (angle < 20)
				angle = 20;
			Jump(GetAim(angle));
			SetSpriteJumping();
		}

		public void Jump(Vector2 vec)
		{
			if(Position.y + Height < GameManager.GM.Height)
			{
				Vector2Int pos = GetCurrentPos();
				for (int x = 0; x < Width; x++)
				{
					Node n = GameManager.GM.MapGrid[pos.x + x, pos.y + Height];
					if (n.IsSolid(this))
						return;
				}

			}
			Position.y += 1;
			//State = WormState.Jumping;
			AddForce(vec * JumpDistance);
			//Debug.LogFormat("Jump, vel : {0}", Velocity);
		}

		public void GetStuck(float speed)
		{
			Debug.LogFormat("Get stuck {0}", speed);
			Velocity = Vector2.zero;
			State = WormState.Stuck;
			SetSpriteStuck();

		}

		public void GetHurt(int damage, DamageType type)
		{
			SetFalling();
			Damage += damage;
			GameManager.GM.HurtWorm(this, damage);
			//GameManager.GM.ActionComplete = true;
			Debug.LogFormat("Worm {0} damage {1} type {2}", WormInfo.name, damage, type);
		}

		public void SetToDie()
		{
			animator.Play("WormExplode");
			State = WormState.Busy;
		}

		public void Explode()
		{
			Projectile.SpawnExplosion(null, GetCenter(), 1, 15, 150, 5, 40);
			GameManager.GM.KillWorm(this);
			DestroyObject();
		}

		public void GotUp()
		{
			State = WormState.Idle;
		}

		#endregion

		#region Animations

		private void SetAnimationAngle()
		{
			animator.SetFloat(animAngleId, Angle);
		}

		private void SetSpriteIdle()
		{
			animator.SetBool(animIsJumpingId, false);
			if (State == WormState.Hurt)
				animator.Play("WormStandUp");
			else
				animator.Play(animDefaultId);
		}

		private void SetSpriteFacing(bool isFacingLeft)
		{
			if (!isFacingLeft || Velocity.x > 0)
				spriteRenderer.flipX = true;
			else if (isFacingLeft || Velocity.x < 0)
				spriteRenderer.flipX = false;
			
		}

		private void SetSpriteWalking(bool walking)
		{
			animator.SetBool(animWalkingId, walking);
			if(!walking)
				animator.Play(animDefaultId);

		}

		private void SetSpriteSlope(int slope)
		{
			if (slope == 0)
				animator.SetFloat(animWalkingDirId, 0.5f);
			else if (slope < 0)
				animator.SetFloat(animWalkingDirId, 1.0f);
			else if (slope > 0)
				animator.SetFloat(animWalkingDirId, 0);
		}

		private void SetSpriteFalling()
		{
			if (GRandom.GetBool())
				animator.Play("WormFall");
			else
				animator.Play("WormSlide");
		}

		private void SetSpriteStuck()
		{
			animator.Play("WormStuck");
		}

		private void SetSpriteBeginJump()
		{
			animator.SetTrigger(animJumpId);
		}

		private void SetSpriteJumping()
		{
			animator.SetBool(animIsJumpingId, true);
		}

		private void SetSpriteShooting(float angle)
		{
			if (!CurrentWeapon)
				return;

			animator.Play("ActionAngled");
		}

		#endregion

		#region Terrain checks

		//Do poprawy, przy wchodzeniu pod górkę, za mało pikseli bierze pod uwagę, i zmienia animację na chodzenie na wprost często.

		/// <summary>
		/// Checks next node to move to and returns if is it higher or lower (for animation only)
		/// </summary>
		public int GetSlope(float hz)
		{
			Vector2Int pos = GetCurrentPos();
			//Debug.LogFormat("pos : {0}", pos);
			int nx = pos.x;
			if (hz > 0)
				nx += Width;
			else if (hz < 0)
				nx -= 1;
			int ny = pos.y;

			if (nx >= GameManager.GM.Width || nx < 0 || ny >= GameManager.GM.Height || ny < 0)
				return 0;
			Node next = GameManager.GM.MapGrid[nx, ny];

			int nxx = (hz > 0) ? nx + 1 : nx - 1;
			Node next_2 = null;
			if (nxx >= 0 && nxx < Width)
				next_2 = GameManager.GM.MapGrid[nxx, ny];

			if (next.IsSolid(this))
				return 1;
			else if (next_2 && next_2.IsSolid(this))
				return 1;

			int ly = ny - 1;
			Node lower = null;
			if (ly >= 0)
				lower = GameManager.GM.MapGrid[nx, ly];
			Node lower_2 = null;
			if (nxx >= 0 && nxx < Width && ly >= 0)
				lower_2 = GameManager.GM.MapGrid[nxx, ly];

			if (lower && !lower.IsSolid(this))
				return -1;
			else if (lower_2 && !lower_2.IsSolid(this))
				return -1;

			return 0;
		}

		public Vector2Int GetWalkingStep(float hz)
		{
			Vector2Int dir = Vector2Int.zero;
			if (hz > 0)
				dir = Vector2Int.right;
			else if (hz < 0)
				dir = Vector2Int.left;
			else
				return Vector2Int.zero;

			Vector2Int pos = GetCurrentPos();
			//Debug.LogFormat("pos : {0}", pos);
			int nx = pos.x;
			if (hz > 0)
				nx += Width;
			else if (hz < 0)
				nx -= 1;
			int ny = pos.y;

			if (nx >= GameManager.GM.Width || nx < 0 || ny >= GameManager.GM.Height || ny < 0)
				return dir;
			Node next = GameManager.GM.MapGrid[nx, ny];
			//Debug.LogFormat("next : {0}", next);
			if(next.IsSolid(this))						//Step up
			{
				int stepAmount = 1;
				for (int y = 0; y < MaxStepUp; y++)	//Checking how much steps up to make
				{
					int uy = pos.y + 1 + y;
					if (uy >= GameManager.GM.Height)
						continue;
					Node upper = GameManager.GM.MapGrid[nx, uy];
					if(upper.IsSolid(this))
						stepAmount++;
				}
				//Debug.LogFormat("stepAmount : {0}", stepAmount);
				if (stepAmount > MaxStepUp)			//Too big step to make
					return Vector2Int.zero;

				for (int y = 0; y < Height; y++)	//Checking the air over the step to make
				{
					int uy = pos.y + stepAmount + y;
					if (uy >= GameManager.GM.Height)
						continue;
					Node upper = GameManager.GM.MapGrid[nx, uy];
					if (upper.IsSolid(this))			//Blocked by something in the air
						return Vector2Int.zero;		
				}
				for (int x = 1; x < Width; x++)		//Checking the air above ourselves 
				{
					for (int y = Height; y < Height + stepAmount; y++)
					{
						int ux = pos.x;
						if (hz > 0)
							ux = pos.x + x;
						else if (hz < 0)
							ux = pos.x - x + 1;

						int uy = pos.y + y;
						if (ux >= GameManager.GM.Width || ux < 0 || uy >= GameManager.GM.Height)
							continue;
						Node upper = GameManager.GM.MapGrid[ux, uy];
						if (upper.IsSolid(this))
							return Vector2Int.zero;
					}
				}
				return new Vector2Int(dir.x, stepAmount);
			}
			else
			{										//Straight or step down
				for (int y = 1; y < Height; y++)	//Checking air 
				{
					int uy = pos.y + y;
					if (uy >= GameManager.GM.Height)
						continue;
					Node upper = GameManager.GM.MapGrid[nx, uy];
					if(upper.IsSolid(this))				//Blocked by something in the air
						return Vector2Int.zero;
				}
				//Nothing blocking in the air, but checking if there are steps down
				int stepAmount = 0;
				for (int y = 0; y < MaxStepDown; y++)
				{
					//Debug.LogFormat("Checking lower nodes, y : {0}", y);
					int ly = pos.y - 1 - y;

					bool foundSolid = false;
					if (ly < 0)
						continue;
					for (int x = 0; x < Width; x++)
					{
						//Debug.LogFormat("Checking lower nodes, x : {0}", x);

						int lx = pos.x;
						if (hz > 0)
							lx = pos.x + 1 + x;
						else if (hz < 0)
							lx = pos.x - x;
						if (lx >= GameManager.GM.Width || lx < 0)
							continue;
						Node lower = GameManager.GM.MapGrid[lx, ly];
						//Debug.LogFormat("Checking lower nodes, lower : {0}", lower);

						if (lower.IsSolid(this))
						{
							foundSolid = true;
							break;
						}
					}
					if (foundSolid)
						return new Vector2Int(dir.x, stepAmount);
					stepAmount--;
				}
				return new Vector2Int(dir.x, -MaxStepDown);
			}
		}

		#endregion
	}
}
