using UnityEngine;
using System.Collections;
using Epm3d;

/// <summary>
/// Does PO detail display
/// Is switched on and off by PO Manager script
/// </summary>
[RequireComponent (typeof(PoManager))]
public class PoGui : MonoBehaviour
{
   public GUISkin thisMetalGUISkin;
   public GUISkin thisOrangeGUISkin;
   private Rect rectWindow;

   // ref to other scripts on the same game object
   private PoManager _pom;

   void Awake()
   {
      int screenX = Screen.width;
      int guiWidth = 260;
      int guiHeight = 380;
      rectWindow = new Rect(screenX - guiWidth - 5, 5, guiWidth, guiHeight);
   }

   void Start()
   {
      // local ref to PO manager
      _pom = GetComponent<PoManager>();
   }

   void OnGUI()
   {
      //GUI.skin = thisOrangeGUISkin;
      GUI.skin = thisMetalGUISkin;
      rectWindow = GUI.Window(0, rectWindow, DisplayWindow, "PO Details", GUI.skin.GetStyle("window"));
   }

   void DisplayWindow(int windowID)
   { 
      //alpha = Mathf.Lerp(alpha, 1, 0.1f * Time.deltaTime); 
      //guiTexture.color = new Color(1,1,1, alpha);

      int xStart = 20;
      int yStart = 30;
      int yIncr = 20;
      int y = yStart;

      GUI.Label(new Rect(xStart, y, 600, 60), "PO id:" + _pom.PoBusinessDataWithItems.PurchaseOrderId);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Partner id:" + _pom.PoBusinessDataWithItems.PartnerId);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Company:" + _pom.PoBusinessDataWithItems.CompanyName);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Main Product id:" + _pom.PoBusinessDataWithItems.ProductId);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Main Category:" + _pom.PoBusinessDataWithItems.ProductCategory);
      //GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Product:" + _pom.PoBusinessDataWithItems.ProductName);
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Total PO Value:" + _pom.PoBusinessDataWithItems.GrossAmount.ToString() + 
                " " + _pom.PoBusinessDataWithItems.Currency);
      y = y + 5;

      foreach (var poi in _pom.PoBusinessDataWithItems.PoItems)
      {
         string trimItem = poi.PurchaseOrderItem.Trim('0');
         GUI.Label(new Rect(xStart, y += yIncr, 600, 60),
                   "-- Item:" + trimItem + " " + poi.ProductId + " " + poi.Quantity.ToString() + " " + poi.QuantityUnit);
      }

      GUI.Label(new Rect(xStart, 320, 600, 60), "Press Y or Left Mouse to approve"); // TODO lookup key in controls
      GUI.Label(new Rect(xStart, 340, 600, 60), "Press N or Right Mouse to reject");

   }
}
