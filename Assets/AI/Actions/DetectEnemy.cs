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
	private int hasEnemy = 0;
	private Unit us = null;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		hasEnemy = 0;
		bestUnit = null;
		enemy = agent.actionContext.GetContextItem<string>("enemy");
		if(enemy == "")
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(us == null)
		{
			us = agent.actionContext.GetContextItem<Unit>("unit");
			if(us == null)
			{
				us = agent.Avatar.GetComponent<Unit>();
				if(us == null)
					return RAIN.Action.Action.ActionResult.FAILURE;
			}
		}
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		bestUnit = us.DetectEnemies(agent,enemy);
		if(bestUnit == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
		hasEnemy = 1;
		SetVariables(agent);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
		SetVariables(agent);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
	
	private void SetVariables(RAIN.Core.Agent agent)
	{
		agent.actionContext.SetContextItem<int>("hasEnemy",hasEnemy);
		if(bestUnit == null || !bestUnit.IsAlive())
			agent.actionContext.SetContextItem<Transform>("target",null);
		else
			agent.actionContext.SetContextItem<Transform>("target",bestUnit.transform);
	}
}