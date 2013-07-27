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
	protected HashSet<int> lookingAt = new HashSet<int>();
	protected int teamID = -1;
	protected static int nextTeamID = 0;
	public Camera mapCamera;
	public Vector3[] spawnPoints;
	public float spawnTime = 8.0f;
	protected float _spawnTime;
	protected List<Leader> leaders = new List<Leader>();
	public Objective[] objectives;
	protected Dictionary<int,Unit> allUnits = new Dictionary<int, Unit>();
	
	/// <summary>
	/// Called once, at the beginning of the game.
	/// Replaces Awake().
	/// </summary>
	protected override void ClassSetup ()
	{
		_spawnTime = spawnTime;
		if(defendObjective != null)
			defendObjective.SetOwner(this);
		commander = this;
		if(isPlayer && player == null)
		{
			player = this;
			uName = "You";
			gameObject.name = "Player";
		}
		if(unitPrefab == null)
			unitPrefab = Resources.Load ("Prefabs/Unit") as GameObject;
		while(unitsToGenerate > 0)
		{
			GenerateUnit(unitPrefab);
			unitsToGenerate--;
		}
		if(mapCamera != null)
		{
			mapCamera = (Instantiate(mapCamera.gameObject) as GameObject).camera;
			mapCamera.name = "Commander "+teamID+"'s Map Camera";
		}
	}
	
	/// <summary>
	/// Called every time the unit respawns and one frame after the beginning of the game.
	/// Replaces Start().
	/// </summary>
	protected override void ClassSpawn ()
	{
		spawnPoint = transform.position;
		isSelectable = false;
		leader = this;
		RegisterUnit(this);
	}
	
	/// <summary>
	/// Called every frame.
	/// Replaces Update().
	/// </summary>
	protected override void ClassUpdate()
	{
		_spawnTime -= Time.deltaTime;
		if(_spawnTime <= 0)
			_spawnTime = spawnTime;
		
		if(!isPlayer)
			return;
		// Everything below here only works on the player.
		GetLookingAt();
		// Selecting a unit:
		if(Input.GetButton("Select"))
		{
			if(Input.GetButtonDown("Select") && Input.GetButton("Upgrade"))
				UpgradeUnits();
			else
				SelectUnits();
		}
		// Deselecting a unit:
		else if(Input.GetButtonDown("Deselect"))
		{
			DeselectUnits();
		}
		// Giving orders:
		else if(Input.GetButtonDown("Order"))
		{
			GiveOrder();
		}
	}
	
	/// <summary>
	/// Adds all Units within the middle section of the screen to the lookingAt HashSet.
	/// Informs each unit within that section that they are being looked at.
	/// If a Unit was being looked at the last time we checked but isn't now, informs it that it is no longer being looked at.
	/// </summary>
	protected void GetLookingAt()
	{
		// Only players need to worry about this.
		if(!isPlayer)
			return;
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
	}
	
	/// <summary>
	/// Selects all Units we are currently looking at.
	/// </summary>
	public void SelectUnits()
	{
		// Make sure we're actually looking at something.
		if(lookingAt.Count == 0)
		{
			GetLookingAt();
			if(lookingAt.Count == 0)
			{
				// We aren't actually looking at anything; no need to continue.
				return;
			}
		}
		int[] delectableSelectables = new int[lookingAt.Count];
		lookingAt.CopyTo(delectableSelectables);
		SelectUnits(delectableSelectables);
	}
	
	/// <summary>
	/// Takes an array of Unit IDs and selects them.
	/// </summary>
	/// <param name='selection'>
	/// The Unit IDs to select.
	/// </param>
	public void SelectUnits(int[] selection)
	{
		foreach(int i in selection)
		{
			SelectUnits(i);
		}
	}
	
	/// <summary>
	/// Selects a unit by its ID.
	/// </summary>
	/// <param name='selected'>
	/// The Unit ID to select.
	/// </param>
	public void SelectUnits(int selected)
	{
		if(!selectedUnits.Contains(selected) && unitID.ContainsKey(selected) && unitID[selected].Select())
			selectedUnits.Add(selected);
	}
	
	/// <summary>
	/// Upgrades all units we are currently looking at.
	/// </summary>
	public void UpgradeUnits()
	{
		if(lookingAt.Count == 0)
		{
			GetLookingAt();
			if(lookingAt.Count == 0)
			{
				// We aren't actually looking at anything; no need to continue.
				return;
			}
		}
		int[] delectableSelectables = new int[lookingAt.Count];
		lookingAt.CopyTo(delectableSelectables);
		UpgradeUnits(delectableSelectables);
	}
	
	/// <summary>
	/// Takes an array of Unit IDs and upgrades them.
	/// </summary>
	/// <param name='selection'>
	/// The Unit IDs to select.
	/// </param>
	public void UpgradeUnits(int[] selection)
	{
		foreach(int i in selection)
		{
			UpgradeUnits(i);
		}
	}
	
	/// <summary>
	/// Upgrades/Demotes a Unit by its ID.
	/// </summary>
	/// <returns>
	/// A list of all our Leaders.
	/// </returns>
	/// <param name='selected'>
	/// The Unit to promote or demote.
	/// </param>
	public Leader[] UpgradeUnits(int selected)
	{
		if(unitID.ContainsKey(selected))
		{
			Unit unit = unitID[selected];
			if(unit is Leader)
			{
				Leader leader = (Leader)unit;
				leaders.Remove(leader);
				leader.DowngradeUnit();
			}
			else
			{
				leaders.Add(unit.UpgradeUnit(this));
			}
		}
		return GetLeaders();
	}
	
	/// <summary>
	/// Deselects all units.
	/// </summary>
	public void DeselectUnits()
	{
		int[] ids = selectedUnits.ToArray();
		foreach(int sID in ids)
		{
			unitID[sID].Deselect();
		}
		selectedUnits.Clear();
	}
	
	/// <summary>
	/// Gives orders to all selected units, moving them to a specified location.
	/// </summary>
	public void GiveOrder()
	{
		// AI don't need to rely on the camera for raycasting, and can move to exact coordinates in any event.
		if(!isPlayer)
			return;
		Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		RaycastHit hit;
		if (Physics.Raycast(selectRay, out hit,Mathf.Infinity))
		{
			Unit hitUnit = hit.transform.GetComponentInChildren<Unit>();
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
	
	public override void GiveOrder(Order order, Transform target)
	{
		if(isPlayer)
		{
			int[] ids = selectedUnits.ToArray();
			foreach(int id in ids)
			{
				GiveOrder(order,target,unitID[id]);
			}
		}
		else
		{
			Unit[] units = GetNonAssignedUnits();
			foreach(Unit unit in units)
			{
				GiveOrder(order,target,unit);
			}
		}
	}
	
	public override void GiveOrder(Order order, Transform target, Unit unit)
	{
		unit.RecieveOrder(order,target,this);
	}
	
	protected override void CreateWeapon(Weapon weapon)
	{
		if(isPlayer)
		{
			if(this.weapon != null && weapon.gameObject.activeInHierarchy)
			{
				this.weapon.transform.parent = null;
			}
			this.weapon = Instantiate(weapon) as Weapon;
			this.weapon.owner = this;
			this.weapon.transform.parent = Camera.main.transform;
			this.weapon.transform.localPosition = this.weapon.GetLocation();
			this.weapon.transform.localRotation = Quaternion.Euler(90,0,0);
		}
		else
		{
			base.CreateWeapon(weapon);
		}
	}
	
	public void AddUnit(Unit unit)
	{
		int id = unit.GetID();
		if(allUnits.ContainsKey(id))
		{
			allUnits[id] = unit;
		}
		else
		{
			allUnits.Add(id,unit);
		}
	}
	
	public override void RemoveUnit (int id)
	{
		base.RemoveUnit (id);
		if(allUnits.ContainsKey(id))
			allUnits.Remove(id);
	}
	
	public int GetUnitCount()
	{
		return allUnits.Count;
	}
	
	public Unit[] GetAllUnits()
	{
		Unit[] units = new Unit[allUnits.Count];
		allUnits.Values.CopyTo(units,0);
		return units;
	}
	
	public Unit[] GetNonAssignedUnits()
	{
		List<Unit> unitList = new List<Unit>();
		foreach(KeyValuePair<int, Unit> kvp in unitID)
		{
			Unit uValue = kvp.Value;
			if(uValue is Commander || uValue is Leader)
				continue;
			unitList.Add(uValue);
		}
		return unitList.ToArray();
	}
	
	public void SetObjectives(Objective[] objectives)
	{
		this.objectives = objectives;
	}
	
	public void SetRespawnTime(float newTime)
	{
		spawnTime = newTime;
		// Only change the time until next spawn if the new time would be shorter -- we want to maximize time each player spends playing!
		if(newTime - _spawnTime < 0.00f)
		{
			_spawnTime = newTime;
		}
	}
	
	public void AddRespawnTime(float newTime)
	{
		spawnTime += newTime;
		if(spawnTime - _spawnTime < 0.00f)
		{
			_spawnTime = newTime;
		}
	}
	
	public float GetTimeToRespawn()
	{
		return _spawnTime;
	}
	
	protected override void CreateID ()
	{
		teamID = nextTeamID;
		nextTeamID++;
		uName = gameObject.name;
		base.CreateID ();
	}
	
	public Leader[] GetLeaders()
	{
		List<Leader> leaderList = leaders;
		if(GetNonAssignedUnits().Length > 0)
			leaderList.Add(this);
		return leaderList.ToArray();
	}
	
	public override int GetTeamID ()
	{
		if(teamID == -1)
			CreateID();
		return teamID;
	}
	
	public void GenerateUnit(GameObject unit)
	{
		GameObject unitInstance = Instantiate(unit) as GameObject;
		unitInstance.tag = gameObject.tag;
		Unit unitScript = unitInstance.GetComponent<Unit>();
		if(unitScript == null)
			unitScript = unitInstance.AddComponent<Unit>();
		unitScript.RegisterLeader(this);
		unitInstance.transform.position = GetSpawnPoint();
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
	
	public Vector3 GetSpawnPoint()
	{
		if(spawnPoints == null || spawnPoints.Length == 0)
			return transform.position + new Vector3(Random.Range(-10.0F, 10.0F), 1001, Random.Range(-10.0F, 10.0F));
		return spawnPoints[Mathf.RoundToInt(Random.Range(0,spawnPoints.Length - 1))];
	}
	
	public override Commander GetCommander()
	{
		return this;
	}
}
