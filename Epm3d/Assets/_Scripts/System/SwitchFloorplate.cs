using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Generic floorplate switch, with events for standing on, autorepeat, getting off
/// Handles switch movement and audio.
/// </summary>
public class SwitchFloorplate : MonoBehaviour
{
   public event EventHandler<EventArgs> OnSwitchSteppedOn = delegate {};
   public event EventHandler<EventArgs> OnSwitchSteppedOff = delegate {};
   public event EventHandler<EventArgs> OnSwitchAutoRepeat = delegate {};

   public Texture2D SwitchFloorplateTexture;
   public Color TintWhenDown;

   public AudioClip AudioClipStepOn;
   public AudioClip AudioClipStepOff;
   public AudioClip AudioClipAutoRepeat;

   private Color _origTint;
   private bool _isStandingOnSwitch;
   private float _origYposition;
   private float _currentYposition;
   private float _targetYposition;
   private float _movementTolerance = 0.01f;
   private float _buttonSpeedMetersPerSec = 10f;

   void Awake()
   {
      _origYposition = gameObject.transform.localPosition.y;
      _currentYposition = _origYposition;
      gameObject.GetComponent<Renderer>().material.mainTexture = SwitchFloorplateTexture;
      
   }

   // Use this for initialization
   void Start()
   {
      _isStandingOnSwitch = false;
      _origTint = gameObject.GetComponent<Renderer>().material.color;
      
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }

   void FixedUpdate()
   {
      _currentYposition = gameObject.transform.localPosition.y;
      
      if (Mathf.Abs(_currentYposition - _targetYposition) > _movementTolerance)
      {
         //print(child.gameObject.name + ", " + newY);
         Vector3 posLocal = gameObject.transform.localPosition;

         posLocal.y = Mathf.Lerp(posLocal.y, _targetYposition, _buttonSpeedMetersPerSec * Time.fixedDeltaTime); 
         gameObject.transform.localPosition = posLocal;

      }
   }


   void OnTriggerEnter(Collider other)
   {
      //PrintEventObject("OnTriggerEnter", other);
      
      if (other.gameObject.tag == "Player" && !_isStandingOnSwitch)
      {
         _isStandingOnSwitch = true;
         //print("switch entered");
         OnSwitchSteppedOn(this, null);

         // step on noise
         GetComponent<AudioSource>().clip = AudioClipStepOn;
         GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(0.9f, 1.1f);
         GetComponent<AudioSource>().volume = 1f;
         GetComponent<AudioSource>().Play();

         // button moves down
         _targetYposition = -0.10f;

         // darken
         gameObject.GetComponent<Renderer>().material.color = TintWhenDown;
      }
   }

   void OnTriggerExit(Collider other)
   {
      //PrintEventObject("OnTriggerExit", other);
      
      if (other.gameObject.tag == "Player" && _isStandingOnSwitch)
      {
         _isStandingOnSwitch = false;
         //print("switch exited");
         OnSwitchSteppedOff(this, null);
      
         // step off noise
         GetComponent<AudioSource>().clip = AudioClipStepOff;
         GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(0.9f, 1.1f);
         GetComponent<AudioSource>().volume = 1f;
         GetComponent<AudioSource>().Play();

         // button mowes up
         _targetYposition = _origYposition;
      
         // back to orig tint
         gameObject.GetComponent<Renderer>().material.color = _origTint;
      }
   }


   void PrintEventObject(string eventName, Collider other)
   {
      string n = other.gameObject.name;
      string t = other.gameObject.tag;
      print("trigger " + eventName + " triggered by name " + n + " with tag " + t);
   }

}
