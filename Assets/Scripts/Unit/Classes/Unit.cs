using System.Collections.Generic;
using UnityEngine;

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
/// All data about our current order.
/// </summary>
public class OrderData
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OrderData"/> class.
	/// The simplest way to initialize. Requires a Leader which gave the orders and a Unit to recieve them.
	/// If nothing is changed, this will give the unit a "stop" command.
	/// </summary>
	/// <param name='leader'>
	/// Who is giving the orders?
	/// </param>
	/// <param name='unit'>
	/// Who is recieving the orders?
	/// </param>
	public OrderData(Leader leader, Unit unit)
	{
		this.leader = leader;
		this.unit = unit;
		SetOrder(Order.stop, false);
	}
	/// <summary>
	/// Where is this telling the Unit to be sent to?
	/// </summary>
	private Transform orderTarget = null;
	/// <summary>
	/// Where are we actually being sent to?
	/// </summary>
	private Transform moveTarget = null;
	/// <summary>
	/// How are we supposed to act along the way?
	/// </summary>
	private Order order = Order.stop;
	/// <summary>
	/// What Unit are we giving these orders to?
	/// </summary>
	private Unit unit = null;
	/// <summary>
	/// Who created this order?
	/// </summary>
	private Leader leader = null;
	/// <summary>
	/// How this Unit should move on the way to fulfilling the order.
	/// </summary>
	private MoveType moveType = MoveType.Strict;
	/// <summary>
	/// Has the movement type been changed from its default state?
	/// </summary>
	private bool moveTypeChanged = false;
	/// <summary>
	/// Has the Unit updated this?
	/// </summary>
	private bool updatedByUnit = false;
	/// <summary>
	/// Where was the unit when it recieved this order?
	/// </summary>
	private Vector3 unitLocation = Vector3.zero;

	/// <summary>
	/// Sets the order target (the general location we're going to).
	/// </summary>
	/// <param name='target'>
	/// The transform that we're moving towards.
	/// </param>
	public void SetTarget(Transform target)
	{
		unitLocation = unit.transform.position;
		orderTarget = target;
	}
	/// <summary>
	/// Gets our order target (the general location we're going to).
	/// </summary>
	/// <returns>
	/// The order target that we're moving to.
	/// </returns>
	public Transform GetOrderTarget()
	{
		return orderTarget;
	}

	/// <summary>
	/// Sets the exact location that we're moving towards.
	/// If this is set by the Unit that we're giving the orders, it automatically sends the updated location to the lader.
	/// </summary>
	/// <param name='moveTarget'>
	/// The exact Transform we're moving to.
	/// </param>
	/// <param name='setter'>
	/// The Unit setting this move target.
	/// </param>
	public void SetMoveTarget(Transform moveTarget, Unit setter)
	{
		this.moveTarget = moveTarget;
		if (setter == unit)
		{
			UpdatedByUnit();
		}
	}
	public Transform GetMoveTarget()
	{
		if (moveTarget == null)
			return orderTarget;
		return moveTarget;
	}

	public void SetUnit(Unit unit)
	{
		this.unit = unit;
		UpdatedByUnit();
	}

	public void SetOrder(Order order, bool autoMoveType)
	{
		this.order = order;
		unitLocation = unit.transform.position;
		if (!autoMoveType)
			return;
		if (order == Order.attack || order == Order.defend)
		{
			SetMoveType(MoveType.Loose);
		}
		else if (order == Order.stop)
		{
			SetMoveType(MoveType.DefendSelf);
		}
		else if (order == Order.move)
		{
			SetMoveType(MoveType.Strict);
		}
	}
	public Order GetOrder()
	{
		return order;
	}

	public void SetMoveType(MoveType movementType)
	{
		moveTypeChanged = true;
		moveType = movementType;
	}
	public bool MoveTypeWasChanged()
	{
		return moveTypeChanged;
	}
	public MoveType GetMoveType()
	{
		return moveType;
	}

	public void UpdatedByUnit()
	{
		unitLocation = unit.transform.position;
		updatedByUnit = true;
		leader.UnitUpdateOrder(unit, this);
	}
	public bool WasUpdatedByUnit()
	{
		return updatedByUnit;
	}

	public Leader GetLeader()
	{
		return leader;
	}
	public Vector3 GetOrderLocation()
	{
		return unitLocation;
	}
}

/// <summary>
/// The type of this Unit.
/// This is mostly for the promotion code:
/// MonoBehaviours can't be created using the "new" keyword, so we have to use an enum to specify what type of Unit is being made.
/// </summary>
public enum UnitType
{
	/// <summary>
	/// Basic Unit.
	/// </summary>
	Unit,
	/// <summary>
	/// Basic Leader.
	/// </summary>
	Leader,
	/// <summary>
	/// Basic Commander.
	/// </summary>
	Commander
}

/// <summary>
/// What is this Unit currently doing?
/// </summary>
public enum UnitStatus
{
	/// <summary>
	/// Idle means we are not doing anything of note.
	/// </summary>
	Idle,
	/// <summary>
	/// Moving means we are moving to an objective.
	/// </summary>
	Moving,
	/// <summary>
	/// InCombat means we are being shot at or are in pursuit of an enemy.
	/// </summary>
	InCombat,
	/// <summary>
	/// CapturingObjective means we are presently capturing an objective.
	/// </summary>
	CapturingObjective
}

/// <summary>
/// A Unit is the main object of the game. Anything which can be selected and recieve orders is considered a Unit.
/// Units have a unique ID and a name so the player can tell them apart.
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
		"Tyrone",
		"Jeff",
		"Edward",
		"Jaime",
		"Edward",
		"Robert",
		"Jon",
		"Brandon",
		"Tony"
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
		"Clark",
		"Stark"
	};
	/// <summary>
	/// TRUE if we can hurt our teammates; FALSE if we cannot hurt them.
	/// If friendly fire is disabled, we can also move through our teammates.
	/// </summary>
	public static bool friendlyFire = false;


	// Constants //
	/// <summary>
	/// How far away do we have to be from our leader to move towards it.
	/// </summary>
	protected const float MOVE_TO_LEADER_DISTANCE = 25.0f;
	/// <summary>
	/// How far to chase our enemy.
	/// </summary>
	protected const float MAX_ENEMY_CHASE_RANGE = 35.0f;
	/// <summary>
	/// How long to wait between rechecking for enemies.
	/// </summary>
	protected const float ENEMY_RECHECK_TIME = 7.0f;
	/// <summary>
	/// The AI's horizontal field of view.
	/// </summary>
	protected const float AI_FOV_HORIZONTAL_RANGE = 180.0f;
	/// <summary>
	/// The AI's vertical field of view.
	/// </summary>
	protected const float AI_FOV_VERTICAL_RANGE = 60.0f;
	/// <summary>
	/// We will detect any enemy which is this close to us.
	/// </summary>
	protected const float CLOSE_ENEMY_DETECT_RANGE = 5.0f;
	/// <summary>
	/// How long it takes after spawning before we take damage.
	/// </summary>
	protected const float RESPAWN_BLINK_TIME = 3.0f;
	/// <summary>
	/// How long after not being shot at and not shooting before we are no longer considered "In Combat."
	/// </summary>
	protected const float COMBAT_EXIT_TIME = 8.0f;
	/// <summary>
	/// How often the AI forces a repath when going to an objective.
	/// </summary>
	protected const float AI_REPATH_TIME = 9.0f;
	/// <summary>
	/// The maximum distance a health pack can be before we hunt it down.
	/// </summary>
	protected const float MAX_HEALTH_DISTANCE = 100.0f;
	/// <summary>
	/// How many resources does a base Unit cost?
	/// </summary>
	protected const float UNIT_COST = 20.0f;
	/// <summary>
	/// How many resources does a Leader cost?
	/// </summary>
	protected const float LEADER_COST = 100.0f;
	/// <summary>
	/// How many resources does a Commander cost?
	/// Note: Currently, Commanders should be limited to 1 per team.
	/// </summary>
	protected const float COMMANDER_COST = Mathf.Infinity;

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
	/// <summary>
	/// How much to increment the raycast by when calculating FOV.
	/// </summary>
	protected float raycastIncrementRate = 15.0f;

	// GAMEPLAY
	/// <summary>
	/// The type of this Unit.
	/// </summary>
	protected UnitType unitType = UnitType.Unit;
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
	/// Can we move?
	/// </summary>
	public bool movable = true;
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
	/// The objective we're currently capturing, if any.
	/// </summary>
	protected Objective capturingObjective = null;
	/// <summary>
	/// Does this unit avoid all damage?
	/// </summary>
	public bool immortal = false;
	/// <summary>
	/// How much longer until we respawn?
	/// </summary>
	protected float timeToOurRespawn = 0.0f;
	/// <summary>
	/// Were we just promoted?
	/// </summary>
	protected bool justPromoted = false;
	/// <summary>
	/// Are we finding health?
	/// </summary>
	protected bool findingHealth = false;
	/// <summary>
	/// Where did we enter combat?
	/// </summary>
	protected Vector3 enterCombatPosition = Vector3.zero;
	/// <summary>
	/// What time did we last die?
	/// </summary>
	protected float deathTime = 0.0f;


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
	/// Our leader.
	/// </summary>
	protected Leader leader = null;
	/// <summary>
	/// The unit which last gave us orders..
	/// </summary>
	protected Leader lastOrderer = null;
	/// <summary>
	/// All information about our current order.
	/// </summary>
	protected OrderData orderData;
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
	/// Which layers we ignore for raycasting.
	/// </summary>
	public LayerMask raycastIgnoreLayers;
	/// <summary>
	/// The last unit which damaged us.
	/// </summary>
	protected Unit lastDamager;
	/// <summary>
	/// All areas where we can restore our HP.
	/// </summary>
	protected static GameObject[] regenZones;
	/// <summary>
	/// FALSE while we are still setting up, TRUE once we have spawned and everything is okay.
	/// </summary>
	protected bool running = false;
	/// <summary>
	/// How many frames have passed since we spawned. Once this is above 5, running is considered TRUE.
	/// </summary>
	protected int frameCount = 0;
	/// <summary>
	/// This unit's motor.
	/// </summary>
	protected UnitMotor motor;
	/// <summary>
	/// Our current status.
	/// </summary>
	protected UnitStatus status = UnitStatus.Idle;
	/// <summary>
	/// Our last status.
	/// </summary>
	protected UnitStatus lastStatus = UnitStatus.Idle;
	/// <summary>
	/// How long it has been since we were last in combat.
	/// </summary>
	protected float combatTime = Mathf.Infinity;
	/// <summary>
	/// Where we were on the last frame.
	/// </summary>
	protected Vector3 lastFramePosition = Vector3.zero;
	/// <summary>
	/// How long it's been since we last repathed.
	/// </summary>
	protected float timeSinceLastRepath = 0.0f;
	/// <summary>
	/// What type of movement do we use?
	/// </summary>
	protected MoveType movementType = MoveType.DefendSelf;


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
	protected static Color outlineColor = Color.clear;
	/// <summary>
	/// What color are we when we're selected?
	/// </summary>
	protected static Color selectedColor = Color.green;
	/// <summary>
	/// The sound we make when collecting health.
	/// </summary>
	public AudioClip healthRegen;
	/// <summary>
	/// The noise made every second before we respawn.
	/// </summary>
	public AudioClip respawnBlip = null;
	/// <summary>
	/// The noise made when we respawn.
	/// </summary>
	public AudioClip respawnBeep = null;
	/// <summary>
	/// The noise we make when we die.
	/// </summary>
	public AudioClip deathNoise = null;
	/// <summary>
	/// This is whatever piece of geometry makes a Unit "unique."
	/// For example, Leaders could have crowns which make them stand out to the player.
	/// </summary>
	protected GameObject uniqueGeo = null;
	protected static LayerMask defaultLayer = 0;
	protected static LayerMask leaderLayer = 0;
	protected static LayerMask unitLayer = 0;
	protected static LayerMask hudLayer = 0;
	protected List<Leader> visibleBy = new List<Leader>();


	void Awake()
	{
		if (defaultLayer == 0)
			defaultLayer = LayerMask.NameToLayer("Default");
		if (leaderLayer == 0)
			leaderLayer = LayerMask.NameToLayer("Leaders");
		if (unitLayer == 0)
			unitLayer = LayerMask.NameToLayer("Units");
		if (hudLayer == 0)
			hudLayer = LayerMask.NameToLayer("HUD Only");
		if (regenZones == null)
			regenZones = GameObject.FindGameObjectsWithTag("Regen");
		UnitSetup();
		ClassSetup();
	}

	/// <summary>
	/// Sets up the Unit. This will only be called once.
	/// </summary>
	protected void UnitSetup()
	{
		unitType = UnitType.Unit;
		CreateID();
		if (weapon != null)
		{
			_initialWeapon = weapon;
		}
		_maxHealth = health;
		if (renderer != null && outlineColor == Color.clear)
			outlineColor = renderer.material.GetColor("_OutlineColor");
		if (IsPlayer() || !movable)
			return;
		motor = gameObject.GetComponent<UnitMotor>();
		if (motor == null)
			motor = gameObject.AddComponent<UnitMotor>();
		motor.UpdateUnit(this);
		spawnPoint = Vector3.zero;
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
		// Make the GameObject visible.
		gameObject.SetActive(true);
		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(true);
		}

		if (gameObject.GetComponent<RAIN.Ontology.Decoration>() == null)
		{
			gameObject.AddComponent<RAIN.Ontology.Entity>();
			RAIN.Ontology.Decoration decoration = gameObject.AddComponent<RAIN.Ontology.Decoration>();
			RAIN.Ontology.Aspect aspect = new RAIN.Ontology.Aspect(gameObject.tag, new RAIN.Ontology.Sensation("sight"));
			decoration.aspect = aspect;
		}
		if (shadow != null)
		{
			shadow.layer = gameObject.layer;
		}

		if (leader != null)
			leader.RegisterUnit(this);
		if (gameObject.tag == "Red")
			enemyName = "Blue";
		else
			enemyName = "Red";

		// Call class-specific spawn code.
		ClassSpawn();

		// Makes sure the GameObject is the right color.
		SetTeamColor();

		// Sometimes (especially when we get promoted) we run into a bug where all our variables are reset when they shouldn't be.
		if (skipSpawn)
		{
			skipSpawn = false;
			return;
		}

		// Reset all variables to their initial state.
		Debug.Log("Resetting variables on " + this);
		if (!IsPlayer())
		{
			SetOutlineColor(outlineColor);
		}
		justPromoted = false;
		ResetTarget();
		health = _maxHealth;
		if (_initialWeapon != null)
		{
			CreateWeapon(_initialWeapon);
			if (weapon != null)
			{
				weapon.gameObject.layer = gameObject.layer;
			}
		}

		Commander commander = GetCommander();
		if (commander == null)
		{
			Debug.Log(uName + "'s Commander is null!");
		}
		else
		{
			foreach (Unit unit in commander.GetAllUnits())
			{
				if (unit == this || !unit.IsAlive())
					continue;
				Physics.IgnoreCollision(collider, unit.collider, true);
			}
			if (!(unitType == UnitType.Commander) && commander.IsAlive())
			{
				Physics.IgnoreCollision(collider, commander.collider, true);
			}
		}
		// Move it to the spawn point.
		if (spawnPoint == Vector3.zero)
		{
			if (commander != null)
			{
				spawnPoint = commander.GetSpawnPoint();
				transform.position = spawnPoint;
				if (commander.attackObjective == null)
				{
					Vector3 ourRot = transform.rotation.eulerAngles;
					float look = commander.transform.rotation.eulerAngles.y;
					ourRot.y = look;
					transform.rotation = Quaternion.Euler(ourRot);
				}
				else
				{
					Vector3 objectivePos = commander.attackObjective.transform.position;
					Vector3 ourRot = Quaternion.LookRotation(objectivePos - transform.position).eulerAngles;
					float look = ourRot.y;
					ourRot.y = look;
					transform.rotation = Quaternion.Euler(ourRot);
				}
			}
		}

		CancelInvoke();
		timeToOurRespawn = 0.0f;
		if (!gameObject.activeInHierarchy && IsPlayer())
			Camera.main.audio.PlayOneShot(respawnBeep);

		immortal = true;
		Invoke("CanTakeDamage", RESPAWN_BLINK_TIME);
		weapon.ammo = _initialWeapon.ammo;
		// Force a recheck of any AI functions.
		HandleAI(true);
	}

	/// <summary>
	/// Creates things that should happen when this class spawns. This will be called every time this Unit dies and respawns.
	/// </summary>
	protected virtual void ClassSpawn()
	{
		gameObject.layer = unitLayer;
		transform.localScale = Vector3.one;
	}

	protected virtual void UnitStart()
	{ }

	protected virtual void ClassStart()
	{ }

	protected void SetTeamColor()
	{
		if (renderer != null)
			renderer.material.color = teamColor;
		if (uniqueGeo != null && uniqueGeo.renderer != null)
		{
			Material[] mats = uniqueGeo.renderer.materials;
			mats[1].color = teamColor;
			uniqueGeo.renderer.materials = mats;
		}
	}

	protected void SetOutlineColor(Color color)
	{
		Renderer[] renderers = transform.root.GetComponentsInChildren<Renderer>();
		foreach (Renderer render in renderers)
		{
			if (render.GetComponent<Weapon>() != null)
				continue;
			Material[] mats = render.materials;
			foreach (Material mat in mats)
			{
				if (mat.HasProperty("_OutlineColor"))
				{
					mat.SetColor("_OutlineColor", color);
				}
			}
			render.materials = mats;
		}
	}

	protected void CanTakeDamage()
	{
		immortal = false;
	}

	protected virtual void CreateWeapon(Weapon weapon)
	{
		if (this.weapon != null && weapon.gameObject.activeInHierarchy)
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
		if (movePrefab == null)
		{
			movePrefab = Resources.Load("Prefabs/MoveTarget") as GameObject;
		}
		if (defendPrefab == null)
		{
			defendPrefab = Resources.Load("Prefabs/DefendTarget") as GameObject;
		}
		if (attackPrefab == null)
		{
			attackPrefab = Resources.Load("Prefabs/AttackTarget") as GameObject;
		}
		if (selectPrefab == null)
		{
			selectPrefab = Resources.Load("Prefabs/SelectEffect") as GameObject;
		}
	}

	void Update()
	{
		if (Input.GetButtonDown("Pause") && IsPlayer())
		{
			PauseMenu.Pause();
		}
		if (!IsAlive() || PauseMenu.IsPaused())
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
		if (!IsAlive() && Time.time > deathTime + timeToOurRespawn)
		{
			Spawn();
		}
		/*if(running || IsOwnedByPlayer())
			return;
		Commander commander = GetCommander();
		if(spawnPoint == Vector3.zero && commander != null)
		{
			spawnPoint = commander.GetSpawnPoint();
		}
		transform.position = spawnPoint;
		if(frameCount >= 5)
			running = true;
		else
			frameCount++;*/
	}

	/// <summary>
	/// Updates values specific to only this class. Called every frame that we are alive.
	/// </summary>
	protected virtual void ClassUpdate()
	{ }

	void LateUpdate()
	{
		if (!IsAlive())
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
		CheckStatus();
	}

	/// <summary>
	/// Handles any values which should only happen after everything else has happened this turn for this class only.
	/// Called every frame we are alive.
	/// </summary>
	protected virtual void ClassLateUpdate()
	{ }

	/// <summary>
	/// Creates/Destroys the selection visuals.
	/// </summary>
	public void CreateSelected()
	{
		if (!IsOwnedByPlayer() || IsPlayer() || !isSelectable)
			return;
		if (isSelected)
		{
			if (selectEffect == null)
			{
				selectEffect = Instantiate(selectPrefab) as GameObject;
				selectEffect.transform.parent = transform;
				selectEffect.transform.localPosition = Vector3.zero;
			}
			if (movable && moveEffect == null && orderData != null)
			{
				Order order = orderData.GetOrder();
				Transform moveTarget = orderData.GetMoveTarget();
				if (moveTarget == null)
				{
					moveTarget = orderData.GetOrderTarget();
					if (moveTarget != null)
					{
						moveTarget = motor.MakeMoveTarget(moveTarget);
						orderData.SetMoveTarget(moveTarget, this);
					}
				}
				if (moveTarget != null)
				{
					if (order == Order.attack)
					{
						moveEffect = Instantiate(attackPrefab) as GameObject;
					}
					else if (order == Order.defend)
					{
						moveEffect = Instantiate(defendPrefab) as GameObject;
					}
					else if (order == Order.move)
					{
						moveEffect = Instantiate(movePrefab) as GameObject;
					}
					moveEffect.transform.parent = moveTarget;
					moveEffect.transform.localPosition = Vector3.zero;
				}
			}
			if (moveEffect != null)
				moveEffect.layer = gameObject.layer;
			if (selectEffect != null)
				selectEffect.layer = gameObject.layer;
		}
		else if (selectEffect != null || moveEffect != null)
		{
			ResetEffects();
		}
	}

	public void Damage(float damageAmount, Unit damager)
	{
		if (immortal || health <= 0)
			return;
		health -= Mathf.RoundToInt(damageAmount);
		if (damager == null)
			return;
		if (status != UnitStatus.InCombat && status != UnitStatus.CapturingObjective)
		{
			SetStatus(UnitStatus.InCombat);
		}
		combatTime = 0.0f;
		lastDamager = damager;
		if (bestUnit == null)
		{
			bestUnit = damager;
			Shoot(bestUnit);
		}
	}

	protected void CheckHealth()
	{
		if (health <= 0)
			OnDie();
	}

	protected void CheckStatus()
	{
		combatTime += Time.deltaTime;
		if (status == UnitStatus.CapturingObjective)
			return;
		if (status == UnitStatus.InCombat)
		{
			if (combatTime > COMBAT_EXIT_TIME)
			{
				LeaveCombat();
			}
			else
			{
				return;
			}
		}
		Vector3 pos = transform.position;
		float dist = Vector3.Distance(pos, lastFramePosition);
		if (dist == 0)
		{
			SetStatus(UnitStatus.Idle);
		}
		else
		{
			SetStatus(UnitStatus.Moving);
		}
	}

	public void LeaveCombat()
	{
		if (status != UnitStatus.InCombat)
			return;
		status = lastStatus;
	}

	public void SetStatus(UnitStatus uStatus)
	{
		if (uStatus == UnitStatus.InCombat)
		{
			if (movementType == MoveType.Strict || status == UnitStatus.InCombat)
				return;
			enterCombatPosition = transform.position;
			lastStatus = status;
			status = uStatus;
			return;
		}
		if (status == uStatus || status == UnitStatus.InCombat || status == UnitStatus.CapturingObjective)
			return;
		status = uStatus;
	}

	public UnitStatus GetStatus()
	{
		return status;
	}

	protected virtual void CreateID()
	{
		id = currentID;
		currentID++;
		if (uName == "")
		{
			uName = firstNames[Mathf.RoundToInt(Random.Range(0, firstNames.Length))] + " " + lastNames[Mathf.RoundToInt(Random.Range(0, lastNames.Length))];
			if (label != null)
				label.SetLabelText(uName);
		}
		gameObject.name = uName;
	}

	public bool IsSelectable()
	{
		return isSelectable;
	}

	public int GetID()
	{
		if (id == -1)
			CreateID();
		return id;
	}

	public virtual int GetTeamID()
	{
		if (leader == null)
			return -1;
		return leader.GetCommander().GetTeamID();
	}

	public virtual void RegisterLeader(Leader newLeader)
	{
		if (newLeader == null)
			return;
		leader = newLeader;
		if (!leader.IsOwnedByPlayer())
			Destroy(label);
		Deselect();
		ResetTarget();
		lastOrderer = null;
		aBase = leader.aBase;
		teamColor = leader.teamColor;
		leader.RegisterUnit(this);
		renderer.material.color = teamColor;
		SetOutlineColor(outlineColor);
		float distanceFromLeader = Vector3.Distance(transform.position, leader.transform.position);
		if (leader != (Leader)Commander.player && distanceFromLeader >= MOVE_TO_LEADER_DISTANCE)
		{
			OrderData supportLeader = new OrderData(leader, this);
			supportLeader.SetOrder(Order.move, false);
			supportLeader.SetMoveType(MoveType.DefendSelf);
			supportLeader.SetTarget(leader.transform);
			RecieveOrder(supportLeader);
		}
		else
		{
			leader.UnitRequestOrders(this);
		}
		if (IsOwnedByPlayer() && !IsLedByPlayer())
			MessageList.Instance.AddMessage(uName + ", acknowledging " + leader.name + " as my new leader.");
	}

	public void MoveTo(GameObject target, string targetName, bool parent, bool useRandom, MoveType moveType, string reason, bool debug)
	{
		if (!movable)
			return;
		timeSinceLastRepath = 0.0f;
		motor.MoveTo(target, targetName, parent, useRandom, moveType, reason, debug);
		CreateSelected();
	}

	public void MoveTo(Transform target, MoveType moveType, string reason, bool debug)
	{
		if (!movable || orderData == null)
			return;
		if (target == orderData.GetOrderTarget())
		{
			target = motor.MakeMoveTarget(target);
			orderData.SetMoveTarget(target, this);
		}
		timeSinceLastRepath = 0.0f;
		motor.MoveTo(target, moveType, reason, debug);
		CreateSelected();
	}

	public virtual void RecieveOrder(OrderData data)
	{
		if (!isSelectable || movable && (motor == null || !motor.enabled) || IsPlayer())
			return;
		Order order = data.GetOrder();
		if (data.MoveTypeWasChanged())
		{
			movementType = data.GetMoveType();
		}
		Transform target = data.GetOrderTarget();
		if ((order == Order.stop || target == null) && movable)
		{
			motor.StopNavigation(uName + " recieved a stop order from its OrderData.", false);
			return;
		}
		if (target == transform || orderData != null && order == orderData.GetOrder() && target == orderData.GetOrderTarget())
			return;
		//Debug.Log (this+" has recieved "+order);
		ResetTarget();
		orderData = data;
		lastOrderer = orderData.GetLeader();
		Objective objective = target.GetComponent<Objective>();
		if (objective != null)
		{
			currentObjective = objective;
			if (order == Order.attack)
				attackObjective = objective;
			else if (order == Order.defend)
				defendObjective = objective;
		}
		if (movable)
		{
			if (target.GetComponent<Unit>() == null)
			{
				target = motor.MakeMoveTarget(target);
			}
			else
			{
				target = motor.MakeMoveTarget(target.gameObject, uName + "'s Attack Target", true, false);
			}
			if (Vector3.Distance(target.position, transform.position) < UnitMotor.MOVE_CLOSE_ENOUGH_DISTANCE)
			{
				if (order != Order.defend)
				{
					orderData.SetTarget(null);
					orderData.SetOrder(Order.stop, true);
					orderData.UpdatedByUnit();
					ResetTarget();
					motor.OnTargetReached();
				}
				return;
			}
			orderData.SetMoveTarget(target, this);
			MoveTo(target, movementType, uName + " is moving due to order recieved by " + lastOrderer, false);
		}
		// This is a quick-and-dirty way for players to see that the unit has recieved orders correctly.
		if (lastOrderer == (Leader)Commander.player)
		{
			Debug.Log(this + " is moving to " + target);
			MessageList.Instance.AddMessage(uName + ", acknowledging " + order.ToString() + " order.");
		}
	}

	public MoveType GetMoveType()
	{
		return movementType;
	}

	public Order GetOrder()
	{
		if (status == UnitStatus.InCombat)
			return Order.attack;
		if (orderData == null)
			return Order.stop;
		return orderData.GetOrder();
	}

	public Leader GetLastOrderer()
	{
		return lastOrderer;
	}

	public Leader GetLeader()
	{
		return leader;
	}

	public Transform RequestTarget()
	{
		orderData = leader.UnitRequestOrders(this);
		if (orderData == null)
			return null;
		Transform moveTarget = orderData.GetOrderTarget();
		if (moveTarget == null)
		{
			if (!movable)
				return null;
			ResetTarget(true);
			return null;
		}
		orderData.SetMoveTarget(motor.MakeMoveTarget(moveTarget), this);
		return orderData.GetMoveTarget();
	}

	public Transform GetMoveTarget()
	{
		if (this == null || motor == null || orderData == null || !movable)
			return null;
		if (status == UnitStatus.InCombat && bestUnit != null)
			return bestUnit.transform;
		Transform moveTarget = orderData.GetMoveTarget();
		if (moveTarget == null)
		{
			moveTarget = orderData.GetOrderTarget();
			if (moveTarget == null)
				return null;
		}
		if (Vector3.Distance(moveTarget.position, transform.position) < UnitMotor.MOVE_CLOSE_ENOUGH_DISTANCE)
		{
			orderData.SetOrder(Order.stop, true);
			orderData.UpdatedByUnit();
			ResetTarget();
		}
		return moveTarget;
	}

	public bool Select(Leader selector)
	{
		if (!isSelectable || !IsAlive())
		{
			isSelected = false;
			return false;
		}
		isSelected = true;
		if (selector != (Leader)Commander.player)
			return true;
		CreateSelected();
		SetOutlineColor(selectedColor);
		return true;
	}

	public virtual void IsSeen(Leader seer, bool seen)
	{
		if (seen && seer.IsAlive())
		{
			if (!visibleBy.Contains(seer))
			{
				visibleBy.Add(seer);
				ChangeLayer(defaultLayer);
			}
		}
		else
		{
			if (visibleBy.Contains(seer))
				visibleBy.Remove(seer);
			if (visibleBy.Count == 0)
				ChangeLayer(unitLayer);
		}
	}

	public virtual void IsLookedAt(bool lookedAt)
	{
		if (label == null)
			return;
		if (MapView.IsShown())
		{
			label.isLookedAt = false;
			return;
		}
		label.isLookedAt = lookedAt;
		if (lookedAt)
			label.SetLabelText(GenerateLabel());
	}

	public virtual string GenerateLabel()
	{
		if (label == null)
			return "";
		if (uName != "")
			gameObject.name = uName;
		string labelS = uName + "\n";
		labelS = labelS + "Health: " + health + " / 100\n";
		labelS = labelS + GetClass();
		if (IsLedByPlayer())
		{
			if (uniqueGeo == null)
				label.ChangeOffset(label.offset - new Vector3(0, 0.35f, 0));
		}
		else
		{
			label.ChangeOffset(label.offset);
			labelS = labelS + "\nLeader: " + leader.name;
		}
		return labelS;
	}

	protected virtual string GetClass()
	{
		return "Grunt";
	}

	public GameObject GetLabel()
	{
		if (label == null)
			return null;
		label.SetLabelText(GenerateLabel());
		return label.GetText();
	}

	public void Deselect()
	{
		if (!isSelectable)
			return;
		isSelected = false;
		SetOutlineColor(outlineColor);
		CreateSelected();
		if (!IsLedByPlayer())
			return;
	}

	protected void OnDie()
	{
		if (!IsAlive())
			return;
		deathTime = Time.time;
		if (heatmapBlock != null)
			heatmapBlock.AddDeath();
		lastDamager.AddKill(this);
		if (leader != null)
			leader.RemoveUnit(id);
		if (currentObjective != null)
			currentObjective.RemovePlayer(this);
		if (capturingObjective != null)
			capturingObjective.RemovePlayer(this);
		IsLookedAt(false);
		if (leader != null)
			leader.RemoveUnit(id);
		if (weapon != null)
			weapon.Drop();
		if (movable && motor != null)
		{
			motor.StopNavigation(uName + " died.", false);
		}
		Deselect();
		ResetEffects();
		running = false;
		findingHealth = false;
		// Reset spawn point.
		Commander commander = GetCommander();
		if (commander != null)
		{
			if (IsLedByPlayer())
			{
				string deathMessage = uName;
				if (IsPlayer())
				{
					deathMessage = deathMessage + " were killed by " + lastDamager.name + ". " +
									"Respawning in " + Mathf.RoundToInt(GetCommander().GetTimeToRespawn()) + " seconds.";
					if (deathNoise != null)
						Camera.main.audio.PlayOneShot(deathNoise);
				}
				else
				{
					deathMessage = deathMessage + " was killed by " + lastDamager.name + ".";
					if (deathNoise != null)
						audio.PlayOneShot(deathNoise);
				}
				MessageList.Instance.AddMessage(deathMessage);
			}
			timeToOurRespawn = commander.GetTimeToRespawn();
			Debug.Log("Respawning in: " + timeToOurRespawn.ToString());
			Invoke("Spawn", timeToOurRespawn);
		}
		OnClassDie();
		foreach (Transform child in transform)
		{
			if (child.GetComponent<Objective>() != null)
			{
				child.parent = null;
				continue;
			}
			if (child.GetComponent<AudioListener>() != null)
			{
				child.parent = null;
				continue;
			}
			child.gameObject.SetActive(false);
		}
		gameObject.SetActive(false);
	}

	protected virtual void OnClassDie()
	{
		spawnPoint = Vector3.zero;
	}

	public static float GetCost(UnitType unitType)
	{
		if (unitType == UnitType.Unit)
		{
			return UNIT_COST;
		}
		else if (unitType == UnitType.Leader)
		{
			return LEADER_COST;
		}
		else if (unitType == UnitType.Commander)
		{
			return COMMANDER_COST;
		}
		return Mathf.Infinity;
	}

	public void OnTargetReached()
	{
		leader.OnUnitReachedTarget(this);
		SetStatus(UnitStatus.Idle);
	}

	public void OnCapturingObjective(Objective objective)
	{
		if (objective is Base)
			return;
		if (status != UnitStatus.InCombat)
		{
			lastStatus = status;
		}
		capturingObjective = objective;
		status = UnitStatus.CapturingObjective;
	}

	public void OnCapturedObjective()
	{
		capturingObjective = null;
		if (status == UnitStatus.CapturingObjective)
			status = lastStatus;
	}

	public void AddKill(Unit dead)
	{
		kills++;
		if (IsLedByPlayer())
			MessageList.Instance.AddMessage(uName + " killed " + dead.name + ".");
	}

	public bool IsAlive()
	{
		bool isAlive = this != null && gameObject != null && gameObject.activeInHierarchy;
		if (isAlive && weapon == null && health <= 0) // Useful for debugging; automatically spawns the GameObject if we re-enable it from the inspector.
		{
			Debug.LogWarning(this + " was re-enabled. Spawning.");
			Spawn();
		}
		return isAlive;
	}

	public float GetHealthPercent()
	{
		float _health = 0.00f;
		if (!IsAlive())
			return _health;
		_health = (float)health / (float)_maxHealth;
		return health;
	}

	public void ChangeLayer(LayerMask newLayer)
	{
		LayerMask gameObjectLayer = gameObject.layer;
		if (gameObjectLayer == leaderLayer || gameObjectLayer == raycastIgnoreLayers)
			return;
		gameObject.layer = newLayer;
		foreach (Transform child in transform)
		{
			if (child.gameObject.layer == 2 || child.gameObject.layer == raycastIgnoreLayers || child.gameObject.layer == leaderLayer)
				continue;
			child.gameObject.layer = newLayer;
		}
	}

	public void JustPromoted()
	{
		justPromoted = true;
	}

	public Leader UpgradeUnit(Commander commander)
	{
		Leader upgrade = gameObject.AddComponent<Leader>();
		upgrade.JustPromoted();
		upgrade.SetOutlineColor(outlineColor);
		upgrade.CloneUnit(this);
		upgrade.RegisterCommander(commander);
		upgrade.CreateSelected();
		if (IsOwnedByPlayer())
			upgrade.ChangeLayer(defaultLayer);
		if (IsOwnedByPlayer())
			MessageList.Instance.AddMessage(uName + ", acknowledging promotion to Leader.");
		Destroy(this); // This script will not be destroyed until the end of this frame.
		return upgrade;
	}

	public virtual Commander GetCommander()
	{
		if (leader == null) // Haven't set anything up yet.
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
		if (IsPlayer() || !IsAlive())
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
		if (bestUnit != null && !bestUnit.IsAlive())
		{
			bestUnit = null;
			LeaveCombat();
			force = true;
		}
		if (status == UnitStatus.Idle || movementType == MoveType.Loose || status == UnitStatus.InCombat && movementType == MoveType.DefendSelf)
			FindShootTargets();
		if (force || status != UnitStatus.InCombat && timeSinceLastRepath > AI_REPATH_TIME)
		{
			if (currentObjective != null)
				RepathToObjective();
		}
		timeSinceLastRepath += Time.deltaTime;
	}

	/// <summary>
	/// Handles the AI for this class only. Called before we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPreUniversal(bool force)
	{ }

	/// <summary>
	/// Handles the AI for this class only. Called after we have handled universal AI.
	/// </summary>
	protected virtual void HandleClassAIPostUniversal(bool force)
	{ }

	/// <summary>
	/// Makes the AI find and shoot at a specific target.
	/// </summary>
	protected void FindShootTargets()
	{
		float chaseRange = MAX_ENEMY_CHASE_RANGE;
		if (bestUnit != null)
		{
			if (bestUnit.HasObjective())
			{
				chaseRange *= 1.5f;
			}
		}
		bestUnit = null;
		if (status == UnitStatus.InCombat && Vector3.Distance(transform.position, enterCombatPosition) > chaseRange)
		{
			LeaveCombat();
			if (movable)
			{
				if (orderData == null)
				{
					motor.MoveTo(enterCombatPosition, uName + "'s Combat Return Target", MoveType.DefendSelf, "Returning to position where we entered combat.", false);
				}
				else
				{
					Transform moveTarget = orderData.GetMoveTarget();
					if (moveTarget == null)
					{
						Transform orderTarget = orderData.GetOrderTarget();
						if (orderTarget == null)
						{
							motor.MoveTo(enterCombatPosition, uName + "'s Combat Return Target", MoveType.DefendSelf, "Returning to position where we entered combat.", false);
							return;
						}
						else
						{
							moveTarget = motor.MakeMoveTarget(orderTarget);
							orderData.SetMoveTarget(moveTarget, this);
						}
					}
					motor.MoveTo(moveTarget, MoveType.DefendSelf, "Returning to position where we entered combat.", false);
				}
			}
			return;
		}
		DetectEnemies(enemyName);
		if (bestUnit == null)
		{
			if (status == UnitStatus.InCombat)
			{
				LeaveCombat();
			}
			return;
		}
		Shoot(bestUnit);
	}

	/// <summary>
	/// Forces a repath on the next frame, so long as our objective is not null.
	/// </summary>
	public void ForceRepath()
	{
		if (currentObjective == null)
		{
			Debug.LogWarning("Can't force a repath next frame: our current objective is null!");
			return;
		}
		timeSinceLastRepath = AI_REPATH_TIME + 1;
	}

	protected void RepathToObjective()
	{
		if (!movable || currentObjective == null)
			return;
		ResetTarget();
		MoveTo(motor.MakeMoveTarget(currentObjective.transform), movementType, uName + " is responding to a repath call.", false);
	}

	public bool HasObjective()
	{
		return capturingObjective != null;
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
	public Unit DetectEnemies(string enemyAspect)
	{
		if (bestUnit != null && bestUnit.IsAlive() && Vector3.Distance(bestUnit.transform.position, transform.position) <= weapon.range)
			return bestUnit;
		lastDetectTime = Time.time;
		Unit[] units = DetectUnits(enemyAspect);
		if (units.Length == 0)
		{
			bestUnit = null;
			return null;
		}
		bestUnit = null;
		// Assign a score to each enemy:
		float score = Mathf.Infinity;
		foreach (Unit unit in units)
		{
			if (unit == null || !unit.IsAlive())
				continue;
			float uScore = Vector3.Distance(unit.transform.position, transform.position);
			uScore *= unit.GetHealthPercent();
			if (bestUnit == null || uScore < score)
			{
				bestUnit = unit;
				score = uScore;
			}
		}
		if (!bestUnit.IsAlive())
			bestUnit = null;
		return bestUnit;
	}

	public Unit[] DetectUnits(string unitTag)
	{
		if (weapon == null)
			return new Unit[0];
		float maxVerticalFOV = AI_FOV_VERTICAL_RANGE / 2;
		float currentVerticalFOV = -maxVerticalFOV - raycastIncrementRate;
		Vector3 position = transform.position;
		float sightRange = weapon.range + 15.0f;
		Vector3 fovDirection = transform.forward;
		RaycastHit hitInfo;
		List<Unit> detectedUnits = new List<Unit>();
		for (; currentVerticalFOV <= maxVerticalFOV; currentVerticalFOV += raycastIncrementRate)
		{
			float maxFOV = AI_FOV_HORIZONTAL_RANGE / 2;
			float currentFOV = -maxFOV;
			for (; currentFOV <= maxFOV; currentFOV += raycastIncrementRate)
			{
				fovDirection = Quaternion.Euler(currentVerticalFOV, currentFOV, 0) * transform.forward;
				if (Physics.Raycast(position, fovDirection, out hitInfo, sightRange, raycastIgnoreLayers))
				{
					Unit unit = hitInfo.transform.GetComponent<Unit>();
					if (unit == null || detectedUnits.Contains(unit) || unit.tag != unitTag || !unit.IsAlive())
						continue;
					detectedUnits.Add(unit);
				}
				Debug.DrawRay(position, fovDirection, Color.magenta);
			}
		}
		for (float closeDetectAmount = 0; closeDetectAmount < 360; closeDetectAmount += raycastIncrementRate)
		{
			fovDirection = Quaternion.Euler(0, closeDetectAmount, 0) * transform.forward;
			if (Physics.Raycast(position, fovDirection, out hitInfo, CLOSE_ENEMY_DETECT_RANGE, raycastIgnoreLayers))
			{
				Unit unit = hitInfo.transform.GetComponent<Unit>();
				if (unit == null || detectedUnits.Contains(unit) || unit.tag != unitTag || !unit.IsAlive())
					continue;
				detectedUnits.Add(unit);
			}
			Debug.DrawRay(position, fovDirection, Color.cyan);
		}
		return detectedUnits.ToArray();
	}

	public Unit[] DetectUnits(string unitTag, float maxDistance)
	{
		List<Unit> detectedUnits = new List<Unit>();
		Vector3 position = transform.position;
		float maxY = AI_FOV_VERTICAL_RANGE / 2;
		for (float currentYAngle = -maxY; currentYAngle <= maxY; currentYAngle += raycastIncrementRate)
		{
			for (float i = 0; i < 360; i += raycastIncrementRate)
			{
				Vector3 fovDirection = Quaternion.Euler(currentYAngle, i, 0) * transform.forward;
				RaycastHit hitInfo;
				if (Physics.Raycast(position, fovDirection, out hitInfo, maxDistance, raycastIgnoreLayers))
				{
					Unit unit = hitInfo.transform.GetComponent<Unit>();
					if (unit == null || detectedUnits.Contains(unit) || unitTag != "" && unit.tag != unitTag || !unit.IsAlive())
						continue;
					detectedUnits.Add(unit);
				}
				Debug.DrawRay(position, fovDirection, Color.yellow);
			}
		}
		return detectedUnits.ToArray();
	}

	public virtual void LookAt(Vector3 target)
	{
		if (motor != null)
		{
			motor.LookAt(target);
		}
		else
		{
			Debug.LogWarning("TODO!");
		}
	}

	public bool Shoot(Unit enemy)
	{
		if (enemy == null || !enemy.IsAlive() || weapon == null || IsPlayer())
			return false;
		if (weapon.ammo <= 0)
			return false;
		float range = weapon.range;
		if (range < Vector3.Distance(enemy.transform.position, transform.position))
			return false;
		LookAt(enemy.transform.position);
		Vector3 weaponForward = weapon.transform.up;
		Ray shotRay = new Ray(weapon.transform.position, weaponForward);
		Debug.DrawRay(weapon.transform.position, weaponForward, Color.red);
		RaycastHit hitInfo;
		if (Physics.Raycast(shotRay, out hitInfo, range, raycastIgnoreLayers))
		{
			if (hitInfo.transform.tag == "Ground")
			{
				return false;
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
		if (status != UnitStatus.InCombat && status != UnitStatus.CapturingObjective)
		{
			SetStatus(UnitStatus.InCombat);
		}
		combatTime = 0.0f;
		weapon.Shoot();
		float accuracy = weapon.GetAccuracy();
		if (accuracy > 0.65f || movementType == MoveType.Strict)
		{
			return true;
		}
		else
		{
			MoveTo(enemy.gameObject, uName + "'s Attack Target", true, false, movementType, uName + " is moving to shoot at " + enemy, false);
			/*if(agent.MoveTo(enemy.transform.position,deltaTime))
			{
				return RAIN.Action.Action.ActionResult.SUCCESS;
			}*/
		}
		return true;
	}

	public Transform FindHealth()
	{
		float dist = Mathf.Infinity;
		Transform regen = null;
		foreach (GameObject go in regenZones)
		{
			Healthpack healthPack = go.GetComponent<Healthpack>();
			if (healthPack != null)
			{
				if (healthPack.IsDisabled())
					continue;
			}
			float gDist = Vector3.Distance(transform.position, go.transform.position);
			if (gDist < dist)
			{
				regen = go.transform;
				dist = gDist;
			}
		}
		float bDist = Vector3.Distance(transform.position, aBase.transform.position);
		if (bDist < dist)
		{
			regen = aBase.transform;
			dist = bDist;
		}
		if (dist < MAX_HEALTH_DISTANCE)
		{
			if (IsLedByPlayer() && !findingHealth && (regen != aBase.transform || dist > MAX_ENEMY_CHASE_RANGE))
			{
				Invoke("CreateHealthPackMessage", 1.0f);
			}
			findingHealth = true;
			MoveTo(regen.gameObject, uName + "'s Regen Target", true, false, MoveType.DefendSelf, uName + " is trying to restore its health.", false);
		}
		return regen;
	}

	public void CreateHealthPackMessage()
	{
		if (IsAlive())
			MessageList.Instance.AddMessage(uName + " is low on health (" + health + " HP). Moving to healthpack.");
	}

	public bool RestoreHealth(int amount)
	{
		if (!IsAlive() || health >= 100)
			return false;
		health += amount;
		health = Mathf.Min(health, 100);
		if (healthRegen != null && amount > 30)
			gameObject.GetComponentInChildren<AudioSource>().PlayOneShot(healthRegen);
		if (movable && orderData != null)
		{
			Order order = orderData.GetOrder();
			if (order == Order.stop)
			{
				motor.MoveTo(orderData.GetOrderLocation(), uName + "'s Health Return Target", MoveType.DefendSelf, "Returning to location we recieved an order", false);
				return true;
			}
			Transform moveTarget = orderData.GetMoveTarget();
			if (moveTarget == null)
			{
				Transform orderTarget = orderData.GetOrderTarget();
				if (orderTarget == null)
				{
					motor.MoveTo(orderData.GetOrderLocation(), uName + "'s Health Return Target", MoveType.DefendSelf, "Returning to location we recieved an order", false);
					return true;
				}
				else
				{
					moveTarget = motor.MakeMoveTarget(orderTarget);
				}
			}
			MoveTo(moveTarget, movementType, uName + " is returning from restoring its health.", false);
		}
		return true;
	}

	public void Score()
	{
		captures++;
		GetCommander().OnScore();
		OnCapturedObjective();
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
		if (commander == null)
			return false;
		return commander.IsPlayer();
	}

	public virtual bool IsPlayer()
	{
		return false;
	}

	public float GetTimeUntilRespawn()
	{
		return timeToOurRespawn;
	}

	public UnitType GetUnitType()
	{
		return unitType;
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
		if (movable && motor != null)
			motor.StopNavigation(uName + " reset its target.", false);
		//Debug.Log ("Resetting target.");
		orderData = null;
		if (effects)
			ResetEffects();
	}

	protected void ResetEffects()
	{
		if (moveEffect != null)
		{
			Destroy(moveEffect);
			moveEffect = null;
		}
		if (!isSelected && selectEffect != null)
		{
			Destroy(selectEffect);
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
		status = oldClone.status;
		movementType = oldClone.movementType;
		kills = oldClone.kills;
		lastDamager = oldClone.lastDamager;
		health = oldClone.health;
		weapon = oldClone.weapon;
		_initialWeapon = oldClone._initialWeapon;
		aBase = oldClone.aBase;
		raycastIgnoreLayers = Commander.player.raycastIgnoreLayers;
		weapon.Pickup(this);
		leader.ReplaceUnit(id, this);
		if (isSelected)
			SetOutlineColor(selectedColor);
		else
			SetOutlineColor(outlineColor);
		orderData = oldClone.orderData;
		if (orderData != null)
			orderData.SetUnit(this);
		skipSpawn = true;
		SetTeamColor();
		Invoke("AllowSpawn", 5.0f);
	}

	public void EnterHeatmapBlock(HeatmapBlock heatBlock)
	{
		heatmapBlock = heatBlock;
	}

	public void ExitHeatmapBlock(HeatmapBlock heatBlock)
	{
		if (heatmapBlock == heatBlock)
			heatmapBlock = null;
	}
}
