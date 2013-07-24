using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// All possible orders that can be given to units.
/// </summary>
public enum Order
{
	move,
	attack,
	defend,
	stop
}

/// <summary>
/// Commander is the class that the player defaults to.
/// It is capable of promoting and demoting units, and all units answer to it.
/// There should only ever be one commander per team.
/// </summary>
public class Commander : Leader
{
	public bool isPlayer = false;
	public int unitsToGenerate = 0;
	public static Commander player = null;
	public static GameObject unitPrefab = null;
	private HashSet<int> lookingAt = new HashSet<int>();
	
	void Awake()
	{
		CreateID();
		RegisterUnit(this);
		if(isPlayer && player == null)
		{
			player = this;
			uName = "You";
			gameObject.name = "Player";
			if(weapon != null)
			{
				weapon = Instantiate(weapon) as Weapon;
				weapon.transform.parent = Camera.main.transform;
				weapon.transform.localPosition = new Vector3(0.25f, -0.2f, 0.25f);
			}
		}
		else if (weapon != null)
		{
			weapon = Instantiate(weapon) as Weapon;
			weapon.transform.parent = transform;
			weapon.transform.localPosition = new Vector3(-0.35f,0.075f,0.75f);
		}
		if(weapon != null)
		{
			weapon.transform.localRotation = Quaternion.Euler(90,0,0);
			weapon.owner = this;
		}
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
		CheckHealth();
		if(!isPlayer)
			return;
		// Everything below here only works on the player.
		HashSet<int> visibleUnits = new HashSet<int>();
		Unit hitUnit = null;
		int id = -1;
		// First, check to see what units are in the middle portion of our screen:
		float selectRadius = 0.45f;
		while(selectRadius < 0.56f)
		{
			Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(selectRadius, selectRadius, 0));
			RaycastHit hit;
			if (Physics.Raycast(selectRay, out hit,Mathf.Infinity))
			{
				hitUnit = hit.transform.GetComponentInChildren<Unit>();
				if(hitUnit != null)
				{
					id = hitUnit.GetID();
					if(unitID.ContainsKey(id))
					{
						hitUnit.IsLookedAt(true);
						visibleUnits.Add(id);
					}
				}
			}
			Debug.DrawRay(selectRay.origin,selectRay.direction);
			selectRadius += 0.01f;
		}
		// Remove all the units which we are still looking at, leaving us with a list of units we were looking at, but are no longer doing so.
		if(lookingAt.Count > 0)
		{
			lookingAt.ExceptWith(visibleUnits);
			if(lookingAt.Count > 0)
			{
				int[] notLookingAt = new int[lookingAt.Count];
				lookingAt.CopyTo(notLookingAt);
				foreach(int i in notLookingAt)
				{
					if(unitID.ContainsKey(i))
						unitID[i].IsLookedAt(false);
				}
			}
		}
		lookingAt = visibleUnits;
		// Selecting a unit:
		if(Input.GetButton("Select"))
		{
			int[] delectableSelectables = new int[lookingAt.Count];
			lookingAt.CopyTo(delectableSelectables);
			if(Input.GetButtonDown("Select") && Input.GetButton("Upgrade"))
			{
				foreach(int i in delectableSelectables)
				{
					if(unitID.ContainsKey(i))
					{
						Unit unit = unitID[i];
						if(unit is Leader)
							((Leader)unit).DowngradeUnit();
						else
							unit.UpgradeUnit(this);
					}
				}
			}
			else
			{
				foreach(int i in delectableSelectables)
				{
					if(!selectedUnits.Contains(i) && unitID.ContainsKey(i) && unitID[i].Select())
						selectedUnits.Add(i);
				}
			}
		}
		// Deselecting a unit:
		else if(Input.GetButtonDown("Deselect"))
		{
			int[] ids = selectedUnits.ToArray();
			foreach(int sID in ids)
			{
				unitID[sID].Deselect();
			}
			selectedUnits.Clear();
		}
		// Giving orders:
		else if(Input.GetButtonDown("Order"))
		{
			Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			RaycastHit hit;
			if (Physics.Raycast(selectRay, out hit,Mathf.Infinity))
			{
				hitUnit = hit.transform.GetComponentInChildren<Unit>();
				if(hitUnit != null)
				{
					id = hitUnit.GetID();
					if(unitID.ContainsKey(id))
					{
						GiveOrder (Order.defend, hit.transform);
					}
					else
					{
						GiveOrder (Order.attack, hit.transform);
					}
				}
				else
				{
					GiveOrder(Order.move,hit.point);
				}
			}
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
	
	protected override string GetClass ()
	{
		return "Commander";
	}
	
	public override Commander GetCommander()
	{
		return this;
	}
}
