using UnityEngine;
using System.Collections;

public class ChartBar : MonoBehaviour
{
   /// <summary>
   /// The index of the corresponding entry in the ChartManager's list of Char values
   /// </summary>
   public int BarIndex;
   public float HeightTolerance;
   public float SpeedMetersPerSec;
   public float CurrentHeight;

   private ParticleSystem _psSteam;
   private float _targetHeight;
   private GameObject _textGameObject;
   private TextMesh _textTextMesh;

   void Awake()
   {
      CurrentHeight = gameObject.transform.localScale.y;
      _targetHeight = CurrentHeight;
   }

   // Use this for initialization
   void Start()
   {
      _psSteam = gameObject.GetComponentInChildren<ParticleSystem>();

      // eg if we are ChartBar_Blue lookfor ChartValue_Blue
      string s = name.Substring(9);
      string sib = "ChartValue_" + s;
      
      // Get sibling that contains the text mesh game object
      Transform parentTransform = transform.parent.transform;
      foreach (Transform child in parentTransform)
      {
         if (child.gameObject.name.Equals(sib))
         {
            // print("found + " + sib);
            _textGameObject = child.gameObject;
            _textTextMesh = _textGameObject.GetComponent<TextMesh>();
         }
      }
   }

   public void SetText(string text)
   {
      if (_textTextMesh != null)
      {
         _textTextMesh.text = text;
      }
   }

   /// <summary>
   /// Sets the target height of the bar, optionally with animation and rumbling, smoke.
   /// Set 2nd parameter to false to jump to the target value instantly, with no sfx.
   /// </summary>
   public void SetTargetHeight(float targetHeight, bool isAnimationDesired)
   {
      if (isAnimationDesired)
      {
         _targetHeight = targetHeight;
         if (_psSteam != null)
         {
            _psSteam.Play(true);
         }
      }
      else
      {
         // set immediately, no movement done in fixed update
         _targetHeight = targetHeight;
         CurrentHeight = targetHeight;

         Vector3 scale = gameObject.transform.localScale;
         scale.y = targetHeight;
         gameObject.transform.localScale = scale;
      }

      SetTextPosition();
   }

   private void SetTextPosition()
   {
      //desired Y pos of label = ( scale of pillar * 0.456175489 ) + 0.49317471
      float ypos = (CurrentHeight * 0.456175489f) + 0.49317471f;
      Vector3 pos = new Vector3(_textGameObject.transform.position.x, ypos, _textGameObject.transform.position.z); 
      if (_textGameObject != null)
      {
         _textGameObject.transform.position = pos;
      }
   }

   void FixedUpdate()
   {
      CurrentHeight = gameObject.transform.localScale.y;

      if (Mathf.Abs(CurrentHeight - _targetHeight) > HeightTolerance)
      {
         //print(child.gameObject.name + ", " + newY);
         Vector3 scale = gameObject.transform.localScale;

         scale.y = Mathf.Lerp(scale.y, _targetHeight, SpeedMetersPerSec * Time.fixedDeltaTime); 
         //scale.y = newY;
         //child.localScale = scale;
         gameObject.transform.localScale = scale;

         SetTextPosition();
      }
   }
}
