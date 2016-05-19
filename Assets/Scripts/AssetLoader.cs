using UnityEngine;
using System.IO;
using System;
using System.Collections;
using RenderHeads.Media.AVProVideo;

public class AssetLoader : MonoBehaviour
{
  public Material imageMaterial;
  public GameObject imageGO;
  private int imageIndex = 0;
  private Texture2D[] loadedTextures;

  public Material movieMaterial;
  public GameObject movieGO;
  private int movieIndex = 0;
  private String[] movieFiles;
  public MediaPlayer moviePlayer;
  private MediaPlayer.FileLocation videoLocation = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;
  private bool moviePlaying = true;

  private bool imageMode = true;
  private bool assetsLoaded = false;

  void OnEnable()
  {
    Reciever.OnGazedComplete += ParseUI;
  }

  void OnDisable()
  {
    Reciever.OnGazedComplete -= ParseUI;
  }

  void ParseUI(InputType type)
  {
    if(type == InputType.back)
    {
      if(imageMode)
      {
        imageIndex--;
        if(imageIndex < 0)
          imageIndex = loadedTextures.Length - 1;
        SetTexture(imageIndex);
      }
      else
      {
        movieIndex--;
        if(movieIndex < 0)
          movieIndex = movieFiles.Length - 1;
        SetMovie(movieIndex);
      }
    }

    if(type == InputType.next)
    {
      if(imageMode)
      {
        imageIndex++;
        if(imageIndex > loadedTextures.Length - 1)
          imageIndex = 0;
        SetTexture(imageIndex);
      }
      else
      {
        movieIndex++;
        if(movieFiles.Length == 0)
          return;
        if(movieIndex > movieFiles.Length - 1)
          movieIndex = 0;
        SetMovie(movieIndex);
      }
    }

    if(type == InputType.swap)
    {
      imageMode = !imageMode;
      if(imageMode)
      {
        moviePlayer.Control.Pause();
        movieGO.SetActive(false);
        imageGO.SetActive(true);
        SetTexture(imageIndex);
      }
      else
      {
        movieGO.SetActive(true);
        imageGO.SetActive(false);
        SetMovie(movieIndex);
      }
    }
  }

  private string[] GetFileNames(string path,string filter)
  {
    string[] files = Directory.GetFiles(path,filter);
    for(int i = 0; i < files.Length; i++)
      files[i] = Path.GetFileName(files[i]);
    return files;
  }

  private IEnumerator GetImage(string path,int num)
  {
    WWW www = new WWW(path);
    yield return www;
    Texture2D imageTexture;
    imageTexture = www.texture;
    loadedTextures[num] = imageTexture;
  }

  private void SetTexture(int index)
  {
    if(index > -1 && index < loadedTextures.Length)
    {
      imageMaterial.SetTexture("_EmissionMap",loadedTextures[index]);
    }
  }

  private void SetMovie(int index)
  {    
    if(movieFiles == null)
      return;
    moviePlayer.CloseVideo();    
    moviePlayer.m_VideoPath = string.Empty;
    moviePlayer.m_VideoPath = movieFiles[index];
    
    if(string.IsNullOrEmpty(moviePlayer.m_VideoPath))
    {
      Debug.Log("null string");
      moviePlayer.CloseVideo();
      movieIndex = 0;
    }
    else
    {
      Debug.Log(moviePlayer.m_VideoPath + " video index: " + index);
      moviePlayer.OpenVideoFromFile(MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder,moviePlayer.m_VideoPath);
      moviePlayer.Control.Play();
    }    
  }

  void GetImages()
  {
    string[] imgFiles = GetFileNames(Application.streamingAssetsPath,"*.png");
    loadedTextures = new Texture2D[imgFiles.Length];

    for(int i = 0; i < loadedTextures.Length; i++)
    {
      string path = "file:///" + Application.streamingAssetsPath + "/" + imgFiles[i];
      Debug.Log(path);
      StartCoroutine(GetImage(path,i));
    }
  }

  void GetMovies()
  {
    movieFiles = GetFileNames(Application.streamingAssetsPath,"*.mp4");
  }

  void Start()
  {
    movieGO.SetActive(false);
    imageGO.SetActive(true);
  }

  void Update()
  {
    if(!assetsLoaded)
    {
      GetImages();
      GetMovies();
      assetsLoaded = true;
    }

    if(Input.GetKeyUp(KeyCode.LeftArrow))
    {
      ParseUI(InputType.back);
    }

    if(Input.GetKeyUp(KeyCode.RightArrow))
    {
      ParseUI(InputType.next);
    }

    if(Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
    {
      ParseUI(InputType.swap);
    }

    if(Input.GetKeyUp(KeyCode.Space))
    {
      moviePlaying = !moviePlaying;

      if(moviePlaying && movieGO.activeInHierarchy == true)
      {
        moviePlayer.Control.Play();
      }

      if(!moviePlaying && movieGO.activeInHierarchy == true)
      {
        moviePlayer.Control.Pause();
      }
    }
  }
}
