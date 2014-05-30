using UnityEngine;
using System.Collections;

public class ChangeTextureToUrlImage : MonoBehaviour
{
   public string url = "http://54.86.47.170:8002/sap/hana/democontent/epm/data/images/HT-1000.jpg";

   IEnumerator Start()
   {
      WWW www = new WWW(url);
      yield return www;
      renderer.material.mainTexture = www.texture;
   }
}

