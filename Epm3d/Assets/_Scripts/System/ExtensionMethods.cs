using UnityEngine;
using System.Collections;

public static class ExtensionMethods
{
   public static void PlayerPrefsSetBool(string name, bool booleanValue)
   //public static void SetBool(this PlayerPrefs p, string name, bool booleanValue)
   // above doesnt work as extensio methods need object instance
   {
      PlayerPrefs.SetInt(name, booleanValue ? 1 : 0);
   }

   public static bool PlayerPrefsGetBool(string name)
   {
      return PlayerPrefs.GetInt(name) == 1 ? true : false;
   }
}