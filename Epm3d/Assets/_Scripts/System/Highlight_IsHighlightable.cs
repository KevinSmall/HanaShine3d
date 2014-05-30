using UnityEngine;
using System.Collections;
using System;
/// <summary>
/// Highlight/Unhighlight visuals and event raiser.
/// If this script gets told it's owning object has been highlighted (done by changing global IsHighlighted, 
/// can be done eg from the Highlight_WhatIsLookedAt script) then do a shader change and raise event.
/// If this script is not told EVERY TICK that it is highlighted, then it will revert itself back to being
/// unhighlighted, and raise another event. See also comments in the Highlight_WhatIsLookedAt script.
/// </summary>
public class Highlight_IsHighlightable : MonoBehaviour
{
   public bool IsHighlighted = false;

   public event EventHandler<EventArgs> OnHighlighted = delegate {};
   public event EventHandler<EventArgs> OnUnhighlighted = delegate {};

   private Shader _shaderNotHighlighted;
   private Shader _shaderHighlighted;

   void Awake()
   {
      gameObject.tag = @"Highlightable";

      _shaderNotHighlighted = gameObject.renderer.material.shader; //Shader.Find(@"Diffuse");
      _shaderHighlighted = Shader.Find(@"Self-Illumin/BumpedSpecular");
   }

   void LateUpdate()
   {
      if (IsHighlighted)
      {
         if (renderer.material.shader != _shaderHighlighted)
         {
            // We have become highlighted
            renderer.material.shader = _shaderHighlighted;
            OnHighlighted(this.gameObject, EventArgs.Empty);
            //Debug.Log("...became Highlighted");
         }         
      }
      else
      {
         //added a check to try and optimize the code. I think an if comparison is faster then
         // setting a shader to an object.
         if (renderer.material.shader != _shaderNotHighlighted)
         {
            // We have become un-highlighted
            renderer.material.shader = _shaderNotHighlighted;  
            OnUnhighlighted(this.gameObject, EventArgs.Empty);
            //Debug.Log("...became Not Highlighted");
         }  
      }
      IsHighlighted = false;
   }
}
