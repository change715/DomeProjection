using UnityEngine;
using System.Collections;

public class HUDFPS : MonoBehaviour
{
	public float updateInterval = 0.5F;

	private float accum = 0; // FPS accumulated over the interval
	private int frames = 0; // Frames drawn over the interval
	private float timeleft; // Left time for current interval
	
	private string _text;
	private GUIStyle _guiStyle;
	
	void Start()
	{
		_guiStyle = new GUIStyle();
		_guiStyle.normal.textColor = Color.white;
		_guiStyle.alignment = TextAnchor.UpperRight;
		_guiStyle.fontSize = 30;
		
		timeleft = updateInterval;
	}

	void Update()
	{
		timeleft -= Time.deltaTime;
		accum += Time.timeScale / Time.deltaTime;
		++frames;

		// Interval ended - update GUI text and start new interval
		if (timeleft <= 0.0)
		{
			// display two fractional digits (f2 format)
			float fps = accum / frames;
			string format = System.String.Format("{0:F2} FPS", fps);
			_text = format;

			if (fps < 30)
				_guiStyle.normal.textColor = Color.yellow;
			else
				if (fps < 10)
					_guiStyle.normal.textColor = Color.red;
				else
					_guiStyle.normal.textColor = Color.green;
			//  DebugConsole.Log(format,level);
			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
	}
	
	void OnGUI()
	{
		GUI.depth = -10;
		GUI.Label(new Rect(0, 10.0f, Screen.width - 10.0f, 100.0f), _text, _guiStyle);
	}
}