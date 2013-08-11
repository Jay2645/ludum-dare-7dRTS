using UnityEngine;
using System.Collections;

public class Flag : Objective {
	public Unit carrying;
	
	public override void RemovePlayer (Unit player)
	{
		base.RemovePlayer (player);
		if(player != carrying)
			return;
		carrying.GetCommander().attackObjective = null;
		carrying.aBase.captureIndex = 1;
		carrying = null;
		transform.parent = null;
		captureIndex = 0;
		Invoke ("Respawn",30.0f);
	}
	
	protected void Respawn()
	{
		carrying = null;
		transform.parent = null;
		attackingContestants = new System.Collections.Generic.List<Unit>();
		defendingContestants = new System.Collections.Generic.List<Unit>();
		transform.position = initialPosition;
		transform.rotation = initialRotation;
		captureIndex = 0;
		Invoke("Reset",Time.deltaTime * 2);
	}
	
	protected override void OnContestantEnter (Unit contestant)
	{
		if(carrying == null && !OwnsObjective(contestant))
		{
			CancelInvoke();
			foreach(Transform child in transform)
			{
				child.parent = null;
			}
			transform.parent = contestant.transform;
			transform.localPosition = Vector3.one;
			transform.localRotation = Quaternion.identity;
			contestant.currentObjective = this;
			carrying = contestant;
			carrying.aBase.captureIndex = 0;
			captureIndex = 1;
			carrying.GetCommander().attackObjective = carrying.aBase;
			carrying.attackObjective = carrying.aBase;
			carrying.currentObjective = carrying.aBase;
			carrying.OnPickupFlag();
			Invoke("Reset",Time.deltaTime * 2);
		}
	}
	
	public override void OnBaseEnter (Unit contestant, Base uBase)
	{
		if(carrying == null || contestant != carrying || carrying.GetCommander() != uBase.owner || owner == uBase.owner)
			return;
		OnCaptured(contestant);
		Respawn();
	}
	
	private void Reset()
	{
		RecheckObjectiveTimer.Reset();
	}
}
