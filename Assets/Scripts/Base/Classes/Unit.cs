using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour 
{
	protected bool isSelectable = true;
	protected int id = -1;
	private static int currentID = 0;
	protected Leader leader = null;
	protected Commander commander = null;
	protected GameObject smokeTrail = null;
	public static GameObject smokeTrailPrefab = null;
	public static GameObject dustPrefab = null;
	public static string[] names = 
	{
		"Buzz Harrington",
		"Luke Marigold",
		"Chris Wong",
		"David Rogers",
		"Will Atkins",
		"Michael Dyer",
		"Dan Randall",
		"Bruce Hunt",
		"Ken Clark"
	};
	protected string uName = "";
	public Color teamColor = Color.white;
	public ObjectLabel label = null;
	protected Order currentOrder = Order.Stop;
	protected Transform moveTarget = null;
	
	void Awake()
	{
		CreateID();
	}
	
	void Start()
	{
		if(transform.position.y > 100)
		{
			InitSmokeParticles();
		}
		renderer.material.color = teamColor;
	}
	
	protected void InitSmokeParticles()
	{
		if(smokeTrailPrefab == null)
		{
			smokeTrailPrefab = Resources.Load("Particles/Smoke Trail") as GameObject;
		}
		if(dustPrefab == null)
		{
			dustPrefab = Resources.Load("Particles/Dust Storm") as GameObject;
		}
		smokeTrail = Instantiate(smokeTrailPrefab) as GameObject;
		smokeTrail.transform.position = transform.position;
		smokeTrail.transform.parent = transform;
	}
	
	protected void RemoveSmokeTrail()
	{
		if(smokeTrail != null)
		{
			if(transform.position.y < 50)
			{
				Destroy(smokeTrail,1.0f);
				smokeTrail = null;
				GameObject dust = Instantiate(dustPrefab) as GameObject;
				dust.transform.position = transform.position;
				dust.transform.parent = transform;
				Destroy(dust,5.75f);
			}
		}
	}
	
	void Update()
	{
		RemoveSmokeTrail();
	}
	
	protected void CreateID()
	{
		id = currentID;
		currentID++;
		if(uName == "")
		{
			uName = names[Mathf.RoundToInt(Random.Range(0,names.Length))];
			gameObject.name = uName;
			if(label != null)
				label.SetLabelText(uName);
		}
	}
	
	public bool IsSelectable()
	{
		return isSelectable;
	}
	
	public int GetID()
	{
		if(id == -1)
			CreateID();
		return id;
	}
	
	public virtual void RegisterLeader(Leader leader)
	{
		this.leader = leader;
		if(leader is Commander)
			commander = (Commander)leader;
		if(commander != Commander.player)
			Destroy(label);
		teamColor = leader.teamColor;
		leader.RegisterUnit(this);
		renderer.material.color = teamColor;
	}
	
	public void RecieveOrder(Order order, Transform target)
	{
		if(target == transform && order != Order.Stop)
			return;
		target = ((GameObject)Instantiate(target.gameObject,target.position,target.rotation)).transform;
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-3,3);
		targetLocation.z += Random.Range(-3,3);
		target.position = targetLocation;
		if(order == Order.Stop || Vector3.Distance(target.position,transform.position) < 1.5f)
		{
			currentOrder = Order.Stop;
			DestroyImmediate(moveTarget.gameObject);
			moveTarget = null;
			return;
		}
		currentOrder = order;
		moveTarget = target;
		if(leader == (Leader)Commander.player)
			MessageList.Instance.AddMessage(uName+", acknowledging "+order.ToString()+" order.");
	}
	
	public Order GetOrder()
	{
		return currentOrder;
	}
	
	public Transform GetMoveTarget()
	{
		if(moveTarget == null)
			return null;
		if(Vector3.Distance(moveTarget.position,transform.position) < 1.5f)
		{
			DestroyImmediate(moveTarget.gameObject);
			currentOrder = Order.Stop;
			moveTarget = null;
		}
		return moveTarget;
	}
	
	public bool Select()
	{
		if(!isSelectable)
			return false;
		if(commander != Commander.player)
			return true;
		renderer.material.SetColor("_OutlineColor",Color.green);
		return true;
	}
	
	public void Deselect()
	{
		if(!isSelectable)
			return;
		renderer.material.SetColor("_OutlineColor",Color.black);
	}
	
	protected void OnDie()
	{
		leader.RemoveUnit(id);
	}
}
