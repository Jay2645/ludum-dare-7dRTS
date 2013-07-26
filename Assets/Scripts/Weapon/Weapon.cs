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
	public int ammo = 800;
	public int clip = 40;
	public int damage = 25;
	public float reloadTime = 1.5f;
	public string weaponName = "Gun";
	public float range = Mathf.Infinity;
	public float shotSpread = 0.25f;
	
	protected float lastShotTime = 0;
	public Projectile projectile = null;
	public Unit owner = null;
	public Vector3 unitPosition = new Vector3(-0.35f,0.075f,0.75f);
	public Vector3 playerPosition = new Vector3(0.35f, -0.2f, 0.45f);
	public static GameObject tracer = null;
	protected bool reloading = false;
	protected int _maxClipSize;
	protected int _maxAmmoCount;
	public AudioClip fire = null;
	public AudioClip reload = null;
	
	protected int projectileHits = 0;
	protected int _projectileHits;
	protected int shotsFired = 0;
	protected int _shotsFired;
	protected float timer = 0.00f;
	
	void Awake()
	{
		if(tracer == null)
		{
			tracer = Resources.Load("Prefabs/Tracer") as GameObject;
		}
		_maxClipSize = clip;
		_maxAmmoCount = ammo;
	}
	
	void Update()
	{
		if(timer >= 1 && _shotsFired > 0)
		{
			projectileHits = _projectileHits;
			shotsFired = _shotsFired;
			timer = 0;
			_projectileHits = 0;
			_shotsFired = 0;
		}
		if(owner == null)
			rigidbody.isKinematic = false;
		else
			rigidbody.isKinematic = true;
		if(light.enabled && Random.value * 3 < 1)
			light.enabled = false;
		if(ammo > 0 && clip <= 0 && !reloading)
		{
			Reload();
			return;
		}
		if(reloading || ammo == 0)
			return;
		if(owner == (Unit)Commander.player)
		{
			if(fireOnce)
			{
				if(Input.GetButtonDown("Fire1"))
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
			if(Input.GetButtonDown("Reload") && !Input.GetButton("Fire1"))
				Reload();
		}
		timer += Time.deltaTime;
	}
	
	/// <summary>
	/// Handles things all weapons do when shooting, such as managing ammo count.
	/// </summary>
	public void Shoot()
	{
		if(reloading || ammo <= 0 || lastShotTime > 0 && Time.time - lastShotTime < timeBetweenShots)
			return;
		_shotsFired++;
		lastShotTime = Time.time;
		if(projectile == null)
		{
			OnShootRaycast();
		}
		else
		{
			OnShootProjectile();
		}
		clip--;
		ammo--;
		if(audio != null && fire != null)
		{
			audio.PlayOneShot(fire);
		}
		if(particleSystem != null)
		{
			particleSystem.Play();
		}
		if(light != null)
		{
			light.intensity = Random.Range(0.0f, 3.5f);
			light.enabled = true;
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
		RaycastHit hit;
		Vector3 shotDirection = ShootError();
		Ray directionRay = new Ray(transform.position,shotDirection);
		Debug.DrawRay(transform.position + transform.up,shotDirection, Color.blue);
		if (Physics.Raycast(directionRay, out hit,Mathf.Infinity))
		{
			if(Random.value * (4 / 3) <= 1)
			{
				GameObject tracerInstance = Instantiate(tracer) as GameObject;
				tracerInstance.transform.position = transform.position + (transform.up * 0.45f);
				tracerInstance.GetComponent<Tracer>().MoveForward(shotDirection);
			}
			Unit hitUnit = hit.transform.root.gameObject.GetComponentInChildren<Unit>();
			if(hitUnit != null)
			{
				_projectileHits++;
				hitUnit.health -= damage;
			}
		}
	}
	
	public void AddHit()
	{
		_projectileHits++;
	}
	
	public float GetAccuracy()
	{
		float accuracy = 0.00f;
		if(shotsFired == 0)
			return accuracy;
		accuracy = (float)projectileHits / (float)shotsFired;
		return accuracy;
	}
	
	protected Vector3 ShootError()
	{
		float sprayX = (1 - Random.value) * shotSpread;
		float sprayY = 25.0f;
		float sprayZ = (1 - Random.value) * shotSpread;
	 	return transform.TransformDirection(new Vector3(sprayX, sprayY, sprayZ));
	}
	
	protected void Reload()
	{
		reloading = true;
		if(audio != null && reload != null)
		{
			audio.PlayOneShot(reload);
		}
		Invoke ("DoneReloading",reloadTime);
	}
	
	protected void DoneReloading()
	{
		int clipDelta = _maxClipSize - clip;
		if(ammo > clipDelta)
			ammo -= clipDelta;
		else
			ammo = 0;
		clip = Mathf.Min(_maxClipSize,ammo);
		reloading = false;
	}
	
	public Vector3 GetLocation()
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
	
	public bool NeedToReload()
	{
		return clip < _maxClipSize;
	}
	
	void OnDisable()
	{
		CancelInvoke();
	}
}
