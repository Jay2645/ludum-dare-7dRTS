using UnityEngine;
using System.Collections;

public class PrimitiveMaker {
	
	private static int planeID = 0;
	
	public static GameObject MakePlane(float size)
	{
		return MakePlane(new Vector2(size,size));
	}
	
	public static GameObject MakePlane(Vector2 size)
	{
		float x = size.x;
		float y = size.y;
		Mesh m = new Mesh();
	    m.name = "Plane "+planeID;
	    m.vertices = new Vector3[]
		{
			new Vector3(-x, -y, 0.01f), 
			new Vector3(x, -y, 0.01f), 
			new Vector3(x, y, 0.01f), 
			new Vector3(-x, y, 0.01f)
		};
	    m.uv = new Vector2[]{new Vector2 (0, 0), new Vector2 (0, 1), new Vector2(1, 1), new Vector2 (1, 0)};
	    m.triangles = new int[]{0, 1, 2, 0, 2, 3};
	    m.RecalculateNormals();
	    GameObject obj = new GameObject("Plane "+planeID);
		obj.AddComponent<MeshRenderer>();
		obj.AddComponent<MeshCollider>();
	    obj.AddComponent<MeshFilter>().mesh = m;
		planeID++;
		return obj;
	}
}
