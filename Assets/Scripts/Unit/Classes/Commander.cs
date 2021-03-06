using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Commander is the class that the player defaults to.
/// It is capable of promoting and demoting units, and all units answer to it.
/// There should only ever be one commander per team.
/// </summary>
public class Commander : Leader
{
	protected static Commander[] commanders;
	public bool isPlayer = false;
	public int unitsToGenerate = 0;
	public static Commander player = null;
	public static GameObject unitPrefab = null;
	protected HashSet<int> lookingAt = new HashSet<int>();
	protected int teamID = -1;
	protected static int nextTeamID = 0;
	public MapView mapCamera;
	public Camera guiCamera;
	protected Vector3 cameraPosition;
	public Transform[] spawnPoints;
	public float spawnTime = 8.0f;
	protected float _spawnTime;
	protected Dictionary<int, Leader> leaders = new Dictionary<int, Leader>();
	public Objective[] objectives;
	protected Order currentOrder = Order.move;
	protected Dictionary<int, Unit> allUnits = new Dictionary<int, Unit>();
	protected Dictionary<int, GameObject> unitCards = new Dictionary<int, GameObject>();
	protected static Order[] orderList = { Order.move, Order.attack, Order.defend, Order.stop };
	protected int currentOrderIndex = 0;
	protected const float RANDOM_SPAWN_RANGE = 10.0f;
	protected int leaderCount = 0;
	protected const int MAX_LEADER_COUNT = 9;
	public int teamScore = 0;
	public AudioClip goalScored = null;
	public AudioClip gatesOpen = null;
	public AudioClip jump = null;
	protected List<Unit> backloggedUnits = new List<Unit>();
	protected List<int> newlySelectedUnits = new List<int>();
	protected static GameObject cardBackground = null;
	protected float resourceCount = Mathf.Infinity;
	protected static PhysicalText respawnText;
	public Font respawnFont;
	public bool showTutorial = false;
	protected static Rect guiRect;
	protected static string[] guiTooltips = new string[]
	{
		"Default Controls:",
		"WASD / Arrow Keys: Move",
		"Mouse1: Shoot",
		"Mouse2: Give Orders",
		"Scroll Wheel: Change Orders",
		"R: Reload Weapon",
		"Spacebar: Jump",
		"E: Toggle Unit Selected",
		"Q: Deselect all Units",
		"Shift + E: Promote/Demote the Unit you're looking at",
		"Ctrl + E: Assign selected Units to be led by the Leader you're currently looking at",
		"1-9: Quick-Select Leader",
		"Ctrl + 1-9: Quick-Assign Units to Leader",
		"M: Toggle RTS Map View",
		"Press any key to dismiss."
	};
	protected static bool isInSetup = false;
	protected static Rect setupRect;
	protected static string setupString = "SETUP";
	protected static Rect setupEndRect;
	protected static string setupEnd = "Press Enter when ready";
	public GameObject[] navBlockers;

	protected static void AddCommander(Commander commander)
	{
		if (commanders == null)
		{
			commanders = new Commander[1];
			commanders[0] = commander;
			return;
		}
		List<Commander> commanderList = new List<Commander>(commanders);
		commanderList.Add(commander);
		commanders = commanderList.ToArray();
	}

	public static void CallCommanderMethod(string methodName, object parameter)
	{
		if (commanders == null)
			return;
		foreach (Commander com in commanders)
		{
			if (parameter == null)
			{
				com.BroadcastMessage(methodName);
			}
			else
			{
				com.BroadcastMessage(methodName, parameter);
			}
		}
	}

	/// <summary>
	/// Called once, at the beginning of the game.
	/// Replaces Awake().
	/// </summary>
	protected override void ClassSetup()
	{
		unitType = UnitType.Commander;
		spawnPoint = transform.position;
		_spawnTime = spawnTime;
		cameraPosition = Camera.main.transform.localPosition;
		if (defendObjective != null)
			defendObjective.SetOwner(this);
		commander = this;
		if (isPlayer && player == null)
		{
			player = this;
			uName = "You";
			gameObject.name = "Player";
			Screen.showCursor = false;
			isInSetup = true;
			if (cardBackground == null)
				cardBackground = Resources.Load("Prefabs/Unit Card Background") as GameObject;
			if (guiCamera != null)
			{
				respawnText = new PhysicalText(guiCamera.ViewportToWorldPoint(new Vector3(0.075f, 0.8f, 0.5f)));
				respawnText.textString = "Respawn in: ";
				respawnText.font = respawnFont;
				respawnText.text.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
				respawnText.text.transform.localRotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
			}
			showTutorial = true;
			guiRect = new Rect(0, 0, 500, Screen.height / guiTooltips.Length + 1);
			//Time.timeScale = 0.0f;
			float setupHeight = 20.0f;
			float setupWidth = 150.0f;
			float setupLeft = Screen.width / 2 - setupWidth / 2;
			setupEndRect = new Rect(setupLeft, setupHeight / 1.25f, setupWidth, setupHeight);
			setupRect = setupEndRect;
			setupWidth /= 2;
			setupRect.y = 0;
			setupLeft = Screen.width / 2 - setupWidth / 2;
			setupRect.x = setupLeft;
		}
		if (unitPrefab == null)
			unitPrefab = Resources.Load("Prefabs/Unit") as GameObject;
		while (unitsToGenerate > 0)
		{
			GenerateUnit(unitPrefab);
			unitsToGenerate--;
		}
		raycastIncrementRate = 5.0f;
		AddCommander(this);
	}

	/// <summary>
	/// Called every time the unit respawns and one frame after the beginning of the game.
	/// Replaces Start().
	/// </summary>
	protected override void ClassSpawn()
	{
		isSelectable = false;
		leader = this;
		RegisterUnit(this);
		if (mapCamera != null)
		{
			mapCamera.SetCommander(this);
		}
		if (isPlayer)
		{
			Camera.main.transform.parent = transform;
			Camera.main.transform.localPosition = cameraPosition;
			if (weapon != null)
			{
				weapon.gameObject.layer = leaderLayer;
			}
			if (guiCamera != null)
			{
				guiCamera.clearFlags = CameraClearFlags.Nothing;
				respawnText.text.layer = defaultLayer;
			}
		}
		currentOrder = Order.move;
		currentOrderIndex = 0;
		transform.position = spawnPoint + new Vector3(0.0f, 0.3f, 0.0f);
		transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
	}

	/// <summary>
	/// Called every frame.
	/// Replaces Update().
	/// </summary>
	protected override void ClassUpdate()
	{
		_spawnTime -= Time.deltaTime;
		if (_spawnTime <= 0)
			_spawnTime = spawnTime;

		if (!isPlayer)
			return;
		recheckLayerTimer += Time.time;
		foreach (KeyValuePair<int, Leader> kvp in leaders)
		{
			Leader currentLeader = kvp.Value;
			if (currentLeader == null || !currentLeader.IsAlive())
				continue;
			int leaderID = kvp.Key;
			string id = "Select Group " + leaderID.ToString();
			if (!Input.GetButtonDown(id))
				continue;
			if (Input.GetButton("Assign"))
			{
				foreach (int i in selectedUnits.ToArray())
				{
					if (unitID.ContainsKey(i))
					{
						unitID[i].RegisterLeader(currentLeader);
					}
				}
			}
			else
				SelectUnits(currentLeader.GetID());
		}
		if (recheckLayerTimer > RECHECK_LAYER_TIME)
		{
			Unit[] layerChange = ChangeNearbyUnitLayers(gameObject.tag);
			CheckUnitLayerDiff(layerChange);
			recheckLayerTimer = 0.0f;
			UpdateCards();
		}
		if (MapView.IsShown())
		{
			return;
		}
		// Everything below here only works on the player.
		GetLookingAt();
		// Selecting a unit:
		if (Input.GetButton("Select"))
		{
			if (Input.GetButtonDown("Select") && Input.GetButton("Upgrade"))
				UpgradeUnits();
			else if (Input.GetButtonDown("Select") && Input.GetButton("Assign"))
				AssignSelection();
			else
				SelectUnits();
		}
		else if (newlySelectedUnits.Count > 0)
		{
			newlySelectedUnits.Clear();
		}
		// Deselecting a unit:
		if (Input.GetButtonDown("Deselect"))
		{
			DeselectUnits();
		}
		// Giving orders:
		else if (Input.GetButtonDown("Order"))
		{
			GiveOrder();
		}
		float input = Input.GetAxis("Mouse ScrollWheel");
		if (input != 0)
		{
			if (input > 0 && currentOrderIndex < orderList.Length - 1)
			{
				currentOrderIndex++;
			}
			else if (input < 0 && currentOrderIndex > 0)
			{
				currentOrderIndex--;
			}
			currentOrder = orderList[currentOrderIndex];
		}
		if (Input.GetKeyDown(KeyCode.F9))
		{
			Application.CaptureScreenshot("screenshot.png");
		}
		if (Input.GetButtonDown("Jump") && jump != null && GetComponent<CharacterController>().isGrounded)
		{
			Camera.main.audio.PlayOneShot(jump);
		}
	}

	void OnGUI()
	{
		if (!isInSetup)
			return;
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			isInSetup = false;
			if (navBlockers != null)
			{
				foreach (GameObject go in navBlockers)
				{
					Destroy(go);
				}
			}
			if (gatesOpen != null)
			{
				Camera.main.audio.PlayOneShot(gatesOpen);
			}
		}
		GUI.Label(setupRect, setupString);
		GUI.Label(setupEndRect, setupEnd);
		if (!showTutorial)
			return;
		if (Input.anyKey && Time.time >= 1.5f)
		{
			showTutorial = false;
			//Time.timeScale = 1.0f;
			return;
		}
		for (int i = 0; i < guiTooltips.Length; i++)
		{
			guiRect.y = i * (guiTooltips.Length + 2);
			GUI.Label(guiRect, guiTooltips[i]);
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
		if (!isPlayer || !IsAlive())
			return;
		HashSet<int> visibleUnits = new HashSet<int>();
		Unit hitUnit = null;
		int id = -1;
		// First, check to see what units are in the middle portion of our screen:
		float selectRadius = 0.45f;
		bool scan = true;
		while (scan && selectRadius < 0.56f && IsAlive())
		{
			Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(selectRadius, selectRadius, 0));
			RaycastHit[] hits = Physics.RaycastAll(selectRay, 1000.0f, raycastIgnoreLayers);
			if (hits != null && hits.Length > 0)
			{
				foreach (RaycastHit h in hits)
				{
					Leader hitLeader = h.transform.GetComponentInChildren<Leader>();
					if (hitLeader != null)
					{
						id = hitLeader.GetID();
						if (unitID.ContainsKey(id))
						{
							hitLeader.IsLookedAt(true);
							visibleUnits.Add(id);
						}
						scan = false;
						break;
					}
				}
				RaycastHit hit;
				if (scan && Physics.Raycast(selectRay, out hit, Mathf.Infinity, raycastIgnoreLayers))
				{
					hitUnit = hit.transform.GetComponentInChildren<Unit>();
					if (hitUnit != null)
					{
						id = hitUnit.GetID();
						if (unitID.ContainsKey(id))
						{
							hitUnit.IsLookedAt(true);
							visibleUnits.Add(id);
						}
						scan = false;
					}
				}
			}
			Debug.DrawRay(selectRay.origin, selectRay.direction);
			selectRadius += 0.01f;
		}
		// Remove all the units which we are still looking at, leaving us with a list of units we were looking at, but are no longer doing so.
		if (lookingAt.Count > 0)
		{
			lookingAt.ExceptWith(visibleUnits);
			if (lookingAt.Count > 0)
			{
				int[] notLookingAt = new int[lookingAt.Count];
				lookingAt.CopyTo(notLookingAt);
				foreach (int i in notLookingAt)
				{
					if (unitID.ContainsKey(i))
						unitID[i].IsLookedAt(false);
				}
			}
		}
		lookingAt = visibleUnits;
	}

	public override void IsSeen(Leader seer, bool seen) { }

	/// <summary>
	/// Selects all Units we are currently looking at.
	/// </summary>
	public void SelectUnits()
	{
		// Make sure we're actually looking at something.
		if (lookingAt.Count == 0)
		{
			GetLookingAt();
			if (lookingAt.Count == 0)
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
		foreach (int i in selection)
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
		if (!unitID.ContainsKey(selected) || newlySelectedUnits.Contains(selected))
			return;
		Unit unit = unitID[selected];
		newlySelectedUnits.Add(selected);
		if (selectedUnits.Contains(selected) && isPlayer)
		{
			unit.Deselect();
			selectedUnits.Remove(selected);
		}
		else
		{
			unit.Select(this);
			selectedUnits.Add(selected);
		}
		if (guiCamera == null || !isPlayer)
			return;
		UpdateCards();
	}

	protected override void OnClassDie()
	{
		if (!isPlayer)
			return;
		DeselectUnits();
		foreach (int i in lookingAt)
		{
			unitID[i].IsLookedAt(false);
		}
		if (guiCamera != null)
		{
			guiCamera.clearFlags = CameraClearFlags.SolidColor;
			respawnText.text.layer = hudLayer;
			respawnText.textString = "Respawning in " + Mathf.RoundToInt(timeToOurRespawn) + " seconds.";
		}
		InvokeRepeating("PlayRespawn", timeToOurRespawn - Mathf.Floor(timeToOurRespawn), 1.0f);
	}

	protected void PlayRespawn()
	{
		if (timeToOurRespawn <= 0)
			return;
		Camera.main.audio.PlayOneShot(respawnBlip);
		timeToOurRespawn--;
		UpdateRespawnTimer();
	}

	public void UpdateRespawnTimer()
	{
		float tempTime = timeToOurRespawn;
		Debug.Log("Respawning in " + tempTime + " seconds.");
		respawnText.textString = "Respawning in " + Mathf.RoundToInt(tempTime) + " seconds.";
	}

	protected string MakeDeathMessage()
	{
		string respawnTime = "Respawning in " + Mathf.RoundToInt(timeToOurRespawn) + " seconds.";
		string deathMessage = "You were killed by " + lastDamager.name + ". " + respawnTime;
		respawnText.textString = respawnTime;
		return deathMessage;
	}

	public static bool IsInSetup()
	{
		return isInSetup;
	}

	protected void MakeCard(Unit unit, float count)
	{
		if (!isPlayer || guiCamera == null || !unit.IsOwnedByPlayer())
			return;
		GameObject labelGO = unit.GetLabel();
		if (labelGO == null)
			return;
		GameObject unitCard = Instantiate(labelGO) as GameObject;
		unitCard.layer = hudLayer;
		unitCard.transform.parent = guiCamera.transform;
		unitCard.transform.localRotation = Quaternion.identity;
		unitCard.transform.localScale = Vector3.one / 27.5f;
		GameObject unitCardBG = Instantiate(cardBackground) as GameObject;
		unitCardBG.transform.parent = unitCard.transform;
		unitCardBG.transform.localScale = new Vector3(125.0f, 50.0f, 1.0f);
		unitCardBG.transform.localPosition = new Vector3(60.0f, -25.0f, 0.0f);
		unitCardBG.transform.localRotation = Quaternion.identity;
		float percentage = (count - 1.00f) / 10.00f;
		unitCard.transform.position = guiCamera.ViewportToWorldPoint(new Vector3(0, 1 - percentage, 1));
		int uID = unit.GetID();
		if (unitCards.ContainsKey(uID))
			Destroy(unitCards[uID]);
		unitCards.Add(uID, unitCard);
	}

	protected void UpdateCards()
	{
		if (!isPlayer)
			return;
		foreach (KeyValuePair<int, GameObject> kvp in unitCards)
		{
			Destroy(kvp.Value);
		}
		unitCards.Clear();
		if (selectedUnits.Count == 0)
			return;
		float count = 1.00f;
		foreach (int i in selectedUnits.ToArray())
		{
			Unit unit = unitID[i];
			if (!unit.IsAlive())
				continue;
			MakeCard(unitID[i], count);
			count++;
		}
	}

	/// <summary>
	/// Upgrades all units we are currently looking at.
	/// </summary>
	public void UpgradeUnits()
	{
		if (lookingAt.Count == 0)
		{
			GetLookingAt();
			if (lookingAt.Count == 0)
			{
				// We aren't actually looking at anything; no need to continue.
				return;
			}
		}
		int[] delectableSelectables = new int[lookingAt.Count];
		lookingAt.CopyTo(delectableSelectables);
		if (delectableSelectables.Length == 0 || !unitID.ContainsKey(delectableSelectables[0]))
			return;
		UnitType type = unitID[delectableSelectables[0]].GetUnitType();
		if (type == UnitType.Unit)
		{
			type = UnitType.Leader;
		}
		else if (type == UnitType.Leader)
		{
			type = UnitType.Unit;
		}
		UpgradeUnits(delectableSelectables, type);
	}

	/// <summary>
	/// Takes an array of Unit IDs and upgrades them.
	/// </summary>
	/// <param name='selection'>
	/// The Unit IDs to select.
	/// </param>
	public void UpgradeUnits(int[] selection, UnitType type)
	{
		foreach (int i in selection)
		{
			UpgradeUnits(i, type);
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
	public Leader[] UpgradeUnits(int selected, UnitType type)
	{
		if (unitID.ContainsKey(selected))
		{
			Unit unit = unitID[selected];
			if (unit.GetUnitType() == type)
				return GetLeaders();
			PromoteUnit(unit, type);
		}
		if (!isPlayer)
			return GetLeaders();
		UpdateCards();
		return GetLeaders();
	}

	public Unit PromoteUnit(Unit unit, UnitType type)
	{
		// If we're already of the correct type, no need to do anything.
		if (unit.GetUnitType() == type)
			return unit;
		// Get how much this UnitType will cost.
		float resourceCost = Unit.GetCost(type);
		// If it's too expensive, we can't buy it.
		if (resourceCost > resourceCount)
		{
			// The player gets a message seeing why it didn't work.
			if (IsPlayer())
			{
				MessageList.Instance.AddMessage("You don't have enough resources to upgrade " + unit.name + " to " + type.ToString() + "!");
			}
			Debug.LogWarning("Too expensive to upgrade " + unit + " to a " + type.ToString() + "!");
			return unit;
		}
		// 
		switch (type)
		{
			case UnitType.Commander:
				{
					// Not implemented.
					Debug.LogError("You are trying to make a new Commander. This is not supported currently!");
					return null;
				}
			case UnitType.Leader:
				{
					if (leaderCount >= MAX_LEADER_COUNT)
					{
						if (IsPlayer())
							MessageList.Instance.AddMessage("You have too many Leaders already!");
						Debug.LogWarning("We have too many Leaders already!");
						return null;
					}
					Leader leader = unit.UpgradeUnit(this);
					for (int i = 1; i <= MAX_LEADER_COUNT; i++)
					{
						if (leaders.ContainsKey(i))
							continue;
						leaders.Add(i, leader);
						break;
					}
					unitID[leader.GetID()] = leader;
					leaderCount++;
					return leader;
				}
			case UnitType.Unit:
			default:
				{
					Leader leader = unit as Leader;
					if (leader == null)
						return unit;
					int i = -1;
					foreach (KeyValuePair<int, Leader> kvp in leaders)
					{
						if (kvp.Value != leader)
							continue;
						i = kvp.Key;
						break;
					}
					leaders.Remove(i);
					unit = leader.DowngradeUnit();
					unitID[unit.GetID()] = unit;
					leaderCount--;
					return unit;
				}
		}
	}

	public void ValidateSelected()
	{
		if (!isPlayer || selectedUnits.Count == 0)
			return;
		foreach (int u in selectedUnits.ToArray())
		{
			unitID[u].CreateSelected();
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
		foreach (int i in lookingAtArray)
		{
			if (unitID.ContainsKey(i))
			{
				Unit u = unitID[i];
				if (u.GetUnitType() == UnitType.Leader)
				{
					leader = (Leader)u;
					break;
				}
			}
			else if (leaderLookup.ContainsKey(i))
			{
				leader = leaderLookup[i];
				break;
			}
		}
		if (leader == null)
			return;
		foreach (int i in selectedUnits.ToArray())
		{
			if (unitID.ContainsKey(i))
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
		foreach (int sID in ids)
		{
			unitID[sID].Deselect();
		}
		selectedUnits.Clear();
		UpdateCards();
	}

	/// <summary>
	/// Gives orders to all selected units, moving them to a specified location.
	/// </summary>
	public void GiveOrder()
	{
		// AI don't need to rely on the camera for raycasting, and can move to exact coordinates in any event.
		if (!isPlayer)
			return;
		Ray selectRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		RaycastHit hit;
		if (Physics.Raycast(selectRay, out hit, Mathf.Infinity, player.raycastIgnoreLayers))
		{
			Unit hitUnit = hit.transform.GetComponentInChildren<Unit>();
			if (hitUnit != null)
			{
				id = hitUnit.GetID();
				if (unitID.ContainsKey(id))
				{
					GiveOrder(Order.defend, hit.transform);
				}
				else
				{
					GiveOrder(Order.attack, hit.transform);
				}
			}
			else
			{
				GiveOrder(currentOrder, hit.point);
			}
		}
	}

	public override void GiveOrder(Order order, Transform target)
	{
		if (isPlayer)
		{
			int[] ids = selectedUnits.ToArray();
			foreach (int id in ids)
			{
				GiveOrder(order, target, unitID[id]);
			}
		}
		else
		{
			Unit[] units = GetNonAssignedUnits();
			foreach (Unit unit in units)
			{
				GiveOrder(order, target, unit);
			}
		}
	}

	protected override void CreateWeapon(Weapon weapon)
	{
		if (isPlayer)
		{
			if (this.weapon != null && weapon.gameObject.activeInHierarchy)
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
		if (backloggedUnits.Count == 0)
			return;
		Unit[] units = backloggedUnits.ToArray();
		foreach (Unit u in units)
		{
			if (!u.IsAlive())
				continue;
			AddUnit(u);
			backloggedUnits.Remove(u);
		}
	}

	public void AddUnit(Unit unit)
	{
		if (!IsAlive())
		{
			backloggedUnits.Add(unit);
			Invoke("AddAllUnits", timeToOurRespawn + 0.5f);
			return;
		}
		if (!unit.IsAlive())
		{
			backloggedUnits.Add(unit);
			Invoke("AddAllUnits", unit.GetTimeUntilRespawn() + 0.5f);
			return;
		}
		int id = unit.GetID();
		if (allUnits.ContainsKey(id))
		{
			allUnits[id] = unit;
		}
		else
		{
			allUnits.Add(id, unit);
		}
		if (friendlyFire || unit == this)
			return;
		Collider[] cols = unit.GetComponents<Collider>();
		foreach (Collider col in cols)
		{
			if (col.isTrigger || col.gameObject.GetComponent<Weapon>() != null)
				continue;
			col.enabled = true;
		}
		Unit[] allOurUnits = GetAllUnits();
		foreach (Unit u in allOurUnits)
		{
			if (u == unit || !u.IsAlive())
				continue;
			Physics.IgnoreCollision(u.collider, unit.collider, true);
		}
		Physics.IgnoreCollision(unit.collider, collider, true);
	}

	public override void RemoveUnit(int id)
	{
		if (leaderLookup.ContainsKey(id))
		{
			leaderLookup.Remove(id);
		}
		if (selectedUnits.Contains(id))
			selectedUnits.Remove(id);
		if (allUnits.ContainsKey(id))
			allUnits.Remove(id);
	}

	public int GetUnitCount()
	{
		return allUnits.Count;
	}

	public Unit[] GetAllUnits()
	{
		Unit[] units = new Unit[allUnits.Count];
		allUnits.Values.CopyTo(units, 0);
		return units;
	}

	public Unit[] GetNonAssignedUnits()
	{
		List<Unit> unitList = new List<Unit>();
		foreach (KeyValuePair<int, Unit> kvp in unitID)
		{
			Unit uValue = kvp.Value;
			if (uValue.GetUnitType() == UnitType.Commander || uValue.GetUnitType() == UnitType.Leader)
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
		if (newTime - _spawnTime < 0.00f)
		{
			_spawnTime = newTime;
		}
	}

	public void AddRespawnTime(float newTime)
	{
		spawnTime += newTime;
		if (spawnTime - _spawnTime < 0.00f)
		{
			_spawnTime = newTime;
		}
	}

	public float GetTimeToRespawn()
	{
		if (_spawnTime < 2.0f)
			return spawnTime;
		return _spawnTime;
	}

	protected override void CreateID()
	{
		teamID = nextTeamID;
		nextTeamID++;
		uName = gameObject.name;
		base.CreateID();
	}

	public Leader[] GetLeaders()
	{
		List<Leader> leaderList = new List<Leader>(leaders.Values);
		if (GetNonAssignedUnits().Length > 0)
			leaderList.Add(this);
		return leaderList.ToArray();
	}

	public override int GetTeamID()
	{
		if (teamID == -1)
			CreateID();
		return teamID;
	}

	public void GenerateUnit(GameObject unit)
	{
		GameObject unitInstance = Instantiate(unit) as GameObject;
		unitInstance.tag = gameObject.tag;
		Unit unitScript = unitInstance.GetComponent<Unit>();
		if (unitScript == null)
			unitScript = unitInstance.AddComponent<Unit>();
		unitScript.RegisterLeader(this);
		//unitInstance.transform.position = GetSpawnPoint();
	}

	public bool IsEnemy(Unit unit)
	{
		return !unitID.ContainsKey(unit.GetID());
	}

	public override void RegisterLeader(Leader leader)
	{
		this.leader = this;
	}

	protected override string GetClass()
	{
		return "Commander";
	}

	public Vector3 GetSpawnPoint()
	{
		if (spawnPoints == null || spawnPoints.Length == 0)
		{
			Debug.LogWarning("No spawn points set! Getting random spawn point.");
			Vector3 randomPos = spawnPoint;
			randomPos += new Vector3(Random.Range(-RANDOM_SPAWN_RANGE, RANDOM_SPAWN_RANGE), 1001, Random.Range(-RANDOM_SPAWN_RANGE, RANDOM_SPAWN_RANGE));
			Vector3 ourPos = transform.position;
			float distance = Mathf.Pow(ourPos.x - randomPos.x, 2) + Mathf.Pow(ourPos.z - randomPos.z, 2);
			if (distance < 2.0f)
			{
				randomPos += new Vector3(2, 0, 2);
			}
			Ray findFloorRay = new Ray(randomPos, Vector3.down);
			RaycastHit floor;
			if (Physics.Raycast(findFloorRay, out floor, Mathf.Infinity, player.raycastIgnoreLayers))
			{
				return floor.point + Vector3.up;
			}
			else return Vector3.zero;
		}
		Transform spawn = spawnPoints[Mathf.RoundToInt(Random.Range(0, spawnPoints.Length - 1))];
		if (spawn == null)
		{
			for (int i = 0; i < spawnPoints.Length; i++)
			{
				if (spawn != null)
					break;
				spawn = spawnPoints[i];
			}
		}
		if (spawn == null)
		{
			spawnPoints = null;
			return GetSpawnPoint();
		}
		return spawn.position;
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

	public override bool IsLedByPlayer()
	{
		return isPlayer;
	}

	public override bool IsOwnedByPlayer()
	{
		return isPlayer;
	}

	public override bool IsPlayer()
	{
		return isPlayer;
	}
}
