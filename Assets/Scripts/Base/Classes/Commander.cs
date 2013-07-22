using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Commander : Leader
{
	public bool isPlayer = false;
	public int unitsToGenerate = 0;
	protected Dictionary<int,Unit> unitID = new Dictionary<int, Unit>();
	public static Commander player = null;
	public static GameObject unitPrefab = null;
	
	void Awake()
	{
		CreateID();
		RegisterUnit(this);
		if(isPlayer && player == null)
			player = this;
		if(unitPrefab == null)
			unitPrefab = Resources.Load ("Prefabs/Unit") as GameObject;
		while(unitsToGenerate > 0)
		{
			GenerateUnit(unitPrefab);
			unitsToGenerate--;
		}
	}
	
	void Start()
	{
		isSelectable = false;
		leader = this;
	}
	
	public void GenerateUnit(GameObject unit)
	{
		GameObject unitInstance = Instantiate(unit) as GameObject;
		Unit unitScript = unit.GetComponent<Unit>();
		if(unitScript == null)
			unitScript = unit.AddComponent<Unit>();
		unitScript.RegisterLeader(this);
		unitInstance.transform.position = transform.position + new Vector3(Random.Range(-10.0F, 10.0F), 1001, Random.Range(-10.0F, 10.0F));
	}
	
	public void RegisterUnit(Unit unit)
	{
		unitID.Add(unit.GetID(),unit);
	}
	
	public bool IsEnemy(Unit unit)
	{
		return !unitID.ContainsKey(unit.GetID());
	}
	
	public override void RegisterLeader (Leader leader)
	{
		this.leader = this;
	}
}
