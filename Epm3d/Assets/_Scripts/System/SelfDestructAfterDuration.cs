using UnityEngine;
using System.Collections;

public class SelfDestructAfterDuration : MonoBehaviour
{
   public float DurationUntilDestruct;

   // Use this for initialization
   void Start()
   {
      Destroy(gameObject, DurationUntilDestruct);
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }
}
