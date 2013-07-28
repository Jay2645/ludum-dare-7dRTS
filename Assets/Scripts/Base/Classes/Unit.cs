using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A Unit is the main object of the game. Anything which can be selected and recieve orders is considered a Unit.
/// Units have a unique ID and a name so the player can tell them apart.
/// They currently fall from the sky upon spawn, akin to ODSTs from the Halo series.
/// Units can be promoted or demoted to Leader class by a Commander; however, there is only one Commander which can be active at any given time.
/// </summary>
public class Unit : MonoBehaviour 
{
	protected bool isSelectable = true;
	protected bool isSelected = false;
	protected int id = -1;
	private static int currentID = 0;
	protected Leader leader = null;
	protected GameObject smokeTrail = null;
	public static GameObject smokeTrailPrefab = null;
	public static GameObject dustPrefab = null;
	public static GameObject movePrefab = null;
	public static GameObject attackPrefab = null;
	public static GameObject defendPrefab = null;
	public static string[] firstNames = 
	{
		"Buzz",
		"Luke",
		"Chris",
		"David",
		"Will",
		"Michael",
		"Dan",
		"Bruce",
		"Ken",
		"Jeff",
		"Edward",
		"Jaime"
	};
	public static string[] lastNames = 
	{
		"Harrington",
		"Marigold",
		"Wong",
		"Rogers",
		"Atkins",
		"Dyer",
		"Randall",
		"Hunt",
		"Clark"
	};
	protected string uName = "";
	public Color teamColor = Color.white;
	public ObjectLabel label = null;
	public Order currentOrder = Order.stop;
	protected Leader lastOrderer = null;
	protected Transform moveTarget = null;
	protected Transform orderTarget = null;
	protected GameObject moveEffect = null;
	public int health = 100;
	protected int _maxHealth;
	public Weapon weapon = null;
	protected Weapon _initialWeapon;
	public Objective defendObjective;
	public Objective attackObjective;
	public Objective currentObjective;
	public Vector3 spawnPoint = Vector3.one;
	protected bool skipSpawn = false;
	public GameObject shadow = null;
	
	void Awake()
	{
		UnitSetup();
		ClassSetup();
	}
	
	/// <summary>
	/// Sets up the Unit. This will only be called once.
	/// </summary>
	protected void UnitSetup()
	{
		CreateID();
		if(weapon != null)
		{
			_initialWeapon = weapon;
		}
		_maxHealth = health;
	}
	
	/// <summary>
	/// Sets up whatever class this is. This will only be called once.
	/// </summary>
	protected virtual void ClassSetup()
	{
		
	}
	
	void Start()
	{
		Spawn();
	}
	
	/// <summary>
	/// Spawns this Unit. This will be called every time the Unit dies and respawns.
	/// </summary>
	protected void Spawn()
	{
		// Sometimes (especially when we get promoted) we run into a bug where all our variables are reset when they shouldn't be.
		if(skipSpawn)
		{
			skipSpawn = false;
			return;
		}
		// Make the GameObject visible.
		gameObject.SetActive(true);
		gameObject.layer = LayerMask.NameToLayer("Units");
		foreach(Transform child in transform)
		{
			child.gameObject.SetActive(true);
		}
		
		if(gameObject.GetComponent<RAIN.Ontology.Decoration>() == null)
		{
			gameObject.AddComponent<RAIN.Ontology.Entity>();
			RAIN.Ontology.Decoration decoration = gameObject.AddComponent<RAIN.Ontology.Decoration>();
			RAIN.Ontology.Aspect aspect = new RAIN.Ontology.Aspect(gameObject.tag,new RAIN.Ontology.Sensation("sight"));
			decoration.aspect = aspect;
		}
		
		// Reset all variables to their initial state.
		Debug.Log ("Resetting variables on "+this);
		currentOrder = Order.stop;
		if(moveTarget != null)
		{
			DestroyImmediate(moveTarget.gameObject);
			moveTarget = null;
			orderTarget = null;
		}
		if(moveEffect != null)
		{
			DestroyImmediate(moveEffect);
			moveEffect = null;
		}
		health = _maxHealth;
		if(_initialWeapon != null)
		{
			CreateWeapon(_initialWeapon);
			if(weapon != null)
			{
				weapon.gameObject.layer = gameObject.layer;
			}
		}
		if(shadow != null)
		{
			shadow.layer = gameObject.layer;
		}
		
		if(leader != null)
			leader.RegisterUnit(this);
		
		// Call class-specific spawn code.
		ClassSpawn();
		
		// Makes sure the GameObject is the right color.
		if(renderer != null)
			renderer.material.color = teamColor;
		
		// Move it to the spawn point.
		if(spawnPoint == Vector3.zero)
		{
			Commander commander = GetCommander();
			if(commander != null)
			{
				spawnPoint = commander.GetSpawnPoint();
				transform.position = spawnPoint;
			}
		}
	}
	
	/// <summary>
	/// Creates things that should happen when this class spawns. This will be called every time this Unit dies and respawns.
	/// </summary>
	protected virtual void ClassSpawn()
	{
		if(transform.position.y > 100)
		{
			InitSmokeParticles();
		}
	}
	
	protected virtual void CreateWeapon(Weapon weapon)
	{
		if(this.weapon != null && weapon.gameObject.activeInHierarchy)
		{
			this.weapon.transform.parent = null;
		}
		this.weapon = Instantiate(weapon) as Weapon;
		this.weapon.owner = this;
		this.weapon.transform.parent = transform;
		this.weapon.transform.localPosition = this.weapon.GetLocation();
		this.weapon.transform.localRotation = Quaternion.Euler(90,0,0);
	}
	
	protected void InitSmokeParticles()
	{
		if(smokeTrailPrefab == null)
		{
			smokeTrailPrefab = Resources.Load("Particles/Smoke Trail") as GameObject;
		}
		if(dustPrefab == null)
		{
			dustPrefab = Resources.Load("Particles/Dust Storm") as GameObject;
		}
		if(movePrefab == null)
		{
			movePrefab = Resources.Load("Prefabs/MoveTarget") as GameObject;
		}
		if(defendPrefab == null)
		{
			defendPrefab = Resources.Load("Prefabs/DefendTarget") as GameObject;
		}
		if(attackPrefab == null)
		{
			attackPrefab = Resources.Load("Prefabs/AttackTarget") as GameObject;
		}
		smokeTrail = Instantiate(smokeTrailPrefab) as GameObject;
		smokeTrail.transform.position = transform.position;
		smokeTrail.transform.parent = transform;
	}
	
	protected void RemoveSmokeTrail()
	{
		if(smokeTrail != null)
		{
			if(transform.position.y < 50)
			{
				Destroy(smokeTrail,1.0f);
				smokeTrail = null;
				GameObject dust = Instantiate(dustPrefab) as GameObject;
				dust.transform.position = transform.position;
				dust.transform.parent = transform;
				Destroy(dust,5.75f);
			}
		}
	}
	
	void Update()
	{
		if(!IsAlive())
			return;
		UnitUpdate();
		ClassUpdate();
	}
	
	protected void UnitUpdate()
	{
		CheckHealth();
	}
	
	protected virtual void ClassUpdate()
	{
		RemoveSmokeTrail();
	}
	
	protected void CreateSelected()
	{	
		if(isSelected && currentOrder != Order.stop && moveTarget != null)
		{
			if(moveEffect == null)
			{
				if(currentOrder == Order.move)
				{
					moveEffect = (GameObject)Instantiate(movePrefab);
				}
				else if(currentOrder == Order.defend)
				{
					moveEffect = (GameObject)Instantiate(defendPrefab);
				}
				else if(currentOrder == Order.attack)
				{
					moveEffect = (GameObject)Instantiate(attackPrefab);
				}
				if(moveEffect != null)
				{
					moveEffect.transform.parent = moveTarget;
					moveEffect.transform.localPosition = Vector3.zero;
				}
			}
		}
		else
		{
			if(moveEffect == null)
				return;
			Destroy(moveEffect);
			moveEffect = null;
		}
	}
	
	protected void CheckHealth()
	{
		if(health <= 0)
			OnDie();
	}
	
	protected virtual void CreateID()
	{
		id = currentID;
		currentID++;
		if(uName == "")
		{
			uName = firstNames[Mathf.RoundToInt(Random.Range(0,firstNames.Length))]+ " " + lastNames[Mathf.RoundToInt(Random.Range(0,lastNames.Length))];
			gameObject.name = uName;
			if(label != null)
				label.SetLabelText(uName);
		}
	}
	
	public bool IsSelectable()
	{
		return isSelectable;
	}
	
	public int GetID()
	{
		if(id == -1)
			CreateID();
		return id;
	}
	
	public virtual int GetTeamID()
	{
		if(leader == null)
			return -1;
		return leader.GetCommander().GetTeamID();
	}
	
	public virtual void RegisterLeader(Leader leader)
	{
		this.leader = leader;
		if(leader.GetCommander() != Commander.player)
			Destroy(label);
		Deselect();
		currentOrder = Order.stop;
		if(moveTarget != null)
		{
			DestroyImmediate(moveTarget.gameObject);
			moveTarget = null;
			orderTarget = null;
		}
		lastOrderer = null;
		float distanceFromLeader = Vector3.Distance(transform.position,leader.transform.position);
		if(distanceFromLeader >= 50.0f)
		{
			RecieveOrder(Order.move,leader.transform,null);
		}
		teamColor = leader.teamColor;
		leader.RegisterUnit(this);
		renderer.material.color = teamColor;
	}
	
	public virtual void RecieveOrder(Order order, Transform target, Leader giver)
	{
		if(target == null || target == transform && order != Order.stop || order == currentOrder && target == orderTarget)
			return;
		//Debug.Log (this+" has recieved "+order);
		if(moveTarget != null)
		{
			DestroyImmediate(moveTarget.gameObject);
			moveTarget = null;
		}
		lastOrderer = giver;
		currentOrder = order;
		if(order == Order.stop)
			return;
		orderTarget = target;
		Objective objective = target.GetComponent<Objective>();
		if(target.GetComponent<Unit>() != null || objective != null)
		{
			if(objective != null)
			{
				currentObjective = objective;
				if(order == Order.attack)
					attackObjective = objective;
				else if(order == Order.defend)
					defendObjective = objective;
			}
			Transform newTarget = ((GameObject)Instantiate(new GameObject())).transform;
			newTarget.gameObject.name = "Dummy Game Object";
			newTarget.parent = target;
			newTarget.localPosition = Vector3.zero;
			target = newTarget;
		}
		target = ((GameObject)Instantiate(target.gameObject,target.position,target.rotation)).transform;
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-3,3);
		targetLocation.z += Random.Range(-3,3);
		target.position = targetLocation;
		target.parent = orderTarget;
		if(Vector3.Distance(target.position,transform.position) < 5.0f)
		{
			if(order != Order.defend)
			{
				currentOrder = Order.stop;
				if(moveTarget != null || moveTarget.gameObject != null)
					DestroyImmediate(moveTarget.gameObject);
				moveTarget = null;
			}
			return;
		}
		moveTarget = target;
		CreateSelected();
		// This is a quick-and-dirty way for players to see that the unit has recieved orders correctly.
		if(leader == (Leader)Commander.player)
			MessageList.Instance.AddMessage(uName+", acknowledging "+order.ToString()+" order.");
	}
	
	public Order GetOrder()
	{
		return currentOrder;
	}
	
	public Leader GetLastOrderer()
	{
		return lastOrderer;
	}
	
	public Transform GetMoveTarget()
	{
		if(moveTarget == null)
			return null;
		if(Vector3.Distance(moveTarget.position,transform.position) < 5.0f)
		{
			DestroyImmediate(moveTarget.gameObject);
			currentOrder = Order.stop;
			moveTarget = null;
		}
		return moveTarget;
	}
	
	public bool Select()
	{
		if(!isSelectable || !IsAlive())
		{
			isSelected = false;
			return false;
		}
		isSelected = true;
		if(leader != (Leader)Commander.player)
			return true;
		CreateSelected();
		renderer.material.SetColor("_OutlineColor",Color.green);
		return true;
	}
	
	public void IsLookedAt(bool lookedAt)
	{
		if(label == null)
			return;
		label.isLookedAt = lookedAt;
		if(lookedAt)
			label.SetLabelText(GenerateLabel());
	}
	
	public string GenerateLabel()
	{
		string labelS = uName+"\n";
		labelS = labelS + GetClass()+"\n";
		labelS = labelS + "Weapon: ";
		if(weapon == null)
			labelS = labelS + "None\n";
		else
			labelS = labelS + weapon.weaponName+"\n";
		labelS = labelS + "Health: "+health+" / 100";
		return labelS;
	}
	
	protected virtual string GetClass()
	{
		return "Grunt";
	}
	
	public void Deselect()
	{
		if(!isSelectable || !IsAlive())
			return;
		isSelected = false;
		if(leader.GetCommander() != Commander.player)
			return;
		CreateSelected();
		renderer.material.SetColor("_OutlineColor",Color.black);
	}
	
	protected void OnDie()
	{
		if(!IsAlive())
			return;
		if(leader != null)
			leader.RemoveUnit(id);
		if(currentObjective != null)
			currentObjective.RemovePlayer(this);
		IsLookedAt(false);
		gameObject.SetActive(false);
		if(leader != null)
			leader.RemoveUnit(id);
		if(weapon != null)
			Destroy(weapon.gameObject);
		Deselect();
		Destroy (moveEffect);
		if(moveTarget != null)
		{
			Destroy (moveTarget.gameObject);
			moveTarget = null;
			orderTarget = null;
		}
		foreach(Transform child in transform)
		{
			child.gameObject.SetActive(false);
		}
		// Reset spawn point.
		spawnPoint = Vector3.zero;
		Commander commander = GetCommander();
		if(commander != null)
		{
			float respawnTime = commander.GetTimeToRespawn();
			Debug.Log ("Respawning in: "+respawnTime.ToString());
			Invoke("Spawn",respawnTime);
		}
	}
	
	public bool IsAlive()
	{
		bool isAlive = gameObject != null && gameObject.activeInHierarchy;
		if(isAlive && weapon == null && health <= 0) // Useful for debugging; automatically spawns the GameObject if we re-enable it from the inspector.
			Spawn();
		return isAlive;
	}
	
	public float GetHealthPercent()
	{
		float _health = 0.00f;
		if(!IsAlive())
			return _health;
		_health = (float)health / (float)_maxHealth;
		return health;
	}
	
	public Leader UpgradeUnit(Commander commander)
	{
		Leader upgrade = gameObject.AddComponent<Leader>();
		upgrade.CloneUnit(this);
		upgrade.RegisterCommander(commander);
		if(leader == (Leader)Commander.player)
			MessageList.Instance.AddMessage(uName+", acknowledging promotion to Leader.");
		Destroy(this); // This script will not be destroyed until the end of this frame.
		return upgrade;
	}
	
	public virtual Commander GetCommander()
	{
		if(leader == null) // Haven't set anything up yet.
			return null;
		return leader.GetCommander();
	}
	
	/// <summary>
	/// Uses RAIN's detection system to find the best nearby enemy to attack.
	/// </summary>
	/// <returns>
	/// The best nearby enemy capable of being attacked.
	/// </returns>
	/// <param name='agent'>
	/// The RAIN agent.
	/// </param>
	/// <param name='enemy'>
	/// A string representing the aspect owned by our enemy.
	/// </param>
	public Unit DetectEnemies(RAIN.Core.Agent agent,string enemy)
	{
		Unit[] units = DetectUnits(agent,enemy);
		if(units.Length == 0)
			return null;
		Unit bestUnit = null;
		// Assign a score to each enemy:
		float score = Mathf.Infinity;
		foreach(Unit unit in units)
		{
			if(unit == null)
				continue;
			float uScore = Vector3.Distance(unit.transform.position,transform.position);
			uScore *= unit.GetHealthPercent();
			if(bestUnit == null || uScore < score)
			{
				bestUnit = unit;
				score = uScore;
			}
		}
		// Set the lowest-scoring unit to be our target:
		if(bestUnit == null || !bestUnit.IsAlive())
			return null;
		return bestUnit;
	}
	
	public Unit[] DetectUnits(RAIN.Core.Agent agent, string unitTag)
	{
		if(agent == null)
			return DetectUnits(unitTag,50.0f);
		// Sense any nearby enemies:
		agent.GainInterestIn(unitTag);
		agent.BeginSense();
		agent.Sense();
		
		// Fetch all GameObjects sensed:
		GameObject[] gos = new GameObject[10];
		agent.GetAllObjectsWithAspect(unitTag,out gos);
		
		// Narrow the list down to just our living enemies:
		List<Unit> unitList = new List<Unit>();
		foreach(GameObject go in gos)
		{
			Unit unit = go.GetComponent<Unit>();
			if(unit == null || unit.tag != unitTag || !unit.IsAlive())
				continue;
			unitList.Add(unit);
		}
		if(unitList.Count == 0)
			return new Unit[0];
		return unitList.ToArray();
	}
	
	public Unit[] DetectUnits(string unitTag, float maxDistance)
	{
		GameObject[] gos = GameObject.FindGameObjectsWithTag(unitTag);
		if(gos.Length == 0)
			return new Unit[0];
		Vector3 cachedPosition = transform.position;
		List<Unit> closestUnits = new List<Unit>();
		foreach(GameObject go in gos)
		{
			Unit u = go.GetComponent<Unit>();
			if(u == null || go == gameObject)
				continue;
			float distance = Vector3.Distance(cachedPosition,go.transform.position);
			if(distance > maxDistance)
				continue;
			closestUnits.Add(u);
		}
		return closestUnits.ToArray();
	}
	
	public RAIN.Action.Action.ActionResult Shoot(RAIN.Core.Agent agent, float deltaTime, Unit enemy)
	{
		if(enemy == null || !enemy.IsAlive())
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(weapon.ammo <= 0)
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(weapon.range < Vector3.Distance(enemy.transform.position,agent.Avatar.transform.position))
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(agent.LookAt(enemy.transform.position,deltaTime))
			weapon.Shoot();
		float accuracy = weapon.GetAccuracy();
		if(accuracy > 0.85f)
		{
			return RAIN.Action.Action.ActionResult.SUCCESS;
		}
		else
		{
			if(agent.MoveTo(enemy.transform.position,deltaTime))
			{
				return RAIN.Action.Action.ActionResult.SUCCESS;
			}
		}
		return RAIN.Action.Action.ActionResult.RUNNING;
	}
	
	protected void AllowSpawn()
	{
		skipSpawn = false;
	}
	
	public void CloneUnit(Unit oldClone)
	{
		isSelectable = oldClone.isSelectable;
		id = oldClone.id;
		leader = oldClone.leader;
		uName = oldClone.uName;
		smokeTrail = oldClone.smokeTrail;
		teamColor = oldClone.teamColor;
		label = oldClone.label;
		currentOrder = oldClone.currentOrder;
		moveTarget = oldClone.moveTarget;
		orderTarget = oldClone.orderTarget;
		health = oldClone.health;
		weapon = oldClone.weapon;
		weapon.owner = this;
		leader.ReplaceUnit(id, this);
		skipSpawn = true;
		Invoke("AllowSpawn",5.0f);
	}
}
