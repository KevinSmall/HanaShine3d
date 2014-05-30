using UnityEngine;
using System.Collections;

/// <summary>
/// Scales a material to match the owning object's transform
/// Silently fails if the textures do not exist (perf impact? if so could add public var and control in inspector
/// what gets done here)
/// </summary>
public class MaterialCubeScaler : MonoBehaviour
{
   // Use this for initialization
   void Start()
   {  
   }
   
   void Update()
   {
      //Vector2 newScale = new Vector2(Mathf.Cos(Time.time) * 0.5F + 1, Mathf.Sin(Time.time) * 0.5F + 1);
      Vector2 newScale = new Vector2(transform.localScale.x, transform.localScale.y);

      //print(scaleX.ToString() + ", " + scaleY.ToString());
      //renderer.material.mainTextureScale = newScale;  // <-- this is same as line below
      renderer.material.SetTextureScale("_MainTex", newScale);
      renderer.material.SetTextureScale("_BumpMap", newScale);

      // renderer.material.GetTexture(_MainTex).
      // shader properties
      // _Color
      // _MainTex
      // _BumpMap
   }
}

