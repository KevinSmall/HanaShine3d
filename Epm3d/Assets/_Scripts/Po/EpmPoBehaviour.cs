using System.Collections.Generic;
using System;
using UnityEngine;

namespace Epm3d
{
   public enum EpmPoMovementType
   {
      Static,
      MoveRandom,
      MoveRunAway,
      MoveRunToward
   }
   
   /// <summary>
   /// Behaviour data for a PO
   /// </summary>
   [System.Serializable]
   public class EpmPoBehaviour
   {
      public EpmPoMovementType movementType;
           
      /// <summary>
      /// Time in seconds the the PO shakes for, before rocket gets attached 
      /// </summary>
      public float PreRocketWarmUpTime;


      public EpmPoBehaviour()
      {
         //Records = new List<EpmChartRecord>(8);
      }
      
      public void PrintToConsole()
      {
         Debug.Log("PO behaviour assigned by PO factory");
         Debug.Log("  move     :" + this.movementType);
      }
   }
}