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
	// Static variables //
	/// <summary>
	/// The current global Unit ID.
	/// </summary>
	private static int currentID = 0;
	/// <summary>
	/// The prefab instantiated when a unit is selected.
	/// </summary>
	public static GameObject selectPrefab = null;
	/// <summary>
	/// The prefab instantiated when a move order is given.
	/// </summary>
	public static GameObject movePrefab = null;
	/// <summary>
	/// The prefab instantiated when an attack order is given.
	/// </summary>
	public static GameObject attackPrefab = null;
	/// <summary>
	/// The prefab instantiated when a defend order is given.
	/// </summary>
	public static GameObject defendPrefab = null;
	/// <summary>
	/// A list of possible first names for each unit.
	/// </summary>
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
	/// <summary>
	/// A list of possible last names for each unit.
	/// </summary>
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
	
	
	// Constants //
	/// <summary>
	/// How far we have to be from our goal to be "close enough".
	/// </summary>
	protected const float MOVE_CLOSE_ENOUGH_DISTANCE = 2.0f;
	/// <summary>
	/// How far away do we have to be from our leader to move towards it.
	/// </summary>
	protected const float MOVE_TO_LEADER_DISTANCE = 50.0f;
	/// <summary>
	/// The distance our move target can be away from where we were ordered to go.
	/// </summary>
	protected const float RANDOM_TARGET_VARIATION = 2.0f;
	/// <summary>
	/// How long to wait between rechecking for enemies.
	/// </summary>
	protected const float ENEMY_RECHECK_TIME = 5.0f;
	/// <summary>
	/// How often the AI forces a repath when going to an objective.
	/// </summary>
	protected const float AI_REPATH_TIME = 4.0f;
	
	// Unit variables //
	
	// SELECTION
	/// <summary>
	/// Are we able to be selected?
	/// </summary>
	protected bool isSelectable = true;
	/// <summary>
	/// Are we currently selected?
	/// </summary>
	protected bool isSelected = false;
	
	// GAMEPLAY
	/// <summary>
	/// Our current health.
	/// </summary>
	public int health = 100;
	/// <summary>
	/// Our max health.
	/// </summary>
	protected int _maxHealth;
	/// <summary>
	/// Our current weapon.
	/// </summary>
	public Weapon weapon = null;
	/// <summary>
	/// Our initial weapon.
	/// </summary>
	protected Weapon _initialWeapon;
	/// <summary>
	/// The number of kills we've earned.
	/// </summary>
	protected int kills = 0;
	/// <summary>
	/// The number of captures we've had.
	/// </summary>
	protected int captures = 0;
	/// <summary>
	/// Our spawn point.
	/// </summary>
	public Vector3 spawnPoint = Vector3.one;
	/// <summary>
	/// Should we skip our next opprotunity to spawn?
	/// </summary>
	protected bool skipSpawn = false;
	/// <summary>
	/// The base this unit belongs to.
	/// </summary>
	public Base aBase = null;
	/// <summary>
	/// Is this unit currently capturing an objective?
	/// </summary>
	public bool isCapturing = false;
	
	
	// IDENTIFICATION
	/// <summary>
	/// This unit's unique identifier.
	/// </summary>
	protected int id = -1;
	/// <summary>
	/// The name of the unit.
	/// </summary>
	protected string uName = "";
	/// <summary>
	/// The color of our team.
	/// </summary>
	public Color teamColor = Color.white;
	/// <summary>
	/// The team name of our enemy.
	/// </summary>
	public string enemyName = "";
	
	// AI
	/// <summary>
	/// The Transform we are currently moving to.
	/// </summary>
	protected Transform moveTarget = null;
	/// <summary>
	/// The Transform we were ordered to go to.
	/// </summary>
	protected Transform orderTarget = null;
	/// <summary>
	/// Our leader.
	/// </summary>
	protected Leader leader = null;
	/// <summary>
	/// The unit which last gave us orders..
	/// </summary>
	protected Leader lastOrderer = null;
	/// <summary>
	/// Our AI agent.
	/// </summary>
	protected RAIN.Core.Agent agent = null;
	/// <summary>
	/// Our current order.
	/// </summary>
	public Order currentOrder = Order.stop;
	/// <summary>
	/// The objective we're currently defending.
	/// </summary>
	public Objective defendObjective;
	/// <summary>
	/// The objective we're currently attacking.
	/// </summary>
	public Objective attackObjective;
	/// <summary>
	/// Our current objective.
	/// </summary>
	public Objective currentObjective;
	/// <summary>
	/// The time since we last tried to detect enemies.
	/// </summary>
	protected float timeSinceLastDetect = 0.0f;
	/// <summary>
	/// The best enemy unit for us to shoot at.
	/// </summary>
	protected Unit bestUnit;
	protected float timeSinceLastRepath = 0.0f;
	
	
	// VISUALS
	/// <summary>
	/// This unit's shadow, set in the Inspector.
	/// </summary>
	public GameObject shadow = null;
	/// <summary>
	/// This unit's label.
	/// </summary>
	public ObjectLabel label = null;
	/// <summary>
	/// The effect the player sees that marks where we're currently moving to.
	/// </summary>
	protected GameObject moveEffect = null;
	/// <summary>
	/// The effect the player sees that marks we are selected.
	/// </summary>
	protected GameObject selectEffect = null;
	/// <summary>
	/// The heatmap block we're currently in.
	/// </summary>
	protected HeatmapBlock heatmapBlock = null;
	
	
	
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
		string tag = gameObject.tag;
		if(tag == "Red")
			enemyName = "Blue";
		else
			enemyName = "Red";
	}
	
	/// <summary>
	/// Sets up whatever class this is. This will only be called once.
	/// </summary>
	protected virtual void ClassSetup()
	{
		InitPrefabs();
	}
	
	void Start()
	{
		Spawn();
	}
	
	/// <summary>
	/// Spawns this Unit. This will be called every time the Unit dies and respawns or this script is restarted.
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
		ResetTarget();
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
				if(commander.attackObjective == null)
				{
					transform.rotation = commander.transform.rotation;
				}
				else
				{
					transform.LookAt(commander.attackObjective.transform);
				}
			}
		}
		
		// Force a recheck of any AI functions.
		timeSinceLastDetect = ENEMY_RECHECK_TIME + 1;
	}
	
	/// <summary>
	/// Creates things that should happen when this class spawns. This will be called every time this Unit dies and respawns.
	/// </summary>
	protected virtual void ClassSpawn()
	{}
	
	protected virtual void CreateWeapon(Weapon weapon)
	{
		if(this.weapon != null && weapon.gameObject.activeInHierarchy)
		{
			this.weapon.transform.parent = null;
		}
		this.weapon = Instantiate(weapon) as Weapon;
		this.weapon.Pickup(this);
	}
	
	/// <summary>
	/// Initializes all prefabs.
	/// </summary>
	protected void InitPrefabs()
	{
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
		if(selectPrefab == null)
		{
			selectPrefab = Resources.Load("Prefabs/SelectEffect") as GameObject;
		}
	}
	
	void Update()
	{
		if(!IsAlive())
			return;
		UnitUpdate();
		ClassUpdate();
	}
	
	/// <summary>
	/// Updates values on this unit and every class which inherits from it. Called every frame that we are alive.
	/// </summary>
	protected void UnitUpdate()
	{
		CheckHealth();
	}
	
	/// <summary>
	/// Updates values specific to only this class. Called every frame that we are alive.
	/// </summary>
	protected virtual void ClassUpdate()
	{}
	
	void LateUpdate()
	{
		if(!IsAlive())
			return;
		UnitLateUpdate();
		ClassLateUpdate();
	}
	
	/// <summary>
	/// Handles any values which should only happen after everything else has happened this turn for this class and all that inherit from it.
	/// Called every frame we are alive.
	/// </summary>
	protected void UnitLateUpdate()
	{
		HandleAI();
	}
	
	/// <summary>
	/// Handles any values which should only happen after everything else has happened this turn for this class only.
	/// Called every frame we are alive.
	/// </summary>
	protected virtual void ClassLateUpdate()
	{}
	
	/// <summary>
	/// Creates/Destroys the selection visuals.
	/// </summary>
	protected void CreateSelected()
	{	
		if(isSelected)
		{
			selectEffect = Instantiate(selectPrefab) as GameObject;
			selectEffect.transform.parent = transform;
			selectEffect.transform.localPosition = Vector3.zero;
		}
		else if(selectEffect != null)
		{
			Destroy(selectEffect);
			selectEffect = null;
		}
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
			ResetEffects();
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
	
	public virtual void RegisterLeader(Leader newLeader)
	{
		leader = newLeader;
		if(!leader.IsOwnedByPlayer())
			Destroy(label);
		Deselect();
		currentOrder = Order.stop;
		ResetTarget();
		lastOrderer = null;
		float distanceFromLeader = Vector3.Distance(transform.position,leader.transform.position);
		if(distanceFromLeader >= MOVE_TO_LEADER_DISTANCE)
		{
			RecieveOrder(Order.move,leader.transform,null);
		}
		aBase = leader.aBase;
		teamColor = leader.teamColor;
		leader.RegisterUnit(this);
		renderer.material.color = teamColor;
		if(IsOwnedByPlayer() && !IsLedByPlayer())
			MessageList.Instance.AddMessage(uName+", acknowledging "+leader.name+" as my new leader.");
	}
	
	public virtual void RecieveOrder(Order order, Transform target, Leader giver)
	{
		if(target == null || target == transform && order != Order.stop || order == currentOrder && target == orderTarget)
			return;
		//Debug.Log (this+" has recieved "+order);
		ResetTarget();
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
			/*Transform newTarget = new GameObject(target.gameObject.name+"'s Transform Copy").transform;
			newTarget.parent = target;
			newTarget.localPosition = Vector3.zero;
			orderTarget = newTarget;*/
		}
		MakeMoveTarget(target);
		if(Vector3.Distance(moveTarget.position,transform.position) < MOVE_CLOSE_ENOUGH_DISTANCE)
		{
			if(order != Order.defend)
			{
				currentOrder = Order.stop;
				ResetTarget();
			}
			return;
		}
		CreateSelected();
		// This is a quick-and-dirty way for players to see that the unit has recieved orders correctly.
		if(lastOrderer == (Leader)Commander.player)
			MessageList.Instance.AddMessage(uName+", acknowledging "+order.ToString()+" order.");
	}
	
	public void MakeMoveTarget(Transform target)
	{
		GameObject targetGO = new GameObject(uName+"'s Current Target");
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-RANDOM_TARGET_VARIATION,RANDOM_TARGET_VARIATION);
		targetLocation.z += Random.Range(-RANDOM_TARGET_VARIATION,RANDOM_TARGET_VARIATION);
		Transform oldTarget = target;
		target = targetGO.transform;
		target.position = targetLocation;
		target.parent = oldTarget;
		moveTarget = target;
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
		if(moveTarget == null || this == null)
			return null;
		if(Vector3.Distance(moveTarget.position,transform.position) < MOVE_CLOSE_ENOUGH_DISTANCE)
		{
			ResetTarget();
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
		if(!IsLedByPlayer())
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
		if(!isSelectable)
			return;
		isSelected = false;
		if(!IsLedByPlayer())
			return;
		CreateSelected();
		renderer.material.SetColor("_OutlineColor",Color.black);
	}
	
	protected void OnDie()
	{
		if(!IsAlive())
			return;
		if(heatmapBlock != null)
			heatmapBlock.AddDeath();
		if(leader != null)
			leader.RemoveUnit(id);
		if(currentObjective != null)
			currentObjective.RemovePlayer(this);
		IsLookedAt(false);
		gameObject.SetActive(false);
		if(leader != null)
			leader.RemoveUnit(id);
		if(weapon != null)
			weapon.Drop();
		Deselect();
		ResetEffects();
		foreach(Transform child in transform)
		{
			child.gameObject.SetActive(false);
		}
		// Reset spawn point.
		Commander commander = GetCommander();
		if(commander != this as Commander)
			spawnPoint = Vector3.zero;
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
		upgrade.CreateSelected();
		if(IsOwnedByPlayer())
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
	/// Handles AI functions.
	/// Anything which is handled by a behavior tree is not included.
	/// </summary>
	protected void HandleAI()
	{
		if(IsPlayer() || !IsAlive())
			return;
		HandleClassAIPreUniversal();
		HandleUniversalAI();
		HandleClassAIPostUniversal();
	}
	
	/// <summary>
	/// Handles the AI for every class.
	/// </summary>
	protected void HandleUniversalAI()
	{
		if(timeSinceLastDetect > ENEMY_RECHECK_TIME)
		{
			FindShootTargets();
			timeSinceLastDetect = 0.0f;
		}
		if(timeSinceLastRepath > AI_REPATH_TIME && currentObjective != null)
		{
			RepathToObjective();
		}
		timeSinceLastDetect += Time.deltaTime;
		timeSinceLastRepath += Time.deltaTime;
	}
	
	/// <summary>
	/// Handles the AI for this class only. Called before we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPreUniversal()
	{}
	
	/// <summary>
	/// Handles the AI for this class only. Called after we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPostUniversal()
	{}
	
	/// <summary>
	/// Makes the AI find and shoot at a specific target.
	/// </summary>
	protected void FindShootTargets()
	{
		//Unit enemy = DetectEnemies(agent,enemyName);
	}
	
	/// <summary>
	/// Forces a repath on the next frame, so long as our objective is not null.
	/// </summary>
	public void ForceRepath()
	{
		if(currentObjective == null)
		{
			Debug.LogWarning("Can't force a repath next frame: our current objective is null!");
			return;
		}
		timeSinceLastRepath = AI_REPATH_TIME + 1;
	}
	
	protected void RepathToObjective()
	{
		ResetTarget();
		MakeMoveTarget(currentObjective.transform);
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
	public Unit DetectEnemies(RAIN.Core.Agent rAgent,string enemyAspect)
	{
		Unit[] units = DetectUnits(rAgent,enemyAspect);
		if(units.Length == 0)
			return null;
		bestUnit = null;
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
	
	public Unit[] DetectUnits(RAIN.Core.Agent rAgent, string unitTag)
	{
		if(rAgent == null)
			return DetectUnits(unitTag,50.0f);
		agent = rAgent;
		// Sense any nearby enemies:
		agent.GainInterestIn(unitTag);
		agent.GainInterestIn(unitTag+" Gunshot");
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
		
		gos = new GameObject[10];
		agent.GetAllObjectsWithAspect(unitTag+" Gunshot",out gos);
		foreach(GameObject go in gos)
		{
			if(go.audio == null)
				continue;
			Weapon gun = go.GetComponent<Weapon>();
			if(gun == null || !gun.HasShotRecently())
				continue;
			Unit unit = gun.GetOwner();
			if(unit == null || unit.tag != unitTag || !unit.IsAlive())
				continue;
			Debug.Log ("Found a gun.");
			unitList.Add(unit);
		}
		
		if(unitList.Count == 0)
			return new Unit[0];
		return unitList.ToArray();
	}
	
	public void SetAgent(RAIN.Core.Agent agent)
	{
		this.agent = agent;
	}
	
	public Unit[] DetectUnits(string unitTag, float maxDistance)
	{
		if(agent != null)
			return DetectUnits(agent,unitTag);
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
	
	/// <summary>
	/// Allows the AI to aim and shoot at a target.
	/// </summary>
	/// <param name='agent'>
	/// The AI agent.
	/// </param>
	/// <param name='deltaTime'>
	/// The time it's taken since the last step in the behavior tree, as reported by the agent.
	/// </param>
	/// <param name='enemy'>
	/// The enemy we're trying to shoot at.
	/// </param>
	public RAIN.Action.Action.ActionResult Shoot(RAIN.Core.Agent agent, float deltaTime, Unit enemy)
	{
		if(enemy == null || !enemy.IsAlive())
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(weapon.ammo <= 0)
			return RAIN.Action.Action.ActionResult.FAILURE;
		float range = weapon.range;
		if(range < Vector3.Distance(enemy.transform.position,agent.Avatar.transform.position))
			return RAIN.Action.Action.ActionResult.FAILURE;
		Ray shotRay = new Ray(transform.position,transform.forward);
		RaycastHit hitInfo;
		if(Physics.Raycast(shotRay, out hitInfo, range))
		{
			if(hitInfo.transform.tag == "Ground")
				return RAIN.Action.Action.ActionResult.FAILURE;
			Unit unit = hitInfo.collider.gameObject.GetComponent<Unit>();
			if(unit != null && IsFriendly(unit))
			{
				return RAIN.Action.Action.ActionResult.FAILURE;
			}
		}
		else
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
	
	public void Score()
	{
		captures++;
		GetCommander().OnScore();
	}
	
	public bool IsFriendly(Unit other)
	{
		return GetCommander() == other.GetCommander();
	}
	
	public virtual bool IsLedByPlayer()
	{
		return leader == (Leader)Commander.player;
	}
	
	public virtual bool IsOwnedByPlayer()
	{
		return GetCommander().IsPlayer();
	}
	
	public virtual bool IsPlayer()
	{
		return false;
	}
	
	protected void AllowSpawn()
	{
		skipSpawn = false;
	}
	
	public void OnPickupFlag()
	{
		ResetTarget(false);
	}
	
	protected void ResetTarget()
	{
		ResetTarget(true);
	}
	
	protected void ResetTarget(bool effects)
	{
		if(orderTarget != null && orderTarget.name.Contains("Copy"))
		{
			Destroy(orderTarget.gameObject);
		}
		if(moveTarget != null)
		{
			Destroy(moveTarget.gameObject);
			moveTarget = null;
			orderTarget = null;
		}
		if(effects)
			ResetEffects();
	}
	
	protected void ResetEffects()
	{
		if(moveEffect != null)
		{
			DestroyImmediate(moveEffect);
			moveEffect = null;
		}
		if(selectEffect != null)
		{
			DestroyImmediate(selectEffect);
			selectEffect = null;
		}
	}
	
	public void CloneUnit(Unit oldClone)
	{
		isSelectable = oldClone.isSelectable;
		isSelected = oldClone.isSelected;
		id = oldClone.id;
		leader = oldClone.leader;
		uName = oldClone.uName;
		teamColor = oldClone.teamColor;
		label = oldClone.label;
		currentOrder = oldClone.currentOrder;
		moveTarget = oldClone.moveTarget;
		orderTarget = oldClone.orderTarget;
		health = oldClone.health;
		weapon = oldClone.weapon;
		aBase = oldClone.aBase;
		weapon.Pickup(this);
		leader.ReplaceUnit(id, this);
		skipSpawn = true;
		Invoke("AllowSpawn",5.0f);
	}
	
	public void EnterHeatmapBlock(HeatmapBlock heatBlock)
	{
		heatmapBlock = heatBlock;
	}
	
	public void ExitHeatmapBlock(HeatmapBlock heatBlock)
	{
		if(heatmapBlock == heatBlock)
			heatmapBlock = null;
	}
}
