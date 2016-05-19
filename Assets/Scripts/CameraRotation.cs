using UnityEngine;
using System.Collections;

public class CameraRotation : MonoBehaviour
{

  public float xRotationMin = 10.0f;
  public float xRotationMax = 35.0f;
  public float maxSpeed = 5.0f;
  public float acceleration = 0.1f;
  public float deceleration = 8.0f;
  public float maxZoom = 10.0f;
  public float minZoom = 3.0f;
  public float zoomThreshold = 0.05f;

  private bool xAcceleration = false;
  private bool yAcceleration = false;
  private bool zoomAcceleration = false;

  GameObject child;
  Vector3 startRotation = new Vector3();
  float xVector = 0f;
  float yVector = 0f;
  float zoomVector = 0f;

  // Use this for initialization
  void Start()
  {
    startRotation = this.transform.eulerAngles;
    child = transform.GetChild(0).gameObject;
  }

  // Update is called once per frame
  void Update()
  {

    //MouseWheel Zoom
    zoomAcceleration = false;

    float d = Input.GetAxis("ScrollWheel");
    if(d > zoomThreshold || d < -zoomThreshold)
    {
      zoomAcceleration = true;
      zoomVector += acceleration;
    }
    else
    {
      zoomAcceleration = false;
    }

    if(Input.GetKey(KeyCode.Q))
    {
      d -= acceleration;
      zoomAcceleration = true;
    }
    if(Input.GetKey(KeyCode.E))
    {
      d += acceleration;
      zoomAcceleration = true;
    }

    if(!zoomAcceleration)
    {
      zoomVector = zoomVector / deceleration;
    }

    child.transform.localPosition = new Vector3(0f,0f,(Mathf.Clamp((child.transform.localPosition.z + d),minZoom,maxZoom)));

    //Main Rotation
    yAcceleration = false;
    xAcceleration = false;

    if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.RightArrow))
    {
      yVector += acceleration;
      yAcceleration = true;
    }

    if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow))
    {
      yVector -= acceleration;
      yAcceleration = true;
    }

    if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
    {
      xVector += acceleration;
      xAcceleration = true;
    }

    if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
    {
      xVector -= acceleration;
      xAcceleration = true;
    }

    if(!xAcceleration)
    {
      xVector = xVector / deceleration;
    }

    if(!yAcceleration)
    {
      yVector = yVector / deceleration;
    }

    xVector = Mathf.Clamp(xVector,-maxSpeed,maxSpeed);
    yVector = Mathf.Clamp(yVector,-maxSpeed,maxSpeed);

    Vector3 newRotation = new Vector3(xVector,yVector,0f);
    newRotation += this.transform.rotation.eulerAngles;

    float clampedX = Mathf.Clamp(newRotation.x,xRotationMin,xRotationMax);
    newRotation = new Vector3(clampedX,newRotation.y,newRotation.z);

    transform.localRotation = Quaternion.Euler(newRotation);
  }
}
