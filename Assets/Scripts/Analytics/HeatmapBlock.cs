﻿using System.Collections.Generic;
using UnityEngine;

public class HeatmapBlock : MonoBehaviour
{

	private Vector3 location;
	private int deathCount = 0;
	private int _deathCount = 0;
	private int moveCount = 0;
	private int _moveCount = 0;
	private HeatmapBlock moveBlock;
	private HeatmapBlock deathBlock;
	private Color blockColor = Color.blue;
	private const float HEATMAP_DECAY_TIME = 30.0f;
	private const float MAX_ALLOWABLE_DISTANCE = 1.0f;
	private const float ADJUST_COLOR_BY = 0.2f;
	private LayerMask heatData;
	private LayerMask deathData;
	private LayerMask moveData;
	private LayerMask ignoreRaycast;
	public static HeatmapBlock[] allBlocks;
	private List<Unit> containedUnits = new List<Unit>();

	void Start()
	{
		location = transform.position;
		blockColor.a = ADJUST_COLOR_BY;
		renderer.material.color = blockColor;
		renderer.material.shader = Shader.Find("GUI/3D Text Shader");
		heatData = LayerMask.NameToLayer("Heat Data");
		deathData = LayerMask.NameToLayer("Death Data");
		moveData = LayerMask.NameToLayer("Move Data");
		ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
		if (gameObject.layer != heatData && gameObject.layer != deathData && gameObject.layer != moveData)
		{
			gameObject.layer = heatData;
		}
	}

	public void SetLocation(Vector3 newLocation)
	{
		location = newLocation;
		transform.position = location;
	}

	public void AddDeath()
	{
		AddDeath(gameObject.layer == heatData);
	}

	public void AddMove()
	{
		AddMove(gameObject.layer == heatData);
	}

	public void AddDeath(bool createBlock)
	{
		deathCount++;
		_deathCount++;
		if (createBlock)
		{
			if (deathBlock == null)
			{
				Vector3 deathLocation = location;
				deathLocation.y -= 2;
				GameObject deathGO = Instantiate(GameMetrics.heatBlockStaticPrefab) as GameObject;
				deathGO.transform.position = deathLocation;
				deathGO.layer = LayerMask.NameToLayer("Death Data");
				deathBlock = deathGO.GetComponent<HeatmapBlock>();
				if (deathBlock == null)
					deathBlock = deathGO.AddComponent<HeatmapBlock>();
				deathBlock.SetLocation(deathLocation);
				deathBlock.transform.parent = transform;
			}
			deathBlock.AddDeath(false);
		}
		AdjustColor(allBlocks);
		Invoke("RemoveDeath", HEATMAP_DECAY_TIME);
	}

	public void AddMove(bool createBlock)
	{
		moveCount++;
		_moveCount++;
		if (createBlock)
		{
			if (moveBlock == null)
			{
				Vector3 moveLocation = location;
				moveLocation.y -= 1;
				GameObject moveGO = Instantiate(GameMetrics.heatBlockStaticPrefab) as GameObject;
				moveGO.transform.position = moveLocation;
				moveGO.layer = LayerMask.NameToLayer("Move Data");
				moveBlock = moveGO.GetComponent<HeatmapBlock>();
				if (moveBlock == null)
					moveBlock = moveGO.AddComponent<HeatmapBlock>();
				moveBlock.SetLocation(moveLocation);
				moveBlock.transform.parent = transform;
			}
			moveBlock.AddMove(false);
		}
		AdjustColor(allBlocks);
		Invoke("RemoveMove", HEATMAP_DECAY_TIME);
	}

	private void RemoveDeath()
	{
		_deathCount--;
		if (gameObject.layer != heatData)
			AdjustColor(allBlocks);
	}

	private void RemoveMove()
	{
		_moveCount--;
		if (gameObject.layer != heatData)
			AdjustColor(allBlocks);
	}

	public int GetDecayedDeathCount()
	{
		return _deathCount;
	}

	public int GetDecayedMoveCount()
	{
		return _moveCount;
	}

	public int GetTotalDeathCount()
	{
		return deathCount;
	}

	public int GetTotalMoveCount()
	{
		return moveCount;
	}

	public void AdjustColor(HeatmapBlock[] heatmapLocations)
	{
		// Reset our block's color.
		blockColor = Color.blue;
		blockColor.a = ADJUST_COLOR_BY;

		// The Heat Data layer keeps track of all deaths and moves.
		if (gameObject.layer == heatData)
		{
			// Iterate through deaths and change the color.
			for (int i = 0; i < deathCount; i++)
			{
				ChangeColor(1);
			}
			// Iterate through moves and change the color.
			for (int i = 0; i < moveCount; i++)
			{
				ChangeColor(1);
			}
		}
		// The Death Data layer only keeps track of deaths.
		else if (gameObject.layer == deathData)
		{
			// Iterate through deaths and change color.
			for (int i = 0; i < _deathCount; i++)
			{
				ChangeColor(1);
			}
		}
		// The Move Data layer only keeps track of movement.
		else if (gameObject.layer == moveData)
		{
			// Iterate through moves and change color.
			for (int i = 0; i < _moveCount; i++)
			{
				ChangeColor(1);
			}
		}

		// This changes the color based upon our neighbor's locations.
		foreach (HeatmapBlock item in heatmapLocations)
		{
			// Skip this item if it doesn't pertain to our interests.
			if (item == this || item.gameObject.layer != gameObject.layer)
				continue;

			// The Heat Data layer wants to keep track of everything.
			int deathCount = item.GetTotalDeathCount();
			int moveCount = item.GetTotalMoveCount();
			// If this is an empty item, skip it.
			if (moveCount == 0 && deathCount == 0)
				continue;

			// Check to see how far away this item is.
			float distance = (new Vector2(item.transform.position.x, item.transform.position.z) - new Vector2(location.x, location.z)).sqrMagnitude;
			// If it's too far, skip it.
			if (distance > MAX_ALLOWABLE_DISTANCE)
				continue;

			// If we're tracking only deaths, replace total numbers with the decayed numbers.
			if (gameObject.layer == deathData)
				deathCount = item.GetDecayedDeathCount();
			// If we're tracking only moves, replace total numbers with the decayed numbers.
			if (gameObject.layer == moveData)
				moveCount = item.GetDecayedMoveCount();

			// If we're tracking deaths and this has no deaths, skip it.
			if (gameObject.layer == deathData && deathCount == 0)
				continue;
			// If we're tracking moves and this has no moves, skip it.
			if (gameObject.layer == moveData && moveCount == 0)
				continue;

			// Calculate the distance modifier:
			float mod = MAX_ALLOWABLE_DISTANCE; // Start at the max distance.
			mod -= distance; // Subtract the actual distance.
			mod /= MAX_ALLOWABLE_DISTANCE; // Get the difference as a percentage of the max distance.

			// Iterate through every death and change the color.
			if (gameObject.layer == heatData || gameObject.layer == deathData)
			{
				for (int i = 0; i < deathCount; i++)
				{
					ChangeColor(mod);
				}
			}
			// Iterate through every move and change the color.
			if (gameObject.layer == heatData || gameObject.layer == moveData)
			{
				for (int i = 0; i < moveCount; i++)
				{
					ChangeColor(mod);
				}
			}
		}

		// If we have child blocks, go recursively on those.
		if (moveBlock != null)
		{
			moveBlock.AdjustColor(heatmapLocations);
		}
		if (deathBlock != null)
		{
			deathBlock.AdjustColor(heatmapLocations);
		}

		// Update our color.
		renderer.material.color = blockColor;
	}

	private void ChangeColor(float mod)
	{
		// Color starts blue (cool) by default and moves towards red (hot)
		blockColor.r += ADJUST_COLOR_BY * mod;
		blockColor.b -= ADJUST_COLOR_BY * mod;
		blockColor.a += ADJUST_COLOR_BY * mod;
	}

	void OnTriggerEnter(Collider col)
	{
		Unit unit = col.gameObject.GetComponent<Unit>();
		if (gameObject.layer == ignoreRaycast || unit == null || containedUnits.Contains(unit) || !unit.IsAlive())
			return;
		unit.EnterHeatmapBlock(this);
		AddMove(gameObject.layer == heatData);
		containedUnits.Add(unit);
	}

	void OnTriggerExit(Collider col)
	{
		Unit unit = col.gameObject.GetComponent<Unit>();
		if (gameObject.layer == ignoreRaycast || unit == null || !containedUnits.Contains(unit))
			return;
		unit.ExitHeatmapBlock(this);
		containedUnits.Remove(unit);
	}

	void OnDisable()
	{
		if (deathCount <= 0 && moveCount <= 0)
		{
			//Debug.Log("No units moved into or died at "+location);
			return;
		}
		// If we're a child block, let our parent handle it instead.
		if (moveBlock == null && deathBlock == null && (deathCount <= 0 || moveCount <= 0))
			return;
		// Log relevant info:
		/*Debug.Log("Heatmap data for "+location+":");
		if(deathCount > 0)
			Debug.Log ("Total deaths: "+deathCount);
		if(moveCount > 0)
			Debug.Log ("Total times units passed through: "+moveCount);*/
	}
}
