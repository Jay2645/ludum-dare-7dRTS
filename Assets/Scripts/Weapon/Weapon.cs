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
	protected Unit owner = null;
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
	protected bool lightEnabled = false;
	protected LayerMask layers;
	
	protected float WEAPON_DESPAWN_TIME = 30.0f;
	
	void Start()
	{
		if(tracer == null)
		{
			tracer = Resources.Load("Prefabs/Tracer") as GameObject;
		}
		if(owner != null && light != null)
		{
			light.color = owner.teamColor;
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
		if(lightEnabled && Random.value * 3 < 1)
		{
			lightEnabled = false;
			light.enabled = false;
		}
		if(ammo > 0 && clip <= 0 && !reloading)
		{
			Reload();
			return;
		}
		if(reloading || ammo == 0)
			return;
		if(owner == (Unit)Commander.player && !MapView.IsShown())
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
		if(lightEnabled)
		{
			if(MapView.IsShown())
			{
				light.enabled = false;
			}
			else
			{
				light.enabled = true;
			}
		}
		timer += Time.deltaTime;
	}
	
	/// <summary>
	/// Handles things all weapons do when shooting, such as managing ammo count.
	/// </summary>
	public void Shoot()
	{
		if(this == null || gameObject == null) // For some reason, we keep trying to shoot after we've been destroyed.
			return;
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
		if(owner == null || MapView.IsShown() && owner.gameObject.layer != LayerMask.NameToLayer("Default"))
		{
			if(light != null)
			{
				light.enabled = false;
				lightEnabled = false;
			}
			return;
		}
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
			lightEnabled = true;
			light.enabled = true;
		}
	}
	
	/// <summary>
	/// Shoots a physical projectile out of the gun, which has a definite travel time.
	/// </summary>
	protected virtual void OnShootProjectile()
	{
		Projectile proj = Instantiate(projectile,transform.position,transform.rotation) as Projectile;
		proj.gameObject.transform.position = transform.position + transform.up + ShootError();
		proj.damage = damage;
		proj.SetOwner(owner);
		proj.MoveForward(transform.up * proj.speed);
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
				tracerInstance.layer = gameObject.layer;
				tracerInstance.transform.position = transform.position + (transform.up * 0.45f);
				tracerInstance.GetComponent<Projectile>().MoveForward(shotDirection);
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
		if(this == null || gameObject == null) // For some reason this is firing even after the script has been destroyed?
			return Vector3.zero;
		float sprayX = (1 - Random.value) * shotSpread / 5;
		float sprayZ = (1 - Random.value) * shotSpread / 5;
	 	return transform.TransformDirection(new Vector3(sprayX, 0, sprayZ));
	}
	
	protected void Reload()
	{
		if(clip == _maxClipSize || ammo == 0)
			return;
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
	
	public void Drop()
	{
		CancelInvoke();
		transform.parent = null;
		collider.enabled = true;
		rigidbody.isKinematic = false;
		rigidbody.useGravity = true;
		owner = null;
		Invoke ("Despawn",WEAPON_DESPAWN_TIME);
	}
	
	public void Pickup(Unit owner)
	{
		CancelInvoke();
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		collider.enabled = false;
		if(owner is Commander)
		{
			if((Commander)owner == Commander.player)
			{
				transform.parent = Camera.main.transform;
				transform.localPosition = playerPosition;
			}
			else
			{	
				transform.parent = owner.transform;
				transform.localPosition = unitPosition;
			}
		}
		else
		{			
			transform.parent = owner.transform;
			transform.localPosition = unitPosition;
		}
		transform.localRotation = Quaternion.Euler(90,0,0);
		this.owner = owner;
	}
	
	protected void Despawn()
	{
		Destroy(gameObject);
	}
	
	void OnDisable()
	{
		CancelInvoke();
	}
}
