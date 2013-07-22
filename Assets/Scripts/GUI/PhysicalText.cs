using UnityEngine;
using System.Collections;

public class PhysicalText
{
	public PhysicalText (Vector3 position)
	{
		GameObject parent = new GameObject("PhysicalText Parent");
		parent.transform.position = position;
		location = parent.transform;
		MakeTextObject();
	}
	
	public PhysicalText(Vector3 position, Color mainColor)
	{
		GameObject parent = new GameObject("PhysicalText Parent");
		parent.transform.position = position;
		location = parent.transform;
		MakeTextObject();
		color = mainColor;
		buttonNormalColor = mainColor;
	}
	
	public PhysicalText(Transform location)
	{
		this.location = location;
		MakeTextObject();
	}
	
	public PhysicalText(Transform location, Color mainColor)
	{
		this.location = location;
		MakeTextObject();
		color = mainColor;
		buttonNormalColor = mainColor;
	}
	
	public Transform location;
	public GameObject text;
	public TextMesh mesh;
	public float widthMod = 4;
	public Texture texture
	{
		get
		{
			if(mat == null)
				return null;
			return mat.mainTexture;
		}
		set
		{
			mat = new Material(Shader.Find("Unlit/Transparent ZWrite"));
			mat.mainTexture = value;
			MakeTextObject();
			MakeButton();
		}
	}
	public MeshRenderer render;
	
	public Font font
	{
		get
		{
			if(mesh == null)
				return null;
			return mesh.font;
		}
		set
		{
			MakeFontMaterial(value);
		}
	}
	public string textString
	{
		get
		{
			if(mesh == null)
				return "";
			return mesh.text;
		}
		set
		{
			if(mesh == null)
			{
				DestroyText();
				text = new GameObject();
				text.transform.parent = location;
				text.transform.localPosition = Vector3.zero;
				text.transform.localRotation = Quaternion.identity;
				mesh = text.AddComponent<TextMesh>();
				mesh.text = value;
				MakeTextObject();
			}
			else
				mesh.text = value;
			SetMaxBounds(maxWidth,maxHeight);
			text.name = "PhysicalText: "+value;
		}
	}
	public Color color
	{
		get
		{
			if(mat == null)
				return buttonNormalColor;
			return mat.color;
		}
		set
		{
			if(mat == null)
				buttonNormalColor = value;
			else
				mat.color = value;
		}
	}
	public Material mat;
	public float planeSize = 0.0125f;
	
	public bool makeButton = false;
	public Color buttonNormalColor = Color.white;
	public Color buttonHoverColor = Color.red;
	public Color buttonClickedColor = Color.white;
	private bool buttonClicked = false;
	private float maxWidth = Mathf.Infinity;
	private float maxHeight = Mathf.Infinity;
	
	private void MakeTextObject()
	{
		if(texture != null)
		{
			DestroyText();
			text = PrimitiveMaker.MakePlane(planeSize);
			text.transform.parent = location;
			text.transform.localPosition = Vector3.zero;
			text.transform.localRotation = Quaternion.identity;
			text.name = texture.name;
			render = text.GetComponent<MeshRenderer>();
			MakeTextureMaterial();
			mat.mainTexture = texture;
			render.material = mat;
		}
		else if(textString != "")
		{
			if(render == null)
				render = text.GetComponent<MeshRenderer>();
			if(render == null)
				render = text.AddComponent<MeshRenderer>();
			if(mesh == null)
				mesh = text.GetComponent<TextMesh>();
			if(mesh == null)
				mesh = text.AddComponent<TextMesh>();
			text.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
			if(font != null)
			{
				MakeFontMaterial(mesh.font);
			}
			mesh.richText = true;
			mesh.text = textString;
		}
		MakeButton(makeButton);
	}
	
	private void MakeTextureMaterial()
	{
		if(mat == null && texture == null)
			mat = new Material(Shader.Find("GUI/3D Text Shader"));
		else if(mat == null && texture != null)
		{	
			mat = new Material(Shader.Find("Unlit/Transparent ZWrite"));
			mat.mainTexture = texture;
		}
		color = buttonNormalColor;
		render.material = mat;
	}
	
	private void MakeFontMaterial(Font newFont)
	{
		mat = new Material(Shader.Find("GUI/3D Text Shader"));
		if(mesh != null)
		{
			if(newFont == null || newFont.name == "Arial")
			{
				newFont = Resources.Load("Fonts/arial") as Font;
			}
			mat.mainTexture = newFont.material.mainTexture;
			newFont.material = mat;
			mesh.font = newFont;
			if(render == null)
				return;
			render.material = mat;
		}
	}
	
	public void SetMaxBounds(float width, float height)
	{
		if(width == Mathf.Infinity && height == Mathf.Infinity)
			return;
		maxWidth = width;
		maxHeight = height;
		text.transform.localScale = Vector3.one;
		Vector3 boundsSize = render.bounds.size;
		//Pass 1: Maximize height:
		float hScale = height / boundsSize.y;
		Vector3 heightScale = new Vector3(hScale,hScale, 1);
		text.transform.localScale = heightScale;
		// Check to see if width is within acceptable limits:
		boundsSize = render.bounds.size;
		if(width >= boundsSize.x)
			return;
		
		//Pass 2: Maximize width:
		float wScale = width / boundsSize.x;
		Vector3 widthScale = new Vector3(wScale,wScale, 1);
		//Scale the height by the width:
		Vector3 scale = Vector3.Scale(heightScale, widthScale);
		text.transform.localScale = scale;
		
		//Check to see if we need to knock the text onto multiple lines:
		if(wScale > 0.4f || !textString.Contains(" "))
			return;
		int numLines;
		if(wScale < 0.005f)
			numLines = Mathf.RoundToInt((wScale * 1000f)* widthMod * 10);
		else if(wScale < 0.05f)
			numLines = Mathf.RoundToInt((wScale * 100f)*widthMod);
		else if(wScale > 0.3 && wScale < 0.05)
			numLines = 2;
		else
			numLines = Mathf.RoundToInt(wScale * 10f);
		Debug.Log (wScale);
		string newString = textString;
		int remaining = newString.Length;
		//Check to see if we've already split us onto the required number of lines:
		if(numLines <= newString.Split('\n').Length)
			return;
		//if(numLines > 2)
		//	numLines --;
		//Max number of characters on one line:
		int charLimit = Mathf.CeilToInt((float)remaining / (float)numLines);
		while(charLimit < 16)
		{
			numLines--;
			charLimit = Mathf.CeilToInt((float)remaining / (float)numLines);
		}
		Debug.Log (text.name+" requires "+numLines+" lines.");
		Debug.Log ("Characrer limit:" +charLimit);
		//Format our string to make it into a single-line string:
		newString = newString.Replace("\n", " ");
		newString = newString.Replace("  ", " ");
		//numLines++; //Accomodate the inevitable overrun we'll encounter.
		string[] lines = new string[numLines];
		int charIndex = 0;
		for (int i = 0; i < numLines; i++)
		{
			if(remaining <= 1)
				break;
			string splitString;
			if(numLines - 1 == i) //Make sure we don't make too many lines, even if it means going over character limits:
			{
				splitString = newString.Substring(charIndex);
				lines[i] = splitString.Trim();
				continue;
			}
			//Split along the maximum number of characters:
			string subString = newString.Substring(charIndex, Mathf.Min(charLimit,remaining));
			//Trim our substring to end after the last space:
			int indexOfLastSpace = subString.LastIndexOf(' ');
			if(indexOfLastSpace > -1)
				splitString = subString.Substring(0,subString.LastIndexOf(' ')+1);
			else
				splitString = subString;
			remaining -= splitString.Length;
			charIndex += splitString.Length;
			lines[i] = splitString.Trim() +"\n";
		}
		newString = "";
		foreach(string s in lines)
		{
			newString = newString+s;
		}
		Debug.Log("Split "+text.name+"'s string into:\n"+newString);
		textString = newString; //This will auto-resize us.
	}
	
	public void MakeButton()
	{
		MakeButton(true);
	}
	
	public void UnMakeButton()
	{
		MakeButton(false);
	}
	
	public void MakeButton(bool make)
	{
		if(text == null || (makeButton == make && text.GetComponent<MakeButton>() != null))
			return;
		makeButton = make;
		if(makeButton)
		{
			MakeButton button = text.AddComponent<MakeButton>();
			button.text = this;
			button.clickedColor = buttonClickedColor;
			button.normalColor = buttonNormalColor;
			button.rolloverColor = buttonHoverColor;
		}
		else
		{
			MakeButton button = text.GetComponent<MakeButton>();
			if(button == null)
				return;
			button.disableClick = true;
			button.LerpColor(buttonNormalColor);
			MonoBehaviour.Destroy(button,1.25f);
			button = null;
		}
	}
	
	public void ButtonClicked()
	{
		buttonClicked = true;
	}
	
	public bool IsButtonClicked()
	{
		if(buttonClicked)
		{
			buttonClicked = false;
			return true;
		}
		return false;
	}
	
	public void DestroyText()
	{
		if(text != null)
			MonoBehaviour.DestroyImmediate(text);
	}
}
