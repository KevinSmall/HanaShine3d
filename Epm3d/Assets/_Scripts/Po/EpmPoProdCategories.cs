using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using Epm3d;

namespace Epm3d
{
   public enum EpmPoProductCategory
   {
      Electronics,
      Notebooks,
      Speakers
   }
   /// <summary>
   /// Epm po textures.
   /// Holds all possible PO textures as properties. These get assigned in inspector and compiled
   /// into build.  Needed because SP6 doesnt have images saved with the data in HANA.
   /// </summary>
   [System.Serializable]
   public class EpmPoProdCategories
   {
      public Color Notebooks;
      public Color Electronics;
      public Color Speakers;
      public Color Beamer; 
      //pub Color Flat screens
      //Flat-screen television 
      //Graphic cards 
      public Color Handheld;
      public Color Handhelds; 
      public Color Hardware; 
      public Color Headset; 
      //High Tech 
      //Ink jet printers 
      //Input device
      public Color Keyboards;
      //Laser printers 
      //MP3-Player
      public Color Mice;
      //Multi function printers 
      public Color Others; 
      public Color PC;
      public Color Scanner; 
      //Scanner and Printer 
      public Color Software;
      //Workstation ensemble 
      
      /// <summary>
      /// Gets the tint for thr product category
      /// </summary>
      public Color GetTintForProductCategory(string prodCat)
      {  
         string propertyName = prodCat;
         FieldInfo propertyInfo = this.GetType().GetField(propertyName);
         if (propertyInfo == null)
         {
            Debug.Log("no tint found for category " + prodCat);
            return Color.white;
         }
         else
         {
            return (Color)propertyInfo.GetValue(this);
         }
      }
   }
}