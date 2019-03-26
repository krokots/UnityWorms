using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	[CreateAssetMenu(fileName = "new_weapon", menuName = "WormsClone/Weapon", order = 2)]
	public class Weapon : ScriptableObject
	{
		public string Name;

		public bool Chargable;						//The weapon gets charged before shooting
		public bool Aiming;							//The weapon is aimed at a position (e.g. air strike, teleport)

		public AnimationClip ShootAnimation;		//If shot, worm will play this animation
		public AnimationClip ShootAnimationUp;		//If shot, worm will play this animation
		public AnimationClip ShootAnimationDown;	//If shot, worm will play this animation
		public GameObject Projectile;				//If shot, worm will spawn this projectile

		public float MinAngle;						//Worm min angle is set to that after selecting weapon
		public float MaxAngle;						//-''- max -''-
		public float MinPower;						//If spawned projectile, this is minimum force that will be added to initial velocity
		public float MaxPower;                      //If spawned projectile, this is maximum force -''-

		public float AfterTime;                     //How long after shooting you can control worm
		public bool ControlWeapon;                  //If true, you will have control over weapon after shooting (eg. sheep)	

		public bool OverrideIdleAnims;				//If true, the idle animations will be swapped for following ones:
		public AnimationClip IdleStraight;			//Animation for standing and looking straight
		public AnimationClip IdleUp;				//-''- looking up
		public AnimationClip IdleDown;				//-''- looking down

		public bool OverrideMoveAnims;				//-''- for moving animation.
		public AnimationClip MoveStraight;
		public AnimationClip MoveUp;
		public AnimationClip MoveDown;

		public int GetWidth()
		{
			return Projectile.GetComponent<Projectile>().Width;
		}

		public int GetHeight()
		{
			return Projectile.GetComponent<Projectile>().Height;
		}
	}


}