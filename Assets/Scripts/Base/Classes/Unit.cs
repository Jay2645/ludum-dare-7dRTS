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
	/// <summary>
	/// TRUE if we can hurt our teammates; FALSE if we cannot hurt them.
	/// If friendly fire is disabled, we can also move through our teammates.
	/// </summary>
	public static bool friendlyFire = false;
	
	
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
	protected const float ENEMY_RECHECK_TIME = 7.0f;
	/// <summary>
	/// How often the AI forces a repath when going to an objective.
	/// </summary>
	protected const float AI_REPATH_TIME = 9.0f;
	/// <summary>
	/// The AI's horizontal field of view.
	/// </summary>
	protected const float AI_FOV_HORIZONTAL_RANGE = 180.0f;
	/// <summary>
	/// The AI's vertical field of view.
	/// </summary>
	protected const float AI_FOV_VERTICAL_RANGE = 60.0f;
	/// <summary>
	/// How much to increment the raycast by when calculating FOV.
	/// </summary>
	protected const float FOV_RAYCAST_INCREMENT_RATE = 15.0f;
	/// <summary>
	/// We will detect any enemy which is this close to us.
	/// </summary>
	protected const float CLOSE_ENEMY_DETECT_RANGE = 5.0f;
	/// <summary>
	/// How long it takes after spawning before we take damage.
	/// </summary>
	protected const float RESPAWN_BLINK_TIME = 3.0f;
	
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
	protected int health = 100;
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
	/// <summary>
	/// Does this unit avoid all damage?
	/// </summary>
	public bool immortal = false;
	/// <summary>
	/// How much longer until we respawn?
	/// </summary>
	protected float timeToOurRespawn = 0.0f;
	
	
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
	public Order currentOrder = Order.attack;
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
	protected float lastDetectTime = 0.0f;
	/// <summary>
	/// The best enemy unit for us to shoot at.
	/// </summary>
	protected Unit bestUnit;
	/// <summary>
	/// The time since our last repath call.
	/// </summary>
	protected float timeSinceLastRepath = 0.0f;
	/// <summary>
	/// Our old move target.
	/// </summary>
	protected Transform oldMoveTarget = null;
	/// <summary>
	/// Our old order.
	/// </summary>
	protected Order oldOrder = Order.stop;
	/// <summary>
	/// Are we shooting?
	/// </summary>
	protected bool isShooting = false;
	/// <summary>
	/// Which layers we ignore for raycasting.
	/// </summary>
	public LayerMask raycastIgnoreLayers;
	/// <summary>
	/// The last unit which damaged us.
	/// </summary>
	protected Unit lastDamager;
	
	
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
	/// <summary>
	/// The color of our outline.
	/// </summary>
	protected Color outlineColor = Color.black;
	/// <summary>
	/// The noise made every second before we respawn.
	/// </summary>
	public AudioClip respawnBlip = null;
	/// <summary>
	/// The noise made when we respawn.
	/// </summary>
	public AudioClip respawnBeep = null;
	
	
	
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
		if(renderer != null)
			outlineColor = renderer.material.GetColor("_OutlineColor");
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
		UnitStart();
		ClassStart();
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
		CancelInvoke();
		timeToOurRespawn = 0.0f;
		if(!gameObject.activeInHierarchy && IsPlayer())
			Camera.main.audio.PlayOneShot(respawnBeep);
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
		if(gameObject.tag == "Red")
			enemyName = "Blue";
		else
			enemyName = "Red";
		
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
		immortal = true;
		Invoke ("CanTakeDamage",RESPAWN_BLINK_TIME);
		
		// Force a recheck of any AI functions.
		HandleAI(true);
	}
	
	/// <summary>
	/// Creates things that should happen when this class spawns. This will be called every time this Unit dies and respawns.
	/// </summary>
	protected virtual void ClassSpawn()
	{}
	
	protected virtual void UnitStart()
	{}
	
	protected virtual void ClassStart()
	{}
	
	protected void CanTakeDamage()
	{
		immortal = false;
	}
	
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
	
	public void Damage(float damageAmount, Unit damager)
	{
		if(immortal || health <= 0)
			return;
		health -= Mathf.RoundToInt(damageAmount);
		lastDamager = damager;
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
		if(isShooting)
			return Order.attack;
		return currentOrder;
	}
	
	public Leader GetLastOrderer()
	{
		return lastOrderer;
	}
	
	public Transform GetMoveTarget()
	{
		if(this == null)
			return null;
		if(isShooting && bestUnit != null)
			return bestUnit.transform;
		if(moveTarget == null)
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
		renderer.material.SetColor("_OutlineColor",outlineColor);
	}
	
	protected void OnDie()
	{
		if(!IsAlive())
			return;
		if(heatmapBlock != null)
			heatmapBlock.AddDeath();
		lastDamager.AddKill(this);
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
			if(child.GetComponent<Objective>() != null)
			{
				child.parent = null;
				continue;
			}
			if(child.GetComponent<AudioListener>() != null)
			{
				child.parent = null;
				continue;
			}
			child.gameObject.SetActive(false);
		}
		// Reset spawn point.
		Commander commander = GetCommander();
		if(commander != this as Commander)
			spawnPoint = Vector3.zero;
		if(commander != null)
		{
			if(IsLedByPlayer())
			{
				string deathMessage = uName;
				if(IsPlayer())
					deathMessage = 	deathMessage+" were killed by "+lastDamager.name+". " +
									"Respawning in "+Mathf.RoundToInt(GetCommander().GetTimeToRespawn())+" seconds.";
				else
					deathMessage = deathMessage+" was killed by "+lastDamager.name+".";
				MessageList.Instance.AddMessage(deathMessage);
			}
			timeToOurRespawn = commander.GetTimeToRespawn();
			Debug.Log ("Respawning in: "+timeToOurRespawn.ToString());
			if(IsPlayer())
				 InvokeRepeating("PlayRespawn",timeToOurRespawn - Mathf.Floor(timeToOurRespawn),1.0f);
			Invoke("Spawn",timeToOurRespawn);
		}
	}
	
	public void AddKill(Unit dead)
	{
		kills++;
		if(IsLedByPlayer())
			MessageList.Instance.AddMessage(uName+" killed "+dead.name+".");
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
	/// 
	protected void HandleAI()
	{
		HandleAI(false);
	}
	protected void HandleAI(bool force)
	{
		if(IsPlayer() || !IsAlive())
			return;
		HandleClassAIPreUniversal(force);
		HandleUniversalAI(force);
		HandleClassAIPostUniversal(force);
	}
	
	/// <summary>
	/// Handles the AI for every class.
	/// </summary>
	protected void HandleUniversalAI(bool force)
	{
		//if(force || timeSinceLastDetect > ENEMY_RECHECK_TIME || isShooting)
		//{
			FindShootTargets();
			//timeSinceLastDetect = 0.0f;
		//}
		if(force || timeSinceLastRepath > AI_REPATH_TIME && !isShooting)
		{
			if(currentObjective != null)
				RepathToObjective();
		}
		//timeSinceLastDetect += Time.deltaTime;
		timeSinceLastRepath += Time.deltaTime;
	}
	
	/// <summary>
	/// Handles the AI for this class only. Called before we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPreUniversal(bool force)
	{}
	
	/// <summary>
	/// Handles the AI for this class only. Called after we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPostUniversal(bool force)
	{}
	
	/// <summary>
	/// Makes the AI find and shoot at a specific target.
	/// </summary>
	protected void FindShootTargets()
	{
		if(agent == null)
			return;
		DetectEnemies(agent,enemyName);
		if(bestUnit == null)
		{
			if(isShooting)
			{
				moveTarget = oldMoveTarget;
				oldMoveTarget = null;
				isShooting = false;
				currentOrder = oldOrder;
				oldOrder = Order.attack;
			}
			return;
		}
		/*if(oldMoveTarget == null)
			oldMoveTarget = moveTarget;
		if(oldOrder == Order.attack)
			oldOrder = currentOrder;
		currentOrder = Order.attack;
		moveTarget = bestUnit.transform;
		isShooting = true;*/
		RAIN.Action.Action.ActionResult result = Shoot(agent,Time.deltaTime,bestUnit);
		if(result == RAIN.Action.Action.ActionResult.FAILURE)
		{
			moveTarget = oldMoveTarget;
			oldMoveTarget = null;
			isShooting = false;
			currentOrder = oldOrder;
			oldOrder = Order.attack;
		}
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
		if(currentObjective == null)
			return;
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
		if(bestUnit != null && bestUnit.IsAlive() && Vector3.Distance(bestUnit.transform.position,transform.position) <= weapon.range)
			return bestUnit;
		lastDetectTime = Time.time;
		Unit[] units = DetectUnits(rAgent,enemyAspect);
		if(units.Length == 0)
		{
			bestUnit = null;
			return null;
		}
		bestUnit = null;
		// Assign a score to each enemy:
		float score = Mathf.Infinity;
		foreach(Unit unit in units)
		{
			if(unit == null || !unit.IsAlive())
				continue;
			float uScore = Vector3.Distance(unit.transform.position,transform.position);
			uScore *= unit.GetHealthPercent();
			if(bestUnit == null || uScore < score)
			{
				bestUnit = unit;
				score = uScore;
			}
		}
		if(!bestUnit.IsAlive())
			bestUnit = null;
		return bestUnit;
	}
	
	public Unit[] DetectUnits(RAIN.Core.Agent rAgent, string unitTag)
	{
		if(weapon == null)
			return new Unit[0];
		float maxVerticalFOV = AI_FOV_VERTICAL_RANGE / 2;
		float currentVerticalFOV = -maxVerticalFOV - FOV_RAYCAST_INCREMENT_RATE;
		Vector3 position = transform.position;
		float sightRange = weapon.range + 15.0f;
		Vector3 fovDirection = transform.forward;
		RaycastHit hitInfo;
		List<Unit> detectedUnits = new List<Unit>();
		for(; currentVerticalFOV <= maxVerticalFOV; currentVerticalFOV += FOV_RAYCAST_INCREMENT_RATE)
		{
			float maxFOV = AI_FOV_HORIZONTAL_RANGE / 2;
			float currentFOV = -maxFOV;
			for(;currentFOV <= maxFOV; currentFOV += FOV_RAYCAST_INCREMENT_RATE)
			{
				fovDirection = Quaternion.Euler(currentVerticalFOV,currentFOV,0) * transform.forward;
				if(Physics.Raycast(position,fovDirection,out hitInfo,sightRange,raycastIgnoreLayers))
				{
					Unit unit = hitInfo.transform.GetComponent<Unit>();
					if(unit == null || detectedUnits.Contains(unit) || unit.tag != unitTag || !unit.IsAlive())
						continue;
					detectedUnits.Add(unit);
				}
				Debug.DrawRay(position,fovDirection,Color.magenta);
			}
		}
		for(float closeDetectAmount = 0; closeDetectAmount < 360; closeDetectAmount += FOV_RAYCAST_INCREMENT_RATE)
		{
			fovDirection = Quaternion.Euler(0,closeDetectAmount,0) * transform.forward;
			if(Physics.Raycast(position,fovDirection,out hitInfo,CLOSE_ENEMY_DETECT_RANGE,raycastIgnoreLayers))
			{
				Unit unit = hitInfo.transform.GetComponent<Unit>();
				if(unit == null || detectedUnits.Contains(unit) || unit.tag != unitTag || !unit.IsAlive())
					continue;
				detectedUnits.Add(unit);
			}
			Debug.DrawRay(position,fovDirection,Color.cyan);
		}
		return detectedUnits.ToArray();
	}
		/*if(rAgent == null)
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
			if(unit == null || !unit.IsAlive())
				continue;
			Debug.Log (this+" detected "+unit);
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
			if(unit == null || !unit.IsAlive())
				continue;
			Debug.Log (this+" heard "+unit+"'s gunshot.");
			unitList.Add(unit);
		}
		
		if(unitList.Count == 0)
			return new Unit[0];
		return unitList.ToArray();*/
	//}
	
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
		if(range < Vector3.Distance(enemy.transform.position,transform.position))
			return RAIN.Action.Action.ActionResult.FAILURE;
		Quaternion rot = Quaternion.LookRotation(enemy.transform.position - transform.position);
		transform.rotation = Quaternion.Slerp(transform.rotation,rot,Time.deltaTime * 4);
		Vector3 weaponForward = weapon.transform.up;
		Ray shotRay = new Ray(weapon.transform.position,weaponForward);
		Debug.DrawRay(weapon.transform.position,weaponForward,Color.red);
		RaycastHit hitInfo;
		if(Physics.Raycast(shotRay, out hitInfo, range,raycastIgnoreLayers))
		{
			if(hitInfo.transform.tag == "Ground")
			{
				return RAIN.Action.Action.ActionResult.FAILURE;
			}
			/*Unit unit = hitInfo.collider.gameObject.GetComponent<Unit>();
			if(unit != null)
			{
				return RAIN.Action.Action.ActionResult.FAILURE;
			}
			if(IsFriendly(unit))
			{
				Debug.Log (this+" is not shooting in case of friendly fire hitting "+unit);
				return RAIN.Action.Action.ActionResult.FAILURE;
			}*/
		}
		//else
			//return RAIN.Action.Action.ActionResult.FAILURE;
		
		weapon.Shoot();
		float accuracy = weapon.GetAccuracy();
		if(accuracy > 0.65f)
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
	
	protected void PlayRespawn()
	{
		Camera.main.audio.PlayOneShot(respawnBlip);
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
		Commander commander = GetCommander();
		if(commander == null)
			return false;
		return commander.IsPlayer();
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
		enemyName = oldClone.enemyName;
		leader = oldClone.leader;
		uName = oldClone.uName;
		teamColor = oldClone.teamColor;
		label = oldClone.label;
		currentOrder = oldClone.currentOrder;
		moveTarget = oldClone.moveTarget;
		orderTarget = oldClone.orderTarget;
		lastDamager = oldClone.lastDamager;
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
