using UnityEngine;
using System.Collections;

public enum InputType
{
none,
back,
swap,
next
}

public class Reciever : MonoBehaviour {

  public InputType inputType;

  public float coolDown = 0.5f;
  private bool takeInput = true;
  private bool cancelled = false;  

  public float responeTime = 2.0f;
  private float currentTime = 0f;

  public delegate void GazedCompleteAction(InputType type);
  public static event GazedCompleteAction OnGazedComplete;

  void OnEnable()
  {
    RayCaster.OnGazed += Viewed;
  }

  void OnDisable()
  {
    RayCaster.OnGazed -= Viewed;
  }

  void ScaleBack()
  {
    LeanTween.scale(this.gameObject,new Vector3(.75f,.75f,.75f),0.25f);
  }

  void OnMouseDown()
  {
    if(OnGazedComplete != null)
    {
      OnGazedComplete(inputType);
    }
    StartCoroutine(CoolDown(3.0f));
    currentTime = 0;
    LeanTween.scale(this.gameObject,new Vector3(1f,1f,1f),0.5f);
    Invoke("ScaleBack",0.5f);
  }

  IEnumerator CoolDown(float delay)
  {
    takeInput = false;
    yield return new WaitForSeconds(delay);
    takeInput = true;
    yield return null;
  }

  void Viewed(string obj)
  {
    if(obj == this.name)
    {
      currentTime += Time.deltaTime;
      cancelled = false;      
    }

    if(obj == "cancel")
    {
      if(!cancelled)
      {
        Cancel();
        cancelled = true;
      }      
    }  

    if(currentTime > responeTime)
    {
      if(OnGazedComplete != null)
      {
        OnGazedComplete(inputType);
      }
      StartCoroutine(CoolDown(3.0f));
      currentTime = 0;
    }

    if(takeInput)
    {
      StartCoroutine(CoolDown(coolDown));
      if(currentTime > 0 && currentTime < 0.5f)
      {
        LeanTween.scale(this.gameObject,new Vector3(1f,1f,1f),0.5f);
      }

      if(currentTime > 0.5f && currentTime < 1.0f)
      {
        LeanTween.color(this.gameObject,new Color(.6f,.5f,.5f,.33f),0.5f);
      }

      if(currentTime > 1.0f && currentTime < 1.5f)
      {
        LeanTween.color(this.gameObject,new Color(.7f,.4f,.4f,.45f),0.5f);
      }

      if(currentTime > 1.5f && currentTime < 2.0f)
      {
        LeanTween.color(this.gameObject,new Color(.8f,.3f,.3f,.66f),0.5f);
      }
    }    
  }

  void Cancel()
  {
    currentTime = 0;
    LeanTween.scale(this.gameObject,new Vector3(.75f,.75f,.75f),0.25f);
    LeanTween.color(this.gameObject,new Color(0.5f,0.5f,0.5f,0.1f),0.25f);
  }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
