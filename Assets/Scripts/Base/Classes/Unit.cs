using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour 
{
	protected bool isSelectable = true;
	protected int id = -1;
	private static int currentID = 0;
	protected Leader leader = null;
	
	void Awake()
	{
		CreateID();
	}
	
	protected void CreateID()
	{
		id = currentID;
		currentID++;
	}
	
	public bool IsSelectable()
	{
		return isSelectable;
	}
	
	public int GetID()
	{
		return id;
	}
	
	public virtual void RegisterLeader(Leader leader)
	{
		this.leader = leader;
		
	}
}
