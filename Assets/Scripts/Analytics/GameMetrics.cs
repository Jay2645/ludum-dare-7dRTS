using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct UnitHeatData
{
	public Vector3 position;
	public float time;
	public bool isDeath;
}

public class GameMetrics : MonoBehaviour {
	
	public static UnitHeatData[] moveData;
	public static UnitHeatData[] deathData;
	public static UnitHeatData[] heatData;
	public Camera heatmapCam;
	public GameObject heatBlockPrefab;
	/// <summary>
	/// A static version of the Heat Block prefab.
	/// Static variables can't be defined in the inspector, so we have to assign this ourselves.
	/// </summary>
	public static GameObject heatBlockStaticPrefab;
	private static Dictionary<Vector3,HeatmapBlock> blockLocations = new Dictionary<Vector3, HeatmapBlock>();
	private float time = 0.0f;
	private const float HEATMAP_UPDATE_FREQUENCY = 2.0f;
	private const float HEATMAP_SPACE_AMOUNT = 5.0f;
	private LayerMask originalMask;
	private bool tookScreenShot = false;
	
	void Start()
	{
		if(heatBlockPrefab != null && heatBlockStaticPrefab == null)
		{
			heatBlockStaticPrefab = heatBlockPrefab;
		}
		if(heatmapCam != null)
			originalMask = heatmapCam.cullingMask;
		CreateMesh();
	}
	
	// Update is called once per frame
	void Update ()
	{
		time += Time.deltaTime;
		if(time > HEATMAP_UPDATE_FREQUENCY)
		{
			time = 0.0f;
			if(tookScreenShot)
			{
				heatmapCam.depth = -2;
				heatmapCam.cullingMask = originalMask;
			}
		}
	}
	
	void LateUpdate()
	{
		if(heatmapCam == null)
			return;
		
		if(Input.GetKeyDown(KeyCode.F1) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = 1 << LayerMask.NameToLayer("Heat Data") | 1 << LayerMask.NameToLayer("Default");
			Application.CaptureScreenshot("Heatmaps/All.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
		if(Input.GetKeyDown(KeyCode.F2) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = 1 << LayerMask.NameToLayer("Death Data") | 1 << LayerMask.NameToLayer("Default");
			Application.CaptureScreenshot("Heatmaps/Deaths.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
		if(Input.GetKeyDown(KeyCode.F3) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = 1 << LayerMask.NameToLayer("Move Data") | 1 << LayerMask.NameToLayer("Default");
			Application.CaptureScreenshot("Heatmaps/Movement.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
	}
	
	private void CreateMesh()
	{
		Vector3 raycast = transform.position;
		raycast.x = Mathf.RoundToInt(raycast.x);
		raycast.y = 30.0f;
		raycast.z = Mathf.RoundToInt(raycast.z);
		Ray cornerRay = new Ray(raycast,Vector3.down);
		
		// Find furthest location on Z axis.
		cornerRay.origin = MoveRay(cornerRay,Vector3.forward, true);
		// Find furthest location on X axis.
		cornerRay.origin = MoveRay(cornerRay,Vector3.left, true);
		PlaceMesh(cornerRay,Vector3.right * HEATMAP_SPACE_AMOUNT,Vector3.back * HEATMAP_SPACE_AMOUNT);
		HeatmapBlock.allBlocks = GetHeatBlockLocations();
	}
	
	private Vector3 MoveRay(Ray raycast, Vector3 moveBy, bool checkFalseNegative)
	{
		Vector3 origin = raycast.origin;
		int steps = 0;
		
		// Keep raycasting until we don't hit the ground anymore.
		while(Physics.Raycast(raycast,50.0f))
		{
			origin = origin + moveBy;
			raycast.origin = origin;
			steps++;
		}
		// Once loop breaks, go back one step.
		origin = origin - moveBy;
		if(!checkFalseNegative)
			return origin;
		raycast.origin = origin;
		
		// Do test recast in every cardinal direction to prevent false negatives.
		bool falseNegative = false;
		// Get the dot product to make sure we only cast perpendicular to the moveBy Vector.
		float verticalDotProduct = Vector3.Dot(moveBy,Vector3.back);
		if(verticalDotProduct < 0.5f && verticalDotProduct > -0.5f)
		{
			Vector3 vOrigin = origin + Vector3.back;
			raycast.origin = vOrigin;
			for(int i = 0; i < Mathf.Max(10,steps); i++)
			{
				if(Physics.Raycast(raycast,50.0f))
				{
					falseNegative = true;
					break;
				}
				vOrigin = vOrigin + Vector3.back;
				raycast.origin = vOrigin;
			}
			if(falseNegative)
			{
				vOrigin = vOrigin + moveBy;
				raycast.origin = vOrigin;
				if(Physics.Raycast(raycast,50.0f))
				{
					return MoveRay(raycast,moveBy, true);
				}
				vOrigin = vOrigin - moveBy;
			}
			vOrigin = origin + Vector3.forward;
			raycast.origin = vOrigin;
			for(int i = 0; i < Mathf.Max(10,steps); i++)
			{
				if(Physics.Raycast(raycast,50.0f))
				{
					falseNegative = true;
					break;
				}
				vOrigin = vOrigin + Vector3.forward;
				raycast.origin = vOrigin;
			}
			if(falseNegative)
			{
				vOrigin = vOrigin + moveBy;
				raycast.origin = vOrigin;
				if(Physics.Raycast(raycast,50.0f))
				{
					return MoveRay(raycast,moveBy, true);
				}
				vOrigin = vOrigin - moveBy;
			}
		}
		float horizontalDotProduct = Vector3.Dot(moveBy, Vector3.left);
		if(horizontalDotProduct < 0.5f && horizontalDotProduct > -0.5f)
		{
			Vector3 hOrigin = origin + Vector3.left;
			raycast.origin = hOrigin;
			for(int i = 0; i < Mathf.Max(10,steps); i++)
			{
				if(Physics.Raycast(raycast,50.0f))
				{
					falseNegative = true;
					break;
				}
				hOrigin = hOrigin + Vector3.left;
				raycast.origin = hOrigin;
			}
			if(falseNegative)
			{
				hOrigin = hOrigin + moveBy;
				raycast.origin = hOrigin;
				if(Physics.Raycast(raycast,50.0f))
				{
					return MoveRay(raycast,moveBy, true);
				}
				hOrigin = hOrigin - moveBy;
			}
			hOrigin = origin + Vector3.right;
			raycast.origin = hOrigin;
			for(int i = 0; i < Mathf.Max(10,steps); i++)
			{
				if(Physics.Raycast(raycast,50.0f))
				{
					falseNegative = true;
					break;
				}
				hOrigin = hOrigin + Vector3.right;
				raycast.origin = hOrigin;
			}
			if(falseNegative)
			{
				hOrigin = hOrigin + moveBy;
				raycast.origin = hOrigin;
				if(Physics.Raycast(raycast,50.0f))
				{
					return MoveRay(raycast,moveBy, true);
				}
				hOrigin = hOrigin - moveBy;
			}
		}
		return origin;
	}
	
	private void PlaceMesh(Ray raycast, Vector3 moveDirectionOne, Vector3 moveDirectionTwo)
	{
		Vector3 origin = raycast.origin;
		while(Physics.Raycast(raycast, 50.0f))
		{
			PlaceHeatBlock(raycast.origin);
			origin = origin + moveDirectionOne;
			raycast.origin = origin;
		}
		origin = origin - moveDirectionOne + moveDirectionTwo;
		raycast.origin = origin;
		if(Physics.Raycast(raycast,50.0f))
		{
			while(Physics.Raycast(raycast, 50.0f))
			{
				PlaceHeatBlock(raycast.origin);
				origin = origin - moveDirectionOne;
				raycast.origin = origin;
			}
			origin = origin + moveDirectionOne + moveDirectionTwo;
			raycast.origin = origin;
			if(Physics.Raycast(raycast,50.0f))
			{
				PlaceMesh(raycast,moveDirectionOne,moveDirectionTwo);
			}
		}
	}
	
	private void PlaceHeatBlock(Vector3 position)
	{
		position.x = Mathf.RoundToInt(position.x);
		position.y = Mathf.RoundToInt(position.y);
		position.z = Mathf.RoundToInt(position.z);
		
		GameObject heatGO = Instantiate(heatBlockPrefab) as GameObject;
		heatGO.transform.position = position;
		HeatmapBlock heatBlock = heatGO.GetComponent<HeatmapBlock>();
		if(heatBlock == null)
			heatBlock = heatGO.AddComponent<HeatmapBlock>();
		heatBlock.SetLocation(position);
		if(blockLocations.ContainsKey(position))
		{
			blockLocations[position] = heatBlock;
		}
		else
		{
			blockLocations.Add(position,heatBlock);
		}
		heatGO.transform.parent = transform;
	}
	
	
	public static HeatmapBlock[] GetHeatBlockLocations()
	{
		HeatmapBlock[] valueArray = new HeatmapBlock[blockLocations.Count];
		blockLocations.Values.CopyTo(valueArray,0);
		return valueArray;
	}
}
