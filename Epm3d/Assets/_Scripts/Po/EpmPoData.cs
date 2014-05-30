using System.Collections.Generic;
using System;
using UnityEngine;

namespace Epm3d
{
   /// <summary>
   /// Custom event args to pass PO number
   /// </summary>
   public class PoApprovedEventArgs : EventArgs
   {
      public string PurchaseOrderId;
   }

   /// <summary>
   /// Various PO field names
   /// </summary>
   public enum EpmPoFieldName
   {
      PurchaseOrderId,     // "0300000000"
      PurchaseOrderItem,   // "0000000010"
      PartnerId,           // "0100000000"
      CompanyName,         // "SAP"
      GrossAmount,         // "13224.47"    header level total value
      Currency,            // "EUR"
      LifecycleDesc,       // "New"
      ApprovalDesc,        // "Approved"
      ConfirmationDesc,    // "Initial"
      OrderingDesc,        // "Initial"
      ProductId,           // "HT-1000"
      ProductTypeCode,     // "PR"
      ProductCategory,     // "Notebooks"
      ProductName,         // "Notebook Basic 15"
      ProductDesc,         // "Notebook Basic 15 with 1.7GHz - 15 XGA - 1024MB DDR2 SDRAM - 40GB Hard Disc"
      PartnerCity,         // "Walldorf"
      PartnerPostalCode,   // "69190"
      Quantity,            // "1"
      QuantityUnit         // "EA"
   }
   
   /// <summary>
   /// All the data for a single PO line
   /// </summary>
   [System.Serializable]
   public class EpmPoData
   {
      public string Title = "";
      
      /// <summary>
      /// PO id eg "0300000000"
      /// </summary>
      public string PurchaseOrderId;  
      /// <summary>
      /// PO item eg "0000000010"
      /// </summary>
      public string PurchaseOrderItem;  
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

      public int Quantity;

      public string QuantityUnit;


      public EpmPoData()
      {
         //Records = new List<EpmChartRecord>(8);
      }
      
      public void PrintToConsole()
      {
         Debug.Log("  po id    :" + this.PurchaseOrderId);
         Debug.Log("  po item  :" + this.PurchaseOrderItem);
         Debug.Log("  partner  :" + this.PartnerId);
         Debug.Log("  company  :" + this.CompanyName);
         Debug.Log("  amount   :" + this.GrossAmount.ToString());
         Debug.Log("  currency :" + this.Currency);
         Debug.Log("  prod id  :" + this.ProductId);
         Debug.Log("  prod cat :" + this.ProductCategory);
         Debug.Log("  prod name:" + this.ProductName);
         Debug.Log("  qty      :" + this.Quantity);
         Debug.Log("  qty unit :" + this.QuantityUnit);

      }
   }
}