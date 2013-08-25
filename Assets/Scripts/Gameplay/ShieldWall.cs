using UnityEngine;
using System.Collections;

public class ShieldWall : MonoBehaviour 
{
	public AudioClip shieldHit;
	public AudioClip moveThroughShield;
	public Shield shield;

	void OnTriggerEnter(Collider col)
	{
		Unit unit = col.GetComponent<Unit>();
		Projectile proj = col.GetComponent<Projectile>();
		if(unit == null && proj == null)
			return;
		if (unit != null)
		{
			if(moveThroughShield == null)
				return;
			if(unit.IsPlayer())
				Camera.main.audio.PlayOneShot(moveThroughShield);
			else
				unit.audio.PlayOneShot(moveThroughShield);
			return;
		}
		if (proj != null)
		{
			if(shieldHit != null)
				audio.PlayOneShot(shieldHit);
			if (proj.owner == null || proj.owner.GetCommander() == shield.team)
			{
				return;
			}
			else
			{
				shield.Damage(proj.damage);
			}
			return;
		}
	}
}
