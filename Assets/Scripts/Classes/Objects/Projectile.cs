using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class Projectile : RectObject
	{

		[Header("Projectile")]
		public int ExplosionRadius;
		public float ExplosionForce;
		public int PuffCount;
		public float PuffForce;
		private System.Action OnExplosion;

		public bool DestroyOnImpact;
		public bool ExplodeOnImpact;

		public GameObject ExplosionObject;
		public GameObject ExplosionPuffObject;


		private void Update()
		{
			Delta = GameManager.GM.Delta;
			StoreLastTick();
			Tick(Delta);
		}

		public static void SpawnExplosion(GameObject expl, Vector2 pos, int size, int radius, float force, int puffs, float puffForce, GameObject puffObject = null, System.Action onExplosion = null)
		{
			if (expl == null)
				expl = GameManager.GM.DefaultExplosionObject;
			RectObject obj = GameManager.GM.SpawnObject(expl, pos);
			if (obj is Projectile)
			{
				Projectile proj = (Projectile)obj;
				proj.ExplosionRadius = radius;
				proj.ExplosionForce = force;
				proj.PuffCount = puffs;
				proj.PuffForce = puffForce;
				if (puffObject == null)
					puffObject = GameManager.GM.DefaultExplosionPuffObject;
				proj.ExplosionPuffObject = puffObject;
				proj.OnExplosion = onExplosion;

				Animator anim = proj.GetComponent<Animator>();
				if(anim)
					anim.SetInteger("size", size);
			}
		}

		private void SpawnExplosion(GameObject expl, Vector2 pos, int size = 0)
		{
			SpawnExplosion(expl, pos, size, ExplosionRadius, ExplosionForce, PuffCount, PuffForce, ExplosionPuffObject);
		}

		private RectObject SpawnExplosionPuff(GameObject puff, float angle, float dst, float force)
		{
			if (puff == null)
				return null;
			angle = angle * Mathf.PI / 180.0f;
			Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			Vector2 pos = Position + vel * dst;
			RectObject obj = GameManager.GM.SpawnObject(puff, pos);
			obj.Velocity = vel * force;
			return obj;
		}

		public void SpawnExplosionPuffs()
		{
			for (int i = 0; i < PuffCount; i++)
			{
				float angle = GRandom.GetFloat(1, 360.0f);
				float dist = GRandom.GetFloat(2.0f, ExplosionRadius / 2.0f);
				SpawnExplosionPuff(ExplosionPuffObject, angle, dist, PuffForce);
			}
		}

		private bool Impact(Vector2 pos)
		{
			bool destroyed = false;
			if (ExplodeOnImpact && ExplosionObject)
				SpawnExplosion(ExplosionObject, pos);
			if (DestroyOnImpact)
			{
				Destroy(gameObject);
				destroyed = true;
			}
			return destroyed;
		}

		public void Explode()
		{
			if (OnExplosion != null)
				OnExplosion.Invoke();
			GameManager.GM.Explode(GetCurrentPos(), ExplosionRadius, 0, ExplosionForce);
		}

		protected override void OnBounce(Collision col, Vector2 pos, Vector2 oldvel, ref Vector2 newvel, Vector2 collisionNormal)
		{
			bool destroyed = Impact(pos);
			if (!destroyed)
				base.OnBounce(col, pos, oldvel, ref newvel, collisionNormal);
		}

		protected override void OnStuck(Vector2 pos)
		{
			bool destroyed = Impact(pos);
			if (!destroyed)
				base.OnStuck(pos);
		}

		protected override void OnStop(Vector2 pos)
		{
			bool destroyed = Impact(pos);
			if (!destroyed)
				base.OnStop(pos);
		}
	}
}
