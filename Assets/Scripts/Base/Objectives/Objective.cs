﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Objective : MonoBehaviour {
	public Commander owner = null;
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
		Unit unitEntered = gameObject.GetComponent<Unit>();
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
	
	public virtual void RemovePlayer(Unit player)
	{
		if(attackingContestants.Contains(player))
			attackingContestants.Remove(player);
		else if(defendingContestants.Contains(player))
			attackingContestants.Remove(player);
	}
}
