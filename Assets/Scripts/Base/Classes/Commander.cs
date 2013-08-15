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
	public MapView mapCamera;
	protected Vector3 cameraPosition;
	public Vector3[] spawnPoints;
	public float spawnTime = 8.0f;
	protected float _spawnTime;
	protected List<Leader> leaders = new List<Leader>();
	public Objective[] objectives;
	protected Dictionary<int,Unit> allUnits = new Dictionary<int, Unit>();
	protected static Order[] orderList = {Order.move,Order.attack,Order.defend,Order.stop};
	protected int currentOrderIndex = 0;
	protected float RANDOM_SPAWN_RANGE = 10.0f;
	protected int leaderCount = 0;
	protected int MAX_LEADER_COUNT = 4;
	public int teamScore = 0;
	public AudioClip goalScored = null;
	protected List<Unit> backloggedUnits = new List<Unit>();
	
	/// <summary>
	/// Called once, at the beginning of the game.
	/// Replaces Awake().
	/// </summary>
	protected override void ClassSetup ()
	{
		spawnPoint = transform.position;
		_spawnTime = spawnTime;
		cameraPosition = Camera.main.transform.localPosition;
		if(defendObjective != null)
			defendObjective.SetOwner(this);
		commander = this;
		if(isPlayer && player == null)
		{
			player = this;
			uName = "You";
			gameObject.name = "Player";
			Screen.showCursor = false;
		}
		if(unitPrefab == null)
			unitPrefab = Resources.Load ("Prefabs/Unit") as GameObject;
		while(unitsToGenerate > 0)
		{
			GenerateUnit(unitPrefab);
			unitsToGenerate--;
		}
		raycastIncrementRate = 5.0f;
	}
	
	/// <summary>
	/// Called every time the unit respawns and one frame after the beginning of the game.
	/// Replaces Start().
	/// </summary>
	protected override void ClassSpawn ()
	{
		isSelectable = false;
		leader = this;
		RegisterUnit(this);
		if(mapCamera != null)
		{
			mapCamera.SetCommander(this);
		}
		if(isPlayer)
		{
			Camera.main.transform.parent = transform;
			Camera.main.transform.localPosition = cameraPosition;
			if(weapon != null)
			{
				weapon.gameObject.layer = leaderLayer;
			}
		}
		currentOrder = Order.move;
		currentOrderIndex = 0;
		transform.position = transform.position + new Vector3(0.0f, 0.3f,0.0f);
		transform.localScale = new Vector3(1.3f,1.3f,1.3f);
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
		recheckLayerTimer += Time.time;
		if(recheckLayerTimer > RECHECK_LAYER_TIME)
		{
			Unit[] layerChange = ChangeNearbyUnitLayers(gameObject.tag);
			CheckUnitLayerDiff(layerChange);
			recheckLayerTimer = 0.0f;
		}
		if(MapView.IsShown())
		{
			return;
		}
		// Everything below here only works on the player.
		GetLookingAt();
		// Selecting a unit:
		if(Input.GetButton("Select"))
		{
			if(Input.GetButtonDown("Select") && Input.GetButton("Upgrade"))
				UpgradeUnits();
			else if(Input.GetButtonDown("Select") && Input.GetButton("Assign"))
				AssignSelection();
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
		float input = Input.GetAxis("Mouse ScrollWheel");
		if(input != 0)
		{
			if(input > 0 && currentOrderIndex < orderList.Length - 1)
			{
				currentOrderIndex++;
			}
			else if(input < 0 && currentOrderIndex > 0)
			{
				currentOrderIndex--;
			}
			currentOrder = orderList[currentOrderIndex];
		}
		if(Input.GetKeyDown(KeyCode.F9))
		{
			Application.CaptureScreenshot("screenshot.png");
		}
	}
	
	/// <summary>
	/// Adds all Units within the middle section of the screen to the lookingAt HashSet.
	/// Informs each unit within that section that they are being looked at.
	/// If a Unit was being looked at the last time we checked but isn't now, informs it that it is no longer being looked at.
	/// </summary>
	protected void GetLookingAt()
	{
		// Only living players need to worry about this.
		if(!isPlayer || !IsAlive())
			return;
		HashSet<int> visibleUnits = new HashSet<int>();
		Unit hitUnit = null;
		int id = -1;
		// First, check to see what units are in the middle portion of our screen:
		float selectRadius = 0.45f;
		bool scan = true;
		while(scan && selectRadius < 0.56f && IsAlive())
		{
			Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(selectRadius, selectRadius, 0));
			RaycastHit hit;
			if (Physics.Raycast(selectRay, out hit,Mathf.Infinity,player.raycastIgnoreLayers))
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
					scan = false;
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
	
	public override void IsSeen (Leader seer, bool seen) {}
	
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
		if(!selectedUnits.Contains(selected) && unitID.ContainsKey(selected) && unitID[selected].Select(this))
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
	
	public bool CanUpgradeUnit()
	{
		return leaderCount < MAX_LEADER_COUNT;
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
				DemoteUnit(unit);
			}
			else
			{
				PromoteUnit(unit);
			}
		}
		return GetLeaders();
	}
	
	public Leader PromoteUnit(Unit unit)
	{
		if(unit is Leader)
			return (Leader)unit;
		if(leaderCount >= MAX_LEADER_COUNT)
			return null;
		Leader leader = unit.UpgradeUnit(this);
		leaders.Add(leader);
		unitID[leader.GetID()] = leader;
		leaderCount++;
		return leader;
	}
	
	public void DemoteUnit(Unit unit)
	{
		if(unit is Leader)
		{
			Leader leader = (Leader)unit;
			leaders.Remove(leader);
			unit = leader.DowngradeUnit();
			unitID[unit.GetID()] = unit;
			leaderCount--;
		}
	}
	
	/// <summary>
	/// Assigns our selection to a leader we're looking at.
	/// </summary>
	public void AssignSelection()
	{
		Leader leader = null;
		int[] lookingAtArray = new int[lookingAt.Count];
		lookingAt.CopyTo(lookingAtArray);
		foreach(int i in lookingAtArray)
		{
			if(unitID.ContainsKey(i))
			{
				Unit u = unitID[i];
				if(u is Leader)
				{
					leader = (Leader)u;
					break;
				}
			}
			else if(leaderLookup.ContainsKey(i))
			{
				leader = leaderLookup[i];
				break;
			}
		}
		if(leader == null)
			return;
		foreach(int i in selectedUnits.ToArray())
		{
			if(unitID.ContainsKey(i))
			{
				unitID[i].RegisterLeader(leader);
			}
		}
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
		if (Physics.Raycast(selectRay, out hit,Mathf.Infinity,player.raycastIgnoreLayers))
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
				GiveOrder(currentOrder,hit.point);
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
			this.weapon.Pickup(this);
		}
		else
		{
			base.CreateWeapon(weapon);
		}
	}
	
	protected void AddAllUnits()
	{
		if(backloggedUnits.Count == 0)
			return;
		Unit[] units = backloggedUnits.ToArray();
		foreach(Unit u in units)
		{
			if(!u.IsAlive())
				continue;
			AddUnit(u);
			backloggedUnits.Remove(u);
		}
	}
	
	public void AddUnit(Unit unit)
	{
		if(!IsAlive())
		{
			backloggedUnits.Add(unit);
			Invoke ("AddAllUnits",timeToOurRespawn + 0.5f);
			return;
		}
		if(!unit.IsAlive())
		{
			backloggedUnits.Add(unit);
			Invoke ("AddAllUnits",unit.GetTimeUntilRespawn() + 0.5f);
		}
		int id = unit.GetID();
		if(allUnits.ContainsKey(id))
		{
			allUnits[id] = unit;
		}
		else
		{
			allUnits.Add(id,unit);
		}
		if(friendlyFire || unit == this)
			return;
		Unit[] allOurUnits = GetAllUnits();
		foreach(Unit u in allOurUnits)
		{
			if(u == unit)
				continue;
			Physics.IgnoreCollision(u.collider,unit.collider,true);
		}
		Physics.IgnoreCollision(unit.collider,collider,true);
	}
	
	public override void RemoveUnit (int id)
	{
		if(leaderLookup.ContainsKey(id))
		{
			leaderLookup.Remove(id);
		}
		if(selectedUnits.Contains(id))
			selectedUnits.Remove(id);
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
		if(_spawnTime < 2.0f)
			return spawnTime;
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
		{
			Vector3 randomPos = spawnPoint;
			randomPos += new Vector3(Random.Range(-RANDOM_SPAWN_RANGE, RANDOM_SPAWN_RANGE), 1001, Random.Range(-RANDOM_SPAWN_RANGE, RANDOM_SPAWN_RANGE));
			Vector3 ourPos = transform.position;
			float distance = Mathf.Pow(ourPos.x - randomPos.x, 2) + Mathf.Pow(ourPos.z - randomPos.z, 2);
			if(distance < 2.0f)
			{
				randomPos += new Vector3(2,0,2);
			}
			Ray findFloorRay = new Ray(randomPos,Vector3.down);
			RaycastHit floor;
			if(Physics.Raycast(findFloorRay, out floor, Mathf.Infinity,player.raycastIgnoreLayers))
			{
				return floor.point + Vector3.up;
			}
			else return Vector3.zero;
		}
		return spawnPoints[Mathf.RoundToInt(Random.Range(0,spawnPoints.Length - 1))];
	}
	
	public void OnScore()
	{
		teamScore++;
		Camera.main.audio.PlayOneShot(goalScored);
	}
	
	public Order GetCurrentOrder()
	{
		return currentOrder;
	}
	
	public override Commander GetCommander()
	{
		return this;
	}
	
	public override bool IsLedByPlayer ()
	{
		return isPlayer;
	}
	
	public override bool IsOwnedByPlayer ()
	{
		return isPlayer;
	}
	
	public override bool IsPlayer()
	{
		return isPlayer;
	}
}
