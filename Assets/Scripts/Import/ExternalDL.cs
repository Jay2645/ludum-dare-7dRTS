using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ExternalDL : MonoBehaviour {
	
	public Texture2D ImportTexture(string url)
	{
		Texture2D loadTexture = null;
		StartCoroutine(DownloadTexture(url,	texture => 
											loadTexture = texture));
		return loadTexture;
	}
	
	public static Texture[] LoadTexturesFromPath(string directory)
	{
		directory = Application.dataPath +"\\"+directory;
		string fileName = Path.GetFileNameWithoutExtension(directory);
		if(fileName != "")
		{
			Texture[] tex = new Texture[1];
			tex[0] = ImportTextureLocal(directory);
			tex[0].name = fileName;
			return tex;
		}
		if(!Directory.Exists(directory))
			return null;
		List<Texture> texList = LoadDirectoriesRecursive(directory,new List<Texture>());
		return texList.ToArray();
	}
	
	private static List<Texture> LoadDirectoriesRecursive(string folderPath, List<Texture> textures)
	{
		DirectoryInfo folder = new DirectoryInfo(folderPath);
		if(!folder.Exists)
			return textures;
		DirectoryInfo[] subfolders = folder.GetDirectories();
		FileInfo[] files = folder.GetFiles();
		foreach(FileInfo file in files)
		{
			string extension = file.Extension.ToLower();
			if(!extension.Equals(".png") && !extension.Equals(".jpg"))
				continue;
			string url = file.FullName;
			string name = Path.GetFileNameWithoutExtension(url);
			Texture tex = ImportTextureLocal(url);
			tex.name = name;
			textures.Add(tex);
		}
		foreach(DirectoryInfo subfolder in subfolders)
		{
			string fullName = subfolder.FullName;
			textures = LoadDirectoriesRecursive(fullName,textures);
		}
		return textures;
	}
	
	public static Texture2D ImportTextureLocal(string pathSource)
	{
		Texture2D tex = new Texture2D(4, 4);
		try
		{
	        using (FileStream fsSource = new FileStream(pathSource,
	            FileMode.Open, FileAccess.Read))
	        {
	            // Read the source file into a byte array. 
	            byte[] bytes = new byte[fsSource.Length];
	            int numBytesToRead = (int)fsSource.Length;
	            int numBytesRead = 0;
	            while (numBytesToRead > 0)
	            {
	                // Read may return anything from 0 to numBytesToRead. 
	                int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);
	
	                // Break when the end of the file is reached. 
	                if (n == 0)
	                    break;
	
	                numBytesRead += n;
	                numBytesToRead -= n;
	            }
				numBytesToRead = bytes.Length;
	
	            // Write the byte array.
				tex.LoadImage(bytes);
			}
	    }
	    catch (Exception ioEx)
	    {
				Debug.LogException(ioEx);
	    }
		return tex;
	}
	
	public static Dictionary<string,Texture2D> textureCache = new Dictionary<string, Texture2D>();
	
	private static IEnumerator DownloadTexture( string url, System.Action<Texture2D> result)
	{
		if(textureCache.ContainsKey(url))
		{
			Texture2D cachedValue = new Texture2D(2,2);
			if(textureCache.TryGetValue(url,out cachedValue))
				return cachedValue;
		}
		//Debug.Log ("Starting download of "+url);
	    WWW www = new WWW( "file://"+Application.dataPath + "/"+url );
	
	    float elapsedTime = 0.0f;

        while (!www.isDone)
        {
            elapsedTime += Time.deltaTime;
			//Debug.Log ("Loading. Elapsed time: "+elapsedTime);
            if (elapsedTime >= 10.0f) break;
            yield return null;
        }

        if (!www.isDone || !string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("Load Failed! Error: "+www.error);
            result(null);    // Pass null result.
            yield break;
        }
		Debug.LogWarning ("Loaded texture at "+url+"!");
		if(!textureCache.ContainsKey(url))
			textureCache.Add(url,www.texture);
        result(www.texture); // Pass retrieved result.
    }
}
