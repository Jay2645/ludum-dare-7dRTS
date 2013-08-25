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
	public bool teleportToBeginningAndLoop = false;
	public bool reverse = false;

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
		{ //IF we haven't reached the last waypoint yet
			//Compute the lerpTime, accouting for division by zero
			float lerpTime = (Time.time - timeAtLastWaypoint) / waypoints[currentWaypoint].time;
			lerpTime = Mathf.Clamp01(lerpTime); //Clamp lerpTime between 0 and 1;

			//Debug.Log(lerpTime);
			if (startAtCurrentPosition)
			{
				Transform to = waypoints[currentWaypoint].transform;

				gameObject.transform.position = Vector3.Lerp(currentFauxWaypointPos, to.position, lerpTime);
				gameObject.transform.rotation = Quaternion.Lerp(currentFauxWaypointRot, to.rotation, lerpTime);
			}
			else
			{
				Transform from = waypoints[currentWaypoint - 1].transform;
				Transform to = waypoints[currentWaypoint].transform;

				gameObject.transform.position = Vector3.Lerp(from.position, to.position, lerpTime);
				gameObject.transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, lerpTime);
			}
			if (lerpTime >= 1)
			{ //IF we have reached the waypoint
				Waypoint activeWaypoint = waypoints[currentWaypoint];
				activeWaypoint.OnPass();
				if (startAtCurrentPosition)
					startAtCurrentPosition = false;
				else
				{
					currentWaypoint++; //THEN continue to the next
				}
				if (activeWaypoint.terminus)
				{
					if (teleportToBeginningAndLoop)
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
