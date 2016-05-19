using UnityEngine;
using System.Collections;

public class ImageScrollRect : MonoBehaviour 
{
	public Rect _rect;
	public Texture2D[] _images;
	private float _time;
	private int _index;
	private const float SlideTime = 0.25f;
	private const float HoldTime = 3.0f;

	private RenderTexture _target;

	void Start()
	{
		
	}
	
	void Update()
	{
		int w = Mathf.FloorToInt(Screen.width * _rect.width);
		int h = Mathf.FloorToInt(Screen.height * _rect.height);

		if (_target != null)
		{
			if (_target.width != w || _target.height != h)
			{
				RenderTexture.ReleaseTemporary(_target);
				_target = null;
			}
		}

		if (_target == null)
			_target = RenderTexture.GetTemporary(w, h);

		_time += Time.deltaTime;
		if (_time >= HoldTime + SlideTime)
		{
			_time = 0.0f;
			_index = (_index + 1) % _images.Length;
		}
		
		float t = Mathf.Clamp01(_time / SlideTime);

		RenderTexture prev = RenderTexture.active;
		RenderTexture.active = _target;

		Texture2D src = _images[_index];
		
		GL.PushMatrix();
		//GL.LoadOrtho();
		GL.LoadPixelMatrix(0, _target.width, _target.height, 0);
		//GL.LoadIdentity();
		//GL.Clear(true, true, Color.blue);
		Graphics.DrawTexture(new Rect(-_target.width * (1.0f - t), 0, _target.width, _target.height), src);//, new Rect(0, 0, 1, 1), 0, 0, 0, 0);
		
		//Graphics.DrawTexture(new Rect(0, 0, _target.width, _target.height), src, new Rect(0, 0, 1, 1), 0, 0, 0, 0);
		//Debug.Log("" + _target.width + "x" + _target.height);
		GL.PopMatrix();

		RenderTexture.active = prev;
	}

	void OnGUI()
	{
		if (_target == null)
			return;
		
		GUI.depth = -4;

		Rect guiRect = _rect;
		guiRect.x *= Screen.width;
		guiRect.width *= Screen.width;
		guiRect.y *= Screen.height;
		guiRect.height *= Screen.height;
		GUI.DrawTexture(guiRect, _target);
	}
}