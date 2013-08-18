using UnityEngine;
using System.Collections;

/// <summary>
/// A team's home base.
/// We are considering this an objective because there are some gametypes (i.e. CTF) that have goals which are completed when a unit enters this area.
/// Extending Objective allows us access to all the neat helper functions that Objective uses.
/// </summary>
public class Base : Objective {
	
	protected float HEAL_REPEAT_TIME = 4.0f;
	protected int HEAL_AMOUNT = 25;
	protected int AMMO_AMOUNT = 20;
	
	protected override void ObjectiveAwake ()
	{
		InvokeRepeating("HealAllUnits",0.0f,HEAL_REPEAT_TIME);
	}
	
	protected override void OnContestantEnter (Unit contestant)
	{
		if(contestant == null)
			return;
		Objective objective = contestant.transform.root.gameObject.GetComponentInChildren<Objective>();
		if(objective == null)
			return;
		objective.OnBaseEnter(contestant, this);
	}
	
	protected void HealAllUnits()
	{
		foreach(Unit unit in defendingContestants.ToArray())
		{
			if(!unit.IsAlive())
				continue;
			unit.RestoreHealth(HEAL_AMOUNT);
				//Debug.Log ("Healing "+unit);
			if(unit.weapon != null)
				unit.weapon.AddAmmo(AMMO_AMOUNT);
		}
	}
}
