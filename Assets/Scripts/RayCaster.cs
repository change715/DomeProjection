using UnityEngine;
using System.Collections;

public class RayCaster : MonoBehaviour {

  public delegate void GazeAction(string obj);
  public static event GazeAction OnGazed;

	// Use this for initialization
	void Start () {	}
	
	// Update is called once per frame
	void Update () {    
    Ray ray = this.GetComponent<Camera>().ScreenPointToRay(new Vector3(Screen.width / 2,Screen.height / 2, 0));
    RaycastHit hit;
    if(Physics.Raycast(ray,out hit,100.0f))
    {
      if(OnGazed != null)
      {
        RayCaster.OnGazed(hit.collider.name);
      }
    }
    else
    {
      if(OnGazed != null)
      {
        RayCaster.OnGazed("cancel");
      }
    }      
  }
}
