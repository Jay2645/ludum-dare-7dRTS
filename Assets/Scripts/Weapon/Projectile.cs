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
	
	void Start()
	{
		//Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"),LayerMask.NameToLayer("Ignore Raycast"));
		Destroy (gameObject,30.0f);
	}
	
	void OnCollisionEnter(Collision collision)
	{
		DamageGameObject(collision.gameObject);
	}
	
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
