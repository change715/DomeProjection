using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	[AddComponentMenu("AVPro Video/Apply To Mesh")]
	public class ApplyToMesh : MonoBehaviour 
	{
		public MeshRenderer _mesh;
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
			if (_mesh != null)
			{
				Vector2 scale = Vector2.one;
				Vector2 offset = Vector2.zero;
				if (requiresYFlip)
				{
					scale = new Vector2(1.0f, -1.0f);
					offset = new Vector3(0.0f, 1.0f);
				}

				for (int i = 0; i < _mesh.materials.Length; i++)
				{
					Material mat = _mesh.materials[i];
					mat.mainTexture = texture;
					mat.mainTextureScale = scale;
					mat.mainTextureOffset = offset;
				}
			}
		}

		void OnEnable()
		{
			if (_mesh == null)
			{
				_mesh = this.GetComponent<MeshRenderer>();
				if (_mesh == null)
				{
					Debug.LogWarning("[AVProVideo] No mesh renderer set or found in gameobject");
				}
			}
			
			if (_mesh != null)
			{
				Update();
			}
		}
		
		void OnDisable()
		{
			ApplyMapping(_defaultTexture, false);
		}
	}
}