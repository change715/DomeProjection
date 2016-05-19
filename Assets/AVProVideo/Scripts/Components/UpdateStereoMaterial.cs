using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	public class UpdateStereoMaterial : MonoBehaviour
	{
		public Camera _camera;
		public Material _material;

		void Update()
		{
			Camera camera = _camera;
			if (camera == null)
				camera = Camera.main;

			if (camera != null && _material != null)
			{
				_material.SetVector("_cameraPosition", _camera.transform.position);
			}
		}
	}
}