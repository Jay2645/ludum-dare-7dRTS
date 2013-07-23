using UnityEngine;
using System.Collections;

public class ObjectLabel : MonoBehaviour 
{
	public string label = "";
	public Transform target;  // Object that this label should follow
	public Vector3 offset = Vector3.up;    // Units in world space to offset; 1 unit above object by default
	public bool clampToScreen = false;  // If true, label will be visible even if object is off screen
	public float clampBorderSize = 0.05f;  // How much viewport space to leave at the borders when a label is being clamped
	public bool useMainCamera = true;   // Use the camera tagged MainCamera
	public Camera cameraToUse ;   // Only use this if useMainCamera is false
	Camera cam ;
	Transform thisTransform;
	Transform camTransform;
 	private PhysicalText text = null;
	private Transform textTransform = null;
	void Awake () 
    {
	    thisTransform = transform;
	    if (useMainCamera)
	        cam = Camera.main;
	    else
	        cam = cameraToUse;
	    camTransform = cam.transform;
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
	}
	
	void Update()
	{
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