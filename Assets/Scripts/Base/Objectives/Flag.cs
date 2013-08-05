using UnityEngine;
using System.Collections;

public class Flag : Objective {
	public Unit carrying;
	
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
		carrying = null;
		transform.parent = null;
		transform.position = initialPosition;
		transform.rotation = initialRotation;
	}
	
	protected override void OnContestantEnter (Unit contestant)
	{
		if(carrying == null && !OwnsObjective(contestant))
		{
			CancelInvoke();
			transform.parent = contestant.transform;
			transform.localPosition = Vector3.one;
			transform.localRotation = Quaternion.identity;
			contestant.currentObjective = this;
			carrying = contestant;
		}
	}
	
	public override void OnBaseEnter (Unit contestant, Base uBase)
	{
		if(carrying == null || contestant != carrying || carrying.GetCommander() != uBase.owner || owner == uBase.owner)
			return;
		Debug.Log (carrying+" scored!");
		carrying.Score();
		Respawn();
	}
}
