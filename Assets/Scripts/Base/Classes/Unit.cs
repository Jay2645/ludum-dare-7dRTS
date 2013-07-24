using UnityEngine;
using System.Collections;

/// <summary>
/// A Unit is the main object of the game. Anything which can be selected and recieve orders is considered a Unit.
/// Units have a unique ID and a name so the player can tell them apart.
/// They currently fall from the sky upon spawn, akin to ODSTs from the Halo series.
/// Units can be promoted or demoted to Leader class by a Commander; however, there is only one Commander which can be active at any given time.
/// </summary>
public class Unit : MonoBehaviour 
{
	protected bool isSelectable = true;
	protected bool isSelected = false;
	protected int id = -1;
	private static int currentID = 0;
	protected Leader leader = null;
	protected GameObject smokeTrail = null;
	public static GameObject smokeTrailPrefab = null;
	public static GameObject dustPrefab = null;
	public static GameObject movePrefab = null;
	public static GameObject attackPrefab = null;
	public static GameObject defendPrefab = null;
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
	protected Order currentOrder = Order.stop;
	protected Transform moveTarget = null;
	protected GameObject moveEffect = null;
	public int health = 100;
	public Weapon weapon = null;
	
	void Awake()
	{
		CreateID();
		if(weapon != null)
		{
			weapon = Instantiate(weapon) as Weapon;
			weapon.transform.parent = transform;
			weapon.transform.localPosition =  new Vector3(-0.35f,0.075f,0.75f);
			weapon.transform.localRotation = Quaternion.Euler(90,0,0);
			weapon.owner = this;
		}
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
		if(movePrefab == null)
		{
			movePrefab = Resources.Load("Prefabs/MoveTarget") as GameObject;
		}
		if(defendPrefab == null)
		{
			defendPrefab = Resources.Load("Prefabs/DefendTarget") as GameObject;
		}
		if(attackPrefab == null)
		{
			attackPrefab = Resources.Load("Prefabs/AttackTarget") as GameObject;
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
		CheckHealth();
	}
	
	protected void CreateSelected()
	{	
		if(isSelected && currentOrder != Order.stop && moveTarget != null)
		{
			if(moveEffect == null)
			{
				if(currentOrder == Order.move)
				{
					moveEffect = (GameObject)Instantiate(movePrefab);
				}
				else if(currentOrder == Order.defend)
				{
					moveEffect = (GameObject)Instantiate(defendPrefab);
				}
				else if(currentOrder == Order.attack)
				{
					moveEffect = (GameObject)Instantiate(attackPrefab);
				}
				if(moveEffect != null)
				{
					moveEffect.transform.parent = moveTarget;
					moveEffect.transform.localPosition = Vector3.zero;
				}
			}
		}
		else
		{
			if(moveEffect == null)
				return;
			Destroy(moveEffect);
			moveEffect = null;
		}
	}
	
	protected void CheckHealth()
	{
		if(health <= 0)
			OnDie();
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
		if(leader.GetCommander() != Commander.player)
			Destroy(label);
		teamColor = leader.teamColor;
		leader.RegisterUnit(this);
		renderer.material.color = teamColor;
	}
	
	public void RecieveOrder(Order order, Transform target)
	{
		if(target == transform && order != Order.stop)
			return;
		if(moveTarget != null)
		{
			DestroyImmediate(moveTarget.gameObject);
			moveTarget = null;
		}
		target = ((GameObject)Instantiate(target.gameObject,target.position,target.rotation)).transform;
		Vector3 targetLocation = target.position;
		targetLocation.x += Random.Range(-3,3);
		targetLocation.z += Random.Range(-3,3);
		target.position = targetLocation;
		if(order == Order.stop || Vector3.Distance(target.position,transform.position) < 1.5f)
		{
			if(order != Order.defend)
			{
				currentOrder = Order.stop;
				DestroyImmediate(moveTarget.gameObject);
				moveTarget = null;
			}
			return;
		}
		currentOrder = order;
		moveTarget = target;
		CreateSelected();
		// This is a quick-and-dirty way for players to see that the unit has recieved orders correctly.
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
			currentOrder = Order.stop;
			moveTarget = null;
		}
		return moveTarget;
	}
	
	public bool Select()
	{
		if(!isSelectable)
		{
			isSelected = false;
			return false;
		}
		isSelected = true;
		if(leader != (Leader)Commander.player)
			return true;
		CreateSelected();
		renderer.material.SetColor("_OutlineColor",Color.green);
		return true;
	}
	
	public void IsLookedAt(bool lookedAt)
	{
		if(label == null)
			return;
		label.isLookedAt = lookedAt;
		if(lookedAt)
			label.SetLabelText(GenerateLabel());
	}
	
	public string GenerateLabel()
	{
		string labelS = uName+"\n";
		labelS = labelS + GetClass()+"\n";
		labelS = labelS + "Weapon: ";
		if(weapon == null)
			labelS = labelS + "None\n";
		else
			labelS = labelS + weapon.weaponName+"\n";
		labelS = labelS + "Health: "+health+" / 100";
		return labelS;
	}
	
	protected virtual string GetClass()
	{
		return "Grunt";
	}
	
	public void Deselect()
	{
		if(!isSelectable)
			return;
		isSelected = false;
		if(leader.GetCommander() != Commander.player)
			return;
		CreateSelected();
		renderer.material.SetColor("_OutlineColor",Color.black);
	}
	
	protected void OnDie()
	{
		if(leader != null)
			leader.RemoveUnit(id);
		Destroy(gameObject);
	}
	
	public void UpgradeUnit(Commander commander)
	{
		Leader upgrade = gameObject.AddComponent<Leader>();
		upgrade.CloneUnit(this);
		upgrade.RegisterCommander(commander);
		MessageList.Instance.AddMessage(uName+", acknowledging promotion to Leader.");
		Destroy(this);
	}
	
	public void CloneUnit(Unit oldClone)
	{
		isSelectable = oldClone.isSelectable;
		id = oldClone.id;
		leader = oldClone.leader;
		uName = oldClone.uName;
		smokeTrail = oldClone.smokeTrail;
		teamColor = oldClone.teamColor;
		label = oldClone.label;
		currentOrder = oldClone.currentOrder;
		moveTarget = oldClone.moveTarget;
		health = oldClone.health;
		weapon = oldClone.weapon;
		leader.ReplaceUnit(id, this);
	}
}
