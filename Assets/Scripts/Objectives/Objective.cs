﻿using UnityEngine;
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
		ObjectiveAwake();
	}
	
	protected virtual void ObjectiveAwake() {}
	
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
		if(captureIndex != 0 && !(this is Base))
			return;
		Unit unitEntered = other.gameObject.GetComponent<Unit>();
		if(unitEntered == null)
			return;
		if(OwnsObjective(unitEntered))
		{
			//Debug.Log ("Adding "+unitEntered+" to defending units.");
			defendingContestants.Add(unitEntered);
		}
		else
		{
			unitEntered.OnCapturingObjective(this);
			attackingContestants.Add(unitEntered);
		}
		OnContestantEnter(unitEntered);
	}
	
	void OnTriggerExit(Collider other)
	{
		Unit unitExited = other.gameObject.GetComponent<Unit>();
		if(unitExited == null)
			return;
		RemovePlayer(unitExited);
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
		return query != null && owner != null && query.GetCommander() == owner;
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
		Debug.Log (capturer+" scored!");
		capturer.Score();
		capturer.currentObjective = null;
		capturer.attackObjective = null;
		capturer.GetCommander().attackObjective = null;
	}
	
	public Unit[] GetAttackers()
	{
		return attackingContestants.ToArray();
	}
	
	public static Unit[] GetAllUnitsWithObjective(Commander commander, Objective objective)
	{
		Unit[] allUnits = commander.GetAllUnits();
		List<Unit> unitsWithObjective = new List<Unit>();
		foreach(Unit u in allUnits)
		{
			if(u.currentObjective == objective)
				unitsWithObjective.Add(u);
		}
		return unitsWithObjective.ToArray();
	}
}
