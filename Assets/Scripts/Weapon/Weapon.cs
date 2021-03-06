﻿using System.Collections.Generic;
using UnityEngine;

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
	public float shotSpread = 2.0f;

	protected float lastShotTime = 0;
	public Projectile projectile = null;
	protected Unit owner = null;
	public Vector3 unitPosition = new Vector3(-0.35f, 0.075f, 0.75f);
	public Vector3 playerPosition = new Vector3(0.35f, -0.2f, 0.45f);
	public static GameObject tracer = null;
	protected bool reloading = false;
	protected int _maxClipSize = 40;
	protected int _maxAmmoCount = 400;
	public AudioClip fire = null;
	public AudioClip reload = null;
	public AudioClip ammoPickup = null;

	protected int projectileHits = 0;
	protected int _projectileHits;
	protected int shotsFired = 0;
	protected int _shotsFired;
	protected float timer = 0.00f;
	protected bool lightEnabled = false;
	protected LayerMask layers;
	protected BoxCollider physicsCollider;
	protected SphereCollider triggerCollider;

	protected float WEAPON_DESPAWN_TIME = 30.0f;
	protected float RECENT_SHOT_TIME = 7.0f;
	/// <summary>
	/// This is a list containing a bunch of projectiles already loaded into memory.
	/// This avoids the lag caused by many successive Instantiate calls -- when we shoot, we only instantiate the first few projectiles.
	/// </summary>
	protected List<Projectile> projectilePool = new List<Projectile>();

	void Awake()
	{
		physicsCollider = gameObject.GetComponent<BoxCollider>();
		triggerCollider = gameObject.GetComponent<SphereCollider>();
		if (physicsCollider == null)
			physicsCollider = gameObject.AddComponent<BoxCollider>();
		if (triggerCollider == null)
			triggerCollider = gameObject.AddComponent<SphereCollider>();
		physicsCollider.isTrigger = false;
		triggerCollider.isTrigger = true;
		triggerCollider.enabled = false;
		if (_maxAmmoCount > 0)
			ammo = _maxAmmoCount;
	}

	void Start()
	{
		if (tracer == null)
		{
			tracer = Resources.Load("Prefabs/Tracer") as GameObject;
		}
		MakeOwner();
		if (ammo == 0)
			ammo = Mathf.Max(ammo, clip * 4);
		_maxClipSize = clip;
		_maxAmmoCount = ammo;
	}

	void Update()
	{
		if (owner == null)
			return;
		if (timer >= 1 && _shotsFired > 0)
		{
			projectileHits = _projectileHits;
			shotsFired = _shotsFired;
			timer = 0;
			_projectileHits = 0;
			_shotsFired = 0;
		}
		if (lightEnabled && Random.value * 3 < 1)
		{
			lightEnabled = false;
			light.enabled = false;
		}
		if (ammo > 0 && clip <= 0 && !reloading)
		{
			Reload();
			return;
		}
		if (reloading || ammo == 0)
			return;
		if (owner.IsPlayer() && !MapView.IsShown())
		{
			if (fireOnce)
			{
				if (Input.GetButtonDown("Fire1"))
				{
					Shoot();
				}
			}
			else
			{
				if (Input.GetButton("Fire1"))
				{
					Shoot();
				}
			}
			if (Input.GetButtonDown("Reload") && !Input.GetButton("Fire1"))
				Reload();
		}
		if (lightEnabled)
		{
			if (MapView.IsShown())
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

	private void MakeOwner()
	{
		if (owner != null && light != null)
		{
			light.color = owner.teamColor;
			if (audio != null && gameObject.GetComponent<RAIN.Ontology.Decoration>() == null)
			{
				gameObject.AddComponent<RAIN.Ontology.Entity>();
				RAIN.Ontology.Decoration decoration = gameObject.AddComponent<RAIN.Ontology.Decoration>();
				RAIN.Ontology.Aspect aspect = new RAIN.Ontology.Aspect(owner.gameObject.tag + " Gunshot", new RAIN.Ontology.Sensation("sound"));
				decoration.aspect = aspect;
			}
		}
		else
		{
			RAIN.Ontology.Decoration decoration = gameObject.GetComponent<RAIN.Ontology.Decoration>();
			if (decoration != null)
			{
				Destroy(decoration);
				Destroy(gameObject.GetComponent<RAIN.Ontology.Entity>());
			}
		}
	}

	/// <summary>
	/// Handles things all weapons do when shooting, such as managing ammo count.
	/// </summary>
	public void Shoot()
	{
		if (this == null || gameObject == null || owner == null || PauseMenu.IsPaused()) // For some reason, we keep trying to shoot after we've been destroyed.
			return;
		if (reloading || ammo <= 0 || lastShotTime > 0 && Time.time - lastShotTime < timeBetweenShots)
			return;
		_shotsFired++;
		lastShotTime = Time.time;
		if (projectile == null)
		{
			OnShootRaycast();
		}
		else
		{
			OnShootProjectile();
		}
		clip--;
		ammo--;
		if (owner == null || MapView.IsShown() && owner.gameObject.layer != LayerMask.NameToLayer("Default"))
		{
			if (light != null)
			{
				light.enabled = false;
				lightEnabled = false;
			}
			return;
		}
		if (audio != null && fire != null)
		{
			audio.PlayOneShot(fire);
		}
		if (particleSystem != null)
		{
			particleSystem.Play();
		}
		if (light != null)
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
		Projectile proj = GetProjectile();
		proj.transform.position = transform.position + (transform.up * 0.5f);
		proj.transform.rotation = transform.rotation;
		proj.gameObject.layer = gameObject.layer;
		proj.damage = damage;
		proj.SetOwner(owner);
		proj.MoveForward(ShootError());
	}

	/// <summary>
	/// Raycasts forward and damages anything the raycast hits instantly.
	/// </summary>
	protected virtual void OnShootRaycast()
	{
		RaycastHit hit;
		Vector3 shotDirection = ShootError();
		Ray directionRay = new Ray(transform.position, shotDirection);
		Debug.DrawRay(transform.position + transform.up, shotDirection, Color.blue);
		if (Physics.Raycast(directionRay, out hit, Mathf.Infinity, Commander.player.raycastIgnoreLayers))
		{
			if (Random.value * (4 / 3) <= 1)
			{
				Projectile tracerInstance = GetProjectile();
				tracerInstance.gameObject.layer = gameObject.layer;
				tracerInstance.transform.position = transform.position + (transform.up * 0.5f);
				tracerInstance.damage = 0;
				tracerInstance.SetOwner(owner);
				tracerInstance.MoveForward(shotDirection);
			}
			Unit hitUnit = hit.transform.root.gameObject.GetComponentInChildren<Unit>();
			if (hitUnit != null)
			{
				_projectileHits++;
				hitUnit.Damage(damage, owner);
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
		if (shotsFired == 0)
			return accuracy;
		accuracy = (float)projectileHits / (float)shotsFired;
		return accuracy;
	}

	protected Vector3 ShootError()
	{
		if (this == null || gameObject == null) // For some reason this is firing even after the script has been destroyed?
			return Vector3.forward;
		float sprayX = (1 - Random.Range(0.0f, 2.0f)) * shotSpread * 2;
		float sprayZ = (1 - Random.Range(0.0f, 2.0f)) * shotSpread * 2;
		return Quaternion.Euler(sprayX, 0, sprayZ) * transform.up;
	}

	protected void Reload()
	{
		if (clip == _maxClipSize || ammo == 0)
			return;
		reloading = true;
		if (audio != null && reload != null)
		{
			audio.PlayOneShot(reload);
		}
		Invoke("DoneReloading", reloadTime);
	}

	protected void DoneReloading()
	{
		int clipDelta = _maxClipSize - clip;
		if (ammo > clipDelta)
			ammo -= clipDelta;
		else
			ammo = 0;
		clip = Mathf.Min(_maxClipSize, ammo);
		reloading = false;
	}

	public Vector3 GetLocation()
	{
		if (owner != null && owner is Commander && ((Commander)owner).isPlayer)
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

	public bool AddAmmo(int bulletAmount)
	{
		if (ammo == _maxAmmoCount)
			return false;
		ammo += bulletAmount;
		ammo = Mathf.Min(ammo, _maxAmmoCount);
		if (bulletAmount >= _maxClipSize)
			audio.PlayOneShot(ammoPickup);
		return true;
	}

	public void Drop()
	{
		CancelInvoke();
		transform.parent = null;
		physicsCollider.enabled = true;
		triggerCollider.enabled = true;
		rigidbody.isKinematic = false;
		rigidbody.useGravity = true;
		light.enabled = false;
		owner = null;
		Invoke("Despawn", WEAPON_DESPAWN_TIME);
	}

	public void Pickup(Unit owner)
	{
		if (this == null)
			return;
		CancelInvoke();
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		triggerCollider.enabled = false;
		physicsCollider.enabled = false;
		if (owner.IsPlayer())
		{
			transform.parent = Camera.main.transform;
			transform.localPosition = playerPosition;
		}
		else
		{
			transform.parent = owner.transform;
			transform.localPosition = unitPosition;
		}
		transform.localRotation = Quaternion.Euler(90, 0, 0);
		this.owner = owner;
		MakeOwner();
	}

	void OnTriggerEnter(Collider col)
	{
		Unit unit = col.transform.GetComponent<Unit>();
		if (unit == null || !unit.IsAlive() || unit.weapon == null)
			return;
		if (unit.weapon.AddAmmo(ammo + clip))
		{
			CancelInvoke();
			Destroy(gameObject);
		}
	}

	public Unit GetOwner()
	{
		return owner;
	}

	public bool HasShotRecently()
	{
		if (owner == null || lastShotTime == 0.0f)
			return false;
		return lastShotTime < Time.time - RECENT_SHOT_TIME;
	}

	protected void Despawn()
	{
		Destroy(gameObject);
	}

	protected Projectile GetProjectile()
	{
		foreach (Projectile p in projectilePool.ToArray())
		{
			if (p == null || p.gameObject.activeInHierarchy)
				continue;
			p.gameObject.SetActive(true);
			return p;
		}
		// If we've gotten this far, we don't have any valid projectiles in the pool.
		Projectile proj = Instantiate(projectile) as Projectile;
		projectilePool.Add(proj);
		return proj;
	}

	public void RecycleProjectile(Projectile recycle)
	{
		recycle.gameObject.SetActive(false);
		// Make sure it's inside the projectile pool.
		foreach (Projectile p in projectilePool.ToArray())
		{
			if (recycle == p)
			{
				return;
			}
		}
		// If we've gotten this far, for some reason the projectile was not included in the pool.
		projectilePool.Add(recycle);
	}

	void OnDisable()
	{
		CancelInvoke();
	}

	void OnDestroy()
	{
		foreach (Projectile p in projectilePool.ToArray())
		{
			if (p != null)
				Destroy(p.gameObject);
		}
	}
}
