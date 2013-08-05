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
	private int hasOrders = 0;
	private string friend = "";
	private string enemy = "";
	private Order orders = Order.stop;
	
    public override RAIN.Action.Action.ActionResult Start(RAIN.Core.Agent agent, float deltaTime)
    {
		if(unit == null)
			unit = agent.Avatar.GetComponent<Unit>();
		if(isPlayer == -1)
			isPlayer = agent.actionContext.GetContextItem<int>("isPlayer");
		if(friend == "")
			friend = agent.Avatar.tag;
		if(enemy == "")
		{
			if(friend == "Blue")
				enemy = "Red";
			else if(friend == "Red")
				enemy = "Blue";
		}
		hasOrders = 0;
		orders = Order.stop;
		unitType = -1;
		if(unit == null || isPlayer == -1 || friend == "" || enemy == "")
			return RAIN.Action.Action.ActionResult.FAILURE;
		orders = unit.GetOrder();
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }

    public override RAIN.Action.Action.ActionResult Execute(RAIN.Core.Agent agent, float deltaTime)
    {
		SetVariables(agent);
		if(isPlayer == 1)
			return RAIN.Action.Action.ActionResult.SUCCESS;
		if(unit == null)
			unit = agent.Avatar.GetComponent<Unit>();
		Unit nearestEnemy = unit.DetectEnemies(agent,enemy);
		if(nearestEnemy != null)
		{
			Action.ActionResult shootResult = unit.Shoot(agent,deltaTime,nearestEnemy);
			if(shootResult == Action.ActionResult.SUCCESS || shootResult == Action.ActionResult.RUNNING)
				return RAIN.Action.Action.ActionResult.RUNNING;
		}
		
		if(orders == Order.stop)
		{
			agent.MoveTo(agent.Avatar.transform.position,deltaTime);
			return RAIN.Action.Action.ActionResult.FAILURE;
		}
		Transform target = unit.GetMoveTarget();
		if(target == null)
		{
			agent.MoveTo(agent.Avatar.transform.position,deltaTime);
			return RAIN.Action.Action.ActionResult.FAILURE;
		}
		if(agent.MoveTo(target.position,deltaTime))
			return RAIN.Action.Action.ActionResult.SUCCESS;
		return RAIN.Action.Action.ActionResult.RUNNING;
	}

    public override RAIN.Action.Action.ActionResult Stop(RAIN.Core.Agent agent, float deltaTime)
    {
		SetVariables(agent);
        return RAIN.Action.Action.ActionResult.SUCCESS;
    }
	
	private void SetVariables(RAIN.Core.Agent agent)
	{
		agent.actionContext.SetContextItem<int>("isPlayer",isPlayer);
		agent.actionContext.SetContextItem<string>("friend",friend);
		agent.actionContext.SetContextItem<string>("enemy",enemy);
		if(isPlayer != 1)
		{
			int order = 0;
			if(orders == Order.move)
				order = 1;
			else if(orders == Order.attack)
				order = 2;
			else if(orders == Order.defend)
				order = 3;
			if(order != 0)
				hasOrders = 1;
			else
				hasOrders = 0;
			agent.actionContext.SetContextItem<int>("hasOrders",hasOrders);
			agent.actionContext.SetContextItem<int>("order",order);
		}
		agent.actionContext.SetContextItem<Unit>("unit",unit);
		if(unit is Commander)
		{
			if(((Commander)unit).isPlayer)
				isPlayer = 1;
			else
				isPlayer = 0;
			unitType = 3;
		}
		else if(unit is Leader)
		{
			unitType = 2;
		}
		else
		{
			unitType = 1;
		}
		agent.actionContext.SetContextItem<int>("unitType",unitType);
	}
}