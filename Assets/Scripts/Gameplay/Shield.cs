using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{

	public Objective powerSource;
	public Commander team;
	private Material shieldMat;
	private float changeSpeed = 1.0f;
	private Texture texture;
	public Collider[] colliders;
	public AudioClip shieldHit;
	public AudioClip shieldDown;
	public AudioClip shieldRespawn;
	public AudioClip moveThroughShield;
	public int health = 1000;
	private int _maxHealth = 1000;
	private const float RESPAWN_TIME = 15.0f;
	private float maxStrength;
	private Color color;
	private Color _initialColor;

	// Use this for initialization
	void Start()
	{
		_maxHealth = health;
		shieldMat = renderer.material;
		texture = shieldMat.GetTexture("_Texture");
		maxStrength = shieldMat.GetFloat("_Strength");
		if (team != null)
		{
			shieldMat.SetColor("_Color", team.teamColor);
		}
		color = shieldMat.GetColor("_Color");
		_initialColor = color;
		if (colliders == null)
		{
			List<Collider> colliderList = new List<Collider>();
			if (collider != null)
				colliderList.Add(collider);
			foreach (Transform child in transform)
			{
				if (child.GetComponent<Collider>() != null)
					colliderList.Add(child.GetComponent<Collider>());
			}
			colliders = colliderList.ToArray();
		}
		if (colliders != null)
		{
			foreach (Collider col in colliders)
			{
				col.gameObject.AddComponent<AudioSource>();
				ShieldWall wall = col.gameObject.AddComponent<ShieldWall>();
				wall.shieldHit = shieldHit;
				wall.moveThroughShield = moveThroughShield;
				wall.shield = this;
			}
		}
	}

	// Update is called once per frame
	void Update()
	{
		Vector2 offset = shieldMat.GetTextureOffset("_Texture");
		offset.y += shieldMat.GetFloat("_Speed");
		if (offset.y >= texture.width)
			offset.y = 0;
		//offset.x = -offset.y;
		shieldMat.SetTextureOffset("_Texture", offset);
	}

	void OnTriggerEnter(Collider col)
	{
		Debug.Log(col.name);
	}

	public void Damage(int amount)
	{
		health -= amount;
		if (health <= 0)
		{
			if (shieldDown != null)
			{
				if (audio != null)
					audio.PlayOneShot(shieldDown);
				Camera.main.audio.PlayOneShot(shieldDown);
			}
			renderer.enabled = false;
			if (colliders != null)
			{
				foreach (Collider col in colliders)
				{
					col.enabled = false;
				}
			}
			Invoke("Respawn", RESPAWN_TIME);
		}
		else if (team != null)
		{
			float percent = (float)health / (float)_maxHealth;
			float strength = percent * maxStrength;
			Debug.Log(percent);
			shieldMat.SetFloat("_Strength", strength);
			if (team.tag == "Blue")
			{
				int modAmount = Mathf.CeilToInt(percent * 255);
				//color.b -= modAmount;
				color.r += modAmount;
				shieldMat.SetColor("_Color", color);
			}
			else if (team.tag == "Red")
			{
				//color.r -= modAmount;
				color.b = 1.0f - percent;
				Debug.Log(color);
				shieldMat.SetColor("_Color", color);
			}
		}
	}

	public void Respawn()
	{
		health = _maxHealth;
		if (shieldRespawn != null)
		{
			if (audio != null)
				audio.PlayOneShot(shieldRespawn);
			Camera.main.audio.PlayOneShot(shieldRespawn);
		}
		renderer.enabled = true;
		if (colliders != null)
		{
			foreach (Collider col in colliders)
			{
				col.enabled = true;
			}
		}
		color = _initialColor;
		shieldMat.SetColor("_Color", color);
		shieldMat.SetFloat("_Strength", maxStrength);
	}
}
