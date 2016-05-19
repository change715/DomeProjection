 using UnityEngine;
 using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Demos
{
	public class AutoFade : MonoBehaviour
	{
		private static AutoFade		m_Instance	= null;

		private string		m_LevelName		= "";
		private int			m_LevelIndex	= 0;
		private bool		m_Fading		= false;
		private bool		m_StartFadeUp	= false;
		private Texture2D	m_WhiteTexture	= null;
		private Color _color;
		private float _alpha;

		private static AutoFade Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = (new GameObject("AutoFade")).AddComponent<AutoFade>();
				}
				return m_Instance;
			}
		}

		public static bool GetFading
		{
			get
			{
				return Instance.m_Fading;
			}
		}

		private void Awake()
		{
			DontDestroyOnLoad(this);
			m_Instance = this;

			m_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
			m_WhiteTexture.SetPixel(0, 0, Color.white);
			m_WhiteTexture.Apply();

			/*
			m_Material = new Material( Shader.Find ("Hidden/Internal-Colored") );
		//		m_Material.hideFlags = HideFlags.HideAndDontSave;
			m_Material.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			m_Material.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			m_Material.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			m_Material.SetInt ("_ZWrite", 0);
		//		m_Material.SetInt ("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);*/
		}

		private void DrawQuad( Color colour, float fAlpha )
		{
			/*colour.a = fAlpha;

			m_Material.SetPass(0);

			GL.PushMatrix();
			GL.LoadOrtho();

			GL.Begin( GL.QUADS) ;
			GL.Color( colour );
			GL.Vertex3(0, 0, -1);
			GL.Vertex3(0, 1, -1);
			GL.Vertex3(1, 1, -1);
			GL.Vertex3(1, 0, -1);
			GL.End();

			GL.PopMatrix();*/
		}

		void OnGUI()
		{
			if (m_Fading)
			{
				GUI.depth = -10000;
				GUI.color = new Color(_color.r, _color.g, _color.b, _alpha);
				GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), m_WhiteTexture, ScaleMode.StretchToFill);
			}
		}

		private IEnumerator Fade(float fFadeOutTime, float fFadeInTime, Color colour, bool bDoFadeUp)
		{
			float fTime = 0.0f;
			while( fTime < 1.0f )
			{
				_color = colour;
				_alpha = fTime;
				yield return new WaitForEndOfFrame();
				fTime = Mathf.Clamp01(fTime + Time.deltaTime / fFadeOutTime);
			}
			if (m_LevelName != "")
			{
				Application.LoadLevel(m_LevelName);
			}
			else
			{
				Application.LoadLevel(m_LevelIndex);
			}

			if( !bDoFadeUp )
			{
				m_StartFadeUp = false;
				while( !m_StartFadeUp )
				{
					yield return new WaitForEndOfFrame();
				}
			}

			while( fTime > 0.0f )
			{
				_color = colour;
				_alpha = fTime; 
				yield return new WaitForEndOfFrame();
				fTime = Mathf.Clamp01(fTime - Time.deltaTime / fFadeInTime);
			}

			m_Fading = false;
		}

		private IEnumerator FadeUp(float fFadeInTime, Color colour)
		{
			float fTime = 1.0f;
			while( fTime > 0.0f )
			{
				_color = colour;
				_alpha = fTime;
				yield return new WaitForEndOfFrame();
				fTime = Mathf.Clamp01(fTime - Time.deltaTime / fFadeInTime);
			}

			m_Fading = false;
		}

		private void StartFade(float fFadeOutTime, float fFadeInTime, Color colour, bool bDoFadeUp)
		{
			m_Fading = true;
			StartCoroutine( Fade(fFadeOutTime, fFadeInTime, colour, bDoFadeUp) );
		}

		public static void LoadLevel(string levelName, float fFadeOutTime, float fFadeInTime, Color colour, bool bDoFadeUp)
		{
			if( GetFading )
			{
				return;
			}
			Instance.m_LevelName = levelName;
			Instance.StartFade( fFadeOutTime, fFadeInTime, colour, bDoFadeUp );
		}

		public static void LoadLevel(int iLevelIndex, float fFadeOutTime, float fFadeInTime, Color colour, bool bDoFadeUp)
		{
			if( GetFading )
			{
				return;
			}

			Instance.m_LevelName = "";
			Instance.m_LevelIndex = iLevelIndex;
			Instance.StartFade( fFadeOutTime, fFadeInTime, colour, bDoFadeUp );
		}

		public static void DoFadeUp()
		{
			Instance.m_StartFadeUp = true;
		}
	}
}