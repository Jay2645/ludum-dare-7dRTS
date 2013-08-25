using UnityEngine;
using System.Collections;

public class Waypoint : MonoBehaviour {
	public float time = 5.0f;
	public bool terminus = false;
	public bool passed = false;
	void OnDrawGizmos()
	{
		//This is here to allow us to view these in the Editor.
	}
	
	public virtual void OnPass()
	{
		passed = true;
	}
}
