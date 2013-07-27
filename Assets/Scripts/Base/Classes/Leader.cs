using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A leader micromanages a small group of units.
/// While a Commander has to manage many units, a Leader only has to manage units assigned to them.
/// An effective Commander will promote some of their units to Leaders, then distribute the remaining units among the leaders.
/// This way, a Commander just needs to order around the Leaders, and the Leaders will worry about micromanaging their own troops.
/// </summary>
public class Leader : Unit 
{
	protected Dictionary<int,Unit> unitID = new Dictionary<int, Unit>();
	protected static Dictionary<int, Leader> leaderLookup = new Dictionary<int, Leader>();
	protected List<int> selectedUnits = new List<int>();
	protected GameObject orderTarget = null;
	protected Commander commander = null;
	protected float TEMP_GAMEOBJECT_REMOVE_TIME = 7.0f;
	
	protected override void ClassSpawn ()
	{
		if(smokeTrail != null)
			Destroy(smokeTrail);
	}
	
	public void RegisterUnit(Unit unit)
	{
		int id = unit.GetID();
		if(unitID.ContainsKey(id))
			return;
		if(leaderLookup.ContainsKey(id))
			leaderLookup[id].RemoveUnit(id);
		unitID.Add(id,unit);
		commander.AddUnit(unit);
		leaderLookup.Add(id,this);
		if(currentOrder != Order.stop)
		{
			GiveOrder(currentOrder,moveTarget,unit);
		}
		Debug.Log("Registered ID number "+id);
	}
	
	/// <summary>
	/// Registers the commander. Also makes sure that the leader is correct.
	/// </summary>
	/// <param name='commander'>
	/// The commander to register.
	/// </param>
	public void RegisterCommander(Commander commander)
	{
		this.commander = commander;
		RegisterLeader(this);
		commander.RegisterUnit(this);
	}
	
	public override void RegisterLeader (Leader leader)
	{
		this.leader = commander;
	}
	
	public virtual void RemoveUnit(int id)
	{
		if(unitID.ContainsKey(id))
		{
			unitID.Remove(id);
			if(leaderLookup.ContainsKey(id))
			{
				leaderLookup.Remove(id);
			}
			selectedUnits.Remove(id);
		}
	}
	
	public void ReplaceUnit(int id, Unit newUnit)
	{
		bool isSelected = selectedUnits.Contains(id);
		RemoveUnit(id);
		if(isSelected)
			selectedUnits.Add(id);
		RegisterUnit(newUnit);
	}
	
	public override void RecieveOrder (Order order, Transform target, Leader giver)
	{
		Debug.Log ("Recieved order from "+giver+": "+order.ToString()+" "+target.position);
		base.RecieveOrder (order, target, giver);
		GiveOrder(order,target);
	}
	
	public void DowngradeUnit()
	{
		selectedUnits.Clear();
		foreach(KeyValuePair<int, Unit> kvp in unitID)
		{
			kvp.Value.RegisterLeader(commander);
		}
		Unit downgrade = gameObject.AddComponent<Unit>();
		leader = (Leader)commander;
		downgrade.CloneUnit(this);
		MessageList.Instance.AddMessage(uName+", acknowledging demotion to grunt.");
		Destroy(this);
	}
	
	public void GiveOrder(Order order, Vector3 targetPos)
	{
		if(orderTarget != null)
			DestroyImmediate(orderTarget);
		orderTarget = new GameObject("Order Target");
		orderTarget.transform.position = targetPos;
		GiveOrder(order,orderTarget.transform);
		Destroy(orderTarget,TEMP_GAMEOBJECT_REMOVE_TIME);
	}
	
	public void GiveOrder(Order order, Transform target)
	{
		int[] ids = selectedUnits.ToArray();
		foreach(int id in ids)
		{
			GiveOrder(order,target,unitID[id]);
		}
	}
	
	public void GiveOrder(Order order, Transform target, Unit unit)
	{
		bool isCommander = this is Commander;
		// Only give orders to a unit if it doesn't already have orders from someone who outranks us.
		if(!isCommander && unit.GetOrder() != Order.stop && unit.GetLastOrderer() is Commander)
			return;
		unit.RecieveOrder(order,target,this);
	}
	
	protected Objective GetAttackObjective()
	{
		if(attackObjective == null)
			attackObjective = commander.attackObjective;
		return attackObjective;
	}
	
	protected Objective GetDefendObjective()
	{
		if(defendObjective == null)
			defendObjective = commander.defendObjective;
		return defendObjective;
	}
	
	protected override string GetClass ()
	{
		return "Leader";
	}
	
	public override int GetTeamID ()
	{
		if(commander == null)
			return -1;
		return commander.GetTeamID();
	}
	
	/// <summary>
	/// Gets the squad member count.
	/// </summary>
	/// <returns>
	/// The squad member count. Note that this does NOT include us.
	/// </returns>
	public int GetSquadMemberCount()
	{
		return unitID.Count;
	}
	
	public Unit[] GetSquadMembers()
	{
		Unit[] unitArray = new Unit[unitID.Count];
		unitID.Values.CopyTo(unitArray,0);
		return unitArray;
	}
	
	public override Commander GetCommander()
	{
		return commander;
	}
}
