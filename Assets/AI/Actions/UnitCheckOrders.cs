using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

/// <summary>
/// RAIN behavior tree for basic units.
/// Makes units check to see if their commander has given them any orders.
/// If so, they move to assigned location.
/// </summary>
public class UnitCheckOrders : RAIN.Action.Action
{
    public UnitCheckOrders()
    {
        actionName = "UnitCheckOrders";
    }
	
	private Unit unit;
	private int unitType = -1;
	private int isPlayer = -1;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unit == null)
			unit = agent.Avatar.GetComponentInChildren<Unit>();
		if(unitType == -1)
			unitType = agent.actionContext.GetContextItem<int>("unitType");
		if(isPlayer == -1)
			isPlayer = agent.actionContext.GetContextItem<int>("isPlayer");
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unitType == 0)
		{
			if(unit is Commander)
			{
				if(((Commander)unit).isPlayer)
					isPlayer = 1;
				unitType = 3;
			}
			else if(unit is Leader)
				unitType = 2;
			else
				unitType = 1;
			agent.actionContext.SetContextItem<int>("unitType",unitType);
			agent.actionContext.SetContextItem<int>("isPlayer",isPlayer);
		}
		if(isPlayer == 1)
			return RAIN.Action.Action.ActionResult.SUCCESS;
		if(unit.GetOrder() == Order.stop)
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