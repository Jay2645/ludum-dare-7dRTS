using UnityEngine;
using System.Collections;

public class Flag : Objective {
	protected Unit carrying;
	
	public override void RemovePlayer (Unit player)
	{
		base.RemovePlayer (player);
		if(player != carrying)
			return;
		carrying = null;
		transform.parent = null;
		Invoke ("Respawn",30.0f);
	}
	
	protected void Respawn()
	{
		transform.parent = null;
		transform.position = initialPosition;
		transform.rotation = initialRotation;
	}
	
	protected override void OnContestantEnter (Unit contestant)
	{
		if(carrying == null && !OwnsObjective(contestant) && defendingContestants.Count == 0)
		{
			CancelInvoke();
			transform.parent = contestant.transform;
			transform.localPosition = Vector3.one;
			transform.localRotation = Quaternion.identity;
			contestant.currentObjective = this;
			carrying = contestant;
		}
	}
}
