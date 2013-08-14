using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapView : MonoBehaviour {
	
	private static bool isShown = false;
	private static Transform commanderTransform;
	private Commander player;
	private const float MIN_BOX_SIZE = 2.0f;
	private Vector3 lastCursorPosition = Vector3.zero;
	private Rect selectionBox = new Rect(0,0,0,0);
	public Texture boxTexture = null;
	public float scrollSpeed = 2.0f;
	public float deadZone = 10.0f;
	private GUIStyle style;
	
	void Awake()
	{
		style = new GUIStyle();
		Color color = Color.white;
		color.a = 0.25f;
		Texture2D texture = new Texture2D(1,1);
		texture.SetPixel(1,1,color);
		texture.Apply();
		style.normal.background = texture;
	}
	
	// Update is called once per frame
	void LateUpdate () 
	{
		if(Input.GetButtonUp("ShowMap"))
		{
			ShowMap();
		}
		if(!isShown)
			return;
		float input = Input.GetAxis("Mouse ScrollWheel");
		if(input != 0)
		{
			camera.orthographicSize += (input * scrollSpeed * -2);
		}
		if(Input.GetButtonDown("Fire1"))
		{
			HandleLeftClickDown();
		}
		else if(Input.GetButtonUp("Fire1"))
		{
			HandleLeftClickUp();
		}
		else if(Input.GetButtonDown("Order"))
		{
			HandleRightClickDown();
		}
		
		if(Input.GetButton("Fire1"))
			return;
		
		Ray checkLegalMoveRay = new Ray(camera.ViewportToWorldPoint(new Vector3(0.1f,0.5f,0)),Vector3.down);
		RaycastHit hitInfo;
		bool canMoveLeft = Physics.Raycast(checkLegalMoveRay,out hitInfo,Mathf.Infinity);
		checkLegalMoveRay = new Ray(camera.ViewportToWorldPoint(new Vector3(0.9f,0.5f,0)),Vector3.down);
		bool canMoveRight = Physics.Raycast(checkLegalMoveRay,out hitInfo,Mathf.Infinity);
		checkLegalMoveRay = new Ray(camera.ViewportToWorldPoint(new Vector3(0.5f,0.1f,0)),Vector3.down);
		bool canMoveUp = Physics.Raycast(checkLegalMoveRay,out hitInfo,Mathf.Infinity);
		checkLegalMoveRay = new Ray(camera.ViewportToWorldPoint(new Vector3(0.5f,0.9f,0)),Vector3.down);
		bool canMoveDown = Physics.Raycast(checkLegalMoveRay,out hitInfo,Mathf.Infinity);
		
		Vector3 moveTo = gameObject.transform.position;
		float mPosX = Input.mousePosition.x;
		float mPosY = Input.mousePosition.y;
		if (mPosX < deadZone && canMoveLeft)
		{
			moveTo = moveTo + (Vector3.right * -scrollSpeed);
		}
		else if (mPosX >= Screen.width-deadZone && canMoveRight)
		{
			moveTo = moveTo + (Vector3.right * scrollSpeed);
		}
		if (mPosY < deadZone && canMoveUp)
		{
			moveTo = moveTo + (Vector3.forward * -scrollSpeed);
		}
		else if (mPosY >= Screen.height-deadZone && canMoveDown)
		{
			moveTo = moveTo + (Vector3.forward * scrollSpeed);
		}
		
		rigidbody.MovePosition(moveTo);
	}
	
	private void HandleLeftClickDown()
	{
		// start selection rectangle
		lastCursorPosition = Input.mousePosition;
		// *** Start drawing selection rectangle here *** //
	}
	
	void OnGUI() {
		if(!isShown)
			return;
		if(Input.GetButton("Fire1"))
		{
		 	Vector3 currentMousePosition = Input.mousePosition;
	     	//to start, we assume the user is dragging down and to the right, since this will always yield
	     	//positive width and height
	     	float x = lastCursorPosition.x;
	     	float y = lastCursorPosition.y;
	     	float width = currentMousePosition.x - lastCursorPosition.x;
	     	float height = (Screen.height - currentMousePosition.y) - (Screen.height - lastCursorPosition.y);
	     	//if the width is negative (user is dragging leftward), swap the x position and make the width positive
	     	if (width < 0)
	     	{
	     	   x = currentMousePosition.x;
	     	   width = Mathf.Abs(width);
	     	}
	     	//if the height is negative (user is draggin upward), swap the y position and make the height positive
	     	if (height < 0)
	     	{
	     	    y = currentMousePosition.y;
	     	    height = Mathf.Abs(height);
	     	}
	     	//set the rectangle based on the values
	     	selectionBox.x = x;
	     	selectionBox.y = Screen.height - y;
	     	selectionBox.width = width;
	     	selectionBox.height = height;
	
	        if(width > MIN_BOX_SIZE && height > MIN_BOX_SIZE)
			{
				//GUI.DrawTexture(selectionBox, boxTexture, ScaleMode.StretchToFill, true);
				GUI.Box(selectionBox,new GUIContent(),style);
			}
		}
	}

	private void HandleLeftClickUp()
	{
		Vector3 mpos = Input.mousePosition;
		Ray screenRay = camera.ScreenPointToRay(mpos);
		RaycastHit hitInfo;
		float dist = (lastCursorPosition - mpos).sqrMagnitude;

		// firstly, deselect everything before making our new selection
		if(player == null)
		{
			player = Commander.player;
			if(player == null)
				return;
		}
	 	player.Deselect();

		if (dist >= MIN_BOX_SIZE && lastCursorPosition != Vector3.zero) // big enough to warrant making a square and lastCursorPosition exists
		{
			SelectUnitsInRect(lastCursorPosition, mpos);
		}
		else
		{
			if(Physics.Raycast(screenRay, out hitInfo, Mathf.Infinity,player.raycastIgnoreLayers))
			{ 
				// something under our mouse right now.
				Unit unit = hitInfo.collider.GetComponent<Unit>();
				if(unit != null)
					player.SelectUnits(unit.GetID());
			}
		}
		
		// *** End drawing selection rectangle here *** //
	 }

	private void HandleRightClickDown()
	{
		// this chunk gets where the player's mouse is pointing on the movement plane
		if(player == null)
		{
			player = Commander.player;
			if(player == null)
				return;
		}
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity,player.raycastIgnoreLayers))
		{
			Unit enemyUnit = hitInfo.collider.gameObject.GetComponentInChildren<Unit>();
			if(enemyUnit != null)
			{
				if(enemyUnit.IsOwnedByPlayer())
					player.GiveOrder(Order.defend,enemyUnit.transform);
				else
					player.GiveOrder(Order.attack,enemyUnit.transform);
			}
			else
			{
				player.GiveOrder(player.GetCurrentOrder(),hitInfo.point);
			}
		}
	}
	
	private void SelectUnitsInRect(Vector3 corner1, Vector3 corner2)
	{
		Unit[] possible = (Unit[])GameObject.FindObjectsOfType(typeof(Unit));
		List<int> actualList = new List<int>();
		foreach(Unit unit in possible)
		{
			// the way to select units is to turn their world coords into screen coords and see
			// if they exist within the selection rect.
			Vector3 unitScreenPos = camera.WorldToScreenPoint(unit.transform.position);
			float lx = Mathf.Min(corner1.x, corner2.x);
			float ly = Mathf.Min(corner1.y, corner2.y);
			float bx = Mathf.Max(corner1.x, corner2.x);
			float by = Mathf.Max(corner1.y, corner2.y);
			if (unitScreenPos.x > lx && unitScreenPos.y > ly && unitScreenPos.x < bx && unitScreenPos.y < by)
			{
				actualList.Add(unit.GetID());
			}
		}
		int[] actual = actualList.ToArray();
		if(actual.Length == 0)
			return;
		player.SelectUnits(actual);
	}
	
	public void SetCommander(Commander commander)
	{
		if(commander == null)
		{
			commanderTransform = null;
			return;
		}
		commanderTransform = commander.transform;
		player = commander;
		Vector3 newPosition = transform.position;
		newPosition.x = commanderTransform.position.x;
		newPosition.z = commanderTransform.position.z;
		transform.position = newPosition;
	}
	
	private void ShowMap()
	{
		if(camera.depth == 1)
		{
			camera.depth = -1;
			Screen.showCursor = false;
		}
		else
		{
			if(commanderTransform != null)
			{
				Vector3 camPosition = new Vector3(commanderTransform.position.x,transform.position.y,commanderTransform.position.z);
				transform.position = camPosition;
			}
			camera.depth = 1;
			Screen.showCursor = true;
		}
		isShown = camera.depth == 1;
	}
	
	public static bool IsShown()
	{
		return isShown;
	}
	
}
