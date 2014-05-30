using UnityEngine;
using System.Collections;
using System;
using Epm3d;

/// <summary>
/// Chart bar names must start with "ChartBar_"
/// </summary>
[RequireComponent (typeof(ChartGui))] 
public class ChartManager : MonoBehaviour
{
   public EpmDataTypeChart ChartDataType;
   public int ChartMaximumHeight;
   public EpmChartData ChartData;
   public AudioClip BarMoveRumble;

   private ChartGui _chartGui;
   private GameObject _gameObjectTitle = null;
   private bool _isFirstRefresh;

   // Use this for initialization
   void Start()
   {
      GameManager.Instance.OnChartDataChanged += OnChartDataChanged;

      _chartGui = GetComponent<ChartGui>();
      _chartGui.enabled = false;
      //_isFirstRefresh = true; // this cauees first refresh to happen without any animation or sound
      _isFirstRefresh = false;

      // listen to all the child bar events for being highlighted
      foreach (Transform child in transform)
      {
         // get ref to title
         if (child.gameObject.name == "Chart Title")
         {
            _gameObjectTitle = child.gameObject;
         }

         if (child.gameObject.name.StartsWith("ChartBar_"))
         {
            Highlight_IsHighlightable hih = child.GetComponent<Highlight_IsHighlightable>();
            if (hih != null)
            {
               hih.OnHighlighted += OnBarHighlighted;
               hih.OnUnhighlighted += OnBarUnhighlighted;
            }
         }
      }
      // create inits but dont push them to models
      ChartData = new EpmChartData(true);

      // Init the chart data with a title
      SetTitle(ChartDataType.ToString());
   }

   /// <summary>
   /// can only be used after Start called since it relies on finding the title gameobject
   /// </summary>
   public void ChangeChartType(EpmDataTypeChart newChartType)
   {
      ChartDataType = newChartType;  // ensures next time refresh is pressed we listen to correct data from gamemanager
      ChartData.ChartTitle = newChartType.ToString();
      SetTitle(ChartData.ChartTitle);
   }

   private void OnBarHighlighted(object sender, EventArgs e)
   {
      GameObject go = (GameObject)sender;
      if (go != null)
      {
         ChartBar cb = go.GetComponent<ChartBar>();
         if (cb != null)
         {
            if (ChartData != null)
            {
               _chartGui.ChartBarRecord = ChartData.Records[cb.BarIndex];
               _chartGui.enabled = true;
            }
         }
      }
   }
   
   private void OnBarUnhighlighted(object sender, EventArgs e)
   {
      _chartGui.enabled = false;
   }

   // Update is called once per frame
   void Update()
   {
   }

   /// <summary>
   /// Take ChartData variable and push all its values out to the bars, deciding on 
   /// whether sfx are needed
   /// </summary>
   private void PushChartDataToModels(bool isRefreshForcedToBeSilent)
   {
      //--------------------------------------------------------------------------------
      // Find max bar value
      //--------------------------------------------------------------------------------
      float max = -1f;
      foreach (var rec in ChartData.Records)
      {
         //print(rec.Name + " = " + rec.Value);
         max = Math.Max(max, rec.BarValue);
      }
      //--------------------------------------------------------------------------------
      // Decide if difference is enough to warrant rumbling
      //--------------------------------------------------------------------------------
      bool isSfxNeededDuringBarChanges = false;
      {
         int i = 0;
         foreach (Transform child in transform)
         {
            if (child.gameObject.name.StartsWith("ChartBar_"))
            {
               // store the index into the list of epmChartData.Records
               ChartBar cb = child.GetComponent<ChartBar>();
               if (cb != null)
               {
                  cb.BarIndex = i;
               }
               // desired bar target height
               float desiredY = ((ChartData.Records[i].BarValue) / max) * ChartMaximumHeight;
               float currentY = cb.CurrentHeight;
               if (currentY > 0)
               {
                  if ((Mathf.Abs(desiredY - currentY) / currentY) > 0.5f)
                  {
                     isSfxNeededDuringBarChanges = true;
                  }
               }
               i++;
            }
         }
      }
      if (isRefreshForcedToBeSilent)
      {
         isSfxNeededDuringBarChanges = false;
      }

      print("ChartManager object is changing Chart data, type = " + ChartDataType + ", Sfx needed = " + isSfxNeededDuringBarChanges.ToString());
      //--------------------------------------------------------------------------------
      // Set heights of children, with or without sfx as required
      //--------------------------------------------------------------------------------
      foreach (Transform child in transform)
      {
         // only adjust actual bars
         if (child.gameObject.name.StartsWith("ChartBar_"))
         {
            ChartBar cb = child.GetComponent<ChartBar>();
            // set the bar target height, the bar itself takes care of movement and sound
            float newY = ((ChartData.Records[cb.BarIndex].BarValue) / max) * ChartMaximumHeight;
            cb.SetTargetHeight(newY, isSfxNeededDuringBarChanges);
            cb.SetText(ChartData.Records[cb.BarIndex].BarName);
         }
      }
      // Title
      SetTitle(ChartData.ChartTitle);
      // Last Refresh
      ChartData.LastRefresh = DateTime.UtcNow;
      // Rumble
      if (isSfxNeededDuringBarChanges)
      {
         // audio for rumbling bar movement
         gameObject.audio.clip = BarMoveRumble;
         gameObject.audio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
         gameObject.audio.volume = 1f;
         gameObject.audio.Play();
      }
   }

   private void OnChartDataChanged(object sender, ChartEventArgs e)
   {
      if (e.ChartType != ChartDataType)
      {
         // not our type
         return;
      }
      ChartData = GameManager.Instance.GetEpmResponse_Chart();
      if (_isFirstRefresh)
      {
         PushChartDataToModels(true);
         _isFirstRefresh = false;
      }
      else
      {
         PushChartDataToModels(false);
      }
   }

   private void SetTitle(string chartTitle)
   {
      // set title
      if (_gameObjectTitle != null)
      {
         TextMesh tm = _gameObjectTitle.GetComponent<TextMesh>();
         tm.text = chartTitle;
      }
   }
}
