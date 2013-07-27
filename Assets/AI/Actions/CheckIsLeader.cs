using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class CheckIsLeader : RAIN.Action.Action
{
    public CheckIsLeader()
    {
        actionName = "CheckIsLeader";
    }

    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		Unit unit = agent.Avatar.GetComponent<Unit>();
		if(unit is Commander)
		{
			Debug.Log(unit+" is Commander.");
			if(((Commander)unit).isPlayer)
				return RAIN.Action.Action.ActionResult.FAILURE;
			return RAIN.Action.Action.ActionResult.SUCCESS;	
		}
		else if(unit is Leader)
		{
			return RAIN.Action.Action.ActionResult.SUCCESS;	
		}
        return RAIN.Action.Action.ActionResult.FAILURE;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}