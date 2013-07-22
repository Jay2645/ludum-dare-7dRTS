using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Leader : Unit 
{
	protected Dictionary<int,Unit> unitID = new Dictionary<int, Unit>();
	protected static Dictionary<int, Leader> leaderLookup = new Dictionary<int, Leader>();
	
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
		}
	}
}
