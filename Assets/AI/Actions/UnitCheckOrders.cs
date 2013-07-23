using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class UnitCheckOrders : RAIN.Action.Action
{
    public UnitCheckOrders()
    {
        actionName = "UnitCheckOrders";
    }
	
	private Unit unit;

    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unit == null)
			unit = agent.Avatar.GetComponentInChildren<Unit>();
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unit.GetOrder() == Order.Stop)
		{
			agent.actionContext.SetContextItem<int>("hasOrders",0);
			agent.MoveTo(agent.Avatar.transform.position,0.0f);
			return RAIN.Action.Action.ActionResult.FAILURE;
		}
		Transform target = unit.GetMoveTarget();
		if(target == null)
		{
			agent.actionContext.SetContextItem<int>("hasOrders",0);
			agent.MoveTo(agent.Avatar.transform.position,0.0f);
			return RAIN.Action.Action.ActionResult.FAILURE;
		}
		agent.actionContext.SetContextItem<int>("hasOrders",1);
		if(agent.MoveTo(target.position,0.5f))
			return RAIN.Action.Action.ActionResult.SUCCESS;
		return RAIN.Action.Action.ActionResult.RUNNING;
	}

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}