using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Demos
{
	public class Loader : MonoBehaviour
	{
		private Texture2D _backTexture;
		private GUIStyle _backStyle = new GUIStyle();

		private Texture2D _barTexture;
		private GUIStyle _barStyle = new GUIStyle();
		
		public Texture2D _logo;
		
		private AsyncOperation _asyncLoader;
		
		private float _loadTarget = 0.0f;
		private float _loadProgress = 0.0f;
		private float _fadeTimer = 1.5f;
		private float _fade = 1.0f;
		
		void Start()
		{
			_backTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			_backTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 1.0f));
			_backTexture.Apply(false, false);
			_backStyle.normal.background = _backTexture;

			_barTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			_barTexture.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f, 0.9f));
			_barTexture.Apply(false, false);
			_barStyle.normal.background = _barTexture;
			
			Application.runInBackground = true;
			
			DontDestroyOnLoad(this);
			StartCoroutine(LoadGame());
		}
		
		void Update()
		{
			if (_asyncLoader != null)
			{
				_loadTarget = _asyncLoader.progress;
			}
			else
			{
				_fadeTimer = (_fadeTimer - Time.deltaTime * 0.75f);
				_fade = Mathf.Clamp01(_fadeTimer);
				//_backTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, _fade));
				//_backTexture.Apply(false, false);
				//Debug.Log("fade: " + _fade);
				
				if (_fade <= 0.0f)
				{
					//Destroy(this.gameObject);
					this.enabled = false;
				}
			}
			_loadTarget += 0.01f;
			_loadProgress = Mathf.MoveTowards(_loadProgress, _loadTarget, Time.deltaTime);
			_loadProgress = Mathf.Clamp01(_loadProgress);
		}
		
		void OnGUI()
		{
			GUI.depth = -10;
			if (_fade > 0.0f)
			{
				GUI.color = new Color(1.0f, 1.0f, 1.0f, _fade);
				GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _backStyle);
				int ws = Screen.width / 6;
				int hs = Screen.height / 6;
				if (_logo != null)
					GUI.DrawTexture(new Rect((Screen.width - ws)/2, (Screen.height - hs)/2, ws, hs),  _logo, ScaleMode.ScaleToFit);
				//if (_loadProgress < 1.0f)
				{
					int logoBottom = ((Screen.height - hs)/2) + hs;
					int barWidth = Screen.width / 6;
					int barHeight = 24;
					int barLeftX = barWidth;
					int barRightX = Screen.width - barWidth;
					int barTopY = (Screen.height / 2) - barHeight / 2;
					if (_logo != null)
						barTopY = logoBottom + 8;
					
					GUI.Box(new Rect(barLeftX, barTopY, barRightX - barLeftX, barHeight), GUIContent.none);
					GUI.Box(new Rect(barLeftX + 4 , barTopY + 4, ((barRightX - barLeftX) - 8) * _loadProgress, barHeight - 8), GUIContent.none, _barStyle);
				}
			}
		}
		
	    IEnumerator LoadGame()
		{
	        _asyncLoader = Application.LoadLevelAsync(1);
	        yield return _asyncLoader;
			_asyncLoader = null;
			_loadTarget = 1.0f;
		}
	}
}