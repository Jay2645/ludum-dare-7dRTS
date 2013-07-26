using UnityEngine;
using System.Collections;

/// <summary>
/// Places a label above a unit's head.
/// From the Unity Wiki.
/// </summary>
public class ObjectLabel : MonoBehaviour 
{
	public string label = "";
	public Transform target;  // Object that this label should follow
	public Vector3 offset = Vector3.up;    // Units in world space to offset; 1 unit above object by default
	public Camera cameraToUse;
	Transform thisTransform;
	Transform camTransform;
 	private PhysicalText text = null;
	private Transform textTransform = null;
	private float _yScale = 0.0f;
	private float _xScale = 0.0f;
	private float _zScale = 0.0f;
	public bool isLookedAt = false;
	
	void OnEnable () 
    {
	    thisTransform = transform;
	    if (cameraToUse == null)
	        cameraToUse = Camera.main;
	    camTransform = cameraToUse.transform;
		if(target == null)
			target = thisTransform;
		if(target != thisTransform)
		{
			GameObject targetGO = new GameObject("Object Label Target");
			targetGO.transform.position = target.position;
			targetGO.transform.parent = target;
			target = targetGO.transform;
		}
		if(text == null)
		{
			text = new PhysicalText(target);
		}
		text.textString = label;
		textTransform = text.text.transform;
		textTransform.position = target.position + offset;
		textTransform.parent = target;
		target.localScale = Vector3.zero;
	}
	
	void Update()
	{
		if(!isLookedAt)
		{
			if(_yScale > 0)
			{
				_xScale = Mathf.Lerp(_xScale,0,Time.deltaTime * 5);
				_yScale = Mathf.Lerp(_yScale,0,Time.deltaTime * 5);
				_zScale = Mathf.Lerp(_zScale,0,Time.deltaTime * 5);
			}
			else
			{
				target.localScale = Vector3.zero;
				return;
			}
		}
		else if (_yScale < 1)
		{
			_xScale = Mathf.Lerp(_xScale,1,Time.deltaTime * 5);
			_yScale = Mathf.Lerp(_yScale,1,Time.deltaTime * 5);
			_zScale = Mathf.Lerp(_zScale,1,Time.deltaTime * 5);
		}
		target.localScale = new Vector3(_xScale,_yScale,_zScale);
		textTransform.LookAt(camTransform);
		textTransform.Rotate(0,180,0);
	}
	
	public void SetLabelText(string newText)
	{
		label = newText;
		if(text != null)
			text.textString = label;
	}
	
	void OnDisable()
	{
		Destroy (text.text);
	}
}