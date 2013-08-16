using UnityEngine;
using System.Collections;

/// <summary>
/// A team's home base.
/// We are considering this an objective because there are some gametypes (i.e. CTF) that have goals which are completed when a unit enters this area.
/// Extending Objective allows us access to all the neat helper functions that Objective uses.
/// </summary>
public class Base : Objective {

	protected override void OnContestantEnter (Unit contestant)
	{
		if(contestant == null)
			return;
		Objective objective = contestant.transform.root.gameObject.GetComponentInChildren<Objective>();
		if(objective == null)
			return;
		objective.OnBaseEnter(contestant, this);
	}
}
