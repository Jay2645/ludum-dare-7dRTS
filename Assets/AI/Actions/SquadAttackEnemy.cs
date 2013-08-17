using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class SquadAttackEnemy : RAIN.Action.Action
{
    public SquadAttackEnemy()
    {
        actionName = "SquadAttackEnemy";
    }
	
	Unit unitTarget = null;
	Leader leader = null;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		unitTarget = null;
		leader = agent.Avatar.GetComponent<Leader>();
		if(leader == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
		Transform target = agent.actionContext.GetContextItem<Transform>("target");
		if(target == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
		unitTarget = target.gameObject.GetComponent<Unit>();
		if(unitTarget == null || !unitTarget.IsAlive())
			return RAIN.Action.Action.ActionResult.FAILURE;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		OrderData data = new OrderData(leader,leader);
		data.SetOrder(Order.attack,true);
		data.SetTarget(unitTarget.transform);
		leader.RecieveOrder(data);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}