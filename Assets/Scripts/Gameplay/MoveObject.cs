using UnityEngine;

public class MoveObject : MonoBehaviour
{

	public Waypoint[] waypoints;
	protected int currentWaypoint = 0; //What waypoint are going toward
	protected float timeAtLastWaypoint; //When did we reach the last waypoint
	protected bool stopWaypointing = false;
	protected bool startAtCurrentPosition;
	protected Vector3 currentFauxWaypointPos;
	protected Quaternion currentFauxWaypointRot;
	public float speedMultiplier = 1.0f;
	public bool teleportToBeginningAndLoop = false;
	public bool reverse = false;
	public bool reverseAtEnd = false;
	public bool useConstantTime = false;
	public float constantTime = 0.0f;

	void Start()
	{
		if (waypoints.Length > 0)
		{
			if (reverse)
				ReverseWaypoints();
			//Teleport the camera to the first position
			gameObject.transform.position = waypoints[currentWaypoint].transform.position;
			gameObject.transform.rotation = waypoints[currentWaypoint].transform.rotation;

			currentWaypoint++;
			timeAtLastWaypoint = Time.time;
			waypoints[waypoints.Length - 1].terminus = true;
		}
	}

	// Update is called once per frame
	void Update()
	{
		if ((waypoints.Length > currentWaypoint || startAtCurrentPosition) && !stopWaypointing)
		{
			Waypoint movingTo = waypoints[currentWaypoint];
			float moveTime;
			if (useConstantTime)
			{
				moveTime = constantTime;
			}
			else
			{
				moveTime = movingTo.time / speedMultiplier;
			}
			if (moveTime == 0)
				moveTime = 0.01f;
			float lerpTime = (Time.time - timeAtLastWaypoint) / moveTime;
			lerpTime = Mathf.Clamp01(lerpTime); //Clamp lerpTime between 0 and 1;

			if (startAtCurrentPosition)
			{
				Transform to = movingTo.transform;
				gameObject.transform.position = Vector3.Lerp(currentFauxWaypointPos, to.position, lerpTime);
				gameObject.transform.rotation = Quaternion.Lerp(currentFauxWaypointRot, to.rotation, lerpTime);
			}
			else
			{
				Transform from = waypoints[currentWaypoint - 1].transform;
				Transform to = movingTo.transform;

				gameObject.transform.position = Vector3.Lerp(from.position, to.position, lerpTime);
				gameObject.transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, lerpTime);
			}
			if (lerpTime >= 1 || Vector3.Distance(movingTo.transform.position, transform.position) <= 0.5)
			{
				// Waypoint has been reached.
				movingTo.OnPass();
				if (startAtCurrentPosition)
					startAtCurrentPosition = false;
				else
				{
					currentWaypoint++; //THEN continue to the next
				}
				if (movingTo.terminus)
				{
					if (reverseAtEnd)
					{
						reverse = !reverse;
						ReverseWaypoints();
						currentWaypoint = 1;
					}
					else if (teleportToBeginningAndLoop)
					{
						currentWaypoint = 1;
						gameObject.transform.position = waypoints[0].transform.position;
						gameObject.transform.rotation = waypoints[0].transform.rotation;
						for (int i = 1; i < waypoints.Length; i++)
						{
							waypoints[i].passed = false;
						}
					}
					else
					{
						stopWaypointing = true;
					}
				}
				timeAtLastWaypoint = Time.time;
			}
		}
	}

	public Waypoint CurrentWaypoint()
	{
		if (currentWaypoint >= waypoints.Length)
			return waypoints[waypoints.Length - 1];
		return waypoints[currentWaypoint];
	}

	public void SetWaypoints(Waypoint[] points)
	{
		SetWaypoints(points, false);
	}

	public void SetWaypoints(Waypoint[] points, bool startFromCurrentPos)
	{
		stopWaypointing = false;
		timeAtLastWaypoint = Time.time;
		startAtCurrentPosition = startFromCurrentPos;
		if (startAtCurrentPosition)
		{
			currentFauxWaypointPos = gameObject.transform.position;
			currentFauxWaypointRot = gameObject.transform.rotation;
			currentWaypoint = 0;
		}
		else
			currentWaypoint = 1;
		points[points.Length - 1].terminus = true;
		waypoints = points;
	}

	public void TeleportToEnd()
	{
		currentWaypoint = waypoints.Length;
		int waypointNum = currentWaypoint - 1;
		gameObject.transform.position = waypoints[waypointNum].transform.position;
		gameObject.transform.rotation = waypoints[waypointNum].transform.rotation;
		gameObject.transform.parent = waypoints[waypointNum].transform;
		stopWaypointing = true;
		Debug.Log("Teleported " + gameObject.name + " to the end of its path.");
	}

	public void ContinueWaypointing(bool teleportToNext)
	{
		if (waypoints.Length <= currentWaypoint)
		{
			Debug.LogWarning("There are no waypoints to continue to.");
			return;
		}
		stopWaypointing = false;
		if (teleportToNext)
		{
			gameObject.transform.position = waypoints[currentWaypoint].transform.position;
			gameObject.transform.rotation = waypoints[currentWaypoint].transform.rotation;
		}
	}

	public void ReverseWaypoints()
	{
		int waypointCount = waypoints.Length;
		Waypoint[] reverseWaypoints = new Waypoint[waypointCount];
		waypointCount--;
		for (int i = 0; i < waypoints.Length; i++)
		{
			reverseWaypoints[i] = waypoints[waypointCount];
			reverseWaypoints[i].terminus = false;
			reverseWaypoints[i].passed = false;
			waypointCount--;
		}
		reverseWaypoints[waypoints.Length - 1].terminus = true;
		waypoints = reverseWaypoints;
	}
}
