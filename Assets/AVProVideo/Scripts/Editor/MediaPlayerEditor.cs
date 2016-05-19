#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2 && !UNITY_5_3_0
#define AVPRO_UNITY_PLATFORM_TVOS
#endif
#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2_0
#define AVPRO_UNITY_IOS_ALLOWHTTPDOWNLOAD
#endif
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2015-2016 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Editor
{
	[InitializeOnLoad]
	[CustomEditor(typeof(MediaPlayer))]
	public class MediaPlayerEditor : UnityEditor.Editor
	{
		private static Texture2D _icon;
		private static int _platformIndex = 0;
		private static bool _expandPlatformOverrides = false;
		private static bool _expandEvents = false;
		private static bool _expandPreview = false;
		private static bool _expandAbout = false;
		private static List<string> _recentFiles = new List<string>(16);

		private const string SettingsPrefix = "AVProVideo-MediaPlayerEditor-";
		private const int MaxRecentFiles = 16;

		private const string LinkPluginWebsite = "http://renderheads.com/product/avpro-video/";
		private const string LinkForumPage = "http://forum.unity3d.com/threads/released-avpro-video-complete-video-playback-solution.385611/";
		private const string LinkAssetStorePage = "https://www.assetstore.unity3d.com/#!/content/56355";
		private const string LinkEmailSupport = "mailto:unitysupport@renderheads.com";
		private const string LinkUserManual = "http://downloads.renderheads.com/docs/UnityAVProVideo.pdf";
		private const string LinkScriptingClassReference = "http://downloads.renderheads.com/docs/AVProVideoClassReference-1.3.0/";

		private const string SupportMessage = "If you are reporting a bug, please include any relevant files and details so that we may remedy the problem as fast as possible.\n\n" +
			"Essential details:\n" +
			"+ ErrorMessage\n" +
			"      + The exact error message\n" + 
			"      + The console/output log if possible\n" +
			"+ Hardware\n" +
			"      + Phone / tablet / device type and OS version\n" +
			"+ Development environment\n" + 
			"      + Unity version\n" +
			"      + Development OS version\n" +
			"      + AVPro Video plugin version\n" +
			" + Video details\n" + 
			"      + Resolution\n" +
			"      + Codec\n" +
			"      + Frame Rate\n" +
			"      + Better still, include a link to the video file\n";
			

		[MenuItem("GameObject/AVPro Video/Media Player", false, 0)]
		public static void CreateMediaPlayerEditor()
		{
			GameObject go = new GameObject("AVPro Video MediaPlayer");
			go.AddComponent<MediaPlayer>();
			Selection.activeGameObject = go;
		}

		private static void LoadSettings()
		{
			_expandPlatformOverrides = EditorPrefs.GetBool(SettingsPrefix + "ExpandPlatformOverrides", false);
			_expandEvents = EditorPrefs.GetBool(SettingsPrefix + "ExpandEvents", false);
			_expandPreview = EditorPrefs.GetBool(SettingsPrefix + "ExpandPreview", false);
			_platformIndex = EditorPrefs.GetInt(SettingsPrefix + "PlatformIndex", 0);

			string recentFilesString = EditorPrefs.GetString(SettingsPrefix + "RecentFiles", string.Empty);
			_recentFiles = new List<string>(recentFilesString.Split(new string[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries));
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetBool(SettingsPrefix + "ExpandPlatformOverrides", _expandPlatformOverrides);
			EditorPrefs.SetBool(SettingsPrefix + "ExpandEvents", _expandEvents);
			EditorPrefs.SetBool(SettingsPrefix + "ExpandPreview", _expandPreview);
			EditorPrefs.SetInt(SettingsPrefix + "PlatformIndex", _platformIndex);

			string recentFilesString = string.Empty;
			if (_recentFiles.Count > 0)
			{
				recentFilesString = string.Join(";", _recentFiles.ToArray());
			}
			EditorPrefs.SetString(SettingsPrefix + "RecentFiles", recentFilesString);
		}

		void OnEnable()
		{
			LoadSettings();
		}

		void OnDisable()
		{
			SaveSettings();
		}

		private static bool IsPathWithin(string fullPath, string targetPath)
		{
			return fullPath.StartsWith(targetPath);
		}

		private static string GetPathRelativeTo(string root, string fullPath)
		{
			string result = fullPath.Remove(0, root.Length);
			if (result.StartsWith(System.IO.Path.DirectorySeparatorChar.ToString()) || result.StartsWith(System.IO.Path.AltDirectorySeparatorChar.ToString()))
			{
				result = result.Remove(0, 1);
			}
			return result;
		}

		public override bool RequiresConstantRepaint()
		{
			MediaPlayer media = (this.target) as MediaPlayer;
			return (Application.isPlaying && media != null && media.Control != null && _expandPreview);
		}
		
		public override void OnInspectorGUI()
		{
			MediaPlayer media = (this.target) as MediaPlayer;

			//DrawDefaultInspector();

			serializedObject.Update();

			GUILayout.Space(6f);

			/////////////////// FILE PATH

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("File", EditorStyles.boldLabel);

			GUI.enabled = !Application.isPlaying;

			SerializedProperty propLocation = serializedObject.FindProperty("m_VideoLocation");
			EditorGUILayout.PropertyField(propLocation);

			SerializedProperty propPath = serializedObject.FindProperty("m_VideoPath");
			EditorGUILayout.PropertyField(propPath);

			GUI.enabled = true;

			if (!Application.isPlaying)
			{
				GUILayout.BeginHorizontal();
				OnInspectorGUI_RecentButton(propPath, propLocation);
				GUI.color = Color.green;
				if (GUILayout.Button("BROWSE"))
				{
					string startFolder = GetStartFolder(propPath.stringValue, (MediaPlayer.FileLocation)propLocation.enumValueIndex);
					string videoPath = media.m_VideoPath;
					string fullPath = string.Empty;
					MediaPlayer.FileLocation fileLocation = media.m_VideoLocation;
					if (Browse(startFolder, ref videoPath, ref fileLocation, ref fullPath))
					{
						propPath.stringValue = videoPath;
						propLocation.enumValueIndex = (int)fileLocation;
						EditorUtility.SetDirty(target);

						AddToRecentFiles(fullPath);
					}
				}

				GUILayout.EndHorizontal();

				ShowFileWarningMessages(propPath.stringValue, (MediaPlayer.FileLocation)propLocation.enumValueIndex, media.m_AutoOpen, Platform.Unknown);
				GUI.color = Color.white;
			}

			OnInspectorGUI_CopyableFilename(media.m_VideoPath);

			EditorGUILayout.EndVertical();

			/////////////////// STARTUP FIELDS

			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Startup", EditorStyles.boldLabel);

			SerializedProperty propAutoOpen = serializedObject.FindProperty("m_AutoOpen");
			EditorGUILayout.PropertyField(propAutoOpen);

			SerializedProperty propAutoStart = serializedObject.FindProperty("m_AutoStart");
			EditorGUILayout.PropertyField(propAutoStart);
			EditorGUILayout.EndVertical();

			/////////////////// PLAYBACK FIELDS

			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Playback", EditorStyles.boldLabel);

			if (!Application.isPlaying)
			{
				SerializedProperty propLoop = serializedObject.FindProperty("m_Loop");
				EditorGUILayout.PropertyField(propLoop);

				SerializedProperty propVolume = serializedObject.FindProperty("m_Volume");
				EditorGUILayout.PropertyField(propVolume);

				SerializedProperty propMuted = serializedObject.FindProperty("m_Muted");
				EditorGUILayout.PropertyField(propMuted);
			}
			else
			{
				if (media.Control != null)
				{
					media.m_Loop = media.Control.IsLooping();
					bool newLooping = EditorGUILayout.Toggle("Loop", media.m_Loop);
					if (newLooping != media.m_Loop)
					{
						media.Control.SetLooping(newLooping);
					}

					media.m_Volume = media.Control.GetVolume();
					float newVolume = EditorGUILayout.Slider("Volume", media.m_Volume, 0f, 1f);
					if (newVolume != media.m_Volume)
					{
						media.Control.SetVolume(newVolume);
					}

					media.m_Muted = media.Control.IsMuted();
					bool newMuted = EditorGUILayout.Toggle("Muted", media.m_Muted);
					if (newMuted != media.m_Muted)
					{
						media.Control.MuteAudio(newMuted);
					}
				}
			}

			SerializedProperty propDebugGui = serializedObject.FindProperty("m_DebugGui");
			EditorGUILayout.PropertyField(propDebugGui);
			EditorGUILayout.EndVertical();

			/////////////////// MEDIA PROPERTIES

			/*if (!Application.isPlaying)
			{
				OnInspectorGUI_MediaProperties();
			}*/

			//////////////////// PREVIEW

			OnInspectorGUI_Preview();

			/////////////////// EVENTS

			OnInspectorGUI_Events();

			/////////////////// PLATFORM OVERRIDES

			if (!Application.isPlaying)
			{
				OnInspectorGUI_PlatformOverrides();
			}

			if (serializedObject.ApplyModifiedProperties())
			{
				EditorUtility.SetDirty(target);
			}

			if (!Application.isPlaying)
			{
				OnInspectorGUI_About();
			}
		}

		struct RecentFileData
		{
			public RecentFileData(string path, SerializedProperty propPath, SerializedProperty propLocation, Object target)
			{
				this.path = path;
				this.propPath = propPath;
				this.propLocation = propLocation;
				this.target = target;
			}

			public string path;
			public SerializedProperty propPath;
			public SerializedProperty propLocation;
			public Object target;
		}

		private static void AddToRecentFiles(string path)
		{
			if (!_recentFiles.Contains(path))
			{
				_recentFiles.Insert(0, path);
				if (_recentFiles.Count > MaxRecentFiles)
				{
					_recentFiles.RemoveAt(_recentFiles.Count - 1);
				}
			}
		}

		void RecentMenuCallback_Select(object obj)
		{
			RecentFileData data = (RecentFileData)obj;

			string videoPath = string.Empty;
			MediaPlayer.FileLocation fileLocation = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;
			GetRelativeLocationFromPath(data.path, ref videoPath, ref fileLocation);

			data.propPath.stringValue = videoPath;
			data.propLocation.enumValueIndex = (int)fileLocation;

			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(data.target);
		}

		void RecentMenuCallback_Clear()
		{
			_recentFiles.Clear();
		}

		void RecentMenuCallback_Add()
		{
		}

		private void OnInspectorGUI_CopyableFilename(string path)
		{
			// Display the file name so it's easy to read and copy to the clipboard
			if (!string.IsNullOrEmpty(path))
			{
				// Some GUI hacks here because SelectableLabel wants to be double height and it doesn't want to be centered because it's an EditorGUILayout function...
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = Color.black;
					GUI.color = Color.cyan;
				}
				GUILayout.BeginHorizontal("box");
				GUILayout.FlexibleSpace();
				string text = System.IO.Path.GetFileName(path);
				Vector2 textSize = EditorStyles.boldLabel.CalcSize(new GUIContent(text));
				EditorGUILayout.SelectableLabel(text, EditorStyles.boldLabel, GUILayout.Height(textSize.y), GUILayout.Width(textSize.x));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUI.color = Color.white;
				GUI.backgroundColor = Color.white;
			}
		}

		private void OnInspectorGUI_RecentButton(SerializedProperty propPath, SerializedProperty propLocation)
		{
			GUI.color = Color.white;


			if (GUILayout.Button("RECENT", GUILayout.Width(64f)))
			{
				GenericMenu toolsMenu = new GenericMenu();
				toolsMenu.AddDisabledItem(new GUIContent("Recent Files"));

				// TODO: allow current path to be added.  Perhaps add it automatically when file is loaded?
				/*if (!string.IsNullOrEmpty(propPath.stringValue))
				{
					string path = propPath.stringValue.Replace("/", ">").Replace("\\", ">");
					toolsMenu.AddItem(new GUIContent("Add Current: " + path), false, RecentMenuCallback_Add);
				}*/
				toolsMenu.AddSeparator("");
				
				for (int i = 0; i < _recentFiles.Count; i++)
				{
					string path = _recentFiles[i];
					if (System.IO.File.Exists(path))
					{
						toolsMenu.AddItem(new GUIContent(path.Replace("/", ">").Replace("\\", ">")), false, RecentMenuCallback_Select, new RecentFileData(path, propPath, propLocation, target));
					}
					else
					{
						toolsMenu.AddDisabledItem(new GUIContent(path));
					}
				}
				if (_recentFiles.Count > 0)
				{
					toolsMenu.AddSeparator("");
					toolsMenu.AddItem(new GUIContent("Clear"), false, RecentMenuCallback_Clear);
				}

				toolsMenu.ShowAsContext();
			}
		}

		private static void ShowFileWarningMessages(string filePath, MediaPlayer.FileLocation fileLocation, bool isAutoOpen, Platform platform)
		{
			string finalPath = MediaPlayer.GetFilePath(filePath, fileLocation);

			if (string.IsNullOrEmpty(filePath))
			{
				if (isAutoOpen)
				{
					GUI.color = Color.red;
					GUILayout.TextArea("Error: No file specfied");
					GUI.color = Color.white;
				}
				else
				{
					GUI.color = Color.yellow;
					GUILayout.TextArea("Warning: No file specfied");
					GUI.color = Color.white;
				}
			}
			else
			{
				if (finalPath.Contains("://"))
				{
					if (fileLocation != MediaPlayer.FileLocation.AbsolutePathOrURL)
					{
						GUI.color = Color.yellow;
						GUILayout.TextArea("Warning: URL detected, change location to URL?");
						GUI.color = Color.white;
					}
					else
					{
#if AVPRO_UNITY_IOS_ALLOWHTTPDOWNLOAD
						// Display warning to iOS users if they're trying to use HTTP url without setting the permission
						bool isPlatformIOS = (platform == Platform.iOS) || (platform == Platform.Unknown && BuildTargetGroup.iOS == UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
#if AVPRO_UNITY_PLATFORM_TVOS
						isPlatformIOS |= (platform == Platform.tvOS) ||	(platform == Platform.Unknown && BuildTargetGroup.tvOS == UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
#endif

						if (isPlatformIOS)
						{
							if (filePath.StartsWith("http://") && !PlayerSettings.iOS.allowHTTPDownload)
							{
								GUI.color = Color.yellow;
								GUILayout.TextArea("Warning: Starting with iOS 9 'allow HTTP downloads' must be enabled for HTTP connections (see Player Settings)");
								GUI.color = Color.white;
							}
						}
#endif
						// Display warning for Andorid users if they're trying to use a URL without setting permission
						if (platform == Platform.Android || (platform == Platform.Unknown && BuildTargetGroup.Android == UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup))
						{
							if (!PlayerSettings.Android.forceInternetPermission)
							{
								GUI.color = Color.yellow;
								GUILayout.TextArea("Warning: You may need to set 'Internet Access' to 'require' in your Player Settings for Android builds when using URLs");
								GUI.color = Color.white;
							}
						}
					}
				}
				else
				{

					if (platform == Platform.Unknown || platform == MediaPlayer.GetPlatform())
					{
						if (!System.IO.File.Exists(finalPath))
						{
							GUI.color = Color.red;
							GUILayout.TextArea("Error: File not found");
							GUI.color = Color.white;
						}
					}
				}
			}

			if (fileLocation == MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder)
			{
				if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
				{
					GUILayout.Space(10f);
					GUILayout.BeginHorizontal();
					GUI.color = Color.yellow;
					GUILayout.TextArea("Warning: No StreamingAssets folder found");
					GUI.color = Color.white;

					if (GUILayout.Button("Create Folder"))
					{
						System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
						AssetDatabase.Refresh();
					}
					GUILayout.EndHorizontal();
				}
			}

			GUI.color = Color.white;
		}


		private bool _showAlpha = false;

		private void OnInspectorGUI_VideoPreview(IMediaProducer textureSource)
		{
			Texture texture = null;
			if (textureSource != null)
			{
				texture = textureSource.GetTexture();
			}
			if (texture == null)
				texture = EditorGUIUtility.whiteTexture;

			float ratio = (float)texture.width / (float)texture.height;

			// Reserve rectangle for texture
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			Rect textureRect = GUILayoutUtility.GetRect(Screen.width / 2, Screen.width / 2, (Screen.width / 2) / ratio, (Screen.width / 2) / ratio);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			// Dimensions
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(string.Format("{0}x{1}", texture.width, texture.height));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			// Show alpha toggle
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			_showAlpha = GUILayout.Toggle(_showAlpha, "Show Alpha Channel");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			// Draw the texture
			Matrix4x4 prevMatrix = GUI.matrix;
			if (textureSource != null && textureSource.RequiresVerticalFlip())
			{
				GUIUtility.ScaleAroundPivot(new Vector2(1f, -1f), new Vector2(0, textureRect.y + (textureRect.height / 2)));
			}
			if (!_showAlpha)
			{
				if (!GUI.enabled)
				{
					GUI.color = Color.grey;
				}
				GUI.DrawTexture(textureRect, texture, ScaleMode.ScaleToFit, true);
				GUI.color = Color.white;
			}
			else
			{
				EditorGUI.DrawTextureAlpha(textureRect, texture, ScaleMode.ScaleToFit);
			}
			GUI.matrix = prevMatrix;

			// Select texture button
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Select Texture", GUILayout.ExpandWidth(false)))
			{
				Selection.activeObject = texture;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private void OnInspectorGUI_PlayControls(IMediaControl control, IMediaInfo info)
		{
			GUILayout.Space(8.0f);

			// Slider
			EditorGUILayout.BeginHorizontal();
			bool isPlaying = false;
			if (control != null)
				isPlaying = control.IsPlaying();
			float currentTime = 0f;
			if (control != null)
				currentTime = control.GetCurrentTimeMs();
			float durationTime = 0f;
			if (info != null)
			{
				durationTime = info.GetDurationMs();
				if (float.IsNaN(durationTime))
				{
					durationTime = 0f;
				}
			}
			string timeUsed = Helper.GetTimeString(currentTime / 1000f);
			GUILayout.Label(timeUsed, GUILayout.ExpandWidth(false));

			float newTime = GUILayout.HorizontalSlider(currentTime, 0f, durationTime, GUILayout.ExpandWidth(true));
			if (newTime != currentTime)
			{
				control.Seek(newTime);
			}

			string timeTotal = Helper.GetTimeString(durationTime / 1000f);
			GUILayout.Label(timeTotal, GUILayout.ExpandWidth(false));

			EditorGUILayout.EndHorizontal();

			// Buttons
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Rewind", GUILayout.ExpandWidth(false)))
			{
				control.Rewind();
			}

			if (!isPlaying)
			{
				GUI.color = Color.green;
				if (GUILayout.Button("Play", GUILayout.ExpandWidth(true)))
				{
					control.Play();
				}
			}
			else
			{
				GUI.color = Color.yellow;
				if (GUILayout.Button("Pause", GUILayout.ExpandWidth(true)))
				{
					control.Pause();
				}
			}
			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();
		}

		private struct Native
		{
#if UNITY_EDITOR_WIN
			[System.Runtime.InteropServices.DllImport("AVProVideo")]
			public static extern System.IntPtr GetPluginVersion();
#elif UNITY_EDITOR_OSX
			[System.Runtime.InteropServices.DllImport("AVProVideo")]
			public static extern string AVPGetVersion();
#endif
		}

		private void OnInspectorGUI_About()
		{
			//GUILayout.Space(8f);

			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;
			if (_expandAbout)
			{
				GUI.color = Color.white;
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = Color.black;
				}
			}
			GUILayout.BeginVertical("box");
			GUI.backgroundColor = Color.white;
			if (GUILayout.Button("About", EditorStyles.toolbarButton))
			{
				_expandAbout = !_expandAbout;
			}
			GUI.color = Color.white;

			if (_expandAbout)
			{
				string version = "Unknown";
#if UNITY_EDITOR_WIN
				version = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Native.GetPluginVersion());
#elif UNITY_EDITOR_OSX
				version = Native.AVPGetVersion();
#endif

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (_icon == null)
				{
					_icon = Resources.Load<Texture2D>("AVProVideoIcon");
				}
				if (_icon != null)
				{
					GUILayout.Label(new GUIContent(_icon));
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUI.color = Color.yellow;
				GUILayout.Label("AVPro Video by RenderHeads Ltd", EditorStyles.boldLabel);
				GUI.color = Color.white;
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUI.color = Color.yellow;
				GUILayout.Label("version " + version + " (scripts v" + Helper.ScriptVersion + ")");
				GUI.color = Color.white;
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				

				GUILayout.Space(8f);
				GUI.backgroundColor = Color.white;

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("User Manual", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkUserManual);
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Scripting Class Reference", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkScriptingClassReference);
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();


				GUILayout.Space(8f);


				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Plugin Website", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkPluginWebsite);
				}
				if (GUILayout.Button("Asset Store Page", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkAssetStorePage);
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(8f);

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Unity Forum Page", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkForumPage);
				}
				if (GUILayout.Button("Email Support", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(LinkEmailSupport);
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(8f);

				EditorGUILayout.LabelField("Bug Reporting", EditorStyles.boldLabel);

				EditorGUILayout.SelectableLabel(SupportMessage, EditorStyles.wordWrappedLabel, GUILayout.Height(300f));

			}

			EditorGUILayout.EndVertical();
		}

		private void OnInspectorGUI_Events()
		{
			//GUILayout.Space(8f);
			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;
			if (_expandEvents)
			{
				GUI.color = Color.white;
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = Color.black;
				}
			}

			GUILayout.BeginVertical("box");
			GUI.backgroundColor = Color.white;

			if (GUILayout.Button("Events", EditorStyles.toolbarButton))
			{
				_expandEvents = !_expandEvents;
			}
			GUI.color = Color.white;

			if (_expandEvents)
			{
				SerializedProperty propEvents = serializedObject.FindProperty("m_events");

				EditorGUILayout.PropertyField(propEvents);
			}

			GUILayout.EndVertical();
		}

		private void OnInspectorGUI_MediaProperties()
		{
			GUILayout.Space(8f);
			GUI.color = Color.gray;
			if (_expandPlatformOverrides)
				GUI.color = Color.white;
			if (GUILayout.Button("Media Properties"))
			{
				_expandPlatformOverrides = !_expandPlatformOverrides;
			}
			GUI.color = Color.white;

			if (_expandPlatformOverrides)
			{
				SerializedProperty propStereoPacking = serializedObject.FindProperty("_stereoPacking");
				EditorGUILayout.PropertyField(propStereoPacking, new GUIContent("Stereo Packing"));

				SerializedProperty propDisplayStereoTint = serializedObject.FindProperty("_displayDebugStereoColorTint");
				EditorGUILayout.PropertyField(propDisplayStereoTint, new GUIContent("Debug Stereo Tint"));
			}
		}

		private void OnInspectorGUI_PlatformOverrides()
		{
			//GUILayout.Space(8f);
			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;
			if (_expandPlatformOverrides)
			{
				GUI.color = Color.white;
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = Color.black;
				}
			}
			GUILayout.BeginVertical("box");
			GUI.backgroundColor = Color.white;

			if (GUILayout.Button("Platform Overrides", EditorStyles.toolbarButton))
			{
				_expandPlatformOverrides = !_expandPlatformOverrides;
			}
			GUI.color = Color.white;

			if (_expandPlatformOverrides)
			{
				int platformIndex = GUILayout.Toolbar(_platformIndex, Helper.GetPlatformNames());
				
				if (platformIndex != _platformIndex)
				{
					_platformIndex = platformIndex;

					// We do this to clear the focus, otherwise a focused text field will not change when the Toolbar index changes
					EditorGUI.FocusTextInControl("ClearFocus");
				}

				OnInspectorGUI_PathOverrides();
				switch ((Platform)_platformIndex)
				{
					case Platform.Windows:
						OnInspectorGUI_Override_Windows();
						break;
					case Platform.MacOSX:
						OnInspectorGUI_Override_MacOSX();
						break;
					case Platform.iOS:
						OnInspectorGUI_Override_iOS();
						break;
					case Platform.tvOS:
						OnInspectorGUI_Override_tvOS();
						break;
					case Platform.Android:
						OnInspectorGUI_Override_Android();
						break;
				}
			}
			GUILayout.EndVertical();
		}

		void OnInspectorGUI_Preview()
		{
			MediaPlayer media = (this.target) as MediaPlayer;

			//GUILayout.Space(8f);
			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;

			if (_expandPreview)
			{
				GUI.color = Color.white;
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);
				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = Color.black;
				}
			}
			GUILayout.BeginVertical("box");
			GUI.backgroundColor = Color.white;

			if (GUILayout.Button("Preview", EditorStyles.toolbarButton))
			{
				_expandPreview = !_expandPreview;
			}
			GUI.color = Color.white;

			if (_expandPreview)
			{
				GUI.enabled = (media.TextureProducer != null && media.Info.HasVideo());
				OnInspectorGUI_VideoPreview(media.TextureProducer);
				GUI.enabled = true;

				GUI.enabled = (media.Control != null && media.Control.CanPlay() && media.isActiveAndEnabled && !EditorApplication.isPaused);
				OnInspectorGUI_PlayControls(media.Control, media.Info);
				GUI.enabled = true;
			}

			GUILayout.EndVertical();
		}

		private static string GetStartFolder(string path, MediaPlayer.FileLocation fileLocation)
		{
			// Try to resolve based on file path + file location
			string result = MediaPlayer.GetFilePath(path, fileLocation);
			if (!string.IsNullOrEmpty(result))
			{
				if (System.IO.File.Exists(result))
				{
					result = System.IO.Path.GetDirectoryName(result);
				}
			}

			if (!System.IO.Directory.Exists(result))
			{
				// Just resolve on file location
				result = MediaPlayer.GetPath(fileLocation);
			}
			if (string.IsNullOrEmpty(result))
			{
				// Fallback
				result = Application.streamingAssetsPath;
			}
			return result;
		}

		private void GUI_OverridePath(int platformIndex)
		{
			SerializedProperty location = serializedObject.FindProperty("m_platformVideoLocation");
			SerializedProperty propLocation = location.GetArrayElementAtIndex(platformIndex);

			EditorGUILayout.PropertyField(propLocation, new GUIContent("Location"));

			SerializedProperty path = serializedObject.FindProperty("m_platformVideoPath");
			SerializedProperty propPath = path.GetArrayElementAtIndex(platformIndex);

			EditorGUILayout.PropertyField(propPath, new GUIContent("Video Path"));




			GUILayout.BeginHorizontal();
			OnInspectorGUI_RecentButton(propPath, propLocation);
			GUI.color = Color.green;
			if (GUILayout.Button("BROWSE"))
			{
				string result = string.Empty;
				MediaPlayer.FileLocation fileLocation = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;
				string startFolder = GetStartFolder(propPath.stringValue, (MediaPlayer.FileLocation)propLocation.enumValueIndex);
				string fullPath = string.Empty;
				if (Browse(startFolder, ref result, ref fileLocation, ref fullPath))
				{
					propPath.stringValue = result;
					propLocation.enumValueIndex = (int)fileLocation;
					EditorUtility.SetDirty(target);
				}
			}

			GUILayout.EndHorizontal();

			GUI.color = Color.white;

			// Display the file name so it's easy to read and copy to the clipboard
			MediaPlayer media = (this.target) as MediaPlayer;
			OnInspectorGUI_CopyableFilename(media.m_platformVideoPath[platformIndex]);

			if (GUI.enabled)
			{
				ShowFileWarningMessages(propPath.stringValue, (MediaPlayer.FileLocation)propLocation.enumValueIndex, false, (Platform)platformIndex);
			}
		}

		private void OnInspectorGUI_PathOverrides()
		{
			MediaPlayer media = (this.target) as MediaPlayer;

			media.m_platformVideoPathOverride = Helper.EnsureArraySize(media.m_platformVideoPathOverride, false, (int)Platform.Count);
			media.m_platformVideoPath = Helper.EnsureArraySize(media.m_platformVideoPath, string.Empty, (int)Platform.Count);
			media.m_platformVideoLocation = Helper.EnsureArraySize(media.m_platformVideoLocation, MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder, (int)Platform.Count);

			bool newOverride = EditorGUILayout.Toggle("Override", media.m_platformVideoPathOverride[_platformIndex]);

			if (newOverride != media.m_platformVideoPathOverride[_platformIndex])
			{
				media.m_platformVideoPathOverride[_platformIndex] = newOverride;
				EditorUtility.SetDirty(target);
			}

			GUI.enabled = media.m_platformVideoPathOverride[_platformIndex];
			GUI_OverridePath(_platformIndex);
			GUI.enabled = true;
		}

		private void OnInspectorGUI_Override_Windows()
		{
		}

		private void OnInspectorGUI_Override_MacOSX()
		{
		}

		private void OnInspectorGUI_Override_iOS()
		{
		}

		private void OnInspectorGUI_Override_tvOS()
		{
		}

		private void OnInspectorGUI_Override_Android()
		{
		}

		private static bool Browse(string startPath, ref string filePath, ref MediaPlayer.FileLocation fileLocation, ref string fullPath)
		{
			bool result = false;
#if UNITY_EDITOR_OSX
			string extensions = "mp4,m4v,mov,avi";
			extensions += ",mp3,m4a,aac,ac3,au,aiff,wav";
#else
			string extensions = "Media Files;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.ts;*.webm;*.flv;*.vob;*.ogg;*.ogv;*.mpg;*.wmv;*.3gp";
			extensions += ";Audio Files;*wav;*.mp3;*.mp2;*.m4a;*.wma;*.aac;*.au;*.flac";
#endif
			string path = UnityEditor.EditorUtility.OpenFilePanel("Browse Video File", startPath, extensions);
			if (!string.IsNullOrEmpty(path) && !path.EndsWith(".meta"))
			{
				fullPath = path;
				GetRelativeLocationFromPath(path, ref filePath, ref fileLocation);
				result = true;
			}

			return result;
		}

		private static void GetRelativeLocationFromPath(string path, ref string filePath, ref MediaPlayer.FileLocation fileLocation)
		{
			string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
			projectRoot = projectRoot.Replace('\\', '/');

			if (path.StartsWith(projectRoot))
			{
				if (path.StartsWith(Application.streamingAssetsPath))
				{
					// Must be StreamingAssets relative path
					filePath = GetPathRelativeTo(Application.streamingAssetsPath, path);
					fileLocation = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;
				}
				else if (path.StartsWith(Application.dataPath))
				{
					// Must be Assets relative path
					filePath = GetPathRelativeTo(Application.dataPath, path);
					fileLocation = MediaPlayer.FileLocation.RelativeToDataFolder;
				}
				else
				{
					// Must be project relative path
					filePath = GetPathRelativeTo(projectRoot, path);
					fileLocation = MediaPlayer.FileLocation.RelativeToProjectFolder;
				}
			}
			else
			{
				// Must be persistant data
				if (path.StartsWith(Application.persistentDataPath))
				{
					filePath = GetPathRelativeTo(Application.persistentDataPath, path);
					fileLocation = MediaPlayer.FileLocation.RelativeToPeristentDataFolder;
				}

				// Must be absolute path
				filePath = path;
				fileLocation = MediaPlayer.FileLocation.AbsolutePathOrURL;
			}
		}
	}
}