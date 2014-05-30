using UnityEngine;
using System.Collections;
using Epm3d;

public class ChartFactory : MonoBehaviour
{
   public float DelayBeforeRefresh;

   // Use this for initialization
   void Start()
   {
      Invoke("RefreshChartData1", DelayBeforeRefresh);
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }

   void RefreshChartData1()
   {
      GameManager.Instance.GetEpmData_Chart(EpmDataTypeChart.ProductCategory, "EUR");
      Invoke("RefreshChartData2", DelayBeforeRefresh);
   
   }

   void RefreshChartData2()
   {
      GameManager.Instance.GetEpmData_Chart(EpmDataTypeChart.PartnerCity, "EUR");
      
   }

}
