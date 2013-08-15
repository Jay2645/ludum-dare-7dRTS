using UnityEngine;
using System.Collections;

public class Healthpack : MonoBehaviour {

	private const float ROTATION_SPEED = 15.0f;
	private const float RESPAWN_TIME = 15.0f;
	private const int RESTORE_AMOUNT = 75;
	private bool isEnabled = true;
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 rot = transform.rotation.eulerAngles;
		rot.y += Time.deltaTime * ROTATION_SPEED;
		transform.rotation = Quaternion.Euler(rot);
	}
	
	void OnTriggerEnter(Collider col)
	{
		if(!isEnabled)
			return;
		Unit unit = col.GetComponent<Unit>();
		if(unit == null || !unit.IsAlive())
			return;
		float hpPercent = unit.GetHealthPercent();
		Debug.Log(unit+": "+hpPercent);
		if(hpPercent >= 100 || !unit.RestoreHealth(RESTORE_AMOUNT))
			return;
		isEnabled = false;
		tag = "Untagged";
		Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
		/*foreach(Renderer render in renderers)
		{
			render.enabled = false;
		}*/
		renderer.enabled = false;
		Invoke("Respawn", RESPAWN_TIME);
	}
	
	private void Respawn()
	{
		Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
		/*foreach(Renderer render in renderers)
		{
			render.enabled = true;
		}*/
		renderer.enabled = true;
		tag = "Regen";
		isEnabled = true;
	}
}
