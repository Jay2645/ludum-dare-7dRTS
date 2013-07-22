using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Commander : Leader
{
	public bool isPlayer = false;
	public int unitsToGenerate = 0;
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
	
	void Update()
	{
		if(Input.GetButton("Select"))
		{
			float selectRadius = 0.45f;
			while(selectRadius < 0.56f)
			{
				Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(selectRadius, selectRadius, 0));
				RaycastHit hit;
				if (Physics.Raycast(selectRay, out hit,Mathf.Infinity))
				{
					Unit hitUnit = hit.transform.GetComponentInChildren<Unit>();
					if(hitUnit != null)
					{
						int id = hitUnit.GetID();
						if(!selectedUnits.Contains(id) && hitUnit.Select())
							selectedUnits.Add(hitUnit.GetID());
					}
				}
				Debug.DrawRay(selectRay.origin,selectRay.direction);
				selectRadius += 0.01f;
			}
		}
		else if(Input.GetButtonDown("Deselect"))
		{
			int[] ids = selectedUnits.ToArray();
			foreach(int id in ids)
			{
				unitID[id].Deselect();
			}
			selectedUnits.Clear();
		}
	}
	
	public void GenerateUnit(GameObject unit)
	{
		GameObject unitInstance = Instantiate(unit) as GameObject;
		Unit unitScript = unitInstance.GetComponent<Unit>();
		if(unitScript == null)
			unitScript = unitInstance.AddComponent<Unit>();
		unitScript.RegisterLeader(this);
		unitInstance.transform.position = transform.position + new Vector3(Random.Range(-10.0F, 10.0F), 1001, Random.Range(-10.0F, 10.0F));
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
