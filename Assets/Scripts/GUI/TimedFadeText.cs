//  TimedFadeText.cs
//  From the Unity Wiki
//  Use with MessageList.cs
//  Use on a GUIText Object saved as a PreFab
//  This PreFab will be instantiated by MessageList.cs
//  Check the GUIText Object settings for alignment
//  Check the MessageList.cs settings for placement on screen
//  Based on the work of capnbishop
//  Conversion to csharp by CorrodedSoul
using UnityEngine;
using System.Collections;

[RequireComponent (typeof(GUIText))]
public class TimedFadeText : MonoBehaviour
{
	
	public float lifeTime = 5.0f;			// The number of seconds the GUIText will last before starting to fade
	public float fadeTime = 2.0f;			// The number of seconds to fade until totally transparent
	
	private float _time = 0.0f;				// Static var to track how much time has passed
	private bool _isFading = false;			// Static var to track if we're in the fading stage
	private float _startAlpha = 1.0f;		// Static var to keep track of the initial amount of alpha
	private GUIText _guiText;
//	private float _timePercent;
	
	void Start ()
	{
		
		// This script uses the GUIText's material to set the alpha fade.
		// If the font doesn't have a material, then this won't work, so disable the script.
		_guiText = this.gameObject.GetComponent<GUIText> ();
		if (_guiText.material == null) {
			Debug.LogWarning ("GUIText material missing");	//Changed the comment from original
			enabled = false;
		}
		
		// Get the starting alpha value.
		// If the developer has the text start transparent, then we need to fade from that point.
		_startAlpha = _guiText.material.color.a;
	}
	
	void Update ()
	{
		_time += Time.deltaTime;
		
		if (_isFading) {
			//  We're in the fading stage. If we've reached the end of this stage, then destroy the gameObject.
			if (_time >= fadeTime) {
				Destroy (gameObject);
			}
			
			//  We're still fading, so update the material's alpha color to make it fade a little more.
			_guiText.material.color = new Color (_guiText.material.color.r, _guiText.material.color.g, _guiText.material.color.b, CalcutateAlpha ());
			
		} else if (_time >= lifeTime) {
			//  If we're not fading yet, but should be, then update our values to proceed to the fading stage.
			_isFading = true;
			_time = 0.0f;
		}
		
		//  If we're not fading yet, and don't need to be yet, then nothign will happen at this point. The
		//  text will just exist, and the timer will keep incrementing until there's a state change.
	}
	
	private float CalcutateAlpha ()
	{
		float timePercent;
		float smoothAlpha;
		
		timePercent = Mathf.Clamp01 ((fadeTime - _time) / fadeTime);
		smoothAlpha = Mathf.SmoothStep (0.0f, _startAlpha, timePercent);
		
		return smoothAlpha;
		
	}
}