using UnityEngine;
using System.Collections;
using System;
using Epm3d;

public class ChartSwitchChangeType : MonoBehaviour
{
   private SwitchFloorplate _switchFloorplate;
   private ChartManager _cm;
   
   // Use this for initialization
   void Start()
   {
      _cm = transform.parent.GetComponent<ChartManager>();
      
      _switchFloorplate = GetComponentInChildren<SwitchFloorplate>();
      _switchFloorplate.OnSwitchSteppedOn += OnSwitchSteppedOn;
      _switchFloorplate.OnSwitchSteppedOff += OnSwitchSteppedOff;
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }

   private void OnSwitchSteppedOn(object sender, EventArgs e)
   {
      // rotate through the list of chart data types
      //print("CHANGE TYPE chart switch has received event OnSwitchSteppedOn");
      EpmDataTypeChart newChartType = _cm.ChartDataType;
      newChartType++;

      // wrap
      if (newChartType > EpmDataTypeChart.ProductId)
      {
         newChartType = EpmDataTypeChart.PartnerCompanyName;
      }
      _cm.ChangeChartType(newChartType);
   }

   private void OnSwitchSteppedOff(object sender, EventArgs e)
   {
      //print("CHANGE TYPE chart switch has received event OnSwitchSteppedOff");
   }
}
