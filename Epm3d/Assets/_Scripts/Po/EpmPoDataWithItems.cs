using System.Collections.Generic;
using System;
using UnityEngine;

namespace Epm3d
{   
   /// <summary>
   /// All the data for a PO with all its items
   /// The header level attributes summarise what is underneath (eg ProductId is really item level, but one
   /// is picked from the lines and promoted to be a header attribute)
   /// </summary>
   [System.Serializable]
   public class EpmPoDataWithItems
   {
      public string Title = "";

      // All the below should be just HEADER level attributes
      // See the list PoItems for the items

      /// <summary>
      /// PO id eg "0300000000"
      /// </summary>
      public string PurchaseOrderId;  

      /// <summary>
      /// Partner id eg "0100000000"
      /// </summary>
      public string PartnerId;  
      /// <summary>
      /// Company Name eg "SAP"
      /// </summary>
      public string CompanyName;  
      /// <summary>
      /// Gross Amount eg 100.12
      /// </summary>
      public float GrossAmount;
      /// <summary>
      /// Currency eg "EUR"
      /// </summary>
      public string Currency;  
      
      // ...
      
      public string ProductId;  
      
      /// <summary>
      /// Product Category eg "Notebooks"
      /// </summary>
      public string ProductCategory;  
      /// <summary>
      /// ProductName eg "Notebook Basic 15"
      /// </summary>
      public string ProductName;  

      public List<EpmPoData> PoItems;

      public EpmPoDataWithItems()
      {
         PoItems = new List<EpmPoData>(8);
      }
      
      public void PrintToConsole()
      {
         foreach (var poi in PoItems)
         {         
            Debug.Log("  po id    :" + poi.PurchaseOrderId);
            Debug.Log("  po item  :" + poi.PurchaseOrderItem);
            Debug.Log("  partner  :" + poi.PartnerId);
            Debug.Log("  company  :" + poi.CompanyName);
            Debug.Log("  amount   :" + poi.GrossAmount.ToString());
            Debug.Log("  currency :" + poi.Currency);
            Debug.Log("  prod id  :" + poi.ProductId);
            Debug.Log("  prod cat :" + poi.ProductCategory);
            Debug.Log("  prod name:" + poi.ProductName);
         }
      }
   }
}