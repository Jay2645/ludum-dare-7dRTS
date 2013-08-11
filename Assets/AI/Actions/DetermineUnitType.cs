using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class DetermineUnitType : RAIN.Action.Action
{
    public DetermineUnitType()
    {
        actionName = "DetermineUnitType";
    }

    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		Leader leader = agent.Avatar.GetComponent<Leader>();
		if(leader == null)
			return RAIN.Action.Action.ActionResult.SUCCESS;
		if(leader is Commander)
		{
			agent.actionContext.SetContextItem<int>("unitType",3);
			return RAIN.Action.Action.ActionResult.FAILURE;
		}
		agent.actionContext.SetContextItem<int>("unitType",2);
		return RAIN.Action.Action.ActionResult.FAILURE;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}