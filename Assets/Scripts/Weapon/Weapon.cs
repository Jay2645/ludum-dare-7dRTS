using UnityEngine;
using System.Collections;

/// <summary>
/// A generic weapon class.
/// All weapons must inherit from this class and override critical functions.
/// </summary>
public class Weapon : MonoBehaviour 
{
	public bool fireOnce = true;
	public float timeBetweenShots = 1.5f;
	public int ammo = 2000;
	public int damage = 25;
	public string weaponName = "Gun";
	
	protected float lastShotTime = 0;
	public Projectile projectile = null;
	public Unit owner = null;
	public Vector3 unitPosition = new Vector3(-0.35f,0.075f,0.75f);
	public Vector3 playerPosition = new Vector3(0.35f, -0.2f, 0.45f);
	
	void Update()
	{
		if(owner == (Unit)Commander.player)
		{
			if(fireOnce)
			{
				if(Input.GetButtonDown("Fire1") && (lastShotTime == 0 || Time.time - lastShotTime > timeBetweenShots))
				{
					Shoot ();
				}
			}
			else
			{
				if(Input.GetButton("Fire1"))
				{
					Shoot();
				}
			}
		}
	}
	
	/// <summary>
	/// Handles things all weapons do when shooting, such as managing ammo count.
	/// </summary>
	protected void Shoot()
	{
		if(ammo <= 0)
			return;
		lastShotTime = Time.time;
		if(projectile == null)
		{
			OnShootRaycast();
		}
		else
		{
			OnShootProjectile();
		}
	}
	
	/// <summary>
	/// Shoots a physical projectile out of the gun, which has a definite travel time.
	/// </summary>
	protected virtual void OnShootProjectile()
	{
		Projectile proj = Instantiate(projectile,transform.position,transform.rotation) as Projectile;
		proj.damage = damage;
		proj.owner = owner;
		proj.constantForce.force = transform.up * proj.speed;
	}
	
	/// <summary>
	/// Raycasts forward and damages anything the raycast hits instantly.
	/// </summary>
	protected virtual void OnShootRaycast()
	{
		Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		RaycastHit hit;
		if (Physics.Raycast(selectRay, out hit,Mathf.Infinity))
		{
			Unit hitUnit = hit.transform.GetComponentInChildren<Unit>();
			if(hitUnit != null)
			{
				hitUnit.health -= damage;
			}
		}
		Debug.DrawRay(selectRay.origin,selectRay.direction);
	}
	
	public virtual Vector3 GetLocation()
	{
		if(owner != null && owner is Commander && ((Commander)owner).isPlayer)
		{
			return playerPosition;
		}
		else
		{
			return unitPosition;
		}
	}
}
