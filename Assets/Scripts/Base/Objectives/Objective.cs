using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Objective : MonoBehaviour {
	protected Commander owner = null;
	protected List<Unit> contestants = new List<Unit>();
	
	public void SetOwner(Commander newOwner)
	{
		owner = newOwner;
		gameObject.renderer.material.color = newOwner.teamColor;
	}
	
	void OnTriggerEnter(Collider other)
	{
		Unit unitEntered = other.transform.root.gameObject.GetComponentInChildren<Unit>();
		if(unitEntered == null)
			return;
		contestants.Add(unitEntered);
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
		return query.GetCommander().GetTeamID() == owner.GetTeamID();
	}
}
