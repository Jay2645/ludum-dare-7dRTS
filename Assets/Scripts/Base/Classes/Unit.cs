using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour 
{
	protected bool isSelectable = true;
	protected int id = -1;
	private static int currentID = 0;
	protected Leader leader = null;
	protected GameObject smokeTrail = null;
	public static GameObject smokeTrailPrefab = null;
	public static GameObject dustPrefab = null;
	public const string[] names = 
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
		leader.RegisterUnit(this);
	}
	
	public bool Select()
	{
		if(!isSelectable)
			return false;
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
