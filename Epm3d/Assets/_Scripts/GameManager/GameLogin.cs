using UnityEngine;
using System;
using System.Collections;
using System.Text;
using Epm3d;

/// <summary>
/// GUI to get HANA login info, create singleton GameManager, and start iniital scene.  
/// Usually attached in the gamestart scene. HANA login info stored in PlayerPrefs.
/// Script can be attached to a dummy game object in any scene to have values defaulted in during 
/// testing (allows any scene to be run directly without actually going via login screen) to do this
/// see _isRunImmediatelySkipGui
/// </summary>
public class GameLogin : MonoBehaviour
{
   /// <summary>
   /// Desire to run script immediately during testing, set in the inspector.
   /// This can be attached to a dummy object in a scene to allow the whole login screen 
   /// to be skipped immediately - and all defaults are for offline
   /// </summary>
   public bool _isRunImmediatelySkipGui;

   /// <summary>
   /// Flag if the script was called from the proper main GameLogin screen (scene)
   /// </summary>
   public static bool IsCalledFromLoginScreen;

   /// <summary>
   /// The scene to load when finished (ignored if _isRunImmediatelySkipGui is true)
   /// </summary>
   public string _sceneToLoadWhenFinished;
   public GUISkin thisMetalGUISkin;
   public GUISkin thisOrangeGUISkin;
   private Rect _rectWindowMain;
   private string _gameTitle = "HANA System Details";
   private string _gameVersion = "0.40";

   private const string _epmHostDefault = @"154.199.233.30";
   private const string _epmInstanceDefault = @"00";
   private const string _usernameDefault = "SYSTEM";
   private const string _passwordDefault = ""; //"manager";

   private string _epmHostToEdit = _epmHostDefault;
   private string _epmInstanceToEdit = _epmInstanceDefault;
   private string _usernameToEdit = _usernameDefault;
   private string _passwordToEdit = _passwordDefault;

   private bool _epmIsOfflineToEdit = false;
   private HanaVersion _hanaVersion;
   private bool _isItOkToPressPlayButton = false;
   /// <summary>
   /// connection log displayed on gui
   /// </summary>
   private string _connLog;

   private enum ConnCheckState
   {
      Idle,
      NetCheck,
      ServerUp,
      LoginCred,
      VerDet,
      CheckPassed,
      CheckFailed
   }
   private ConnCheckState _connCheckState;

   void Awake()
   {
      _rectWindowMain = new Rect(20, 20, 640, 520);
   }

   // Use this for initialization
   void Start()
   {
      GameManager.Instance.OnConnNetCheckReturned += OnConnNetCheckReturned;
      GameManager.Instance.OnServerUpReturned += OnServerUpReturned;
      GameManager.Instance.OnLoginCredReturned += OnLoginCredReturned;
      GameManager.Instance.OnVerDetReturned += OnVerDetReturned;
      _connCheckState = ConnCheckState.Idle;

      // if this flag in inspector is false, we must have been called from real GameLogin scene
      if (_isRunImmediatelySkipGui == false)
      {
         GameLogin.IsCalledFromLoginScreen = true;
      }

      // these defaults also set by the "set to defaults" button
      SetDefaults();

      // game version contains version info
#if UNITY_EDITOR
      _gameVersion += ": Editor";
#elif UNITY_WEBPLAYER
      _gameVersion += ": Web Player";
#elif UNITY_STANDALONE
      _gameVersion += ": Standalone";
#else
      _gameVersion += ": Unknown";
#endif

      if (_isRunImmediatelySkipGui)
      {
         // cant set this, it means later calls appear to be offline _epmIsOfflineToEdit = true;
         _sceneToLoadWhenFinished = null;  // if we are skipping gui, leave scene alone
         StartGame();
      }  
   }

   private void SetDefaults()
   {
      _connLog = "";

      // get values from PlayerPrefs if possible
      _epmHostToEdit = PlayerPrefs.GetString("host", _epmHostDefault);
      _epmInstanceToEdit = PlayerPrefs.GetString("instance", _epmInstanceDefault);
      _usernameToEdit = PlayerPrefs.GetString("username", _usernameDefault);
      _passwordToEdit = _passwordDefault;  // dont store pwd

      _hanaVersion = HanaVersion.Unknown;
#if !UNITY_EDITOR
      _epmIsOfflineToEdit = false;
#endif
   }

   // Startscreen GUI
   // OnGUI is called for rendering and handling GUI events.
   // This means that your OnGUI implementation might be called several times per frame (one call per event). 
   // http://docs.unity3d.com/Documentation/Manual/GUIScriptingGuide.html
   void OnGUI()
   {
      if (_isRunImmediatelySkipGui)
         return;

      //GUI.skin = thisOrangeGUISkin;
      GUI.skin = thisMetalGUISkin;
      _rectWindowMain = GUI.Window(0, _rectWindowMain, DisplayWindow, _gameTitle, GUI.skin.GetStyle("window"));
   }

   void DisplayWindow(int windowID)
   {
      bool IsItOkToPressCheckButton = true;

      GUI.Label(new Rect(20, 60, 100, 30), "Host:");
      _epmHostToEdit = GUI.TextField(new Rect(120, 60, 300, 20), _epmHostToEdit, 150);

      GUI.Label(new Rect(20, 90, 100, 30), "Instance:");
      _epmInstanceToEdit = GUI.TextField(new Rect(120, 90, 40, 20), _epmInstanceToEdit, 6);

      GUI.Label(new Rect(20, 120, 100, 30), "Username:");
      _usernameToEdit = GUI.TextField(new Rect(120, 120, 120, 20), _usernameToEdit, 25);
      
      GUI.Label(new Rect(20, 150, 100, 30), "Password:");
      _passwordToEdit = GUI.PasswordField(new Rect(120, 150, 120, 20), _passwordToEdit, "*"[0], 25);
    
      // Tech check, are there enough fields to be able to check?
      if (string.IsNullOrEmpty(_usernameToEdit) || string.IsNullOrEmpty(_passwordToEdit) ||
         string.IsNullOrEmpty(_epmHostToEdit) || string.IsNullOrEmpty(_epmInstanceToEdit))
      {
         IsItOkToPressCheckButton = false;
      }

      // Process check, are we already busy?
      if (_connCheckState == ConnCheckState.NetCheck || 
          _connCheckState == ConnCheckState.ServerUp || 
          _connCheckState == ConnCheckState.LoginCred || 
          _connCheckState == ConnCheckState.VerDet)  
      {
         IsItOkToPressCheckButton = false; 
      }

      // CHECK CONNECTION
      // disable button if fields not all filled
      GUI.enabled = IsItOkToPressCheckButton;
      if (GUI.Button(new Rect(120, 200, 150, 30), "Check Connection"))
      {
         // tidy strings when CHECK is pressed
         _epmHostToEdit = _epmHostToEdit.Trim();
         _epmInstanceToEdit = _epmInstanceToEdit.Trim();
         _usernameToEdit = _usernameToEdit.Trim();         

         // host should not start with http:// or end with /
         if (_epmHostToEdit.StartsWith("http://"))
         {
            _epmHostToEdit = _epmHostToEdit.Substring(7);
         }
         if (_epmHostToEdit.EndsWith("/"))
         {
            _epmHostToEdit = _epmHostToEdit.Trim('/');
         }
         // strip :8000 port information
         if (_epmHostToEdit.Contains(":"))
         {
            int colonPos = _epmHostToEdit.IndexOf(":");
            _epmHostToEdit = _epmHostToEdit.Remove(colonPos);
         }

         // store prefs
         PlayerPrefs.SetString("host", _epmHostToEdit);
         PlayerPrefs.SetString("instance", _epmInstanceToEdit);
         PlayerPrefs.SetString("username", _usernameToEdit);

         CheckConnection();
      }
      GUI.enabled = true;

      // START
      if (_epmIsOfflineToEdit == true)
      {
         _isItOkToPressPlayButton = true;
      }

      GUI.enabled = _isItOkToPressPlayButton;
      if (GUI.Button(new Rect(120, 240, 150, 30), "Start"))
      {
         // ready to go
         StartGame();
      }
      GUI.enabled = true;
      
      // RESET
      if (GUI.Button(new Rect(470, 60, 140, 30), "Reset to Defaults"))
      {
         PlayerPrefs.DeleteKey("host");
         PlayerPrefs.DeleteKey("instance");
         PlayerPrefs.DeleteKey("username");
         SetDefaults();
      }

      if (Debug.isDebugBuild)
      {
         _epmIsOfflineToEdit = GUI.Toggle(new Rect(20, 245, 100, 30), _epmIsOfflineToEdit, "Offline");
      }

      // CONNECTION CHECK LOG
      //"Im a textfield\nIm the second line\nIm the third line\nIm the fourth line"
      GUI.TextArea(new Rect(120, 280, 470, 200), _connLog);

//      GUILayout.BeginArea(new Rect(120, 280, 470, 150));
//      _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(470), GUILayout.Height(150));
//      GUILayout.BeginHorizontal();
//      GUILayout.Label(_connLog);
//      GUILayout.EndHorizontal();
//      GUILayout.EndScrollView();
//      GUILayout.EndArea();

      // version label
      GUI.Label(new Rect(460, 480, 180, 30), "Version " + _gameVersion);
      // Allow dragging
      GUI.DragWindow();
   }



   private void StartGame()
   { 
      // tell Unity not to destroy our gamestate instance when we load a new scene, so that it will persist between scene transitions.
      // calling gamestate.Instance creates the singleton
      DontDestroyOnLoad(GameManager.Instance);
     
      // force offline if we're running gameplay scene directly, and not going via login screen
      if (_isRunImmediatelySkipGui && !GameLogin.IsCalledFromLoginScreen)
      {
         print("WARNING: Forcing offline mode, scene run without GameLogin scene");
         _hanaVersion = HanaVersion.v1_00_60;
         _epmIsOfflineToEdit = true;
         GameManager.Instance.SetOffineMode(true);
      }
      // start new game state
      GameManager.Instance.StartNewGameState(_hanaVersion, 
                                             _epmHostToEdit, _epmInstanceToEdit,
                                             _usernameToEdit, _passwordToEdit,
                                             _epmIsOfflineToEdit, _sceneToLoadWhenFinished);
   }

   private void CheckConnection()
   {
      _connLog = "";

      if (_epmIsOfflineToEdit)
      {
         GameManager.Instance.SetOffineMode(_epmIsOfflineToEdit);
      }

      // Trigger the first connection check
      _connCheckState = ConnCheckState.NetCheck;
      ConnLog("Checking internet connection...");

      // This asynchronously raises event when NetCheck done, and we've hooked event OnConnNetCheckReturned
      // to process results and trigger next check
      GameManager.Instance.GetConnData_NetCheck();
      
      // this just for testing
      //_isItOkToPressPlayButton = true;
   }

   private void OnConnNetCheckReturned(object sender, EventArgs e)
   {
      WwwResponse r = GameManager.Instance.GetConnResponse_NetCheck();

      //print("GameLogin has received event OnConnNetCheckReturned, it contained" + r.WwwError);
      if (String.IsNullOrEmpty(r.WwwError))
         ConnLog("Internet connection ok");
      else
      {
         ConnLog("Internet connection not ok, error: " + r.WwwError);
         ConnLog("Internet connection might be being blocked by a firewall?");
      }
      // Trigger the next connection check
      _connCheckState = ConnCheckState.ServerUp;
      ConnLog("Checking to see if HANA server can be pinged...");
      GameManager.Instance.GetConnData_ServerUp(_epmHostToEdit, _epmInstanceToEdit);
   }

   private void OnServerUpReturned(object sender, EventArgs e)
   {
      WwwResponse r = GameManager.Instance.GetConnResponse_ServerUp();
      //print("GameLogin has received event OnConnNetCheckReturned, it contained" + r.WwwError);
      if (String.IsNullOrEmpty(r.WwwError))
      {
         ConnLog("HANA server pinged ok");
         // Trigger the next connection check
         _connCheckState = ConnCheckState.LoginCred;
         ConnLog("Checking login credentials...");
         GameManager.Instance.StartGamePartial(_epmHostToEdit, _epmInstanceToEdit,
                                               _usernameToEdit, _passwordToEdit);
         GameManager.Instance.GetConnData_LoginCred();
      }
      else
      {
         ConnLog("HANA server ping failed, error: " + r.WwwError);
         _connCheckState = ConnCheckState.CheckFailed;
         ConnLog("If the internet connection check was ok, this means host or instance is wrong, or server is down");
      }
   }

   private void OnLoginCredReturned(object sender, EventArgs e)
   {
      WwwResponse r = GameManager.Instance.GetConnResponse_LoginCred();
      if (String.IsNullOrEmpty(r.WwwError))
      {
         ConnLog("Login credentials ok");
         // Trigger the next connection check
         _connCheckState = ConnCheckState.VerDet;
         ConnLog("Checking HANA version...");
         ConnLog("Check for version " + HanaVersion.v1_00_72.ToString());
         GameManager.Instance.GetConnData_VerDet(HanaVersion.v1_00_72, _epmHostToEdit, _epmInstanceToEdit);
      }
      else
      {
         ConnLog("Login credentials not ok, error: " + r.WwwError);
         ConnLog("Cause is usually incorrect username or password, or the user missing authorisations for EPM demo content");
         _connCheckState = ConnCheckState.CheckFailed;
      }
   }

   private void OnVerDetReturned(object sender, VerDetEventArgs vdea)
   {
      HanaVersion hv = vdea.HanaVer;
      WwwResponse r = GameManager.Instance.GetConnResponse_VerDet(hv);

      if (String.IsNullOrEmpty(r.WwwError))
      {
         // Version determination was ok
         _hanaVersion = hv;
         ConnLog("Version " + hv.ToString() + " is supported");
         // Checks are now complete
         _connCheckState = ConnCheckState.CheckPassed;
         ConnLog("Checks complete, please press Start button!");
         _isItOkToPressPlayButton = true;
      }
      else
      {
         // Check failed, either start a new version check or if we have exhausted
         // supported versions then report failure
         switch (hv)
         {
            case HanaVersion.v1_00_60:
               ConnLog("HANA version is not supported");
               _connCheckState = ConnCheckState.CheckFailed;
               break;
               
            case HanaVersion.v1_00_70:
               // Trigger the next version check
               ConnLog("Check for version " + HanaVersion.v1_00_60.ToString());
               GameManager.Instance.GetConnData_VerDet(HanaVersion.v1_00_60, _epmHostToEdit, _epmInstanceToEdit);         
               break;
               
            case HanaVersion.v1_00_72:
               // Trigger the next version check
               ConnLog("Check for version " + HanaVersion.v1_00_70.ToString());
               GameManager.Instance.GetConnData_VerDet(HanaVersion.v1_00_70, _epmHostToEdit, _epmInstanceToEdit);         
               break;
               
            default:
               break;
         }
      }
   }

   private void ConnLog(string s)
   {
      if (String.IsNullOrEmpty(_connLog))      
         _connLog = s;
      else
         _connLog = _connLog + "\n" + s;
   }

   // Update is called once per frame
   void Update()
   {
   
   }
}
