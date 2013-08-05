using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Objective : MonoBehaviour {
	public Commander owner = null;
	/// <summary>
	/// The capture index must be 0 to flag an objective as "active."
	/// If there are objectives which must be captured in a certain sequence, the next objective in the sequence should be 1, the one after 2, etc.
	/// </summary>
	public int captureIndex = 0;
	protected List<Unit> defendingContestants = new List<Unit>();
	protected List<Unit> attackingContestants = new List<Unit>();
	protected Vector3 initialPosition;
	protected Quaternion initialRotation;
	
	void Awake()
	{
		initialPosition = transform.position;
		initialRotation = transform.rotation;
	}
	
	public void SetOwner(Commander newOwner)
	{
		if(owner == newOwner)
			return;
		List<Unit> oldDefenders = defendingContestants;
		defendingContestants = attackingContestants;
		attackingContestants = oldDefenders;
		owner = newOwner;
		gameObject.renderer.material.color = newOwner.teamColor;
	}
	
	void OnTriggerEnter(Collider other)
	{
		if(captureIndex != 0)
			return;
		Unit unitEntered = other.gameObject.GetComponent<Unit>();
		if(unitEntered == null)
			return;
		if(OwnsObjective(unitEntered))
			defendingContestants.Add(unitEntered);
		else
			attackingContestants.Add(unitEntered);
		OnContestantEnter(unitEntered);
	}
	
	/// <summary>
	/// Called whenever a new Unit begins contesting the objective.
	/// Either side can contest the objective.
	/// If a defending unit contests an objective it already owns, attacking units are prevented from capturing it.
	/// </summary>
	/// <param name='contestant'>
	/// The Unit contesting the objective.
	/// </param>
	protected virtual void OnContestantEnter(Unit contestant)
	{
		
	}
	
	/// <summary>
	/// Checks to see if a Unit's team owns this objective.
	/// </summary>
	/// <returns>
	/// TRUE if the Unit's team owns the objective, else FALSE.
	/// </returns>
	/// <param name='query'>
	/// The Unit in question.
	/// </param>
	protected bool OwnsObjective(Unit query)
	{
		return owner != null && query.GetCommander().GetTeamID() == owner.GetTeamID();
	}
	
	public virtual void OnBaseEnter(Unit contestant, Base uBase)
	{
		
	}
	
	public virtual void RemovePlayer(Unit player)
	{
		if(attackingContestants.Contains(player))
			attackingContestants.Remove(player);
		else if(defendingContestants.Contains(player))
			attackingContestants.Remove(player);
	}
	
	public virtual void OnCaptured(Unit capturer)
	{
		SetOwner(capturer.GetCommander());
	}
}
