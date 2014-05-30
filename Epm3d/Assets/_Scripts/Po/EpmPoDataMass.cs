
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Epm3d
{
   public struct PoKey
   {
      /// <summary>
      /// PO id eg "0300000000"
      /// </summary>
      public string PurchaseOrderId;  
      /// <summary>
      /// PO item eg "0000000010"
      /// </summary>
      public string PurchaseOrderItem; 
   }

   /// <summary>
   /// List of PO keys with Document and Item
   /// (OData cannot aggregate across items)
   /// </summary>
   public class EpmPoDataMass
   {
      public List<PoKey> PoList;  
      
      public EpmPoDataMass()
      {
         PoList = new List<PoKey>(16);
      }
      
      public void PrintToConsole()
      {
         int i = 0;
         foreach (PoKey item in PoList)
         {
            Debug.Log("index: " + i + "po id:" + item.PurchaseOrderId + "po item:" + item.PurchaseOrderItem);
            i++;
         }        
      }
   }
}