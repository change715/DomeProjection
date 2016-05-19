using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Demos
{
	public class DemoController : MonoBehaviour
	{
		public GUISkin _skin;
		public int _demoSceneStart;
		public int _demoSceneEnd;
		public int _exitSceneIndex;
		private string _demoTitle;
		private string _infoText;
		private string _infoTitle;
		private float _infoScroll;
		private float _infoScrollTarget = 1f;

		private float	m_FadeOutTime		= 0.25f;
		private float	m_FadeInTime		= 0.25f;

		private const float virtualWidth = 1280f;
		private const float virtualHeight = 720f;

		void Start()
		{
			Application.runInBackground = true;
	#if UNITY_IPHONE || UNITY_IOS || UNITY_TVOS
			Application.targetFrameRate = 60;
	#endif
			DontDestroyOnLoad(this);

			/*
			// Adjust text sizes according to screen height
			int buttonFontSize = (int)( ( Screen.height * 0.035f) + 0.5f );
			if( buttonFontSize < 18 )	{ buttonFontSize = 10; }
			_skin.button.fontSize = buttonFontSize;
			//
			int boxFontSize = (int)( ( Screen.height * 0.0235f) + 0.5f );
			if( boxFontSize < 18 )		{ boxFontSize = 8; }
			_skin.box.fontSize = boxFontSize;*/
		}
		
		void Update()	
		{
			_infoScroll = Mathf.MoveTowards(_infoScroll, _infoScrollTarget, Time.deltaTime * 4.0f);
			
			if (Input.mousePosition.y > 0.0f)
			{
				float mx = (Input.mousePosition.x / Screen.width) * virtualWidth;
				float my = ((Screen.height - Input.mousePosition.y) / Screen.height) * virtualHeight;
				Vector2 uiMouse = new Vector2(mx, my);

				if (_buttonRect.Contains(uiMouse))
				{
					if (_infoScroll == 0.0f)
						_infoScrollTarget = 1.0f;
				}
				else if (Input.GetMouseButton(0))
				{
					if (_infoScroll == 1.0f)
						_infoScrollTarget = 0.0f;
				}
			}
			
	#if UNITY_ANDROID
				// To handle 'back' button on Android devices
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					Application.Quit();
				}
	#endif
		}
		
		private Rect _buttonRect;
		
		void OnGUI()
		{
			if (Application.loadedLevel < _demoSceneStart || Application.loadedLevel > _demoSceneEnd)
			{
				return;
			}

			GUI.depth = -5;
			GUI.skin = _skin;

			float buttonHeight = virtualHeight / 16f;
			float buttonWidth = virtualWidth / 12f;

			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / virtualWidth, Screen.height / virtualHeight, 1f));

			if( Application.loadedLevel > 1 && Application.loadedLevel < _exitSceneIndex )
			{
				Rect r = new Rect(0f, virtualHeight - buttonHeight, buttonWidth, buttonHeight);
				if (GUI.Button(r, "Prev Demo"))
				{
					PrevLevel();
				}
			}
		
			{
				string text = _infoText;
				Rect infoRect = GUILayoutUtility.GetRect(new GUIContent(text), _skin.box, GUILayout.MaxWidth(virtualWidth / 3));

				Rect r = new Rect((virtualWidth / 2f) - (infoRect.width / 2), (virtualHeight - buttonHeight - (infoRect.height * _infoScroll)), infoRect.width, buttonHeight);
				if (GUI.Button(r, _infoTitle))
				{
					_infoScrollTarget = 1.0f;
				}


				infoRect.x = (virtualWidth / 2) - (infoRect.width / 2);
				infoRect.y = virtualHeight - (infoRect.height * _infoScroll);
				GUI.Box(infoRect, text);

				_buttonRect = r;
				_buttonRect.xMin = Mathf.Min(_buttonRect.xMin, infoRect.xMin);
				_buttonRect.xMax = Mathf.Max(_buttonRect.xMax, infoRect.xMax);
				_buttonRect.yMin = Mathf.Min(_buttonRect.yMin, infoRect.yMin);
				_buttonRect.yMax = Mathf.Max(_buttonRect.yMax, infoRect.yMax);
			}

			Rect buttonRect = new Rect(virtualWidth - buttonWidth, virtualHeight - buttonHeight, buttonWidth, buttonHeight);
			if( Application.loadedLevel == _demoSceneEnd )
			{
				if (GUI.Button(buttonRect, "Info"))
				{
					AutoFade.LoadLevel( _exitSceneIndex, m_FadeOutTime, m_FadeInTime, Color.black, false );
				}
			}
			else
			{
				if (GUI.Button(buttonRect, "Next Demo"))
				{
					NextLevel();
				}
			}
		}
		
		void NextLevel()
		{
			int nextLevel = (Application.loadedLevel + 1);
			if (nextLevel == 0)
			{
				nextLevel++;
			}

			AutoFade.LoadLevel( nextLevel, m_FadeOutTime, m_FadeInTime, Color.black, false );
		}

		void PrevLevel()
		{
			int nextLevel = (Application.loadedLevel - 1);
			if (nextLevel > 0)
			{
				AutoFade.LoadLevel( nextLevel, m_FadeOutTime, m_FadeInTime, Color.black, false );
			}
		}

		void OnLevelWasLoaded(int levelIndex)
		{
			Reset(levelIndex);

			RenderHeads.Media.AVProVideo.Demos.DemoInfo demoInfo = (RenderHeads.Media.AVProVideo.Demos.DemoInfo)GameObject.FindObjectOfType(typeof(RenderHeads.Media.AVProVideo.Demos.DemoInfo));
			if( demoInfo )
			{
				_infoTitle = _demoTitle + demoInfo._title;
				_infoText = demoInfo._description;
			}

			// Start...wait until video is ready, then fade up
			StartCoroutine( WaitForVideoLoad( levelIndex ) );
		}
		
		private IEnumerator WaitForVideoLoad( int levelIndex )
		{
			// Wait till video is loaded and ready/playing before doing the fade up
			RenderHeads.Media.AVProVideo.Demos.DemoInfo demoInfo = (RenderHeads.Media.AVProVideo.Demos.DemoInfo)GameObject.FindObjectOfType(typeof(RenderHeads.Media.AVProVideo.Demos.DemoInfo));
			if( demoInfo && demoInfo._media )
			{
				// Wait for video to be instantiated/loaded
				RenderHeads.Media.AVProVideo.IMediaControl control = demoInfo._media.Control;
				int iNumberFramesWaited = 0;
				int iNumberFramesToWaitToExist = 300;
				//
				while( control == null && iNumberFramesWaited < iNumberFramesToWaitToExist )
				{
	//				Debug.LogError("MobileVideo: Waiting for video to exist: " + demoInfo._media.m_VideoPath + " | levelIndex: " + levelIndex);

					control = demoInfo._media.Control;
					++iNumberFramesToWaitToExist;
					yield return new WaitForEndOfFrame();
				}

				// Wait 3 frames for display object to update
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				if( control != null )
				{
					// Wait for the video to be ready-to-play/started-playing
					bool bExit = false;
					while( !bExit )
					{
	//					Debug.LogError("MobileVideo: Waiting for video to be playing: " + demoInfo._media.m_VideoPath + " | levelIndex: " + levelIndex);

						bExit = (!demoInfo._media.m_AutoOpen || (demoInfo._media.m_AutoStart && control.IsPlaying()) || (!demoInfo._media.m_AutoStart && control.CanPlay()));
						yield return new WaitForEndOfFrame();
					}
				}
			}

			// Fade new level in
	//		Debug.LogError("MobileVideo: Starting fade up for levelIndex: " + levelIndex);
			AutoFade.DoFadeUp();
		}

		private void Reset(int levelIndex)
		{
			_infoScroll = 0f;
			_infoScrollTarget = 1.0f;
			_demoTitle = "Demo " + ((levelIndex - _demoSceneStart) + 1) + "/" + ((_demoSceneEnd - _demoSceneStart) + 1) + " - ";
			_infoTitle = string.Empty;
			_infoText = string.Empty;
		}
	}
}