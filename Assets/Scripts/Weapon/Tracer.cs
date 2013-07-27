using UnityEngine;
using System.Collections;

[RequireComponent (typeof (ConstantForce))]
public class Tracer : MonoBehaviour {
	private const float TRACER_MOVE_SPEED = 500.0f;
	private const float DESTROY_TRACER_TIME = 0.5f;
	public float minLightIntensity = 0.25f;
	public float maxLightIntensity = 1.0f;
	
	void Awake()
	{
		Destroy(gameObject,DESTROY_TRACER_TIME);
		if(light != null)
		{
			if(Random.value * 3 > 1)
			{
				light.enabled = false;
			}
			else
			{
				light.intensity = Random.Range(minLightIntensity,maxLightIntensity);
			}
		}
	}
	
	public void MoveForward(Vector3 direction)
	{
		MoveForward(direction,TRACER_MOVE_SPEED);
	}
	
	public void MoveForward(Vector3 direction, float moveSpeed)
	{
		constantForce.force = Vector3.Normalize(direction) * moveSpeed;
	}
	
	void OnCollisionEnter()
	{
		Destroy(gameObject);
	}
}
