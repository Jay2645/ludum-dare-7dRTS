using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class DetectEnemy : RAIN.Action.Action
{
    public DetectEnemy()
    {
        actionName = "DetectEnemy";
    }
	
	private string enemy = "";
	private Unit bestUnit;
	int hasEnemy = 0;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		hasEnemy = 0;
		bestUnit = null;
		enemy = agent.actionContext.GetContextItem<string>("enemy");
		if(enemy == "")
			return RAIN.Action.Action.ActionResult.FAILURE;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		// Cache values:
		Transform us = agent.Avatar.transform;
		
		// Sense any nearby enemies:
		agent.GainInterestIn(enemy);
		agent.BeginSense();
		agent.Sense();
		
		// Fetch all GameObjects sensed:
		GameObject[] gos = new GameObject[10];
		agent.GetAllObjectsWithAspect(enemy,out gos);
		
		// Narrow the list down to just our living enemies:
		List<Unit> unitList = new List<Unit>();
		foreach(GameObject go in gos)
		{
			Unit unit = go.GetComponent<Unit>();
			if(unit == null || go.tag != enemy || !unit.IsAlive())
				continue;
			unitList.Add(unit);
		}
		if(unitList.Count == 0)
			return RAIN.Action.Action.ActionResult.FAILURE;
		Unit[] units = unitList.ToArray();
		
		// Assign a score to each enemy:
		float score = Mathf.Infinity;
		foreach(Unit unit in units)
		{
			if(unit == null)
				continue;
			float uScore = Vector3.Distance(unit.transform.position,us.position);
			uScore *= unit.GetHealthPercent();
			if(bestUnit == null || uScore < score)
			{
				bestUnit = unit;
				score = uScore;
			}
		}
		// Set the lowest-scoring unit to be our target:
		if(bestUnit == null || !bestUnit.IsAlive())
			return RAIN.Action.Action.ActionResult.FAILURE;
		hasEnemy = 1;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
		agent.actionContext.SetContextItem<int>("hasEnemy",hasEnemy);
		if(bestUnit == null)
			agent.actionContext.SetContextItem<Transform>("target",null);
		else
			agent.actionContext.SetContextItem<Transform>("target",bestUnit.transform);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}