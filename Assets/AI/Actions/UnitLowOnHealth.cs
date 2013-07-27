using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

/// <summary>
/// Returns FAILURE if the unit is missing or is healthy, returns SUCCESS if it is low on health.
/// </summary>
public class UnitLowOnHealth : RAIN.Action.Action
{
    public UnitLowOnHealth()
    {
        actionName = "UnitLowOnHealth";
    }
	
	private Unit unit = null;
	private int isHealthy = 0;
	private const float DANGEROUS_HEALTH_PERCENT = 0.35f;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		isHealthy = 1;
		if(unit == null)
		{
			unit = agent.actionContext.GetContextItem<Unit>("unit");
			if(unit == null)
				return RAIN.Action.Action.ActionResult.FAILURE;
		}
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unit != null && unit.GetHealthPercent() < DANGEROUS_HEALTH_PERCENT)
		{
			isHealthy = 0;
			SetVariables(agent);
			return RAIN.Action.Action.ActionResult.SUCCESS;
		}
        return RAIN.Action.Action.ActionResult.FAILURE;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
		SetVariables(agent);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
	
	private void SetVariables(RAIN.Core.Agent agent)
	{
		agent.actionContext.SetContextItem<int>("isHealthy",isHealthy);
	}
}