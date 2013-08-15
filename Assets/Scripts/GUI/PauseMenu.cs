using UnityEngine;
using System.Collections;

public class PauseMenu
{
	private static float lastTimeScale = 1.0f;
	private static bool isPaused = false;
	
	public static void Pause()
	{
		if(Time.timeScale > 0.0f)
		{
			lastTimeScale = Time.timeScale;
			Time.timeScale = 0.0f;
			isPaused = true;
		}
		else if(Time.timeScale == 0.0f)
		{
			Time.timeScale = lastTimeScale;
			isPaused = false;
		}
	}
	
	public static bool IsPaused()
	{
		return isPaused;
	}
}
