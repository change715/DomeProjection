using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Demos
{
	public class ExitScreen : MonoBehaviour
	{
		public Texture2D _background;
		public GUISkin _skin;

		private float _time;
		private float m_FadeOutTime = 0.25f;
		private float m_FadeInTime = 0.25f;


		void Start()
		{
			HUDFPS fps = (HUDFPS)GameObject.FindObjectOfType(typeof(HUDFPS));
			if (fps)
			{
				fps.enabled = false;
			}

			/*
			// Adjust text sizes according to screen height
			int buttonFontSize = (int)((Screen.height * 0.035f) + 0.5f);
			if (buttonFontSize < 18) { buttonFontSize = 10; }
			_skin.button.fontSize = buttonFontSize;
			//
			int boxFontSize = (int)((Screen.height * 0.0235f) + 0.5f);
			if (boxFontSize < 18) { boxFontSize = 8; }
			_skin.box.fontSize = boxFontSize;*/
		}

		void Update()
		{
			// To handle 'back' button on Android devices or close in Windows
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Application.Quit();
			}
		}

		void OnGUI()
		{
			GUI.skin = _skin;

			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _background);

			int buttonWidth = (int)((Screen.width * 0.15f) + 0.5f);
			int buttonHeight = (int)((Screen.height * 0.07f) + 0.5f);

			Rect buttonRect = new Rect(Screen.width - buttonWidth, Screen.height - buttonHeight, buttonWidth, buttonHeight);
			if (GUI.Button(buttonRect, "Restart"))
			{
				AutoFade.LoadLevel(1, m_FadeOutTime, m_FadeInTime, Color.black, false);
			}

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
			buttonRect.x -= (buttonWidth + 16);
			if (GUI.Button(buttonRect, "Quit"))
			{
				Application.Quit();
			}
#endif
		}
	}
}