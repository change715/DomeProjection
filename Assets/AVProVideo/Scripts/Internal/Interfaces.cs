using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	[System.Serializable]
	public class MediaPlayerEvent : UnityEngine.Events.UnityEvent<MediaPlayer, MediaPlayerEvent.EventType>
	{
		public enum EventType
		{
			MetaDataReady,		// Called when meta data(width, duration etc) is available
			ReadyToPlay,		// Called when the video is loaded and ready to play
			Started,			// Called when the playback starts
			FirstFrameReady,	// Called when the first frame has been rendered
			FinishedPlaying,	// Called when a non-looping video has finished playing

			// TODO: 
			//FinishedSeeking,	// Called when seeking has finished
			//StartLoop,			// Called when the video starts and is in loop mode
			//EndLoop,			// Called when the video ends and is in loop mode
			//Error,				// If there is an error with the playback, details provided on the error
		}
	}

	public interface IMediaPlayer
	{
		void Update();
		void Render();
	}

	public interface IMediaControl
	{
		// TODO: CanPreRoll() PreRoll()
		// TODO: audio panning

		bool	OpenVideoFromFile( string path );

        void    CloseVideo();

        void	SetLooping( bool bLooping );
		bool	IsLooping();

		bool	HasMetaData();
		bool	CanPlay();
		bool	IsPlaying();
		bool	IsSeeking();
		bool	IsPaused();
		bool	IsFinished();

		void	Play();
		void	Pause();
		void	Stop();
		void	Rewind();

		void	Seek(float timeMs);
		float	GetCurrentTimeMs();

		void	MuteAudio(bool bMute);
		bool	IsMuted();
		void	SetVolume(float volume);
		float	GetVolume();
	}

	public interface IMediaInfo
	{
		float	GetDurationMs();
		int		GetVideoWidth();
		int		GetVideoHeight();

		float	GetVideoPlaybackRate();

		bool	HasVideo();
		bool	HasAudio();

		/*float GetVideoFrameRate();
		string GetMediaDescription();
		string GetVideoDescription();
		string GetAudioDescription();*/
		}

	public interface IMediaProducer
	{
		Texture	GetTexture();
		int		GetTextureFrameCount();
		bool	RequiresVerticalFlip();
	}

	public interface IMediaConsumer
	{
	}

	public enum Platform
	{
		Windows,
		MacOSX,
		iOS,
		tvOS,
		Android,
		Count = 5,
		Unknown = 100,
	}

	public static class Helper
	{
		public const string ScriptVersion = "1.3.0";

		public static string GetName(Platform platform)
		{
			string result = "Unknown";
			/*switch (platform)
			{
				case Platform.Windows:
					break;
			}*/
			result = platform.ToString();
			return result;
		}

		public static string[] GetPlatformNames()
		{
			return new string[] { 
				GetName(Platform.Windows), 
				GetName(Platform.MacOSX),
				GetName(Platform.iOS),
				GetName(Platform.tvOS),
				GetName(Platform.Android),
			};
		}

		public static string GetTimeString(float totalSeconds)
		{
			int hours = Mathf.FloorToInt(totalSeconds / (60f * 60f));
			float usedSeconds = hours * 60f * 60f;

			int minutes = Mathf.FloorToInt((totalSeconds - usedSeconds) / 60f);
			usedSeconds += minutes * 60f;

			int seconds = Mathf.RoundToInt(totalSeconds - usedSeconds);

			string result = minutes.ToString("00") + ":" + seconds.ToString("00");
			if (hours > 0)
			{
				result = hours.ToString() + ":" + result;
			}
			return result;
		}

		public static T[] EnsureArraySize<T>(T[] input, T defaultValue, int length)
		{
			T[] result = input;
			if (input.Length != length)
			{
				System.Collections.Generic.List<T> list = new System.Collections.Generic.List<T>(length);
				if (input.Length < length)
				{
					for (int i = 0; i < length; i++)
					{
						if (i < input.Length)
						{
							list.Add(input[i]);
						}
						else
						{
							list.Add(defaultValue);
						}
					}
				}
				else if (input.Length > length)
				{
					for (int i = 0; i < length; i++)
					{
						list.Add(input[i]);
					}

				}
				result = list.ToArray();
			}

			return result;
		}
	}
}