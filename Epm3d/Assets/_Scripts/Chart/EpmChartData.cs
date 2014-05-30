using System.Collections.Generic;
using System;

namespace Epm3d
{
   /// <summary>
   /// Various Epm chart "group-by" categories
   /// </summary>
   public enum EpmDataTypeChart
   {
      PartnerCompanyName,
      ProductCategory,
      PartnerCity,
      PartnerPostalCode,
      ProductId
   }

   /// <summary>
   /// Custom event args to pass chart type
   /// </summary>
   public class ChartEventArgs : EventArgs
   {
      public EpmDataTypeChart ChartType { get; set; }
   }

   public struct EpmChartRecord
   {
      /// <summary>
      /// Category name eg Munich, Notebooks
      /// </summary>
      public string BarName;      
      /// <summary>
      /// Category value eg 100
      /// </summary>
      public float BarValue;
      /// <summary>
      /// Optional category label, eg USD, meters, can leave as null
      /// </summary>
      public string BarValueLabel;
   }

/// <summary>
/// All the data for a chart: title, all the records with name/value/labels
/// </summary>
   public class EpmChartData
   {
      public List<EpmChartRecord> Records;
      public string ChartTitle = "";
      public DateTime LastRefresh = DateTime.UtcNow;

      public EpmChartData(bool isCreatedWithInitialBarRecords)
      {
         Records = new List<EpmChartRecord>(8);
         if (isCreatedWithInitialBarRecords)
         {
            for (int i = 0; i < 5; i++)
            {
               Records.Add(new EpmChartRecord(){
            BarName = "Empty",
            BarValue = 1f,
            BarValueLabel = ""});
            }
         }
      }
   }
}