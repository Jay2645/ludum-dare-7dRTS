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
	}
	
	public void SetOwner(Unit newOwner)
	{
		owner = newOwner;
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>(); // Sometimes, gameObject.renderer returns our TrailRenderer instead.
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
		if(	Mathf.Abs(transform.position.x) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.y) > PROJECTILE_MAX_TRAVEL_DISTANCE || 
			Mathf.Abs(transform.position.z) > PROJECTILE_MAX_TRAVEL_DISTANCE)
		{
			Debug.Log("Travelled too far, removing.");
			DestroyImmediate(gameObject);
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
		if(owner != null && unit == owner)
			return;
		Debug.Log ("Hit: "+collide.name);
		if(unit != null)
		{
			if(owner != null)
				owner.weapon.AddHit();
			if(gameObject.GetComponent<MeshRenderer>() != null) // Tracer damage is handled in Weapon.cs
				unit.health -= damage;
		}
		Destroy (gameObject);
	}
}
