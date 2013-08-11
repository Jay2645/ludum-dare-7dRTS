using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class RecheckObjectiveTimer : RAIN.Action.Action
{
    public RecheckObjectiveTimer()
    {
        actionName = "RecheckObjectiveTimer";
    }
	
	private static float timeSinceCheck = 20.1f;
	private const float RECHECK_TIME = 20.0f;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		
		timeSinceCheck += deltaTime;
		if(timeSinceCheck <= RECHECK_TIME)
			return RAIN.Action.Action.ActionResult.FAILURE;
		timeSinceCheck = 0.0f;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
	
	public static void Reset()
	{
		timeSinceCheck = RECHECK_TIME + 1;
	}
}