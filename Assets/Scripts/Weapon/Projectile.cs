using UnityEngine;
using System.Collections;

/// <summary>
/// The projectile class.
/// Moves at a constant rate, destroys itself upon hitting something.
/// Different types of projectiles should inherit from this class to do unique things (such as blowing up and causing splash damage).
/// </summary>
[RequireComponent (typeof (Rigidbody))]
public class Projectile : MonoBehaviour {
	
	public int damage = 25;
	public float speed = 500.0f;
	public Unit owner;
	private const float PROJECTILE_DESTROY_TIME = 30.0f;
	private const float PROJECTILE_MAX_TRAVEL_DISTANCE = 1500.0f;
	private const float DUD_PROJECTILE_DESTROY_TIME = 0.5f;
	public float minLightIntensity = 0.25f;
	public float maxLightIntensity = 1.0f;
	public bool lightEnabled = true;
	private TrailRenderer trailRenderer = null;
	private MeshRenderer meshRenderer = null;
	private bool disabled = false;
	
	
	void Awake()
	{
		trailRenderer = gameObject.GetComponent<TrailRenderer>();
		meshRenderer = gameObject.GetComponent<MeshRenderer>();
	}
	
	void OnEnable()
	{
		disabled = false;
		// Destroys the projectile if we can safely assume it won't hit anything.
		Invoke("Recycle",PROJECTILE_DESTROY_TIME);
		if(light != null)
		{
			if(Random.value * 3 > 1)
			{
				lightEnabled = false;
				light.enabled = false;
			}
			else
			{
				light.intensity = Random.Range(minLightIntensity,maxLightIntensity);
			}
		}
		if(particleSystem != null)
		{
			particleSystem.Play(true);
		}
	}
	
	public void SetOwner(Unit newOwner)
	{
		owner = newOwner;
		if(owner == null)
			return;
		if(meshRenderer != null && meshRenderer.material != null)
			meshRenderer.material.color = owner.teamColor;
		if(light != null)
			light.color = owner.teamColor;
		if(particleSystem != null)
		{
			gameObject.GetComponent<ParticleSystemRenderer>().material.color = owner.teamColor;
		}
	}
	
	public void MoveForward(Vector3 direction)
	{
		MoveForward(direction,speed);
	}
	
	public void MoveForward(Vector3 direction, float moveSpeed)
	{
		if(moveSpeed > 0 && direction != Vector3.zero)
		{
			disabled = false;
		}
		if(constantForce == null)
			gameObject.AddComponent<ConstantForce>();
		constantForce.force = Vector3.Normalize(direction) * moveSpeed;
	}
	
	void Update()
	{
		if(owner == null)
		{
			Recycle();
			return;
		}
		Ray groundDetection = new Ray(transform.position,transform.forward);
		RaycastHit hitInfo;
		if(Physics.Raycast(groundDetection,out hitInfo,speed,owner.raycastIgnoreLayers))
		{
			if(hitInfo.transform.tag == "Ground")
				Invoke("Recycle",DUD_PROJECTILE_DESTROY_TIME);
		}
		if(	Mathf.Abs(transform.position.x) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.y) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.z) > PROJECTILE_MAX_TRAVEL_DISTANCE)
		{
			Recycle();
			return;
		}
		if(owner == null)
			return;
		if(MapView.IsShown() && owner.gameObject.layer != LayerMask.NameToLayer("Default"))
		{
			if(lightEnabled)
			{
				light.enabled = false;
			}
			if(trailRenderer != null)
			{
				trailRenderer.enabled = false;
			}
			if(meshRenderer != null)
			{
				meshRenderer.enabled = false;
			}
		}
		else
		{
			if(lightEnabled)
			{
				light.enabled = true;
			}
			if(trailRenderer != null)
			{
				trailRenderer.enabled = true;
			}
			if(meshRenderer != null)
			{
				meshRenderer.enabled = true;
			}
		}
	}
	
	void OnCollisionEnter(Collision collision)
	{
		if(collision.gameObject.GetComponent<Weapon>() == null)
			DamageGameObject(collision.gameObject);
		if(collision.gameObject.tag == "Ground")
			Recycle();
	}
	
	void OnTriggerEnter(Collider trigger)
	{
		DamageGameObject(trigger.gameObject);
		if(trigger.tag == "Ground")
			Recycle();
	}
	
	/// <summary>
	/// Checks to see if this projectile hit a unit. If it did and the unit is not us, remove some health from that unit.
	/// After it is done, destroys the projectile.
	/// </summary>
	/// <param name='collide'>
	/// The GameObject we collided with.
	/// </param>
	private void DamageGameObject(GameObject collide)
	{
		if(disabled)
			return;
		// Never collide with certain layers.
		LayerMask layer = collide.layer;
		if(	layer == LayerMask.NameToLayer("Ignore Raycast") ||
			layer == LayerMask.NameToLayer("Heat Data") ||
			layer == LayerMask.NameToLayer("Death Data") ||
			layer == LayerMask.NameToLayer("Move Data"))
			return;
		// Find out if we hit a unit.
		Unit unit = collide.transform.root.GetComponentInChildren<Unit>();
		if(unit != null)
		{
			// If we hit a friendly and friendly fire is disabled, ignore the collision. 
			if(!Unit.friendlyFire && unit == owner || owner.IsFriendly(unit))
			{
				return;
			}
			// If we hit a weapon, we don't damage the unit holding it.
			if(unit.weapon != null && collide == unit.weapon.gameObject)
			{
				Recycle();
				return;
			}
			// Let the weapon that shot us know we hit something.
			if(owner != null)
				owner.weapon.AddHit();
			// Damage the unit, if applicable.
			if(gameObject.GetComponent<MeshRenderer>() != null) // Tracer damage is handled in Weapon.cs
				unit.Damage(damage,owner);
		}
		// Make sure we don't hit someone else afterward.
		Recycle();
	}
	
	private void Recycle()
	{
		CancelInvoke();
		disabled = true;
		if(owner == null || owner.weapon == null)
		{
			Destroy(gameObject);
			return;
		}
		if(particleSystem != null)
		{
			particleSystem.Stop(true);
		}
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;
		owner.weapon.RecycleProjectile(this);
	}
}
