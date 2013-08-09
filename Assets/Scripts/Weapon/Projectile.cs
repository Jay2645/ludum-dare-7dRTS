using UnityEngine;
using System.Collections;

/// <summary>
/// The projectile class.
/// Moves at a constant rate, destroys itself upon hitting something.
/// Different types of projectiles should inherit from this class to do unique things (such as blowing up and causing splash damage).
/// </summary>
[RequireComponent (typeof (Rigidbody))]
[RequireComponent (typeof (ConstantForce))]
public class Projectile : MonoBehaviour {
	
	public int damage = 25;
	public float speed = 500.0f;
	public Unit owner;
	private const float PROJECTILE_DESTROY_TIME = 30.0f;
	private const float PROJECTILE_MAX_TRAVEL_DISTANCE = 1500.0f;
	public float minLightIntensity = 0.25f;
	public float maxLightIntensity = 1.0f;
	public bool lightEnabled = true;
	private TrailRenderer trailRenderer = null;
	private MeshRenderer meshRenderer = null;
	
	void Awake()
	{
		// Destroys the projectile if we can safely assume it won't hit anything.
		Destroy (gameObject,PROJECTILE_DESTROY_TIME);
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
		trailRenderer = gameObject.GetComponent<TrailRenderer>();
		meshRenderer = gameObject.GetComponent<MeshRenderer>();
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
	}
	
	public void MoveForward(Vector3 direction)
	{
		MoveForward(direction,speed);
	}
	
	public void MoveForward(Vector3 direction, float moveSpeed)
	{
		constantForce.force = Vector3.Normalize(direction) * moveSpeed;
	}
	
	void Update()
	{
		if(	Mathf.Abs(transform.position.x) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.y) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.z) > PROJECTILE_MAX_TRAVEL_DISTANCE)
		{
			DestroyImmediate(gameObject);
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
	}
	
	void OnTriggerEnter(Collider trigger)
	{
		DamageGameObject(trigger.gameObject);
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
		if(collide.layer == LayerMask.NameToLayer("Ignore Raycast"))
			return;
		Unit unit = collide.transform.root.GetComponentInChildren<Unit>();
		if(owner != null && (unit == owner || unit != null && unit.weapon != null && collide == unit.weapon.gameObject))
			return;
		if(unit != null)
		{
			if(owner != null)
				owner.weapon.AddHit();
			if(gameObject.GetComponent<MeshRenderer>() != null) // Tracer damage is handled in Weapon.cs
				unit.health -= damage;
		}
		Destroy(gameObject);
	}
}
