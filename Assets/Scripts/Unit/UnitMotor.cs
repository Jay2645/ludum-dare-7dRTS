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
public class UnitMotor : MonoBehaviour {
	
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
	
	
	void Start()
	{
		unit = gameObject.GetComponent<Unit>();
		navigator = gameObject.GetComponent<NavMeshAgent>();
		// We always need to be attached to a unit and have a way of moving.
		if(unit == null || navigator == null)
			Destroy (this);
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		RecalculatePaths();
	}
	
	protected void RecalculatePaths()
	{
		if(navigator == null || !navigator.enabled)
			return;
		if(moveTarget != null)
		{
			if(navigator.hasPath && Vector3.Distance(navigator.pathEndPosition,transform.position) <= MOVE_CLOSE_ENOUGH_DISTANCE)
			{
				StopNavigation();
				return;
			}
			lastPathRecheckTime += Time.fixedDeltaTime;
			if(lastPathRecheckTime > MIN_PATH_RECALC_TIME)
			{
				lastPathRecheckTime = 0.0f;
				navigator.SetDestination(moveTarget.position);
			}
		}
		else if(oldMoveTarget != null)
		{
			moveTarget = oldMoveTarget;
			//Debug.Log (unit+" is returning to old target: "+oldMoveTarget);
			oldMoveTarget = null;
			MoveTo(moveTarget,moveType);
		}
		else if(tempGameObjects.Count > 0)
		{
			//Debug.Log (unit+" is searching temporary game objects for an appropriate target.");
			foreach(GameObject go in tempGameObjects.ToArray())
			{
				if(go == null)
				{
					tempGameObjects.Remove(go);
					continue;
				}
				moveTarget = go.transform;
				MoveTo(moveTarget,moveType);
				break;
			}
		}
	}
	
	public void MoveTo(GameObject target, MoveType movementType)
	{
		MoveTo(target,gameObject.name+"'s Move Target",movementType);
	}
	
	public void MoveTo(GameObject target, string targetName, MoveType movementType)
	{
		MoveTo (target,targetName,false,movementType);
	}
	
	public void MoveTo(GameObject target, string targetName, bool useRandom, MoveType movementType)
	{
		MoveTo(target, targetName, false, useRandom, movementType);
	}
	
	public void MoveTo(GameObject target, string targetName, bool parent, bool useRandom, MoveType movementType)
	{
		foreach(GameObject go in tempGameObjects.ToArray())
		{
			if(go == null)
			{
				tempGameObjects.Remove(go);
				continue;
			}
			if(go.name.Contains(targetName))
			{
				if(useRandom)
				{
					float dist = Vector3.Distance(go.transform.position,target.transform.position);
					if(dist <= Mathf.Pow(RANDOM_TARGET_VARIATION, 3))
					{
						MoveTo(go.transform,movementType);
						return;
					}
				}
				else
				{
					if(go.transform.position == target.transform.position)
					{
						MoveTo(go.transform,movementType);
						return;
					}
				}
				Destroy(go);
				break;
			}
		}
		GameObject targetGO;
		if(useRandom)
		{
			targetGO = MakeMoveTarget(target.transform).gameObject;
			targetGO.name = targetName;
		}
		else
		{
			targetGO = new GameObject(targetName);
			targetGO.transform.position = target.transform.position;
		}
		if(parent)
		{
			targetGO.transform.parent = target.transform;
		}
		tempGameObjects.Add(targetGO);
		MoveTo (targetGO.transform,movementType);
	}
	
	public void MoveTo(Transform target, MoveType movementType)
	{
		if(navigator == null || !navigator.enabled)
			return;
		if(target == transform || target == null)
		{
			StopNavigation();
			unit.SetStatus(UnitStatus.Idle);
			return;
		}
		moveType = movementType;
		if(moveType == MoveType.Strict && oldMoveTarget != null)
		{
			tempGameObjects.Remove(oldMoveTarget.gameObject);
			Destroy(oldMoveTarget.gameObject);
		}
		if(moveTarget == null || moveType == MoveType.Strict || moveType == MoveType.DefendSelf && unit.GetStatus() != UnitStatus.InCombat)
		{
			moveTarget = target;
		}
		else
		{
			if(oldMoveTarget == null)
			{
				//Debug.Log (unit+" is storing "+moveTarget+" as our old move target.");
				oldMoveTarget = moveTarget;
			}
			moveTarget = target;
		}
		//Debug.Log (unit+" is pathfinding to "+moveTarget+" at "+moveTarget.position);
		unit.SetStatus(UnitStatus.Moving);
		navigator.SetDestination(moveTarget.position);
	}
	
	public Transform MakeMoveTarget(Transform target)
	{
		foreach(GameObject go in tempGameObjects.ToArray())
		{
			if(go == null)
			{
				tempGameObjects.Remove(go);
				continue;
			}
			if(go.name.Contains("'s Current Target"))
			{
				Destroy(go);
				break;
			}
		}
		GameObject targetGO = new GameObject(gameObject.name+"'s Current Target");
		tempGameObjects.Add(targetGO);
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-RANDOM_TARGET_VARIATION,RANDOM_TARGET_VARIATION);
		targetLocation.z += Random.Range(-RANDOM_TARGET_VARIATION,RANDOM_TARGET_VARIATION);
		Transform oldTarget = target;
		target = targetGO.transform;
		target.position = targetLocation;
		target.parent = oldTarget;
		if(unit.IsOwnedByPlayer())
			Debug.Log ("Making target.");
		return target;
	}
	
	public Transform GetTarget()
	{
		RecalculatePaths();
		if(oldMoveTarget != null)
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
	
	public void StopNavigation()
	{
		//Debug.Log ("Stopping navigation.");
		if(moveTarget != null)
		{
			if(tempGameObjects.Contains(moveTarget.gameObject))
				tempGameObjects.Remove(moveTarget.gameObject);
			Destroy(moveTarget.gameObject);
		}
		if(navigator == null)
		{
			navigator = gameObject.GetComponent<NavMeshAgent>();
			if(navigator == null)
			{
				Destroy (this);
			}
		}
		if(navigator.hasPath)
			navigator.Stop();
	}
}
