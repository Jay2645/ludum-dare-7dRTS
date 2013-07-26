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
	
	Weapon weapon = null;
	Unit enemy = null;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(weapon == null)
			weapon = agent.Avatar.GetComponent<Unit>().weapon;
		if(weapon == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(enemy == null)
			enemy = agent.actionContext.GetContextItem<Transform>("target").gameObject.GetComponent<Unit>();
		if(enemy == null)
			return RAIN.Action.Action.ActionResult.FAILURE;
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		if(!enemy.IsAlive())
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(weapon.ammo <= 0)
			return RAIN.Action.Action.ActionResult.FAILURE;
		if(weapon.range < Vector3.Distance(enemy.transform.position,agent.Avatar.transform.position))
			return RAIN.Action.Action.ActionResult.FAILURE;
		float accuracy = weapon.GetAccuracy();
		Debug.Log ("Accuracy: "+accuracy.ToString());
		if(accuracy > 0.85f)
		{
			if(agent.LookAt(enemy.transform.position,deltaTime))
			{
				weapon.Shoot();
				return RAIN.Action.Action.ActionResult.SUCCESS;
			}
		}
		else
		{
			if(agent.LookAt(enemy.transform.position,deltaTime))
				weapon.Shoot();
			if(agent.MoveTo(enemy.transform.position,deltaTime))
			{
				return RAIN.Action.Action.ActionResult.SUCCESS;
			}
		}
		return RAIN.Action.Action.ActionResult.RUNNING;
    }

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
}