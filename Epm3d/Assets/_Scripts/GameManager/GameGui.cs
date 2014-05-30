using UnityEngine;
using System.Collections;
using System;

public class GameGui : MonoBehaviour
{
   public event EventHandler<EventArgs> OnNewWaveRequested = delegate {};
   
   public GUISkin thisMetalGUISkin;
   public GUISkin thisOrangeGUISkin;

   /// <summary>
   /// set to true to display end of wave popup
   /// It is the po factory that sets this
   /// </summary>
   public bool IsWaveComplete;

   // The various texts and their shadows, to make something like this:
   // Approved: 1    Rejected: 4    Remaining: 12
   public GUIText PoApprovedVal;
   public GUIText PoApprovedValS;
   public GUIText PoRejectedVal;
   public GUIText PoRejectedValS;
   public GUIText PoRemainingVal;
   public GUIText PoRemainingValS;
   private bool _isHelpVisible;
   private Rect _rectWindowWave;
   private Rect _rectWindowWaveWhenHelpAlso;
   private Rect _rectWindowHelp;
   private int _countPoApproved;
   private int _countPoRejected;
   private int _countPoRemaining;
   //private float _alpha;
   private MouseLook _mouseLook;

   private bool _isPaused;
   private float _lastUnpausedTimeScale;

   void Awake()
   {
      // ref to mouse controller
      GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
      if (camera != null)
      {
         _mouseLook = camera.GetComponent<MouseLook>();
      }

      if (Debug.isDebugBuild)
         _isHelpVisible = false;
      else
         _isHelpVisible = true;
         
      //int screenX = Screen.width;
      //int screenY = Screen.height;
      int guiWidth = 260;
      int guiHeightWave = 160;
      int guiHeightHelp = 260;

      _rectWindowWave = new Rect(10, 35, guiWidth, guiHeightWave);
      _rectWindowWaveWhenHelpAlso = new Rect(10, 40 + guiHeightHelp, guiWidth, guiHeightWave);
      _rectWindowHelp = new Rect(10, 35, guiWidth, guiHeightHelp);

      _isPaused = false;
      _lastUnpausedTimeScale = 1f;
   }

   void Start()
   {
      // init values    
      _countPoApproved = 0;
      _countPoRejected = 0;
      _countPoRemaining = 0;
      RefreshGuiTextObjects();
   }

   void Update()
   {
      // Framerate and pausing in dev builds
      if (Debug.isDebugBuild)
      {
         if (Input.GetKeyUp(KeyCode.I)) // I = halve it
         {
            Time.timeScale = ( 0.5f * Time.timeScale);
         }

         if (Input.GetKeyUp(KeyCode.O)) // O = back to normal
         {
            Time.timeScale = 1f;
         }

         if (Input.GetKeyUp(KeyCode.P)) // P = pause
         {            
            _isPaused = !_isPaused;
            if (_isPaused)
            {
               // We have just become PAUSED
               // store the current timescale so we can restore it after unpausing
               _lastUnpausedTimeScale = Time.timeScale;
               Time.timeScale = 0f;
            }
            else
            {
               // We have just become UNPAUSED
               // restore last timescale
               Time.timeScale = _lastUnpausedTimeScale;
            }
         }
      }

      // Input - toggle help
      if (Input.GetButtonUp("Help"))
      {
         _isHelpVisible = !_isHelpVisible;
      }

      // Input - if wave complete might ask for another
      if (IsWaveComplete)
      {
         if (Input.GetKeyUp(KeyCode.Y))
         {
            OnNewWaveRequested(this, null);
            IsWaveComplete = false;
         }
      }

      // Input - toggle mouse invert
      if (Input.GetKeyUp(KeyCode.M))
      {
         if (_mouseLook != null)
         {
            _mouseLook.InvertY();
         }
      }
   }

   void OnGUI()
   {
      // Help
      if (_isHelpVisible)
      {
         //GUI.skin = thisOrangeGUISkin;
         GUI.skin = thisMetalGUISkin;
         _rectWindowHelp = GUI.Window(10, _rectWindowHelp, DisplayWindowHelp, "Help (F1)", GUI.skin.GetStyle("window"));
      }

      // Wave Clear
      if (IsWaveComplete)
      {
         // wave complete window might have to move down to make room for help
         if (_isHelpVisible)
         {
            GUI.skin = thisMetalGUISkin;
            _rectWindowWaveWhenHelpAlso = GUI.Window(11, _rectWindowWaveWhenHelpAlso, DisplayWindowWave, "Wave Cleared", GUI.skin.GetStyle("window"));
         }
         else
         {
            GUI.skin = thisMetalGUISkin;
            _rectWindowWave = GUI.Window(11, _rectWindowWave, DisplayWindowWave, "Wave Cleared", GUI.skin.GetStyle("window"));
         }
      }
   }

   public int GetPoRemaining()
   {
      return _countPoRemaining;
   }

   private void DisplayWindowHelp(int windowID)
   { 
      //_alpha = Mathf.Lerp(_alpha, 1, 0.1f * Time.deltaTime); 
      //GUI.color = new Color(1,1,1, _alpha);
      
      int xStart = 20;
      int yStart = 30;
      int yIncr = 20;
      int y = yStart;
      
      GUI.Label(new Rect(xStart, y, 600, 60), "Press F1 to toggle this help window.");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "* Use mouse to look around.");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "* Use W A S D or cursor keys");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "  to move around.");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "* Place crosshair over a PO to");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "  see its details. Then press");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "  Y or N to approve or reject it");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "* Interact with Charts by stepping");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "  on the buttons nearby them");
   }

   private void DisplayWindowWave(int windowID)
   { 
      //alpha = Mathf.Lerp(alpha, 1, 0.1f * Time.deltaTime); 
      //guiTexture.color = new Color(1,1,1, alpha);
      
      int xStart = 20;
      int yStart = 30;
      int yIncr = 20;
      int y = yStart;
      
      GUI.Label(new Rect(xStart, y, 600, 60), "PO Wave Completed");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Would you like another wave?");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Press Y for more exciting");
      GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "PO approval action!");
      
      //GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Label:" + ChartBarRecord.BarValueLabel);
      //GUI.Label(new Rect(xStart, y += yIncr, 600, 60), "Last Refreshed: 11.05.2014");
      
   }

   public void PoCreated()
   {
      IsWaveComplete = false; 
      _countPoRemaining++;
      RefreshGuiTextObjects();
   }

   public void PoApproved()
   {
      _countPoApproved++;
      _countPoRemaining--;
      RefreshGuiTextObjects();
   }

   public void PoRejected()
   {
      _countPoRejected++;
      _countPoRemaining--;
      RefreshGuiTextObjects();
   }
   
   private void RefreshGuiTextObjects()
   {
      if (_countPoRemaining < 0)
      {
         _countPoRemaining = 0;
      }
      
      PoApprovedVal.text = "Approved: " + _countPoApproved.ToString();
      PoApprovedValS.text = "Approved: " + _countPoApproved.ToString();
   
      PoRejectedVal.text = "Rejected: " + _countPoRejected.ToString();
      PoRejectedValS.text = "Rejected: " + _countPoRejected.ToString();
   
      PoRemainingVal.text = "Remaining: " + _countPoRemaining.ToString();
      PoRemainingValS.text = "Remaining: " + _countPoRemaining.ToString();      
   }

}
