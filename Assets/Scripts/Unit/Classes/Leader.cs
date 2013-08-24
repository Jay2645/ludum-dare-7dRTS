using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A leader micromanages a small group of units.
/// While a Commander has to manage many units, a Leader only has to manage units assigned to them.
/// An effective Commander will promote some of their units to Leaders, then distribute the remaining units among the leaders.
/// This way, a Commander just needs to order around the Leaders, and the Leaders will worry about micromanaging their own troops.
/// </summary>
public class Leader : Unit
{
	protected Dictionary<int, Unit> unitID = new Dictionary<int, Unit>();
	protected Dictionary<Unit, OrderData> unitCommands = new Dictionary<Unit, OrderData>();
	protected static Dictionary<int, Leader> leaderLookup = new Dictionary<int, Leader>();
	protected List<int> selectedUnits = new List<int>();
	protected GameObject tempOrderTarget = null;
	protected Commander commander = null;
	protected const float TEMP_GAMEOBJECT_REMOVE_TIME = 1.0f;
	protected static Vector3 CROWN_OFFSET = new Vector3(0.0f, 0.3f, 0.0f);
	protected static Vector3 LEADER_SCALE = new Vector3(1.2f, 1.2f, 1.2f);
	protected const float RECHECK_LAYER_TIME = 5.0f;
	protected Unit[] lastDetectedUnits = null;
	public Unit[] ownedUnits;
	protected List<GameObject> outlines;
	protected bool outlinesActive = true;
	protected static GameObject leaderCrown;
	protected static Material outlineMat;
	protected float recheckLayerTimer = 0.0f;

	protected override void ClassSetup()
	{
		unitType = UnitType.Leader;
		raycastIncrementRate = 10.0f;
	}

	protected override void ClassSpawn()
	{
		if (leaderCrown == null)
			leaderCrown = Resources.Load("Prefabs/Leader Crown") as GameObject;
		if (outlineMat == null)
			outlineMat = Resources.Load("Materials/Outline Only") as Material;
		if (uniqueGeo == null)
		{
			uniqueGeo = Instantiate(leaderCrown) as GameObject;
			uniqueGeo.transform.parent = transform;
			uniqueGeo.transform.localRotation = Quaternion.Euler(new Vector3(270, 0, 0));
			uniqueGeo.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
			uniqueGeo.layer = gameObject.layer;
			if (label != null)
				label.ChangeOffset(label.offset + CROWN_OFFSET);
		}
		if (outlines == null && (commander == null || IsOwnedByPlayer()))
		{
			outlines = new List<GameObject>();
			MeshFilter[] meshes = transform.root.GetComponentsInChildren<MeshFilter>();
			foreach (MeshFilter render in meshes)
			{
				if (render.GetComponent<Weapon>() != null)
					continue;
				GameObject outline = new GameObject(render.name + " Outline");
				outline.transform.parent = render.transform;
				outline.transform.localPosition = Vector3.zero;
				outline.transform.localRotation = Quaternion.identity;
				outline.transform.localScale = Vector3.one;
				outline.layer = leaderLayer;
				outline.AddComponent<MeshRenderer>();
				MeshFilter mesh = outline.AddComponent<MeshFilter>();
				mesh.mesh = Instantiate(render.mesh) as Mesh;
				outline.renderer.material = Instantiate(outlineMat) as Material;
				if (outlineMat.HasProperty("_OutlineColor") && renderer.material.HasProperty("_OutlineColor"))
				{
					outline.renderer.material.SetColor("_OutlineColor", renderer.material.GetColor("_OutlineColor"));
				}
				outlines.Add(outline);
			}
		}
		if (label != null)
		{
			label.SetVisibleThroughWalls(true);
		}
		transform.position = transform.position + new Vector3(0, LEADER_SCALE.y - 1, 0);
		transform.localScale = LEADER_SCALE;
	}

	protected override void ClassUpdate()
	{
		if (!IsOwnedByPlayer())
			return;
		// Everything below here only affects the player's team.
		if (MapView.IsShown())
		{
			recheckLayerTimer += Time.deltaTime;
			if (recheckLayerTimer > RECHECK_LAYER_TIME)
			{
				Unit[] layerChange = ChangeNearbyUnitLayers(gameObject.tag);
				CheckUnitLayerDiff(layerChange);
				recheckLayerTimer = 0.0f;
				return;
			}
		}
		else if (recheckLayerTimer > 0.0f)
		{
			recheckLayerTimer = 0.0f;
		}
	}

	protected Unit[] ChangeNearbyUnitLayers(string unitTag)
	{
		Unit[] detectedUnits = null;
		detectedUnits = DetectUnits(unitTag, 50.0f);
		if (detectedUnits.Length == 0)
		{
			return new Unit[0];
		}
		foreach (Unit unit in detectedUnits)
		{
			unit.IsSeen(this, true);
		}
		return detectedUnits;
	}

	protected void CheckUnitLayerDiff(Unit[] newDetectedUnits)
	{
		if (lastDetectedUnits == null || lastDetectedUnits.Length == 0)
		{
			lastDetectedUnits = newDetectedUnits;
			return;
		}
		HashSet<Unit> oldDetectedUnitSet = new HashSet<Unit>(lastDetectedUnits);
		if (oldDetectedUnitSet.Count > 0)
		{
			HashSet<Unit> newDetectedUnitSet = new HashSet<Unit>(newDetectedUnits);
			oldDetectedUnitSet.ExceptWith(newDetectedUnitSet);
			if (oldDetectedUnitSet.Count > 0)
			{
				Unit[] notDetectedAnymore = new Unit[oldDetectedUnitSet.Count];
				oldDetectedUnitSet.CopyTo(notDetectedAnymore);
				foreach (Unit u in notDetectedAnymore)
				{
					if (u == null)
						continue;
					u.IsSeen(this, false);
				}
			}
		}
		lastDetectedUnits = newDetectedUnits;
	}

	public override void IsSeen(Leader seer, bool seen)
	{
		if (seer.IsPlayer())
		{
			ToggleOutlines(seen);
		}
	}

	public override void IsLookedAt(bool lookedAt)
	{
		base.IsLookedAt(lookedAt);
		if (IsOwnedByPlayer())
			ToggleOutlines(true);
	}

	public override string GenerateLabel()
	{
		string labelS = base.GenerateLabel();
		labelS = labelS + "\nLeads " + GetSquadMemberCount() + " Units";
		return labelS;
	}

	protected void ToggleOutlines(bool unitIsVisible)
	{
		if (outlines == null || unitIsVisible != outlinesActive)
			return;
		outlinesActive = !unitIsVisible;
		Color currentColor = outlineColor;
		if (isSelected)
			currentColor = selectedColor;
		foreach (GameObject go in outlines.ToArray())
		{
			go.SetActive(outlinesActive);
			go.renderer.material.SetColor("_OutlineColor", currentColor);
		}
	}

	public void RegisterUnit(Unit unit)
	{
		int id = unit.GetID();
		if (unitID.ContainsKey(id))
			return;
		if (leaderLookup.ContainsKey(id))
			leaderLookup[id].RemoveUnit(id);
		unitID.Add(id, unit);
		commander.AddUnit(unit);
		leaderLookup.Add(id, this);
		if (orderData != null)
		{
			Order currentOrder = orderData.GetOrder();
			Transform orderTarget = orderData.GetOrderTarget();
			GiveOrder(currentOrder, orderTarget, unit);
		}
		ownedUnits = new Unit[unitID.Count];
		unitID.Values.CopyTo(ownedUnits, 0);
		Debug.Log("Registered ID number " + id);
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
		if (outlines != null && !commander.IsPlayer())
		{
			foreach (GameObject outline in outlines.ToArray())
			{
				Destroy(outline);
			}
			outlines = null;
		}
	}

	public override void RegisterLeader(Leader leader)
	{
		this.leader = commander;
	}

	public virtual void RemoveUnit(int id)
	{
		if (unitID.ContainsKey(id))
		{
			unitID.Remove(id);
			if (leaderLookup.ContainsKey(id))
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
		if (isSelected)
			selectedUnits.Add(id);
		RegisterUnit(newUnit);
	}

	public override void RecieveOrder(OrderData data)
	{
		//Debug.Log ("Recieved order from "+giver+": "+order.ToString()+" "+target.position);
		if (!(this is Commander))
			base.RecieveOrder(data);
		GiveOrder(data.GetOrder(), data.GetOrderTarget());
	}

	public Unit DowngradeUnit()
	{
		selectedUnits.Clear();
		Unit[] units = new Unit[unitID.Count];
		unitID.Values.CopyTo(units, 0);
		foreach (Unit u in units)
		{
			u.RegisterLeader(commander);
		}
		Unit downgrade = gameObject.AddComponent<Unit>();
		downgrade.JustPromoted();
		downgrade.renderer.material.SetColor("_OutlineColor", outlineColor);
		leader = (Leader)commander;
		downgrade.CloneUnit(this);
		downgrade.ChangeLayer(unitLayer);
		if (lastDetectedUnits != null)
		{
			foreach (Unit u in lastDetectedUnits)
			{
				u.ChangeLayer(unitLayer);
			}
		}
		if (label != null)
		{
			label.ChangeOffset(label.offset);
			label.SetVisibleThroughWalls(false);
		}
		if (outlines != null)
		{
			GameObject[] outlinedObjects = outlines.ToArray();
			foreach (GameObject outline in outlinedObjects)
			{
				Destroy(outline);
			}
			outlines = null;
		}
		if (IsLedByPlayer())
			MessageList.Instance.AddMessage(uName + ", acknowledging demotion to grunt.");
		Destroy(uniqueGeo);
		Destroy(this);
		return downgrade;
	}

	public void GiveOrder(Order order, Vector3 targetPos)
	{
		if (tempOrderTarget != null)
			DestroyImmediate(tempOrderTarget);
		tempOrderTarget = new GameObject("Order Target");
		tempOrderTarget.transform.position = targetPos;
		GiveOrder(order, tempOrderTarget.transform);
		//Destroy(tempOrderTarget,TEMP_GAMEOBJECT_REMOVE_TIME);
	}

	public virtual void GiveOrder(Order order, Transform target)
	{
		Unit[] squad = GetSquadMembers();
		foreach (Unit unit in squad)
		{
			if (unit == this)
				continue;
			GiveOrder(order, target, unit);
		}
	}

	public void GiveOrder(Order order, Transform target, Unit unit)
	{
		if (!IsPlayer() && unit.GetOrder() != Order.stop && unit.GetLastOrderer() == Commander.player || unit == this)
			return;
		//Debug.Log("Giving "+order+" order.");
		OrderData data = new OrderData(this, unit);
		data.SetOrder(order, true);
		data.SetTarget(target);
		unit.RecieveOrder(data);
		if (unitCommands.ContainsKey(unit))
		{
			unitCommands[unit] = data;
		}
		else
		{
			unitCommands.Add(unit, data);
		}
	}

	public void UnitUpdateOrder(Unit unit, OrderData data)
	{
		if (unitCommands.ContainsKey(unit))
		{
			unitCommands[unit] = data;
		}
		else
		{
			unitCommands.Add(unit, data);
		}
	}

	public void OnUnitReachedTarget(Unit unit)
	{
		if (!unitID.ContainsValue(unit))
			return;
		//Debug.LogWarning("TODO");
	}

	public OrderData UnitRequestOrders(Unit unit)
	{
		if (!unitID.ContainsValue(unit))
			return null;
		//Debug.LogWarning("TODO.");
		return null;
	}

	protected Objective GetAttackObjective()
	{
		if (attackObjective == null)
			attackObjective = commander.attackObjective;
		return attackObjective;
	}

	protected Objective GetDefendObjective()
	{
		if (defendObjective == null)
			defendObjective = commander.defendObjective;
		return defendObjective;
	}

	protected override string GetClass()
	{
		return "Leader";
	}

	public override int GetTeamID()
	{
		if (commander == null)
			return -1;
		return commander.GetTeamID();
	}

	protected void ValidateUnits()
	{
		List<int> removeUnits = new List<int>();
		foreach (KeyValuePair<int, Unit> kvp in unitID)
		{
			if (kvp.Value == null)
				removeUnits.Add(kvp.Key);
		}
		foreach (int uID in removeUnits.ToArray())
		{
			unitID.Remove(uID);
		}
	}

	/// <summary>
	/// Gets the squad member count.
	/// </summary>
	/// <returns>
	/// The squad member count. Note that this does NOT include us.
	/// </returns>
	public int GetSquadMemberCount()
	{
		ValidateUnits();
		return unitID.Count;
	}

	public Unit[] GetSquadMembers()
	{
		Unit[] unitArray = new Unit[GetSquadMemberCount()];
		unitID.Values.CopyTo(unitArray, 0);
		return unitArray;
	}

	public override Commander GetCommander()
	{
		return commander;
	}
}
