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
	public float speed = 25.0f;
	public Unit owner;
	private const float PROJECTILE_DESTROY_TIME = 30.0f;
	
	void Start()
	{
		// Destroys the projectile if we can safely assume it won't hit anything.
		Destroy (gameObject,PROJECTILE_DESTROY_TIME);
	}
	
	void OnCollisionEnter(Collision collision)
	{
		DamageGameObject(collision.gameObject);
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
		Unit unit = collide.GetComponentInChildren<Unit>();
		if(owner != null && unit == owner)
			return;
		if(unit != null)
		{
			unit.health -= damage;
		}
		Destroy (gameObject);
	}
}
