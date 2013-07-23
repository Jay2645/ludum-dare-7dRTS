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
	
	public void RegisterUnit(Unit unit)
	{
		int id = unit.GetID();
		if(unitID.ContainsKey(id))
			return;
		if(leaderLookup.ContainsKey(id))
			leaderLookup[id].RemoveUnit(id);
		unitID.Add(id,unit);
		leaderLookup.Add(id,this);
		Debug.Log("Registered ID number "+id);
	}
	
	public void RemoveUnit(int id)
	{
		if(unitID.ContainsKey(id))
		{
			unitID.Remove(id);
			leaderLookup.Remove(id);
			selectedUnits.Remove(id);
		}
	}
	
	public void GiveOrder(Order order, Vector3 targetPos)
	{
		if(orderTarget != null)
			DestroyImmediate(orderTarget);
		orderTarget = new GameObject("Order Target");
		orderTarget.transform.position = targetPos;
		GiveOrder(order,orderTarget.transform);
		Destroy(orderTarget,7.0f);
	}
	
	public void GiveOrder(Order order, Transform target)
	{
		int[] ids = selectedUnits.ToArray();
		foreach(int id in ids)
		{
			unitID[id].RecieveOrder(order,target);
		}
	}
}
