using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Struct representing one layer from Photoshop.
/// </summary>
public struct PhotoshopLayer
{
	public Texture texture;
	public string name;
	public int x;
	public int y;
	public int layer;
	public Vector2 relativeSize;
	public Vector2 relativeLocation;
	public Vector3 realWorldTopLeftBounds;
	public Vector3 realWorldBottomRightBounds;
	public PhysicalText text;
	public Camera guiCamera;
}

/// <summary>
/// Takes a .csv file and a folder full of exported layers from Photoshop and arranges them into a 2.5D GUI that Unity plays nice with.
/// </summary>
public class PhotoshopGUIMaker : MonoBehaviour {
	
	public Camera guiCamera;
	public string guiPath = "";
	public static int guiDepth = 0;
	private int thisDepth;
	public Texture[] textures;
	public bool render = false;
	
	private PhotoshopLayer[] photoshopLayers;
	private GameObject[] guiGOs;
	private PhysicalText[] guiLayers;
	//Size of the largest element in the GUI, in pixels.
	private Vector2 guiSize = Vector2.zero;
	
	// Use this for initialization
	void Start () 
	{
		bool hasCamera = guiCamera != null;
		if(!hasCamera)
			MakeCamera();
		if(photoshopLayers == null || photoshopLayers.Length == 0)
		{
			if(textures == null || textures.Length == 0)
				SetPath(guiPath);
			else
				SetTextures(textures);
		}
		if(!hasCamera)
			guiDepth++;
	}
	
	private void MakeCamera()
	{
		if(guiCamera == null)
			guiCamera = gameObject.GetComponentInChildren<Camera>();
		if(guiCamera == null)
		{
			guiCamera = gameObject.AddComponent<Camera>();
		}
		guiCamera.clearFlags = CameraClearFlags.Depth;
		thisDepth = guiDepth;
		guiCamera.depth = thisDepth;
	}
	
	public void SetPath(string path)
	{
		guiPath = path;
		if(path == "")
			return;
		if(guiCamera == null)
			MakeCamera();
		SetTextures(ExternalDL.LoadTexturesFromPath(guiPath));
	}
	
	public void SetTextures(Texture[] texs)
	{
		textures = texs;
		if(textures == null)
			return;
		if(guiCamera == null)
			MakeCamera();
		ArrangeLayers();
		for(int i = 0; i < photoshopLayers.Length; i++)
		{
			int l = photoshopLayers[i].layer;
			string layerName = photoshopLayers[i].name;
			PhysicalText text = PlaceImagePlane(photoshopLayers[i]);
			photoshopLayers[i].text = text;
			photoshopLayers[i].guiCamera = guiCamera;
			guiGOs[l] = text.text;
			guiGOs[l].name = layerName;
		}
		if(render)
			guiCamera.depth = guiDepth;
		else
			guiCamera.depth = -100;
	}
	
	public void ShowGUI()
	{
		Render (true);
	}
	
	public void HideGUI()
	{
		Render (false);
	}
	
	public void Render(bool show)
	{
		render = show;
		if(render)
			guiCamera.depth = thisDepth;
		else
			guiCamera.depth = -100;
	}
	
	private void ArrangeLayers()
	{
		guiLayers = new PhysicalText[textures.Length];
		guiGOs = new GameObject[textures.Length];
		photoshopLayers = new PhotoshopLayer[textures.Length];
		bool[] groups = new bool[textures.Length];
		
		// Assign each texture to a PhotoshopLayer struct:
		for(int i = 0; i < textures.Length; i++)
		{
			Texture tex = textures[i];
			float width = tex.width;
			float height = tex.height;
			// Work out GUI size:
			if(width > guiSize.x || height > guiSize.y)
				guiSize = new Vector2(width,height);
			// Work out layer name from filename:
			string texName = tex.name;
			string[] nameValues = texName.Split('_');
			gameObject.name = nameValues[0];
			// *Should* be 3-4 values, in format "FileName_0000_Layer-Name" or "FileName_0000s_0000_Layer-Name":
			groups[i] = false;
			if(nameValues.Length > 3)
			{
				texName = nameValues[3];
				groups[i] = true;
			}
			else if(nameValues.Length > 2)
				texName = nameValues[2];
			else // Wrong format:
				texName = nameValues[0];
			photoshopLayers[i] = new PhotoshopLayer();
			photoshopLayers[i].name = texName.ToUpper();
			photoshopLayers[i].texture = tex;
			photoshopLayers[i].layer = i; //Should be overwritten; if not something has gone wrong.
		}
		
		// Look for the data file containing XY and Layer info:
		guiPath = Application.dataPath+"\\"+guiPath;
		if(!Directory.Exists(guiPath))
			return;
		DirectoryInfo folder = new DirectoryInfo(guiPath);
		FileInfo[] files = folder.GetFiles();
		StreamReader reader = null;
		foreach(FileInfo file in files)
		{
			string extension = file.Extension.ToLower();
			if(!extension.Equals(".txt"))
				continue;
			string url = file.FullName;
			reader = new StreamReader(url);
			break;
		}
		if(reader == null)
			return;
		
		//Found the data file, read the CSV data:
		string line;
		int counter = 0;
		List<PhotoshopLayer> layerList = new List<PhotoshopLayer>();
		while((line = reader.ReadLine()) != null)
		{
			line = line.ToUpper();
			PhotoshopLayer layer = new PhotoshopLayer();
			if(line.Contains("GROUP"))
			{
				//Grouping code here.
				continue;
			}
			else
			{
				string[] values = line.Split(',');
				layer.name = values[0].Replace(' ','-');
				try
				{
					layer.x = int.Parse(values[1]);
					layer.y = int.Parse(values[2]);
				}
				catch(Exception){}
				layer.layer = counter;
				counter++;
				layerList.Add(layer);
			}
		}
		
		// Match the CSV Layers to the main texture structs:
		foreach(PhotoshopLayer csvLayer in layerList.ToArray())
		{
			for(int i = 0; i < photoshopLayers.Length; i++)
			{
				//Name capitalization should be the same:
				if(csvLayer.name != photoshopLayers[i].name)
					continue;
				photoshopLayers[i].x = csvLayer.x;
				photoshopLayers[i].y = csvLayer.y;
				photoshopLayers[i].layer = csvLayer.layer;
				if(!groups[i])
					continue;
			}
		}
		
		// Log relative size and location of each layer:
		for(int i = 0; i < photoshopLayers.Length; i++)
		{
			Texture texture = photoshopLayers[i].texture;
			if(texture == null)
				continue;
			float xLoc = (float)photoshopLayers[i].x;
			float yLoc = (float)photoshopLayers[i].y;
			photoshopLayers[i].relativeSize = new Vector2((float)texture.width / (float)guiSize.x,(float)texture.height / (float)guiSize.y);
			photoshopLayers[i].relativeLocation = new Vector2(xLoc / (float)guiSize.x, 1 - (yLoc / (float)guiSize.y));
		}
	}
	
	private PhysicalText PlaceImagePlane(PhotoshopLayer pLayer)
	{
		//Store some reference variables:
		Texture image = pLayer.texture;
		int layer = pLayer.layer;
		string iName = pLayer.name;
		Debug.LogWarning(iName);
		Vector2 imageSize = new Vector2(image.width,image.height);
		
		// Protect for a not-set GUI size (dividing by 0):	
		if(imageSize.x > guiSize.x || imageSize.y > guiSize.y)
			guiSize = imageSize;
		
		//Work out how big to make the image, based upon its location and size relative to the GUI:
		Vector2 scaledSize = pLayer.relativeSize;
		//Debug.Log ("Size: "+imageSize+", scaled size: "+scaledSize+", GUI Size: "+guiSize);
		Vector2 scaledLocation = pLayer.relativeLocation;
		//Debug.Log ("Location: "+location+", scaled location: "+scaledLocation+", layer: "+layer);
		
		//Work out how far away to place the image, to create layers:
		float distanceFromCamera = 50.0f + layer;

		//Create the physical representation of the texture in the game world:
		Vector3 center = new Vector3(scaledLocation.x + (scaledSize.x / 2), scaledLocation.y - (scaledSize.y / 2), distanceFromCamera);
		Vector3 position = guiCamera.ViewportToWorldPoint(center);
		//Debug.Log("Viewport position: "+center+", real world position: "+position);
		PhysicalText text = new PhysicalText(position);
		text.planeSize = 0.5f;
		Vector3 viewportTopLeft = new Vector3(scaledLocation.x,scaledLocation.y,distanceFromCamera);
		Vector3 viewportBottomRight = new Vector3(scaledLocation.x+scaledSize.x,scaledLocation.y-scaledSize.y,distanceFromCamera);
		//Debug.Log ("Max top left bounds (viewport): "+viewportTopLeft.x+", "+viewportTopLeft.y);
		//Debug.Log ("Max bottom right bounds (viewport): "+viewportBottomRight.x+", "+viewportBottomRight.y);
		Vector3 topLeftBounds = guiCamera.ViewportToWorldPoint(viewportTopLeft);
		Vector3 bottomRightBounds = guiCamera.ViewportToWorldPoint(viewportBottomRight);
		//Debug.Log ("Max top left bounds (real): "+topLeftBounds);
		//Debug.Log ("Max bottom right bounds (real): "+bottomRightBounds);
		float width = Mathf.Abs(topLeftBounds.x - bottomRightBounds.x);
		float height = Mathf.Abs(topLeftBounds.y - bottomRightBounds.y);
		//Debug.Log ("Real world width: "+width+", real world height: "+height);
		GameObject primitive;
		//Check to see if the texture is designated as a "special texture" (i.e. text):
		if(iName.Contains("^"))
		{
			text.textString = iName.Replace("^","");
			text.SetMaxBounds(width, height);
			primitive = text.text;
			primitive.transform.position = topLeftBounds;
			text.color = Color.black;
		}
		else
		{
			text.texture = image;
			primitive = text.text;
			primitive.transform.localEulerAngles = new Vector3(
						primitive.transform.localEulerAngles.x, 
						primitive.transform.localEulerAngles.y + 180, 
						primitive.transform.localEulerAngles.z + 90);
			primitive.transform.localScale = new Vector3(height,width,1);
			primitive.transform.position = position;
		}
		guiLayers[layer] = text;
		pLayer.text = text;
		pLayer.realWorldBottomRightBounds = bottomRightBounds;
		pLayer.realWorldTopLeftBounds = topLeftBounds;
		primitive.name = "GUI Element "+image.name+", layer "+layer;
		return text;
	}
	
	// Legacy:
	[Obsolete("PlaceImagePlane(Texture, int, int, int) is deprecated, please use PlaceImagePlane(PhotoshopLayer) instead.")]
	public GameObject PlaceImagePlane(Texture image, int imageX, int imageY, int layer)
	{
		if(image == null)
		{
			GameObject primitive = PrimitiveMaker.MakePlane(0.5f);
			guiLayers[layer] = null;
			Vector2 location = new Vector2(imageX, imageY);
			Vector2 scaledLocation = new Vector2(location.x / guiSize.x, location.y / guiSize.y);
			float distanceFromCamera = 50.0f + layer;
			Vector3 position = guiCamera.ViewportToWorldPoint(new Vector3(
										scaledLocation.x,
										scaledLocation.y,
										distanceFromCamera));
			primitive.transform.position = position;
			Vector3 topLeftBounds = guiCamera.ViewportToWorldPoint(new Vector3(0,1,distanceFromCamera));
			Vector3 bottomRightBounds = guiCamera.ViewportToWorldPoint(new Vector3(1,0,distanceFromCamera));
			float x = Mathf.Abs(topLeftBounds.x - bottomRightBounds.x);
			float y = Mathf.Abs(topLeftBounds.y - bottomRightBounds.y);
			primitive.transform.localScale = new Vector3(x,y,1);
			primitive.transform.localEulerAngles = new Vector3(primitive.transform.localEulerAngles.x, primitive.transform.localEulerAngles.y + 180, primitive.transform.localEulerAngles.z);
			primitive.name = "Photoshop GUI Element, layer "+layer;
			return primitive;
		}
		PhotoshopLayer pLayer = new PhotoshopLayer();
		pLayer.texture = image;
		pLayer.x = imageX;
		pLayer.y = imageY;
		pLayer.layer = layer;
		pLayer.name = image.name;
		return PlaceImagePlane(pLayer).text;
	}
	
	public PhysicalText FindElement(string layerName)
	{
		layerName = layerName.ToUpper();
		//First check to see if we have it as a PhysicalText:
		for(int i = 0; i < photoshopLayers.Length; i++)
		{
			if(photoshopLayers[i].name == layerName)
				return guiLayers[i];
		}
		//Then see if we can find a partial match:
		PhysicalText matched = null;
		for(int i = 0; i < photoshopLayers.Length; i++)
		{
			if(photoshopLayers[i].name.Contains(layerName))
			{
				Debug.Log ("Found partial match for "+layerName+": "+photoshopLayers[i].name);
				if(matched == null)
					matched = guiLayers[i];
			}
		}
		//We did all we could:
		return matched;
	}
	
	public PhotoshopLayer[] FindLayersWildCard(string partialLayerName)
	{
		partialLayerName = partialLayerName.ToUpper();
		List<PhotoshopLayer> found = new List<PhotoshopLayer>();
		int indexOfWildcard = partialLayerName.IndexOf("*");
		foreach(PhotoshopLayer layer in photoshopLayers)
		{
			if(indexOfWildcard < 0 || partialLayerName.StartsWith("*") && partialLayerName.EndsWith("*"))
			{
				string substring = partialLayerName.Substring(1,partialLayerName.Length - 2);
				if(layer.name.Contains(substring))
				{
					//Debug.LogError(layer.name+", partial match for "+partialLayerName);
					found.Add(layer);
				}
			}
			else if(partialLayerName.StartsWith("*"))
			{
				string substring = partialLayerName.Substring(1,partialLayerName.Length - 1);
				if(layer.name.EndsWith(substring))
				{
					found.Add(layer);
				}
			}
			else if(partialLayerName.EndsWith("*"))
			{
				string substring = partialLayerName.Substring(0,partialLayerName.Length - 1);
				if(layer.name.EndsWith(substring))
				{
					found.Add(layer);
				}
			}
			else
			{
				string[] substrings = partialLayerName.Split('*');
				foreach(string substring in substrings)
				{
					if(layer.name.Contains(substring) && !found.Contains(layer))
					{
						found.Add(layer);
					}
				}
			}
		}
		return found.ToArray();
	}
	
	public PhotoshopLayer FindLayer(string layerName)
	{
		layerName = layerName.ToUpper();
		foreach(PhotoshopLayer layer in photoshopLayers)
		{
			//Debug.LogError(layer.name+", looking for "+layerName);
			if(layer.name == layerName)
				return layer;
		}
		return new PhotoshopLayer();
	}
}
