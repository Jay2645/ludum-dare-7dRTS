using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class DelegateSquadsToObjective : RAIN.Action.Action
{
    public DelegateSquadsToObjective()
    {
        actionName = "DelegateSquadsToObjective";
    }
	
	private Commander commander = null;
	private Objective[] objectives;
	private Objective[] defendObjectives;
	private Objective[] attackObjectives;
	private const float TOO_FAR_AWAY_TO_RESPOND_AMOUNT = 25.0f;
	private const float MIN_DEFENDERS_PERCENTAGE = 0.2f;
	private const float MAX_ATTACKERS_PERCENTAGE = 0.8f;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(commander == null)
		{
			commander = agent.Avatar.GetComponent<Commander>();
			if(commander == null)
				return RAIN.Action.Action.ActionResult.FAILURE;
		}
		Objective attackObjective = commander.attackObjective;
		Objective defendObjective = commander.defendObjective;
		if(attackObjective == null || defendObjective == null)
		{
			Debug.Log ("An objective is null; looking for one.");
			GameObject[] objectiveGOs = GameObject.FindGameObjectsWithTag("Objective");
			List<Objective> objectiveList = new List<Objective>();
			List<Objective> defendObjectiveList = new List<Objective>();
			List<Objective> attackObjectiveList = new List<Objective>();
			foreach(GameObject go in objectiveGOs)
			{
				Objective objective = go.GetComponent<Objective>();
				if(objective == null || objective is Base && objective.captureIndex != 0)
					continue;
				objectiveList.Add(objective);
				if(objective.owner.GetTeamID() == commander.GetTeamID())
				{
					defendObjectiveList.Add(objective);
				}
				else
				{
					attackObjectiveList.Add(objective);
				}
			}
			objectives = objectiveList.ToArray();
			if(defendObjective == null)
				defendObjectives = defendObjectiveList.ToArray();
			if(attackObjective == null)
				attackObjectives = attackObjectiveList.ToArray();
		}
		else
		{
			Debug.Log("Have objectives: "+attackObjective+" and "+defendObjective+".");
			objectives = new Objective[2];
			attackObjectives = new Objective[1];
			defendObjectives = new Objective[1];
			objectives[0] = attackObjectives[0] = attackObjective;
			objectives[1] = defendObjectives[0] = defendObjective;
		}
		if(objectives == null || defendObjectives == null && attackObjectives == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		commander.SetObjectives(objectives);
		if(defendObjectives.Length == 1)
		{
			Leader defenseLeader = AllocateToSingleObjective(defendObjectives[0],MIN_DEFENDERS_PERCENTAGE);
			if(defenseLeader != null)
				defenseLeader.RecieveOrder(Order.defend,defendObjectives[0].transform,commander);
		}
		else if(defendObjectives.Length > 1)
		{
			List<Objective> canBeCapturedList = new List<Objective>();
			List<Objective> atRiskList = new List<Objective>();
			foreach(Objective objective in defendObjectives)
			{
				if(objective.captureIndex == 0)
					canBeCapturedList.Add(objective);
				else if(Mathf.Abs(objective.captureIndex) == 1)
				{
					// First, see if we already have some units stationed nearby that could respond in a reasonable amount of time.
					List<Objective> allObjectives = canBeCapturedList;
					allObjectives.AddRange(atRiskList);
					bool tooFar = true;
					Vector3 pos = objective.transform.position;
					foreach(Objective o in allObjectives.ToArray())
					{
						if(Vector3.Distance(o.transform.position,pos) <= TOO_FAR_AWAY_TO_RESPOND_AMOUNT)
							tooFar = false;
					}
					if(tooFar)
						atRiskList.Add(objective);
				}
			}
			Objective[] canBeCaptured = canBeCapturedList.ToArray();
			Objective[] atRisk = atRiskList.ToArray();
			if(canBeCaptured.Length == 1)
			{
				if(atRisk.Length == 1)
				{
					DefendSingleObjectiveAtRisk(canBeCaptured[0],atRisk[0]);
				}
				else if(atRisk.Length > 1)
				{
					AllocateToSingleObjective(canBeCaptured[0],0.4f);
				}
				else
				{
					AllocateToSingleObjective(canBeCaptured[0],MAX_ATTACKERS_PERCENTAGE);
				}
			}
			else
				DefendMultipleObjectives(canBeCaptured);
		}
		if(attackObjectives.Length == 1)
		{
			Leader attackLeader = AllocateToSingleObjective(attackObjectives[0],MAX_ATTACKERS_PERCENTAGE);
			if(attackLeader != null)
				attackLeader.RecieveOrder(Order.attack,attackObjectives[0].transform,commander);
		}
		else if(attackObjectives.Length > 1)
		{
			List<Objective> canBeCapturedList = new List<Objective>();
			List<Objective> atRiskList = new List<Objective>();
			foreach(Objective objective in attackObjectives)
			{
				if(objective.captureIndex == 0)
					canBeCapturedList.Add(objective);
				else if(Mathf.Abs(objective.captureIndex) == 1)
				{
					// First, see if we already have some units stationed nearby that could attack in a reasonable amount of time.
					List<Objective> allObjectives = canBeCapturedList;
					allObjectives.AddRange(atRiskList);
					bool tooFar = true;
					Vector3 pos = objective.transform.position;
					foreach(Objective o in allObjectives.ToArray())
					{
						if(Vector3.Distance(o.transform.position,pos) <= TOO_FAR_AWAY_TO_RESPOND_AMOUNT)
							tooFar = false;
					}
					if(tooFar)
						atRiskList.Add(objective);
				}
			}
			Objective[] canBeCaptured = canBeCapturedList.ToArray();
			Objective[] atRisk = atRiskList.ToArray();
			if(canBeCaptured.Length == 1)
			{
				if(atRisk.Length == 1)
				{
					//AttackSingleObjectiveAtRisk(canBeCaptured[0],atRisk[0]);
				}
				else if(atRisk.Length > 1)
				{
					AllocateToSingleObjective(canBeCaptured[0],0.4f);
				}
				else
				{
					AllocateToSingleObjective(canBeCaptured[0],MAX_ATTACKERS_PERCENTAGE);
				}
			}
			//else
				//AttackMultipleObjectives(canBeCaptured);
		}
		agent.actionContext.SetContextItem<Objective>("objective",commander.currentObjective);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
	
	private void ChooseCommanderObjective()
	{
		if(defendObjectives.Length == 1)
		{
			if(commander.GetHealthPercent() <= 0.5f || attackObjectives.Length == 0)
			{
				Objective objective = defendObjectives[0];
				commander.RecieveOrder(Order.defend,objective.transform,commander);
				commander.defendObjective = objective;
				commander.currentObjective = objective;
			}
		}
		if(attackObjectives.Length == 1 && defendObjectives.Length == 0)
		{
			Objective objective = attackObjectives[0];
			commander.RecieveOrder(Order.attack,objective.transform,commander);
			commander.attackObjective = objective;
			commander.currentObjective = objective;
		}
		else if(attackObjectives.Length == 1 && defendObjectives.Length == 1)
		{
			if(Random.value * 4 < 1)
			{
				Objective objective = defendObjectives[0];
				commander.RecieveOrder(Order.defend,objective.transform,commander);
				commander.defendObjective = objective;
				commander.currentObjective = objective;
			}
			else
			{
				Objective objective = attackObjectives[0];
				commander.RecieveOrder(Order.attack,objective.transform,commander);
				commander.attackObjective = objective;
				commander.currentObjective = objective;
			}
		}
	}
	
	private Leader AllocateToSingleObjective(Objective objective, float allocateAmount)
	{
		int unitTarget = Mathf.RoundToInt(commander.GetUnitCount() * allocateAmount);
		Leader[] leaders = commander.GetLeaders();
		Leader objectiveLeader = null;
		foreach(Leader leader in leaders)
		{
			if(leader.currentObjective == objective)
			{
				objectiveLeader = leader;
				break;
			}
		}
		if(objectiveLeader == null && unitTarget > 0)
		{
			if(commander.CanUpgradeUnit())
			{
				Unit[] leaderArray = DetermineClosestUnitsToTarget(commander.GetNonAssignedUnits(),objective.transform.position,1,true);
				if(leaderArray.Length == 0)
					return null;
				objectiveLeader = commander.PromoteUnit(leaderArray[0]);
			}
			else
			{
				if(commander.currentObjective == null)
				{
					objectiveLeader = (Leader)commander;
				}
				else
				{
					return null;
				}
			}
		}
		else if(unitTarget == 0)
		{
			if(objectiveLeader == null)
				return null;
			objectiveLeader.DowngradeUnit();
				return null;
		}
		objectiveLeader.currentObjective = objective;
		Unit[] squadMembers = objectiveLeader.GetSquadMembers();
		List<Unit> squadList = new List<Unit>(squadMembers);
		Unit[] objectiveSquad = Objective.GetAllUnitsWithObjective(commander,objective);
		foreach(Unit u in objectiveSquad)
		{
			if(squadList.Contains(u))
				continue;
			squadList.Add(u);
		}
		Unit[] capturing = objective.GetAttackers();
		foreach(Unit u in capturing)
		{
			if(squadList.Contains(u))
				continue;
			squadList.Add(u);
		}
		squadMembers = squadList.ToArray();
		int memberCount = squadMembers.Length + 1;
		if(memberCount == unitTarget)
			return objectiveLeader;
		if(memberCount > unitTarget)
		{
			Unit[] furthestUnits = DetermineClosestUnitsToTarget(squadMembers,objective.transform.position,memberCount - unitTarget,false);
			foreach(Unit unit in furthestUnits)
			{
				unit.RegisterLeader(commander);
			}
		}
		else if(memberCount < unitTarget)
		{
			Unit[] closestUnits = DetermineClosestUnitsToTarget(commander.GetNonAssignedUnits(),objective.transform.position,unitTarget - memberCount,true);
			foreach(Unit unit in closestUnits)
			{
				unit.RegisterLeader(objectiveLeader);
			}
		}
		Debug.Log (objectiveLeader+" is leading the attack on "+objective);
		return objectiveLeader;
	}
	
	/// <summary>
	/// Focuses part our forces primarily on a single objective. 
	/// A small reserve is sent to protect an objective which could be at risk if the objective falls.
	/// </summary>
	/// <param name='objective'>
	/// The objective to defend.
	/// </param>
	/// <param name='atRisk'>
	/// The objective which could be at risk.
	/// </param>
	private void DefendSingleObjectiveAtRisk(Objective objective, Objective atRisk)
	{
		
	}
	
	private void DefendMultipleObjectives(Objective[] canBeCaptured)
	{
		// TODO: Defend multiple objectives.
		Debug.LogWarning("TODO: Defend with multiple objectives.");
		Debug.Log ("Number of objectives attempting to defend: "+canBeCaptured.Length);
		foreach(Objective o in canBeCaptured)
			Debug.Log(o.transform.position);
	}
	
	/// <summary>
	/// Determines which units are closest or furthest from a target.
	/// </summary>
	/// <returns>
	/// The units closest or furthest from a target.
	/// </returns>
	/// <param name='unitPool'>
	/// The pool of Units to draw from.
	/// </param>
	/// <param name='target'>
	/// The target to check.
	/// </param>
	/// <param name='unitNumber'>
	/// How many Units to pull from the Unit pool.
	/// </param>
	/// <param name='nearTarget'>
	/// TRUE if we want to return the units CLOSEST to a target, FALSE if we want to return units FURTHEST from a target.
	/// </param>
	private Unit[] DetermineClosestUnitsToTarget(Unit[] unitPool, Vector3 target, int unitNumber, bool nearTarget)
	{
		// If the Unit pool is just the right size or too small, we don't need to bother sorting them.
		if(unitPool.Length <= unitNumber)
			return unitPool;
		// Keep track of the Unit and its distance from target.
		// Distance is the key here so it's easily sortable.
		SortedDictionary<float,Unit> unitsByDistance = new SortedDictionary<float, Unit>();
		foreach(Unit unit in unitPool)
		{
			if(unit == null)
				continue;
			float dist = Mathf.Pow(target.x - unit.transform.position.x,2) + Mathf.Pow(target.z - unit.transform.position.z,2);
			if(unitsByDistance.ContainsKey(dist))
				continue;
			unitsByDistance.Add(dist,unit);
		}
		if(nearTarget)
			return FindClosestUnits(unitsByDistance,unitNumber);
		else
			return FindFurthestUnits(unitsByDistance,unitNumber);
	}
	
	private Unit[] FindClosestUnits(SortedDictionary<float,Unit> unitsByDistance, int unitNumber)
	{
		int count = 0;
		List<Unit> closestUnits = new List<Unit>();
		// Iterate from closest to farthest.
		foreach(KeyValuePair<float,Unit> kvp in unitsByDistance)
		{
			if(count > unitNumber) // We have all the Units we need.
				break;
			closestUnits.Add(kvp.Value);
			count++;
		}
		return closestUnits.ToArray();
	}
	
	private Unit[] FindFurthestUnits(SortedDictionary<float,Unit> unitsByDistance, int unitNumber)
	{
		int count = 0;
		unitNumber = unitsByDistance.Count - unitNumber;
		List<Unit> closestUnits = new List<Unit>();
		foreach(KeyValuePair<float,Unit> kvp in unitsByDistance)
		{
			if(count >= unitNumber) // Only add it if we're near the end of the dictionary. 
				closestUnits.Add(kvp.Value);
			count++;
		}
		return closestUnits.ToArray();
	}
	
    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
		agent.actionContext.SetContextItem<Objective>("objective",null);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}