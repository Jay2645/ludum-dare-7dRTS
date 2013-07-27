using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class ShootEnemy : RAIN.Action.Action
{
    public ShootEnemy()
    {
        actionName = "ShootEnemy";
    }
	
	Unit enemy = null;
	Unit us = null;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(enemy == null)
			enemy = agent.actionContext.GetContextItem<Transform>("target").gameObject.GetComponent<Unit>();
		if(enemy == null)
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
		return us.Shoot(agent,deltaTime,enemy);
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}