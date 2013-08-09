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
	}
	
	// Update is called once per frame
	void Update ()
	{
		time += Time.deltaTime;
		if(time > HEATMAP_UPDATE_FREQUENCY)
		{
			time = 0.0f;
			//UpdateHeatmaps();
			if(tookScreenShot)
			{
				heatmapCam.depth = -2;
				heatmapCam.cullingMask = originalMask;
			}
		}
	}
	
	void OnDrawGizmosSelected()
	{
		time += Time.deltaTime;
		if(time > HEATMAP_UPDATE_FREQUENCY)
		{
			time = 0.0f;
			UpdateHeatmaps();
		}
	}
	
	void LateUpdate()
	{
		if(heatmapCam == null)
			return;
		
		if(Input.GetKeyDown(KeyCode.F1) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			UpdateHeatmaps();
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = originalMask;
			Application.CaptureScreenshot("Heatmaps/All.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
		if(Input.GetKeyDown(KeyCode.F2) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			UpdateHeatmaps();
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = 1 << LayerMask.NameToLayer("Death Data") | 1 << LayerMask.NameToLayer("Default");
			Application.CaptureScreenshot("Heatmaps/Deaths.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
		if(Input.GetKeyDown(KeyCode.F3) && heatmapCam != null)
		{
			time = HEATMAP_UPDATE_FREQUENCY - 1;
			UpdateHeatmaps();
			heatmapCam.depth = 2;
			heatmapCam.cullingMask = 1 << LayerMask.NameToLayer("Move Data") | 1 << LayerMask.NameToLayer("Default");
			Application.CaptureScreenshot("Heatmaps/Movement.png");
			Debug.Log("Completed writing heatmaps to file.");
			tookScreenShot = true;
		}
	}
	
	public static void AddHeatData(UnitHeatData uHeat)
	{
		if(heatData == null)
		{
			heatData = new UnitHeatData[1];
			heatData[0] = uHeat;
		}
		else
		{
			List<UnitHeatData> heatList = new List<UnitHeatData>(heatData);
			heatList.Add(uHeat);
			heatData = heatList.ToArray();
		}
		if(uHeat.isDeath)
		{
			if(deathData == null)
			{
				deathData = new UnitHeatData[1];
				deathData[0] = uHeat;
			}
			else
			{
				List<UnitHeatData> deathList = new List<UnitHeatData>(deathData);
				deathList.Add(uHeat);
				deathData = deathList.ToArray();
			}
		}
		else
		{
			if(moveData == null)
			{
				moveData = new UnitHeatData[1];
				moveData[0] = uHeat;
			}
			else
			{
				List<UnitHeatData> moveList = new List<UnitHeatData>(moveData);
				moveList.Add(uHeat);
				moveData = moveList.ToArray();
			}
		}
	}
	
	public static HeatmapBlock[] GetHeatBlockLocations()
	{
		HeatmapBlock[] valueArray = new HeatmapBlock[blockLocations.Count];
		blockLocations.Values.CopyTo(valueArray,0);
		return valueArray;
	}
	
	private void UpdateHeatmaps()
	{
		if(heatData == null || heatData.Length == 0)
			return;
		foreach(UnitHeatData uHeatData in heatData)
		{
			Vector3 roundedPosition = uHeatData.position;
			roundedPosition = new Vector3(Mathf.RoundToInt(roundedPosition.x), 15, Mathf.RoundToInt(roundedPosition.z));
			HeatmapBlock heatBlock;
			if(blockLocations.ContainsKey(roundedPosition))
			{
				heatBlock = blockLocations[roundedPosition];
			}
			else
			{
				GameObject heatGO = Instantiate(heatBlockPrefab) as GameObject;
				heatGO.transform.position = roundedPosition;
				heatBlock = heatGO.GetComponent<HeatmapBlock>();
				if(heatBlock == null)
					heatBlock = heatGO.AddComponent<HeatmapBlock>();
				heatBlock.SetLocation(roundedPosition);
				blockLocations.Add(roundedPosition,heatBlock);
			}
			if(uHeatData.isDeath)
			{
				heatBlock.AddDeath();
			}
			else
			{
				heatBlock.AddMove();
			}
			heatBlock.transform.parent = transform;
		}
		HeatmapBlock[] heatBlocks = GetHeatBlockLocations();
		foreach(KeyValuePair<Vector3,HeatmapBlock> kvp in blockLocations)
		{
			HeatmapBlock heatBlock = kvp.Value;
			heatBlock.AdjustColor(heatBlocks);
		}
	}
}
