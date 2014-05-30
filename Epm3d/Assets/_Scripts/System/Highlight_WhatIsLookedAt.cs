using UnityEngine;
using System.Collections;

/// <summary>
/// If the owner object is looking at a "Highlightable" game object, then tell that target game object
/// that is has been highlighted.  If that gameobject does not receive this in every Update tick
/// then it will revert itself back to being un-highlighted
/// This script should be attached to the main camera.
/// The target "highlightable" game objects need script Highlight_IsHighlightable and the tag "Highlightable"
/// </summary>
public class Highlight_WhatIsLookedAt : MonoBehaviour
{
   // Update is called once per frame
   void Update()
   {
      Vector3 fwd = transform.TransformDirection(Vector3.forward);
      RaycastHit Hit;
      
      if (Physics.Raycast(transform.position, fwd, out Hit, 20))
      {
         GameObject go = Hit.collider.gameObject;
         
         if (go.tag == "Highlightable")
         {
            //Debug.Log("Object-Detected");
            go.GetComponent<Highlight_IsHighlightable>().IsHighlighted = true;
         }
      }
   }
}
