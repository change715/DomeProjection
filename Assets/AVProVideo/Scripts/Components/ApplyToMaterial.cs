using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	[AddComponentMenu("AVPro Video/Apply To Material")]
	public class ApplyToMaterial : MonoBehaviour 
	{
		public Material _material;
		public string _texturePropertyName;
		public MediaPlayer _media;
		public Texture2D _defaultTexture;

		void Update()
		{
			bool applied = false;
			if (_media != null && _media.TextureProducer != null)
			{
				Texture texture = _media.TextureProducer.GetTexture();
				if (texture != null)
				{
					ApplyMapping(texture, _media.TextureProducer.RequiresVerticalFlip());
					applied = true;
				}
			}

			if (!applied)
			{
				ApplyMapping(_defaultTexture, false);
			}
		}
		
		private void ApplyMapping(Texture texture, bool requiresYFlip)
		{
			if (_material != null)
			{
				Vector2 scale = Vector2.one;
				Vector2 offset = Vector2.zero;
				if (requiresYFlip)
				{
					scale = new Vector2(1.0f, -1.0f);
					offset = new Vector3(0.0f, 1.0f);
				}

				if (string.IsNullOrEmpty(_texturePropertyName))
				{
					_material.mainTexture = texture;
					_material.mainTextureScale = scale;
					_material.mainTextureOffset = offset;
				}
				else
				{
					_material.SetTexture(_texturePropertyName, texture);
					_material.SetTextureScale(_texturePropertyName, scale);
					_material.SetTextureOffset(_texturePropertyName, offset);
				}
			}
		}

		void OnEnable()
		{
			Update();
		}
		
		void OnDisable()
		{
			ApplyMapping(_defaultTexture, false);
		}
	}
}