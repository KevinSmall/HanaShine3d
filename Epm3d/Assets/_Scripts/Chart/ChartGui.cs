using UnityEngine;
using System.Collections;
using Epm3d;

[RequireComponent (typeof(ChartManager))] 
public class ChartGui : MonoBehaviour
{
   public EpmChartRecord ChartBarRecord;

   public GUISkin thisMetalGUISkin;
   public GUISkin thisOrangeGUISkin;
   private Rect rectWindow;

   // ref to other scripts on the same game object
   private ChartManager _cm;

   void Awake()
   {
      int screenX = Screen.width;
      int guiWidth = 260;
      int guiHeight = 180;
      rectWindow = new Rect(screenX - guiWidth - 5, 5, guiWidth, guiHeight);
   }
   
   void Start()
   {
      // local ref to chart manager
      _cm = GetComponent<ChartManager>();
   }
   
   void OnGUI()
   {
      //GUI.skin = thisOrangeGUISkin;
      GUI.skin = thisMetalGUISkin;
      rectWindow = GUI.Window(0, rectWindow, DisplayWindow, "Chart Bar Detail", GUI.skin.GetStyle("window"));
   }

   void DisplayWindow(int windowID)
   { 
      //alpha = Mathf.Lerp(alpha, 1, 0.1f * Time.deltaTime); 
      //guiTexture.color = new Color(1,1,1, alpha);
      
      int xStart = 20;
      int yStart = 30;
      int yIncr = 20;
      int y = yStart;
      
      GUI.Label(new Rect(xStart, y, 600, 60), "Name:" + ChartBarRecord.BarName);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Value:" + ChartBarRecord.BarValue.ToString());
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Label:" + ChartBarRecord.BarValueLabel);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Last Refreshed: " + _cm.ChartData.LastRefresh.ToShortDateString() +
                " " + _cm.ChartData.LastRefresh.ToShortTimeString());
//      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Main Category:" + _pom.PoBusinessDataWithItems.ProductCategory);
//      //GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Product:" + _pom.PoBusinessDataWithItems.ProductName);
//      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Total PO Value:" + _pom.PoBusinessDataWithItems.GrossAmount.ToString() + 
//                " " + _pom.PoBusinessDataWithItems.Currency);
//      y = y + 5;

      
      //GUI.Label(new Rect(xStart, 320, 600, 60), "Press Y or Left Mouse to approve"); // TODO lookup key in controls
      //GUI.Label(new Rect(xStart, 340, 600, 60), "Press N or Right Mouse to reject");
      
   }
}
