//  MessageList.cs
//  From the Unity Wiki
//  Use with TimedFadeText.cs
//  Attach to an emtpy Game Object
//  Based on the work of capnbishop
//  Conversion to csharp by CorrodedSoul
using UnityEngine;
using System.Collections;
using System.Collections.Generic;//needed to replace the Javascript arrays with List<>.

public class MessageList : MonoBehaviour
{
	public GUIText messagePrefab;									//The prefab for our text object;
	
	public float lineSize = 20.0f;									//Pixel spacing between lines;
	public Vector3 startingPos = new  Vector3 (20, 20, 0);
	public int layerTag = 0;
	public bool insertAbove = true;
	private List<GUIText> _messages;								//Using a List<> instead of a JS dynamic array
	private float _directionFactor = 1.0f;
	
#region Singleton
	/// <summary>
	///   Provide singleton support for this class.
	///   The script must still be attached to a game object, but this will allow it to be called
	///   from anywhere without specifically identifying that game object.
	/// </summary>
	private static MessageList instance;
	
	public static MessageList Instance {
		get {
			if (instance == null) {
				instance = (MessageList)FindObjectOfType (typeof(MessageList));
				if (instance == null)
					Debug.LogError ("There needs to be one active MessageList script on a GameObject in your scene.");
			}
			return instance;
		}
	}
#endregion
	
	void Awake ()
	{
		// First make sure we have a prefab set. If not, disable the script
		if (messagePrefab == null) {
			enabled = false;
			Debug.LogWarning ("Must set the GUIText prefab for MessageList");
		}
		
		if (insertAbove) {
			_directionFactor = 1.0f;
		} else {
			_directionFactor = -1.0f;
		}
		_messages = new List<GUIText> ();
	}
	

/// <summary>
/// AddMessage() accepts a text value and adds it as a status message.
/// All other status messages will be moved along the y axis by a normalized distance of lineSize.
/// AddMessage() also handles automatic removing of any GUIText objects that automatically destroy
/// themselves.
/// </summary>
	public void AddMessage (string messageText)
	{
		GUIText[] currentMessages = _messages.ToArray();
		for(int i = 0; i < currentMessages.Length; i++)
		{
			if(currentMessages[i] == null)
			{
				_messages.TrimExcess();
				continue;
			}
			currentMessages[i].transform.position += new Vector3 (0, _directionFactor * (lineSize / Screen.height), 0);
		}
		GUIText newMessage;
		newMessage = Instantiate (messagePrefab, new Vector3 (startingPos.x / Screen.width, startingPos.y / Screen.height, startingPos.z), transform.rotation) as GUIText;
		newMessage.text = messageText;
		newMessage.gameObject.layer = layerTag;
		_messages.Add (newMessage);
	}
}