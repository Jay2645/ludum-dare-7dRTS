using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How should we behave while moving?
/// Strict means that we will ignore everything while going to our target.
/// DefendSelf means we will only fight back after we've been shot at.
/// Loose means we will attack any enemy we see on sight.
/// </summary>
public enum MoveType
{
	Strict,
	DefendSelf,
	Loose
}

[RequireComponent(typeof(NavMeshAgent))]
public class UnitMotor : MonoBehaviour
{

	/// <summary>
	/// Unity's navigation system.
	/// </summary>
	private NavMeshAgent navigator;
	/// <summary>
	/// The unit this is attached to.
	/// </summary>
	private Unit unit;
	/// <summary>
	/// All temporary GameObjects we have created.
	/// </summary>
	protected List<GameObject> tempGameObjects = new List<GameObject>();
	/// <summary>
	/// The Transform we are currently moving to.
	/// </summary>
	private Transform moveTarget = null;
	/// <summary>
	/// The name of our target.
	/// </summary>
	private string targetName = "";
	/// <summary>
	/// Our current movement type.
	/// </summary>
	private MoveType moveType = MoveType.Loose;
	/// <summary>
	/// If we're diverging from our current orders, this is our old move target.
	/// </summary>
	protected Transform oldMoveTarget = null;
	/// <summary>
	/// If we're diverging from our current orders, this is our old order.
	/// </summary>
	protected Order oldOrder = Order.stop;
	/// <summary>
	/// The last time we rechecked our path.
	/// </summary>
	private float lastPathRecheckTime = 0.0f;

	/// <summary>
	/// How often do we recalculate our path to compensate for movement in our target?
	/// </summary>
	private const float MIN_PATH_RECALC_TIME = 3.0f;
	/// <summary>
	/// The distance our move target can be away from where we were ordered to go.
	/// </summary>
	protected const float RANDOM_TARGET_VARIATION = 2.0f;
	/// <summary>
	/// How far we have to be from our goal to be "close enough".
	/// </summary>
	public const float MOVE_CLOSE_ENOUGH_DISTANCE = 2.0f;
	/// <summary>
	/// How fast we look at something.
	/// </summary>
	protected const float LOOK_AT_SPEED = 4.0f;
	/// <summary>
	/// The reason why we're moving.
	/// </summary>
	public string moveReason = "";

	private int frameCount = 0;
	private bool running = false;


	void Start()
	{
		unit = gameObject.GetComponent<Unit>();
		navigator = gameObject.GetComponent<NavMeshAgent>();
		// We always need to be attached to a unit and have a way of moving.
		if (unit == null || navigator == null)
		{
			Destroy(this);
		}
		navigator.enabled = false;
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		if (PauseMenu.IsPaused())
			return;
		RecalculatePaths();
		if (running)
			return;
		frameCount++;
		if (frameCount > 2)
		{
			running = true;
			navigator.enabled = true;
		}
	}

	protected void RecalculatePaths()
	{
		if (navigator == null || !navigator.enabled)
			return;
		if (moveTarget != null && !moveTarget.gameObject.activeInHierarchy)
		{
			if (tempGameObjects.Contains(moveTarget.gameObject))
			{
				tempGameObjects.Remove(moveTarget.gameObject);
				Destroy(moveTarget.gameObject);
			}
		}
		if (oldMoveTarget != null && !oldMoveTarget.gameObject.activeInHierarchy)
		{
			if (tempGameObjects.Contains(oldMoveTarget.gameObject))
			{
				tempGameObjects.Remove(oldMoveTarget.gameObject);
				Destroy(oldMoveTarget.gameObject);
			}
		}
		if (moveTarget != null)
		{
			if (navigator.hasPath && Vector3.Distance(navigator.pathEndPosition, transform.position) <= MOVE_CLOSE_ENOUGH_DISTANCE)
			{
				OnTargetReached();
				return;
			}
			lastPathRecheckTime += Time.fixedDeltaTime;
			if (lastPathRecheckTime > MIN_PATH_RECALC_TIME)
			{
				lastPathRecheckTime = 0.0f;
				navigator.SetDestination(moveTarget.position);
			}
		}
		else if (oldMoveTarget != null)
		{
			moveTarget = oldMoveTarget;
			//Debug.Log (unit+" is returning to old target: "+oldMoveTarget);
			oldMoveTarget = null;
			MoveTo(moveTarget, moveType, moveReason, false);
		}
		else if (tempGameObjects.Count > 0)
		{
			//Debug.Log (unit+" is searching temporary game objects for an appropriate target.");
			foreach (GameObject go in tempGameObjects.ToArray())
			{
				if (go == null)
				{
					tempGameObjects.Remove(go);
					continue;
				}
				moveTarget = go.transform;
				MoveTo(moveTarget, moveType, moveReason, false);
				break;
			}
		}
		else
		{
			moveTarget = unit.GetMoveTarget();
			if (moveTarget == null)
			{
				moveTarget = unit.RequestTarget();
				if (moveTarget == null)
				{
					moveReason = "We can't find anywhere to move to.";
					return;
					/*Leader leader = unit.GetLeader();
					if(leader == null)
					{
						leader = (Leader)unit.GetCommander();
						if(leader == null)
						{
							moveReason = "We can't find anywhere to move to.";
							return;
						}
					}
					moveTarget = MakeMoveTarget(leader.gameObject,unit.name+"'s Leader Target",true,true);*/
				}
			}
			MoveTo(moveTarget, moveType, moveReason, false);
		}
	}

	public void MoveTo(Vector3 position, string targetName, MoveType movementType, string reason, bool debug)
	{
		foreach (GameObject go in tempGameObjects.ToArray())
		{
			if (go == null)
			{
				tempGameObjects.Remove(go);
				continue;
			}
			if (go.name.Contains(targetName))
			{
				tempGameObjects.Remove(go);
				Destroy(go);
				break;
			}
		}
		GameObject targetGO = new GameObject(targetName);
		targetGO.transform.position = position;
		tempGameObjects.Add(targetGO);
		MoveTo(targetGO.transform, movementType, reason, debug);
	}

	public void MoveTo(GameObject target, MoveType movementType, string reason, bool debug)
	{
		MoveTo(target, gameObject.name + "'s Move Target", movementType, reason, debug);
	}

	public void MoveTo(GameObject target, string targetName, MoveType movementType, string reason, bool debug)
	{
		MoveTo(target, targetName, false, movementType, reason, debug);
	}

	public void MoveTo(GameObject target, string targetName, bool useRandom, MoveType movementType, string reason, bool debug)
	{
		MoveTo(target, targetName, false, useRandom, movementType, reason, debug);
	}

	public void MoveTo(GameObject target, string targetName, bool parent, bool useRandom, MoveType movementType, string reason, bool debug)
	{
		Transform targetTfm = MakeMoveTarget(target, targetName, parent, useRandom);
		MoveTo(targetTfm, movementType, reason, debug);
	}

	public void MoveTo(Transform target, MoveType movementType, string reason, bool debug)
	{
		if (navigator == null || !navigator.enabled)
			return;
		if (target == transform || target == null)
		{
			StopNavigation("We were told to move to ourselves or were otherwise not given a target.", false);
			unit.SetStatus(UnitStatus.Idle);
			return;
		}
		if (target.GetComponent<Objective>() != null || target.GetComponent<Unit>() != null)
		{
			MoveTo(target.gameObject, movementType, reason, debug);
			return;
		}
		moveType = movementType;
		if (moveType == MoveType.Strict && oldMoveTarget != null)
		{
			tempGameObjects.Remove(oldMoveTarget.gameObject);
			Destroy(oldMoveTarget.gameObject);
		}
		if (moveTarget == null || moveType == MoveType.Strict || moveType == MoveType.DefendSelf && unit.GetStatus() != UnitStatus.InCombat)
		{
			moveTarget = target;
		}
		else
		{
			SetTemporaryTarget(target);
		}
		//Debug.Log (unit+" is pathfinding to "+moveTarget+" at "+moveTarget.position);
		moveReason = reason;
		if (debug)
			Debug.Log(unit + " is moving to " + moveTarget + ".\nReason: " + reason);
		unit.SetStatus(UnitStatus.Moving);
		navigator.SetDestination(moveTarget.position);
	}

	private void SetTemporaryTarget(Transform target)
	{
		if (oldMoveTarget == null)
		{
			//Debug.Log (unit+" is storing "+moveTarget+" as our old move target.");
			oldMoveTarget = moveTarget;
		}
		moveTarget = target;
	}

	public Transform MakeMoveTarget(GameObject target, string targetName, bool parent, bool useRandom)
	{
		foreach (GameObject go in tempGameObjects.ToArray())
		{
			if (go == null)
			{
				tempGameObjects.Remove(go);
				continue;
			}
			if (go.name.Contains(targetName))
			{
				if (useRandom)
				{
					float dist = Vector3.Distance(go.transform.position, target.transform.position);
					if (dist <= Mathf.Pow(RANDOM_TARGET_VARIATION, 3))
					{
						return go.transform;
					}
				}
				else
				{
					if (go.transform.position == target.transform.position)
					{
						return go.transform;
					}
				}
				Destroy(go);
				break;
			}
		}
		GameObject targetGO;
		if (useRandom)
		{
			targetGO = MakeMoveTarget(target.transform).gameObject;
			targetGO.name = targetName;
		}
		else
		{
			targetGO = new GameObject(targetName);
			targetGO.transform.position = target.transform.position;
		}
		if (parent)
		{
			targetGO.transform.parent = target.transform;
		}
		tempGameObjects.Add(targetGO);
		return targetGO.transform;
	}

	public Transform MakeMoveTarget(Transform target)
	{
		if (target == null)
			return null;
		foreach (GameObject go in tempGameObjects.ToArray())
		{
			if (go == null)
			{
				tempGameObjects.Remove(go);
				continue;
			}
			if (go.name.Contains("'s Current Target"))
			{
				Destroy(go);
				break;
			}
		}
		GameObject targetGO = new GameObject(gameObject.name + "'s Current Target");
		tempGameObjects.Add(targetGO);
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-RANDOM_TARGET_VARIATION, RANDOM_TARGET_VARIATION);
		targetLocation.z += Random.Range(-RANDOM_TARGET_VARIATION, RANDOM_TARGET_VARIATION);
		Transform oldTarget = target;
		target = targetGO.transform;
		target.position = targetLocation;
		target.parent = oldTarget;
		return target;
	}

	public void LookAt(Vector3 position)
	{
		Quaternion rot = Quaternion.LookRotation(position - transform.position);
		transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * LOOK_AT_SPEED);
	}

	public Transform GetTarget()
	{
		RecalculatePaths();
		if (oldMoveTarget != null)
			return oldMoveTarget;
		return moveTarget;
	}

	public void UpdateUnit(Unit newUnit)
	{
		unit = newUnit;
	}

	public void UpdateMoveType(MoveType type)
	{
		moveType = type;
	}

	public void OnTargetReached()
	{
		StopNavigation(unit + " has successfully reached its target.", false);
		unit.OnTargetReached();
	}

	public void StopNavigation(string reason, bool debug)
	{
		if (moveTarget != null)
		{
			if (tempGameObjects.Contains(moveTarget.gameObject))
				tempGameObjects.Remove(moveTarget.gameObject);
			if (moveTarget.name.Contains(" Target"))
				Destroy(moveTarget.gameObject);
		}
		if (oldMoveTarget != null)
		{
			if (tempGameObjects.Contains(oldMoveTarget.gameObject))
				tempGameObjects.Remove(oldMoveTarget.gameObject);
			if (oldMoveTarget.name.Contains(" Target"))
				Destroy(oldMoveTarget.gameObject);
			oldMoveTarget = null;
		}
		foreach (GameObject go in tempGameObjects.ToArray())
		{
			if (go == null)
				continue;
			if (go.name.Contains(" Target"))
				Destroy(go);
		}
		tempGameObjects.Clear();
		if (navigator == null)
		{
			navigator = gameObject.GetComponent<NavMeshAgent>();
			if (navigator == null)
			{
				Destroy(this);
			}
		}
		if (debug)
			Debug.Log("Stopping navigation. Reason: " + reason);
		if (navigator.hasPath)
		{
			moveReason = reason;
			navigator.Stop();
		}
	}
}
