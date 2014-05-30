using UnityEngine;
using System.Collections;
using System;
using Epm3d;

public class ChartSwitchRefreshAll : MonoBehaviour
{
   private SwitchFloorplate _switchFloorplate;
   private ChartManager _cm;

   // Use this for initialization
   void Start()
   {
      _cm = transform.parent.GetComponent<ChartManager>();

      _switchFloorplate = GetComponentInChildren<SwitchFloorplate>();
      _switchFloorplate.OnSwitchSteppedOn += OnSwitchSteppedOn;
      //_switchFloorplate.OnSwitchSteppedOff += OnSwitchSteppedOff;
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }

   private void OnSwitchSteppedOn(object sender, EventArgs e)
   {
      // Refresh data for ALL charts
      //GameManager.Instance.GetEpmData_Chart(_cm.ChartDataType, "EUR");
   }

//   private void OnSwitchSteppedOff(object sender, EventArgs e)
//   {
//      // nothing and no autorepeat
//      //print("REFRESH chart switch has received event OnSwitchSteppedOff");
//   }
}
