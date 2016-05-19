﻿using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	public abstract class BaseMediaPlayer : IMediaPlayer, IMediaControl, IMediaInfo, IMediaProducer, System.IDisposable
	{
		public abstract string	GetVersion();
		
		public abstract bool	OpenVideoFromFile( string path );
        public abstract void    CloseVideo();

        public abstract void	SetLooping( bool bLooping );
		public abstract bool	IsLooping();

		public abstract bool	HasMetaData();
		public abstract bool	CanPlay();
		public abstract void	Play();
		public abstract void	Pause();
		public abstract void	Stop();
		public abstract void	Rewind();

		public abstract void	Seek(float timeMs);
		public abstract float	GetCurrentTimeMs();

		public abstract float	GetDurationMs();
		public abstract int		GetVideoWidth();
		public abstract int		GetVideoHeight();
		public abstract float	GetVideoPlaybackRate();
		public abstract bool	HasAudio();
		public abstract bool	HasVideo();

		public abstract bool	IsSeeking();
		public abstract bool	IsPlaying();
		public abstract bool	IsPaused();
		public abstract bool	IsFinished();

		public abstract Texture	GetTexture();
		public abstract int		GetTextureFrameCount();
		public abstract bool	RequiresVerticalFlip();


		public abstract void	MuteAudio(bool bMuted);
		public abstract bool	IsMuted();
		public abstract void	SetVolume(float volume);
		public abstract float	GetVolume();

		public abstract void	Update();
		public abstract void	Render();
		public abstract void	Dispose();
	}
}