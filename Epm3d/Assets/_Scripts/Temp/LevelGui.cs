using UnityEngine;
using System.Collections;
using System;
using Epm3d;

public class LevelGui : MonoBehaviour
{
   private bool isChartDataEventReceived = false;
   private bool isSinglePOApprovedEventReceived = false;   

   // Initialize level
   void Start()
   {
      GameManager.Instance.OnChartDataChanged += OnChartDataChanged;
      GameManager.Instance.OnSinglePOApproved += OnSinglePOApproved;
   }

   private void OnChartDataChanged(object sender, EventArgs e)
   {
      print("LevelGui object has received event OnChartDataChanged");
      isChartDataEventReceived = true;  
   }

   private void OnSinglePOApproved(object sender, EventArgs e)
   {
      print("LevelGui object has received event OnSinglePOApproved");
      isSinglePOApprovedEventReceived = true;
   }

   //---------------------------------------------------------------------------------------------------
   // OnGUI()
   //--------------------------------------------------------------------------------------------------- 
   // Provides a GUI on level scenes
   // the GUI gives us two buttons to move between the two level scenes and in the process updates 
   // the "activeLevel" property in the state manager.
   //---------------------------------------------------------------------------------------------------
   void OnGUI()
   {    
      if (GUI.Button(new Rect(30, 30, 150, 30), "Get Chart Data"))
      {
         //GameManager.Instance.GetEpmData_Chart(GameManager.EpmDataTypeChart.ProductCategory, "USD");
         GameManager.Instance.GetEpmData_Chart(EpmDataTypeChart.PartnerCompanyName, "EUR");
      }

      if (GUI.Button(new Rect(430, 30, 150, 30), "Approve PO"))
      {
         GameManager.Instance.GetEpmData_ApprovePO("0300000010");
      }

      // Output stats
      GUI.Label(new Rect(30, 90, 600, 60), "Chart: " + GameManager.Instance.GetEpmResponse_Chart());
      GUI.Label(new Rect(30, 270, 600, 60), "Approve PO: " + GameManager.Instance.GetEpmResponse_ApprovedPO());
      
      if (isChartDataEventReceived)
      {
         GUI.Label(new Rect(30, 360, 600, 60), "Chart data event received");
      }
      if (isSinglePOApprovedEventReceived)
      {
         GUI.Label(new Rect(30, 420, 600, 60), "Single PO approved event received");
      }
   }

   // Update is called once per frame
   void Update()
   {
   
   }
   
   void OnDestroy()
   {
      //GameManager.Instance.OnChartDataChanged -= OnChartDataChanged;
      //GameManager.Instance.OnSinglePODataChanged -= OnSinglePODataChanged;
      //GameManager.Instance.OnSinglePOApproved -= OnSinglePOApproved;
      
      Debug.Log("LevelGui script was destroyed", this);      
   }
}
