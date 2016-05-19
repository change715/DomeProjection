#if UNITY_ANDROID && !UNITY_EDITOR
#define REAL_ANDROID
#endif

using UnityEngine;
using System.Collections;
using System.IO;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	[AddComponentMenu("AVPro Video/Media Player")]
	public class MediaPlayer : MonoBehaviour
	{
		// These fields are just used to setup the properties for a new video that is about to be loaded
		// Once a video has been loaded you should use the interfaces exposed in the properties to
		// change playback properties (eg volume, looping, mute)
		public FileLocation m_VideoLocation = FileLocation.RelativeToStreamingAssetsFolder;
		public string		m_VideoPath;
		public bool			m_AutoOpen			= true;
		public bool			m_AutoStart			= false;
		public bool			m_Loop				= false;
		[Range(0.0f, 1.0f)]
		public float		m_Volume			= 1.0f;
		public bool			m_Muted				= false;
		public bool			m_DebugGui			= false;

		public enum StereoPacking
		{
			None,
			TopBottom,
			LeftRight,
		}
		public StereoPacking _stereoPacking = StereoPacking.None;
		public bool _displayDebugStereoColorTint = false;

		public MediaPlayerEvent		m_events;

		private IMediaControl		m_Control;
		private IMediaProducer		m_Texture;
		private IMediaInfo			m_Info;
		private IMediaPlayer		m_Player;
		private System.IDisposable	m_Dispose;

		private bool		m_VideoOpened			= false;
		private bool		m_AutoStartTriggered	= false;
		private bool		m_WasPlayingOnPause		= false;

		// Debug GUI
		private const int	s_GuiStartWidth		= 10;
		private const int	s_GuiWidth			= 240;
		private int m_GuiPositionX = s_GuiStartWidth;

		// Global init
		private static bool s_GlobalStartup		= false;

		// Events
		private bool m_EventFired_ReadyToPlay		= false;
		private bool m_EventFired_Started			= false;
		private bool m_EventFired_FirstFrameReady	= false;
		private bool m_EventFired_FinishedPlaying	= false;
		private bool m_EventFired_MetaDataReady		= false;

		public enum FileLocation
		{
			AbsolutePathOrURL,
			RelativeToProjectFolder,
			RelativeToStreamingAssetsFolder,
			RelativeToDataFolder,
			RelativeToPeristentDataFolder,
			// TODO: Resource
		}

		// Platform specific
		[HideInInspector]
		public bool[] m_platformVideoPathOverride = new bool[(int)Platform.Count];
		[HideInInspector]
		public string[] m_platformVideoPath = new string[(int)Platform.Count];
		[HideInInspector]
		public FileLocation[] m_platformVideoLocation = new FileLocation[(int)Platform.Count];

		/// <summary>
		/// Properties
		/// </summary>
		
		public IMediaInfo Info
		{
			get { return m_Info; }
		}
		public IMediaControl Control
		{
			get { return m_Control; }
		}

		public IMediaProducer TextureProducer
		{
			get { return m_Texture; }
		}

		public MediaPlayerEvent Events
		{
			get { return m_events; }
		}

		public void Start()
		{
			BaseMediaPlayer mediaPlayer = CreatePlatformMediaPlayer();
			if( mediaPlayer != null )
			{
				// Set-up interface
				m_Control = mediaPlayer;
				m_Texture = mediaPlayer;
				m_Dispose = mediaPlayer;
				m_Info = mediaPlayer;
				m_Player = mediaPlayer;

				if (!s_GlobalStartup)
				{
#if UNITY_5
					Debug.Log(string.Format("[AVProVideo] Initialising AVPro Video (script v{0} plugin v{1}) on {2}/{3} (MT {4})", Helper.ScriptVersion, mediaPlayer.GetVersion(), SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceVersion, SystemInfo.graphicsMultiThreaded));
#else
					Debug.Log(string.Format("[AVProVideo] Initialising AVPro Video (script v{0} plugin v{1}) on {2}/{3}", Helper.ScriptVersion, mediaPlayer.GetVersion(), SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceVersion));
#endif
					s_GlobalStartup = true;
				}

				// Open?
				if( m_AutoOpen )
				{
					OpenVideoFromFile();
				}

				SetPlaybackOptions();

				if (m_Control != null)
				{
					StartCoroutine("FinalRenderCapture");
				}
			}
		}

		private IEnumerator FinalRenderCapture()
		{
			while (Application.isPlaying)
			{
				yield return new WaitForEndOfFrame();

				if (m_Player != null) 
				{
					m_Player.Render();
				}
			}
		}

		private void SetPlaybackOptions()
		{
			// Set playback options
			if (m_Control != null)
			{
				m_Control.SetLooping(m_Loop);
				m_Control.SetVolume(m_Volume);
				m_Control.MuteAudio(m_Muted);
			}
		}

		private BaseMediaPlayer CreatePlatformMediaPlayer()
		{
			BaseMediaPlayer mediaPlayer = null;

			// Setup for running in the editor (Either OSX, Windows or Linux)
#if UNITY_EDITOR
	#if (UNITY_EDITOR_OSX)
		#if UNITY_EDITOR_64
			mediaPlayer = new OSXMediaPlayer();
		#else
			Debug.LogWarning("[AVProVideo] 32-bit OS X Unity editor not supported.  64-bit required.");
		#endif
	#elif UNITY_EDITOR_WIN
			WindowsMediaPlayer.InitialisePlatform();
			mediaPlayer = new WindowsMediaPlayer();
	#endif
#else
			// Setup for running builds
#if (UNITY_STANDALONE_WIN)
			WindowsMediaPlayer.InitialisePlatform();
			mediaPlayer = new WindowsMediaPlayer();
			//Windows_VLC_MediaPlayer.InitialisePlatform();
			//mediaPlayer = new Windows_VLC_MediaPlayer();
#elif (UNITY_STANDALONE_OSX || UNITY_IPHONE || UNITY_IOS || UNITY_TVOS)
			mediaPlayer = new OSXMediaPlayer();
#elif (UNITY_ANDROID)
			// Initialise platform (also unpacks videos from StreamingAsset folder (inside a jar), to the persistent data path)
			AndroidMediaPlayer.InitialisePlatform();
			mediaPlayer = new AndroidMediaPlayer();
#elif (UNITY_WP8)
			// TODO: add Windows Phone 8 suppport
#elif (UNITY_WP81)
			// TODO: add Windows Phone 8.1 suppport
#endif
#endif

			// Fallback
			if (mediaPlayer == null)
			{
				Debug.LogWarning("[AVProVideo] Not supported on this platform.  Using placeholder video!");
				mediaPlayer = new NullMediaPlayer();
			}

			return mediaPlayer;
		}

		public static Platform GetPlatform()
		{
			Platform result = Platform.Unknown;
			
			// Setup for running in the editor (Either OSX, Windows or Linux)
#if UNITY_EDITOR
	#if (UNITY_EDITOR_OSX && UNITY_EDITOR_64)
			result = Platform.MacOSX;
	#elif UNITY_EDITOR_WIN
			result = Platform.Windows;
	#endif
#else
			
			// Setup for running builds
	#if (UNITY_STANDALONE_WIN)
			result = Platform.Windows;
	#elif (UNITY_STANDALONE_OSX)
			result = Platform.MacOSX;
	#elif (UNITY_IPHONE || UNITY_IOS)
			result = Platform.iOS;
	#elif (UNITY_TVOS)
			result = Platform.tvOS;
	#elif (UNITY_ANDROID)
			result = Platform.Android;
	#elif (UNITY_WP8)
			// TODO: add Windows Phone 8 suppport
	#elif (UNITY_WP81)
			// TODO: add Windows Phone 8.1 suppport
	#endif
			
#endif

			return result;
		}

		public static string GetPath(FileLocation location)
		{
			string result = string.Empty;
			switch (location)
			{
				case FileLocation.AbsolutePathOrURL:
					break;
				case FileLocation.RelativeToDataFolder:
					result = Application.dataPath;
					break;
				case FileLocation.RelativeToPeristentDataFolder:
					result = Application.persistentDataPath;
					break;
				case FileLocation.RelativeToProjectFolder:
					result = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
					result = result.Replace('\\', '/');
					break;
				case FileLocation.RelativeToStreamingAssetsFolder:
					result = Application.streamingAssetsPath;
					break;
			}
			return result;
		}

		public static string GetFilePath(string path, FileLocation location)
		{
			string result = string.Empty;
			if (!string.IsNullOrEmpty(path))
			{
				switch (location)
				{
					case FileLocation.AbsolutePathOrURL:
						result = path;
						break;
					case FileLocation.RelativeToDataFolder:
					case FileLocation.RelativeToPeristentDataFolder:
					case FileLocation.RelativeToProjectFolder:
					case FileLocation.RelativeToStreamingAssetsFolder:
						result = System.IO.Path.Combine(GetPath(location), path);
						break;
				}
			}
			return result;
		}

		private string GetPlatformFilePath(ref string filePath, ref FileLocation fileLocation)
		{
			string result = string.Empty;

			Platform platform = GetPlatform();
			if (platform != Platform.Unknown)
			{
				// Override per-platform
				int platformIndex = (int)platform;
				if (m_platformVideoPathOverride[platformIndex])
				{
					filePath = m_platformVideoPath[platformIndex];
					fileLocation = m_platformVideoLocation[platformIndex];
				}
			}

			result = GetFilePath(filePath, fileLocation);
		
			return result;
		}
		
		public bool OpenVideoFromFile(FileLocation location, string path)
		{
			m_VideoLocation = location;
			m_VideoPath = path;
			return OpenVideoFromFile();
		}
		
		private bool OpenVideoFromFile()
		{
			bool result = false;
			// Open the video file
			if ( m_Control != null )
			{
				CloseVideo();

				m_VideoOpened = true;
				m_AutoStartTriggered = !m_AutoStart;
				m_EventFired_MetaDataReady = false;
				m_EventFired_ReadyToPlay = false;
				m_EventFired_Started = false;
				m_EventFired_FirstFrameReady = false;

				// Potentially override the file location
				string fullPath = GetPlatformFilePath(ref m_VideoPath, ref m_VideoLocation);

				if (!string.IsNullOrEmpty(m_VideoPath))
				{
					bool checkForFileExist = true;
					if (fullPath.Contains("://"))
					{
						checkForFileExist = false;
					}
#if (UNITY_ANDROID)
					checkForFileExist = false;
#endif

					if (checkForFileExist && !System.IO.File.Exists(fullPath))
					{
						Debug.LogError("[AVProVideo] File not found: " + fullPath, this);
					}
					else
					{
						Debug.Log("[AVProVideo] Opening " + fullPath);

						if (!m_Control.OpenVideoFromFile(fullPath))
						{
							Debug.LogError("[AVProVideo] Failed to open " + fullPath, this);
						}
						else
						{
							SetPlaybackOptions();
							result = true;
						}
					}
				}
				else
				{
					Debug.LogError("[AVProVideo] No file path specified", this);
				}
			}
			return result;
		}

        public void CloseVideo()
        {
            // Open the video file
            if( m_Control != null )
            {
                m_AutoStartTriggered = false;
                m_VideoOpened = false;
				m_EventFired_ReadyToPlay = false;
				m_EventFired_Started = false;
				m_EventFired_MetaDataReady = false;
				m_EventFired_FirstFrameReady = false;

                m_Control.CloseVideo();
            }
        }

		public void Play()
		{
			if (m_Control != null && m_Control.CanPlay())
			{
				m_Control.Play();
			}
		}

		public void Pause()
		{
			if (m_Control != null && m_Control.IsPlaying())
			{
				m_Control.Pause();
			}
		}

		public void Stop()
		{
			if (m_Control != null)
			{
				m_Control.Stop();
			}
		}

		public void Rewind(bool pause)
		{
			if (m_Control != null)
			{
				if (pause)
				{
					Pause();
				}
				m_Control.Rewind();
			}
		}

		void Update()
		{
			// Auto start the playback
			if (m_Control != null)
			{			
				if (m_VideoOpened && m_AutoStart && !m_AutoStartTriggered && m_Control.CanPlay())
				{
					m_AutoStartTriggered = true;
					m_Control.Play();
				}
			}

			if (m_Player != null)
			{
				// Update
				m_Player.Update();

				// Render
				//m_Player.Render();

				// Finished event
				if (!m_EventFired_FinishedPlaying && !m_Control.IsLooping() && m_Control.CanPlay() && m_Control.IsFinished())
				{
					m_EventFired_FinishedPlaying = true;
					m_events.Invoke(this, MediaPlayerEvent.EventType.FinishedPlaying);
				}
			}
			
			UpdateEvents();
		}

		private void UpdateEvents()
		{
			// TODO: investigate weird bug where instantiated MediaPlayer's have m_events set to NULL..
			if ( m_events != null && m_Control != null)
			{
				// Keep track of whether the Playing state has changed
				if (m_EventFired_Started && !m_Control.IsPlaying())
				{
					// Playing has stopped
					m_EventFired_Started = false;
				}

				if (m_EventFired_FinishedPlaying && m_Control.IsPlaying() && !m_Control.IsFinished())
				{
					m_EventFired_FinishedPlaying = false;
				}

				// Fire events
				if (!m_EventFired_MetaDataReady && m_Control.HasMetaData())
				{
					m_EventFired_MetaDataReady = true;
					m_events.Invoke(this, MediaPlayerEvent.EventType.MetaDataReady);
				}
				if (!m_EventFired_ReadyToPlay && !m_Control.IsPlaying() && m_Control.CanPlay())
				{
					m_EventFired_ReadyToPlay = true;
					m_events.Invoke(this, MediaPlayerEvent.EventType.ReadyToPlay);
				}
				if (!m_EventFired_Started && m_Control.IsPlaying())
				{
					m_EventFired_Started = true;
					m_events.Invoke(this, MediaPlayerEvent.EventType.Started);
				}
				if (m_Texture != null)
				{
					if (!m_EventFired_FirstFrameReady && m_Control.CanPlay() && m_Texture.GetTextureFrameCount() > 0)
					{
						m_EventFired_FirstFrameReady = true;
						m_events.Invoke(this, MediaPlayerEvent.EventType.FirstFrameReady);
					}
				}
			}
		}
#if REAL_ANDROID
// STE: Put this back into the Update() function as we were seeing GL state related UI issues
		// Some versions of Unity have a bug where rendering to texture can only happen from OnPostRender
		/*void OnPostRender()
		{
			if (m_Player != null)
			{
				m_Player.Render();
			}
		}*/
#endif

#if !UNITY_EDITOR
		void OnApplicationFocus( bool focusStatus )
		{
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
//			Debug.Log("OnApplicationFocus: focusStatus: " + focusStatus);

			if( focusStatus )
			{
				if( m_Control != null && m_WasPlayingOnPause )
				{
					m_WasPlayingOnPause = false;
					m_Control.Play();

					Debug.Log("OnApplicationFocus: playing video again");
				}
			}
#endif
		}

		void OnApplicationPause( bool pauseStatus )
		{
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
//			Debug.Log("OnApplicationPause: pauseStatus: " + pauseStatus);

			if( pauseStatus )
			{
				if( m_Control!= null && m_Control.IsPlaying() )
				{
					m_WasPlayingOnPause = true;
					m_Control.Pause();

					Debug.Log("OnApplicationPause: pausing video");
				}
			}
			else
			{
				// Catch coming back from power off state when no lock screen
				OnApplicationFocus( true );
			}
#endif
		}
#endif

		void OnEnable()
		{
			if (m_Control != null && m_WasPlayingOnPause)
			{
				m_AutoStartTriggered = false;
				m_WasPlayingOnPause = false;
				StartCoroutine("FinalRenderCapture");
			}
		}

		void OnDisable()
		{
			if (m_Control != null)
			{
				StopCoroutine("FinalRenderCapture");

				if (m_Control.IsPlaying())
				{
					m_WasPlayingOnPause = true;
					Pause();
				}
			}
		}

		void OnDestroy()
		{
			m_VideoOpened = false;

			if (m_Dispose != null)
			{
				m_Dispose.Dispose();
				m_Dispose = null;
			}
			m_Control = null;
			m_Texture = null;
			m_Info = null;
			m_Player = null;

			// TODO: possible bug if MediaPlayers are created and destroyed manually (instantiated), OnApplicationQuit won't be called!
		}

		void OnApplicationQuit()
		{
			if (s_GlobalStartup)
			{
				Debug.Log("[AVProVideo] Shutdown");

				// Clean up any open media players
				MediaPlayer[] players = Resources.FindObjectsOfTypeAll<MediaPlayer>();
				if (players != null && players.Length > 0)
				{
					for (int i = 0; i < players.Length; i++)
					{
						players[i].CloseVideo();
					}
				}

#if UNITY_EDITOR
#if UNITY_EDITOR_WIN
				WindowsMediaPlayer.DeinitPlatform();
#endif
#else
#if (UNITY_STANDALONE_WIN)
				WindowsMediaPlayer.DeinitPlatform();
#endif
#endif
				s_GlobalStartup = false;
			}
		}


		// This code handles the pause/play buttons in the editor
#if UNITY_EDITOR
		static MediaPlayer()
		{
			UnityEditor.EditorApplication.playmodeStateChanged -= OnUnityPlayModeChanged;
			UnityEditor.EditorApplication.playmodeStateChanged += OnUnityPlayModeChanged;
		}

		private static void OnUnityPlayModeChanged()
		{
			if (UnityEditor.EditorApplication.isPlaying)
			{
				if (UnityEditor.EditorApplication.isPaused)
				{
					MediaPlayer[] players = Resources.FindObjectsOfTypeAll<MediaPlayer>();
					foreach (MediaPlayer player in players)
					{
						player.EditorPause();
					}
				}
				else
				{
					MediaPlayer[] players = Resources.FindObjectsOfTypeAll<MediaPlayer>();
					foreach (MediaPlayer player in players)
					{
						player.EditorUnpause();
					}
				}
			}
		}

		private void EditorPause()
		{
			if (this.isActiveAndEnabled && m_Control != null && m_Control.IsPlaying())
			{
				m_WasPlayingOnPause = true;
				Pause();
			}
		}

		private void EditorUnpause()
		{
			if (this.isActiveAndEnabled && m_Control != null && m_WasPlayingOnPause)
			{
				m_WasPlayingOnPause = false;
				m_AutoStartTriggered = false;
			}
		}
#endif


		public void SetGuiPositionFromVideoIndex(int index)
		{
			m_GuiPositionX = s_GuiStartWidth + (s_GuiWidth * index);
		}

		public void SetDebugGuiEnabled(bool bEnabled)
		{
			m_DebugGui = bEnabled;
		}

		void OnGUI()
		{
			if (m_Info != null && m_DebugGui)
			{
				GUI.depth = -1;

				float debugGuiScale = 1.5f;
				GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(debugGuiScale, debugGuiScale, 1.0f));

				GUILayout.BeginArea(new Rect(m_GuiPositionX, 10, (s_GuiWidth - 10), 150));
				GUILayout.BeginVertical("box");
				GUILayout.Label(System.IO.Path.GetFileName(m_VideoPath));
				GUILayout.Label("Dimensions: " + m_Info.GetVideoWidth() + " x " + m_Info.GetVideoHeight());
				GUILayout.Label("Time: " + (m_Control.GetCurrentTimeMs() * 0.001f).ToString("F1") + "s / " + (m_Info.GetDurationMs() * 0.001f).ToString("F1") + "s");
				GUILayout.Label("Rate: " + m_Info.GetVideoPlaybackRate().ToString("F2") + "Hz");
				GUILayout.EndVertical();
				GUILayout.EndArea();
			}
		}
	}
}