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
	protected GameObject tempOrderTarget = null;
	protected Commander commander = null;
	protected float TEMP_GAMEOBJECT_REMOVE_TIME = 1.0f;
	protected Unit[] lastDetectedUnits = null;
	public Unit[] ownedUnits;
	
	protected override void ClassUpdate ()
	{
		if(!IsOwnedByPlayer())
			return;
		// Everything below here only affects the player's team.
		Unit[] layerChange = ChangeNearbyUnitLayers(gameObject.tag);
		CheckUnitLayerDiff(layerChange);
	}
	
	protected Unit[] ChangeNearbyUnitLayers(string unitTag)
	{
		Unit[] detectedUnits = null;
		detectedUnits = DetectUnits(unitTag,50.0f);
		if(detectedUnits.Length == 0)
		{
			return new Unit[0];
		}
		foreach(Unit unit in detectedUnits)
		{
			unit.gameObject.layer = LayerMask.NameToLayer("Default");
			if(unit.weapon != null)
				unit.weapon.gameObject.layer = LayerMask.NameToLayer("Default");
		}
		return detectedUnits;
	}
	
	protected void CheckUnitLayerDiff(Unit[] newDetectedUnits)
	{
		if(lastDetectedUnits == null || lastDetectedUnits.Length == 0)
		{
			lastDetectedUnits = newDetectedUnits;
			return;
		}
		HashSet<Unit> oldDetectedUnitSet = new HashSet<Unit>(lastDetectedUnits);
		if(oldDetectedUnitSet.Count > 0)
		{
			HashSet<Unit> newDetectedUnitSet = new HashSet<Unit>(newDetectedUnits);
			oldDetectedUnitSet.ExceptWith(newDetectedUnitSet);
			if(oldDetectedUnitSet.Count > 0)
			{
				Unit[] notDetectedAnymore = new Unit[oldDetectedUnitSet.Count];
				oldDetectedUnitSet.CopyTo(notDetectedAnymore);
				foreach(Unit u in notDetectedAnymore)
				{
					if(u == null || u is Leader)
						continue;
					u.gameObject.layer = LayerMask.NameToLayer("Units");
					if(u.weapon != null)
						u.weapon.gameObject.layer = LayerMask.NameToLayer("Units");
				}
			}
		}
		lastDetectedUnits = newDetectedUnits;
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
		ownedUnits = new Unit[unitID.Count];
		unitID.Values.CopyTo(ownedUnits,0);
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
		//Debug.Log ("Recieved order from "+giver+": "+order.ToString()+" "+target.position);
		base.RecieveOrder (order, target, giver);
		GiveOrder(order,target);
	}
	
	public void DowngradeUnit()
	{
		selectedUnits.Clear();
		Unit[] units = new Unit[unitID.Count];
		unitID.Values.CopyTo(units,0);
		foreach(Unit u in units)
		{
			u.RegisterLeader(commander);
		}
		Unit downgrade = gameObject.AddComponent<Unit>();
		leader = (Leader)commander;
		downgrade.CloneUnit(this);
		if(IsLedByPlayer())
			MessageList.Instance.AddMessage(uName+", acknowledging demotion to grunt.");
		Destroy(this);
	}
	
	public void GiveOrder(Order order, Vector3 targetPos)
	{
		if(tempOrderTarget != null)
			DestroyImmediate(tempOrderTarget);
		tempOrderTarget = new GameObject("Order Target");
		tempOrderTarget.transform.position = targetPos;
		GiveOrder(order,tempOrderTarget.transform);
		//Destroy(tempOrderTarget,TEMP_GAMEOBJECT_REMOVE_TIME);
	}
	
	public virtual void GiveOrder(Order order, Transform target)
	{
		Unit[] squad = GetSquadMembers();
		foreach(Unit unit in squad)
		{
			GiveOrder(order,target,unit);
		}
	}
	
	public virtual void GiveOrder(Order order, Transform target, Unit unit)
	{
		if(unit.GetOrder() != Order.stop && unit.GetLastOrderer() == Commander.player)
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
