#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if UNITY_5
	#if !UNITY_5_0 && !UNITY_5_1
		#define AVPROVIDEO_ISSUEPLUGINEVENT_UNITY52
	#endif
#endif
//#define USE_MULTI_TEXTURE
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	public class WindowsMediaPlayer : BaseMediaPlayer
	{
#if USE_MULTI_TEXTURE
		private const int NUM_TEXTURE_FRAMES = 12;
		private const int NUM_FRAMES_BUFFER = 6;
#endif

		private bool		_isPlaying = false;
		private bool		_isPaused = false;
		private bool		_audioMuted = false;
		private float		_volume = 1.0f;
		private bool		_bLoop = false;
		private bool		_canPlay = false;
		private bool		_hasMetaData = false;
		private int			_width = 0;
		private int			_height = 0;
		private bool		_hasAudio = false;
		private bool		_hasVideo = false;
		private bool		_isTextureTopDown = true;
		private Texture2D	_texture;
		private Texture2D[]	_textures;
		private System.IntPtr _instance = System.IntPtr.Zero;
		private float		_playbackRateTimer;
		private int			_lastFrameCount;
		private float		_playbackRate;

		private static bool _isInitialised = false;
		private static string _version = "Plug-in not yet initialised";

#if AVPROVIDEO_ISSUEPLUGINEVENT_UNITY52
		private static System.IntPtr _nativeFunction_UpdateAllTextures;
		private static System.IntPtr _nativeFunction_FreeTextures;
#endif

		public static void InitialisePlatform()
		{
			if (!_isInitialised)
			{
				if (!Native.Init(QualitySettings.activeColorSpace == ColorSpace.Linear, true))
				{
					Debug.LogError("Failing to initialise platform");
				}
				else
				{
					_version = GetPluginVersion();
#if AVPROVIDEO_ISSUEPLUGINEVENT_UNITY52
					_nativeFunction_UpdateAllTextures = Native.GetRenderEventFunc_UpdateAllTextures();
					_nativeFunction_FreeTextures = Native.GetRenderEventFunc_FreeTextures();
#endif
				}
			}
		}

		public static void DeinitPlatform()
		{
			Native.Deinit();
		}

		public override string GetVersion()
		{
			return _version;
		}

		public override bool OpenVideoFromFile(string path)
		{
			CloseVideo();

			string fullPath = path;
			if (!System.IO.Path.IsPathRooted(path) && !path.Contains("://"))
			{
				fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
			}

			_instance = Native.OpenSource(System.IntPtr.Zero, fullPath);

			if (_instance == System.IntPtr.Zero)
			{
				return false;
			}

			string playerDescription = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Native.GetPlayerDescription(_instance));
			Debug.Log("[AVProVideo] Using player type " + playerDescription);

			return true;
		}

        public override void CloseVideo()
        {
			_width = 0;
			_height = 0;
			_hasAudio = _hasVideo = false;
			_hasMetaData = false;
			_canPlay = false;
			_isPaused = false;
			_isPlaying = false;
			_bLoop = false;
			_audioMuted = false;
			_lastFrameCount = 0;
			_playbackRate = 0f;
			_playbackRateTimer = 0f;

			if (_texture != null)
			{
				Texture2D.Destroy(_texture);
				_texture = null;
			}
			if (_textures != null)
			{
				for (int i = 0; i < _textures.Length; i++)
				{
					Texture2D.Destroy(_textures[i]);
					_textures[i] = null;
				}
				_textures = null;
			}
			if (_instance != System.IntPtr.Zero)
			{
				Native.CloseSource(_instance);
				_instance = System.IntPtr.Zero;
			}

			// Issue thread event to free the texture on the GPU
			IssueRenderThreadEvent(Native.RenderThreadEvent.FreeTextures);
        }

        public override void SetLooping(bool looping)
		{
			_bLoop = looping;
			Native.SetLooping(_instance, looping);
		}

		public override bool IsLooping()
		{
			return _bLoop;
		}

		public override bool HasMetaData()
		{
			return _hasMetaData;
		}

		public override bool HasAudio()
		{
			return _hasAudio;
		}

		public override bool HasVideo()
		{
			return _hasVideo;
		}

		public override bool CanPlay()
		{
			return _canPlay;
		}

		public override void Play()
		{
			_isPlaying = true;
			_isPaused = false;
			Native.Play(_instance);
		}

		public override void Pause()
		{
			_isPlaying = false;
			_isPaused = true;
			Native.Pause(_instance);
		}

		public override void Stop()
		{
			_isPlaying = false;
			_isPaused = false;
			Native.Pause(_instance);
		}

		public override bool IsSeeking()
		{
			return Native.IsSeeking(_instance);
		}
		public override bool IsPlaying()
		{
			return _isPlaying;
		}
		public override bool IsPaused()
		{
			return _isPaused;
		}
		public override bool IsFinished()
		{
			return Native.IsFinished(_instance);
		}

		public override float GetDurationMs()
		{
			return Native.GetDuration(_instance) * 1000f;
		}

		public override int GetVideoWidth()
		{
			return _width;
		}
			
		public override int GetVideoHeight()
		{
			return _height;
		}

		public override float GetVideoPlaybackRate()
		{
			return _playbackRate;
		}

#if USE_MULTI_TEXTURE
		public override Texture GetTexture()
		{
			Texture result = null;
			int frameCount = Native.GetTextureFrameCount(_instance);
			// Wait for 3 frames to be read then start the display
			if (frameCount > NUM_FRAMES_BUFFER && _textures != null)
			{
				int lastWrittenBuffer = (frameCount - 1) % NUM_TEXTURE_FRAMES;
				int readBuffer = lastWrittenBuffer - NUM_FRAMES_BUFFER;
				if (readBuffer < 0) readBuffer += NUM_TEXTURE_FRAMES;

				//Debug.Log("returned buffer " + readBuffer + " " + frameCount + " " + Time.frameCount);
				result = _textures[readBuffer];
			}
			return result;
		}
#else
		public override Texture GetTexture()
		{
			Texture result = null;
			if (Native.GetTextureFrameCount(_instance) > 0)
			{
				result = _texture;
			}
			return result;
		}
#endif
		public override int GetTextureFrameCount()
		{
			return Native.GetTextureFrameCount(_instance);
		}

		public override bool RequiresVerticalFlip()
		{
			return _isTextureTopDown;
		}

		public override void Rewind()
		{
			Seek(0.0f);
		}

		public override void Seek(float timeMs)
		{
			Native.SetCurrentTime(_instance, timeMs / 1000f);
		}

		public override float GetCurrentTimeMs()
		{
			return Native.GetCurrentTime(_instance) * 1000f;
		}

		public override void MuteAudio(bool bMuted)
		{
			_audioMuted = bMuted;
			Native.SetMuted(_instance, _audioMuted);
		}

		public override bool IsMuted()
		{
			return _audioMuted;
		}

		public override void SetVolume(float volume)
		{
			_volume = volume;
			Native.SetVolume(_instance, volume);
		}

		public override float GetVolume()
		{
			return _volume;
		}

		public override void Update()
		{
			Native.Update(_instance);

			if (!_canPlay)
			{
				if (!_hasMetaData)
				{
					if (Native.HasMetaData(_instance))
					{
						if (Native.HasVideo(_instance))
						{
							_hasVideo = true;
							_width = Native.GetWidth(_instance);
							_height = Native.GetHeight(_instance);
							if (Mathf.Max(_width, _height) > SystemInfo.maxTextureSize)
							{
								Debug.LogError(string.Format("[AVProVideo] Video dimensions ({0}x{1}) larger than maxTextureSize ({2})", _width, _height, SystemInfo.maxTextureSize));
								_width = _height = 0;
								_hasVideo = false;
							}
						}
						if (Native.HasAudio(_instance))
						{
							_hasAudio = true;
						}

						_hasMetaData = true;
					}
				}
				if (_hasMetaData)
				{
					_canPlay = Native.CanPlay(_instance);
				}
			}

			if (_hasVideo && _width > 0 && _height > 0)
			{
#if USE_MULTI_TEXTURE
				if (_textures == null)
				{
					System.IntPtr ptr = Native.GetTexturePointers(_instance, 0);
					if (ptr != System.IntPtr.Zero)
					{
						_textures = new Texture2D[NUM_TEXTURE_FRAMES];
						for (int i = 0; i < _textures.Length; i++)
						{
							ptr = Native.GetTexturePointers(_instance, i);
							_textures[i] = Texture2D.CreateExternalTexture(_width, _height, TextureFormat.RGBA32, false, false, ptr);
						}

						Debug.Log("creating textures...");
					}
				}
#else
				if (_texture == null)
				{
					System.IntPtr ptr = Native.GetTexturePointer(_instance);
					if (ptr != System.IntPtr.Zero)
					{
						_isTextureTopDown = Native.IsTextureTopDown(_instance);
						_texture = Texture2D.CreateExternalTexture(_width, _height, TextureFormat.RGBA32, false, false, ptr);
						_texture.wrapMode = TextureWrapMode.Clamp;
					}
				}
#endif

			}

			//IssueRenderThreadEvent(Native.RenderThreadEvent.UpdateAllTextures);
		}


		public override void Render()
		{
			// Update playback rate 
			_playbackRateTimer += Time.deltaTime;
			if (_playbackRateTimer > 1f)
			{
				int frameCount = Native.GetTextureFrameCount(_instance);
				_playbackRate = (float)(frameCount - _lastFrameCount) / _playbackRateTimer;
				_playbackRateTimer = 0f;
				_lastFrameCount = frameCount;
			}

			IssueRenderThreadEvent(Native.RenderThreadEvent.UpdateAllTextures);
		}

		public override void Dispose()
		{
			CloseVideo();
		}

		private static int _lastUpdateAllTexturesFrame;

		private static void IssueRenderThreadEvent(Native.RenderThreadEvent renderEvent)
		{
			if (renderEvent == Native.RenderThreadEvent.UpdateAllTextures)
			{
				// We only want to update all textures once per frame
				if (_lastUpdateAllTexturesFrame == Time.frameCount)
					return;

				_lastUpdateAllTexturesFrame = Time.frameCount;
			}

#if AVPROVIDEO_ISSUEPLUGINEVENT_UNITY52
			if (renderEvent == Native.RenderThreadEvent.UpdateAllTextures)
			{
				GL.IssuePluginEvent(_nativeFunction_UpdateAllTextures, 0);
			}
			else if (renderEvent == Native.RenderThreadEvent.FreeTextures)
			{
				GL.IssuePluginEvent(_nativeFunction_FreeTextures, 0);
			}
#else
			GL.IssuePluginEvent(Native.PluginID | (int)renderEvent);
#endif
		}

		private static string GetPluginVersion()
		{
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Native.GetPluginVersion());
		}

		private struct Native
		{
			public const int PluginID = 0xFA60000;

			public enum RenderThreadEvent
			{
				UpdateAllTextures,
				FreeTextures,
			}

			// Global

			[DllImport("AVProVideo")]
			public static extern bool Init(bool linearColorSpace, bool isD3D11NoSingleThreaded);

			[DllImport("AVProVideo")]
			public static extern void Deinit();

			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetPluginVersion();

			[DllImport("AVProVideo")]
			public static extern bool IsTrialVersion();

			// Open and Close

			[DllImport("AVProVideo")]
			public static extern System.IntPtr OpenSource(System.IntPtr instance, [MarshalAs(UnmanagedType.LPWStr)]string path);

			[DllImport("AVProVideo")]
			public static extern void CloseSource(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetPlayerDescription(System.IntPtr instance);


			// Controls

			[DllImport("AVProVideo")]
			public static extern void Play(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern void Pause(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern void SetMuted(System.IntPtr instance, bool muted);

			[DllImport("AVProVideo")]
			public static extern void SetVolume(System.IntPtr instance, float volume);

			[DllImport("AVProVideo")]
			public static extern void SetLooping(System.IntPtr instance, bool looping);

			// Properties

			[DllImport("AVProVideo")]
			public static extern bool HasVideo(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern bool HasAudio(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern int GetWidth(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern int GetHeight(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern float GetDuration(System.IntPtr instance);

			// State

			[DllImport("AVProVideo")]
			public static extern bool HasMetaData(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern bool CanPlay(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern bool IsSeeking(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern bool IsFinished(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern float GetCurrentTime(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern void SetCurrentTime(System.IntPtr instance, float time);

			// Update and Rendering

			[DllImport("AVProVideo")]
			public static extern void Update(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetTexturePointer(System.IntPtr instance);

			[DllImport("AVProVideo")]
			public static extern bool IsTextureTopDown(System.IntPtr instance);

#if USE_MULTI_TEXTURE
			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetTexturePointers(System.IntPtr instance, int index);
#endif
			[DllImport("AVProVideo")]
			public static extern int GetTextureFrameCount(System.IntPtr instance);

#if AVPROVIDEO_ISSUEPLUGINEVENT_UNITY52
			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetRenderEventFunc_UpdateAllTextures();

			[DllImport("AVProVideo")]
			public static extern System.IntPtr GetRenderEventFunc_FreeTextures();
#endif
		}
	}
}
#endif