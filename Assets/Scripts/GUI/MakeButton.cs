using UnityEngine;
using System.Collections;

/// <summary>
/// Gives button functionality to a PhysicalText object.
/// </summary>
public class MakeButton : MonoBehaviour {
	
	public BoxCollider col;
	
	bool mouseIn = false;
	bool _mouseIn = false;
	bool clicked = false;
	public bool disableClick = false;
	public AudioSource rollover;
	public AudioSource click;
	
	public Color normalColor;
	public Color rolloverColor;
	public Color clickedColor;

	private bool isLerping;
	private Color colorLerpTo;
	private int mouseOutFrames = 0;
	private float lerpTime = 1;
	
	public Renderer render;
	
	public PhysicalText text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
			normalColor = _text.buttonNormalColor;
			rolloverColor = _text.buttonHoverColor;
			clickedColor = _text.buttonClickedColor;
		}
	}
	private PhysicalText _text;

	void OnMouseEnter()
	{
		mouseIn = true;
		if(_mouseIn)
			return;
		lerpTime = 0;
		_mouseIn = true;
		if(rollover != null)
			rollover.Play();
	}
	
	void OnMouseExit()
	{
		mouseIn = false;
	}
	
	void OnMouseUp()
	{
		if(_mouseIn)
		{
			clicked = true;
			if(click != null)
				click.Play();
			ClicktimeEvent();
		}
	}
	
	// Use this for initialization
	void Start () 
	{
		col = gameObject.GetComponent<BoxCollider>();
		if(col == null)
			col = gameObject.AddComponent<BoxCollider>();
		//col.size = size;
		//col.center = size / 2;
		//col.isTrigger = true;
		render = gameObject.GetComponentInChildren<Renderer>();
	}
	
	public bool LerpColor(Color lerpTo)
	{
		if(lerpTime > 1)
		{
			isLerping = false;
			return true;
		}
		colorLerpTo = lerpTo;
		if(text == null)
		{
			render.material.color = Color.Lerp(render.material.color,lerpTo,lerpTime);
		}
		else
		{
			_text.color = Color.Lerp(_text.color,lerpTo,lerpTime);
		}
		isLerping = true;
		lerpTime += Time.deltaTime;
		return false;
	}
	
	void Update()
	{
		if(isLerping)
			LerpColor(colorLerpTo);
		if(disableClick)
			return;
		if(_mouseIn && !mouseIn)
		{
			mouseOutFrames++;
			if(mouseOutFrames >= 5)
			{
				_mouseIn = false;
				lerpTime = 0;
			}
		}
		else if(mouseIn)
			mouseOutFrames = 0;
		if(_mouseIn)
		{
			LerpColor(rolloverColor);
		}
		else
		{
			if(clicked)
				LerpColor(clickedColor);
			else
				LerpColor(normalColor);
		}
	}
	
	private void ClicktimeEvent()
	{
		if(text != null)
			_text.ButtonClicked();
	}
}
