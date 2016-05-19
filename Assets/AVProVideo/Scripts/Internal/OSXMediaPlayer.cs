﻿#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE || UNITY_IOS || UNITY_TVOS
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using AOT;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{

//	IMediaInfo
//
//	float	GetDurationMs();
//	int		GetVideoWidth();
//	int		GetVideoHeight();
//
//	float	GetVideoPlaybackRate();
//
//	bool	HasVideo();
//	bool	HasAudio();

//	BaseMediaPlayer
//	
//	public abstract String	GetVersion();
//	
//	public abstract bool	OpenVideoFromFile( string path );
//	public abstract void    CloseVideo();
//	
//	public abstract void	SetLooping( bool bLooping );
//	public abstract bool	IsLooping();
//	
//  public abstract bool	HasMetaData();
//	public abstract bool	CanPlay();
//	public abstract void	Play();
//	public abstract void	Pause();
//	public abstract void	Stop();
//	public abstract void	Rewind();
//	
//	public abstract void	Seek(float timeMs);
//	public abstract float	GetCurrentTimeMs();
//	
//	public abstract float	GetDurationMs();
//	public abstract int		GetVideoWidth();
//	public abstract int		GetVideoHeight();
//	public abstract float	GetVideoPlaybackRate();
//	
//	public abstract bool	IsSeeking();
//	public abstract bool	IsPlaying();
//	public abstract bool	IsPaused();
//	public abstract bool	IsFinished();
//	
//	public abstract Texture	GetTexture();
//	public abstract int		GetTextureFrameCount();
//	public abstract bool	RequiresVerticalFlip();
//	
//	
//	public abstract void	MuteAudio(bool bMuted);
//	public abstract void	SetVolume(float volume);
//	public abstract float	GetVolume();
//	
//	public abstract void	Update();
//	public abstract void	Render();
//	public abstract void	Dispose();
	
	public class OSXMediaPlayer : BaseMediaPlayer
	{
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		private const string PluginName = "AVProVideo";
#elif UNITY_IPHONE || UNITY_IOS || UNITY_TVOS
		private const string PluginName = "__Internal";
#endif

		// Native Interface

		private enum AVPPluginEvent
		{
			Nop,
			Initialise,
			PlayerUpdate,
			PlayerShutdown
		}

		private enum AVPPlayerStatus
		{
			Unknown,
			ReadyToPlay,
			Playing,
			Finished,
			Seeking,
			Failed
		}

		private enum AVPLogFlag
		{
			Error	= 1 << 0,
			Warning	= 1 << 1,
			Info	= 1 << 2,
			Debug	= 1 << 3,
			Verbose	= 1 << 4,
		};

		private enum AVPLogLevel
		{
			Off		= 0,
			Error	= AVPLogFlag.Error,
			Warning	= AVPLogFlag.Error | AVPLogFlag.Warning,
			Info	= AVPLogFlag.Error | AVPLogFlag.Warning | AVPLogFlag.Info,
			Debug	= AVPLogFlag.Error | AVPLogFlag.Warning | AVPLogFlag.Info | AVPLogFlag.Debug,
			Verbose	= AVPLogFlag.Error | AVPLogFlag.Warning | AVPLogFlag.Info | AVPLogFlag.Debug | AVPLogFlag.Verbose,
			All		= -1
		};
		
		[DllImport(PluginName)]
		private static extern string AVPGetVersion();
		
		[DllImport(PluginName)]
		private static extern IntPtr AVPGetRenderEventFunc();

		[DllImport(PluginName)]
		private static extern double AVPPlayerGetCurrentTime(IntPtr player);

		[DllImport(PluginName)]
		private static extern double AVPPlayerGetDuration(IntPtr player);

		[DllImport(PluginName)]
		private static extern int AVPPlayerGetFrameCount(IntPtr player);
		
		[DllImport(PluginName)]
		private static extern double AVPPlayerGetFrameRate(IntPtr player);
		
		[DllImport(PluginName)]
		private static extern double AVPPlayerGetNominalFrameRate(IntPtr player);

		[DllImport(PluginName)]
		private static extern int AVPPlayerGetHandle(IntPtr player);

		[DllImport(PluginName)]
		private static extern AVPPlayerStatus AVPPlayerGetStatus(IntPtr player);
		
		[DllImport(PluginName)]
		private static extern IntPtr AVPPlayerGetTextureHandle(IntPtr player);

		[DllImport(PluginName)]
		private static extern void AVPPlayerGetTextureSize(IntPtr player, out int width, out int height);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerTextureSizeHasChanged(IntPtr player);

		[DllImport(PluginName)]
		private static extern float AVPPlayerGetVolume(IntPtr player);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerHasAudio(IntPtr player);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerHasVideo(IntPtr player);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerHasMetaData(IntPtr player);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerIsLooping(IntPtr player);

		[DllImport(PluginName)]
		private static extern void AVPPlayerSetLooping(IntPtr player, bool looping);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerIsMuted(IntPtr player);

		[DllImport(PluginName)]
		private static extern void AVPPlayerSetMuted(IntPtr player, bool muted);

		[DllImport(PluginName)]
		private static extern void AVPPlayerSetVolume(IntPtr player, float volume);
		
		[DllImport(PluginName)]
		private static extern IntPtr AVPPlayerNew();

		[DllImport(PluginName)]
		private static extern IntPtr AVPPlayerRelease(IntPtr player);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerOpenFile(IntPtr player, string path);

		[DllImport(PluginName)]
		private static extern bool AVPPlayerOpenURL(IntPtr player, string url);

		[DllImport(PluginName)]
		private static extern void AVPPlayerClose(IntPtr player);

		[DllImport(PluginName)]
		private static extern void AVPPlayerPlay(IntPtr player);

		[DllImport(PluginName)]
		private static extern void AVPPlayerPause(IntPtr player);
		
		[DllImport(PluginName)]
		private static extern void AVPPlayerSeekToTime(IntPtr player, double time);

		[DllImport(PluginName)]
		private static extern void AVPPluginRegister();

		[DllImport(PluginName)]
		private static extern void AVPPluginInitialise();

		[DllImport(PluginName)]
		private static extern void AVPPluginSetDebugLogFunction(IntPtr fn);

		// MediaPlayer Interface

		private static bool _initialised = false;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
		private delegate void DebugLogCallbackDelegate(int level, int flags, string str);

#if UNITY_IPHONE || UNITY_IOS || UNITY_TVOS
		[MonoPInvokeCallback(typeof(DebugLogCallbackDelegate))]
#endif
		private static void DebugLogCallback(int level, int flags, string str)
		{
			if ((flags & (int)AVPLogFlag.Error) == (int)AVPLogFlag.Error)
			{
				Debug.LogError(str);
			}
			else if ((flags & (int)AVPLogFlag.Warning) == (int)AVPLogFlag.Warning)
			{
				Debug.LogWarning(str);
			}
			else
			{
				Debug.Log(str);
			}
		}

		private static void IssuePluginEvent(AVPPluginEvent type, int param)
		{
			// Build eventId from the type and param.
			int eventId = 0x5d5ac000 | ((int)type << 8);

			switch (type)
			{
			case AVPPluginEvent.PlayerUpdate:
			case AVPPluginEvent.PlayerShutdown:
				eventId |= param & 0xff;
				break;
			}

#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1
				GL.IssuePluginEvent(AVPGetRenderEventFunc(), eventId);
#else
				GL.IssuePluginEvent(eventId);
#endif
		}

		private static void Initialise()
		{
			if (!_initialised)
			{
				_initialised = true;

#if ((UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && !UNITY_5) || (UNITY_IPHONE || UNITY_IOS || UNITY_TVOS)
				AVPPluginRegister();
#endif

				DebugLogCallbackDelegate callbackDelegate = new DebugLogCallbackDelegate(DebugLogCallback);
				IntPtr func = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
				AVPPluginSetDebugLogFunction(func);

				// Initialisation needs to happen on a render thread.
				IssuePluginEvent(AVPPluginEvent.Initialise, 0);
			}
		}

		private IntPtr _player = IntPtr.Zero;	// Handle to the native player.
		private Texture2D _texture = null;
		private IntPtr _nativeTexture = IntPtr.Zero;
		private int _width = 0;
		private int _height = 0;
		private bool _isMetaDataReady = false;

		public OSXMediaPlayer()
		{
			OSXMediaPlayer.Initialise();
			_player = AVPPlayerNew();
		}
		
		// BaseMediaPlayer Overrides
		
		public override string GetVersion()
		{
			return AVPGetVersion();
		}

		public override bool OpenVideoFromFile(string path)
		{
			if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("file://"))
			{
				return AVPPlayerOpenURL(_player, path);
			}
			else
			{
				return AVPPlayerOpenFile(_player, path);
			}
		}

		public override void CloseVideo()
		{
			AVPPlayerClose(_player);

			if (_texture != null)
			{
				Texture2D.Destroy(_texture);
				_texture = null;
			}
			_width = _height = 0;
			_nativeTexture = IntPtr.Zero;
			_isMetaDataReady = false;
		}

		public override bool IsLooping()
		{
			return AVPPlayerIsLooping(_player);
		}

		public override void SetLooping(bool looping)
		{
			AVPPlayerSetLooping(_player, looping);
		}

		public override bool HasAudio()
		{
			return AVPPlayerHasAudio(_player);
		}

		public override bool HasVideo()
		{
			return AVPPlayerHasVideo(_player);
		}

		public override bool HasMetaData()
		{
			return _isMetaDataReady;
		}

		public override bool CanPlay()
		{
			return AVPPlayerGetStatus(_player) >= AVPPlayerStatus.ReadyToPlay;
		}

		public override void Play()
		{
			AVPPlayerPlay(_player);
		}

		public override void Pause()
		{
			AVPPlayerPause(_player);
		}

		public override void Stop()
		{
			AVPPlayerPause(_player);
		}

		public override void Rewind()
		{
			AVPPlayerSeekToTime(_player, 0.0);
		}

		public override void Seek(float ms)
		{
			AVPPlayerSeekToTime(_player, ms / 1000.0);
		}

		public override float GetCurrentTimeMs()
		{
			return (float)(AVPPlayerGetCurrentTime(_player) * 1000.0f);
		}

		public override float GetDurationMs()
		{
			return (float)(AVPPlayerGetDuration(_player) * 1000.0f);
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
			return (float)AVPPlayerGetFrameRate(_player);
		}

		public override bool IsSeeking()
		{
			return AVPPlayerGetStatus(_player) == AVPPlayerStatus.Seeking;
		}

		public override bool IsPlaying()
		{
			return AVPPlayerGetStatus(_player) == AVPPlayerStatus.Playing;
		}

		public override bool IsPaused()
		{
			return AVPPlayerGetStatus(_player) == AVPPlayerStatus.ReadyToPlay;
		}

		public override bool IsFinished()
		{
			return AVPPlayerGetStatus(_player) == AVPPlayerStatus.Finished;
		}

		public override Texture GetTexture()
		{
			return _texture;
		}

		public override int GetTextureFrameCount()
		{
			return AVPPlayerGetFrameCount(_player);
		}

		public override bool RequiresVerticalFlip()
		{
			return false;
		}

		public override bool IsMuted()
		{
			return AVPPlayerIsMuted(_player);
		}

		public override void MuteAudio( bool bMute )
		{
			AVPPlayerSetMuted(_player, bMute);
		}

		public override void SetVolume(float volume)
		{
			AVPPlayerSetVolume(_player, volume);
		}

		public override float GetVolume()
		{
			return AVPPlayerGetVolume(_player);
		}

		public override void Render()
		{
			OSXMediaPlayer.IssuePluginEvent(AVPPluginEvent.PlayerUpdate, AVPPlayerGetHandle(_player));
		}

		public override void Update()
		{
			// Called before Render.

			// Check for meta data to become available
			if (!_isMetaDataReady)
			{
				if (AVPPlayerHasMetaData(_player) || CanPlay())
				{
					if (HasVideo())
					{
						int width = 0, height = 0;
						AVPPlayerGetTextureSize(_player, out width, out height);

						if (width > 0 && height > 0)
						{
							if (Mathf.Max(width, height) > SystemInfo.maxTextureSize)
							{
								Debug.LogError("[AVProVideo] Video dimensions larger than maxTextureSize");
							}
							else
							{
								_width = width;
								_height = height;
							}
							_isMetaDataReady = true;
						}
					}
					else if (HasAudio())
					{
						_isMetaDataReady = true;
					}
				}
			}

			// Free texture
			if (_texture != null)
			{
				IntPtr native = AVPPlayerGetTextureHandle(_player);
				if (native == IntPtr.Zero)
				{
					if (_texture != null)
					{
						Texture2D.Destroy(_texture);
						_texture = null;
					}
					_width = _height = 0;
					_nativeTexture = IntPtr.Zero;
				}
			}

			if (HasVideo() && _width > 0 && _height > 0)
			{
				IntPtr native = AVPPlayerGetTextureHandle(_player);
				
				int width = 0, height = 0;
				AVPPlayerGetTextureSize(_player, out width, out height);

				if (_texture != null)
				{
					// Point to a new texture
					if (native != _nativeTexture)
					{
						_texture.UpdateExternalTexture(native);
						_nativeTexture = native;

						_width = width;
						_height = height;
					}
				}
				if (_texture == null || width != _width || height != _height)
				{
					TextureFormat format = TextureFormat.BGRA32;
					bool mipmap = false;
					bool linear = false;

					// Free existing texture if any
					if (_texture != null)
					{
						Texture2D.Destroy(_texture);
						_texture = null;
					}
					_width = _height = 0;
					_nativeTexture = IntPtr.Zero;

					_texture = Texture2D.CreateExternalTexture(width, height, format, mipmap, linear, native);
					_nativeTexture = native;
					_width = width;
					_height = height;

					OSXMediaPlayer.IssuePluginEvent(AVPPluginEvent.PlayerUpdate, AVPPlayerGetHandle(_player));
				}
			}
		}

		public override void Dispose()
		{
			OSXMediaPlayer.IssuePluginEvent(AVPPluginEvent.PlayerShutdown, AVPPlayerGetHandle(_player));
			AVPPlayerRelease(_player);
			_player = IntPtr.Zero;
		}

	}
}
#endif
