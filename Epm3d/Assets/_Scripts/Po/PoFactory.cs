using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Epm3d;

/// <summary>
/// Makes call to game manager to request it supply POs that are eligible for approval/rejection.
/// Gamemanager supplies a list of POs, and then this PoFactory creates the PO game objects one at a time.
/// During PO creation, each PO object need one additional call to GameManager to gets all its details.
/// Requires empty child GameObjects called "Generator_*" to mark locations where Pos should spawn.
/// The pyramid of Pos always spawns near the factory, it does use the Generator_* locations.
/// </summary>
public class PoFactory : MonoBehaviour
{
   /// <summary>
   /// The spawn variance, each PO position x y z is +/- this amount from factory center 
   /// </summary>
   public Vector3 SpawnVariance;
   public bool IsToCreatePyramids;
   public GameObject PoTemplate;
   public EpmPoTextures PoTexturesMaster;
   public EpmPoProdCategories PoProdCategoriesMaster;
   public int DesiredPoItemCount;
   public float PoCreateDelayInSeconds;

   private enum FactoryState
   {
      Idle,
      Working
   }
   private FactoryState _factoryState;

   private int _actualPoItemCount;
   private int _actualPoHeaderCount;
   private List<GameObject> _generatorLocations;

   /// <summary>
   /// Starts at 0. increased after PO created
   /// </summary>
   private int _posCreatedSoFar;

   /// <summary>
   /// List of POs to create, containing their key, position, behaviours
   /// Doesn't contain HANA PO data, just GAME PO data
   /// </summary>
   private List<PoKeyWithGameAttrs> _posToCreate;
   private List<Vector3> _pyramidPosns;
   private GameGui _gameGui;
   
   private class PoKeyWithGameAttrs
   {
      public string Doc;
      /// <summary>
      /// Postition
      /// </summary>
      public Vector3 Pos;
      /// <summary>
      /// Rotation
      /// </summary>
      public Quaternion Rot;
      public EpmPoMovementType MoveType;
   }

   // Use this for initialization
   void Start()
   {
      print("PoFactory starting...");
      _factoryState = FactoryState.Idle;
      GameManager.Instance.OnSinglePODataWithItemsChanged += OnSinglePODataWithItemsChanged;
      GameManager.Instance.OnMassListPOChanged += OnMassListPOChanged;

      // Store any generator locations defined as child objects of the factory
      _generatorLocations = new List<GameObject>(2);
      foreach (Transform child in transform)
      {
         if (child.gameObject.name.StartsWith("Generator_"))
         {
            _generatorLocations.Add(child.gameObject);
         }
      }

      // Game Gui, to tell it when we create, andto listen to when a new wave asked for 
      GameObject go = GameObject.Find("GameGui");
      if (go != null)
      {
         _gameGui = go.GetComponent<GameGui>();
         _gameGui.OnNewWaveRequested += OnNewWaveRequested;
      }

      // and we're off!
      DoProductionRun();
   }

   private void OnNewWaveRequested(object sender, EventArgs e)
   {
      DoProductionRun();
   }

   /// <summary>
   /// Do a production run of POs.
   /// </summary>
   private void DoProductionRun()
   {
      // init the factory and spawn POs
      _factoryState = FactoryState.Working;
      _posCreatedSoFar = 0;
      print("Po Factory is starting a production run");
      GameManager.Instance.GetEpmData_MassListPO(DesiredPoItemCount);
   }

   private void AskForPoData()
   {
      if (_posCreatedSoFar >= _posToCreate.Count)
      {
         print("AskForPoData cannot create any more POs");
         return;
      }

      PoKeyWithGameAttrs pok = _posToCreate[_posCreatedSoFar];
      //GameManager.Instance.GetEpmData_SinglePO(pok.Doc, pok.Item);
      GameManager.Instance.GetEpmData_SinglePOwithItems(pok.Doc, 20);
   }

   // Update is called once per frame
   void Update()
   {
      // Check to see if wave complete, in which case tell Gui so it can display end-of-wave dialog
      if (_factoryState == FactoryState.Idle && _gameGui != null)
      {
         if (_gameGui.GetPoRemaining() == 0)
         {
            _gameGui.IsWaveComplete = true;
         }
      }
   }

   private void OnSinglePODataWithItemsChanged(object sender, EventArgs e)
   {
      //print("PoFactory has received event OnSinglePODataWithItemsChanged");
      CreatePoWithItems();
   }

   private void OnMassListPOChanged(object sender, EventArgs e)
   {
      //print("PoFactory has received event OnMassListPOChanged");
      CreateMassPos();
   }

   private void CreateMassPos()
   {
      EpmPoDataMass poDataMass = GameManager.Instance.GetEpmResponse_MassListPO();

      //-------------------------------------------------------// TODO tidy up
      List<string> poDataMassKeyItem = new List<string>();
      foreach (var item in poDataMass.PoList)
      {
         poDataMassKeyItem.Add(item.PurchaseOrderId);
      }
      List<string> poDataMassKeyUnique = poDataMassKeyItem.Distinct().ToList();
      _actualPoHeaderCount = poDataMassKeyUnique.Count;
      print("Unique Count of PO Headers received: " + _actualPoHeaderCount); 
//      foreach (var item in poDataMassKeyUnique)
//      {
//         print(item);
//      }

      _actualPoItemCount = poDataMass.PoList.Count;

      // How many POs can we create?
      int canCreateItemCount = 0;
      if (_actualPoItemCount >= DesiredPoItemCount)
         canCreateItemCount = DesiredPoItemCount;
      else
         canCreateItemCount = _actualPoItemCount;

      // TODO is this still valid
      print("Count of PO items requested/received/will create: " + DesiredPoItemCount.ToString() + "/" + 
         _actualPoItemCount.ToString() + "/" + canCreateItemCount.ToString());
      //poDataMass.PrintToConsole();
      //-------------------------------------------------------

//      // Now copy list poDataMass to _posToCreate, the lst of po keys we will create
//      _posToCreate = new List<PoKeyWithGameAttrs>(canCreateItemCount);
//      for (int i = 0; i < canCreateItemCount; i++)
//      {
//         _posToCreate.Add(new PoKeyWithGameAttrs(){Doc = poDataMass.PoList[i].PurchaseOrderId});
//      }

      // Now copy list poDataMass to _posToCreate, the lst of po keys we will create
      _posToCreate = new List<PoKeyWithGameAttrs>(poDataMassKeyUnique.Count);
      foreach (var item in poDataMassKeyUnique)
      {
         _posToCreate.Add(new PoKeyWithGameAttrs(){Doc = item});
      }


//      foreach (var item in _posToCreate)
//      {
//         print(item.Doc);
//      }

      // Now we're ready to allocate positions and rotations to the POs held in _posToCreate
      CreateMassPos_AddBehaviours(); 

      // And we're off!! This will create each PO one by one
      Invoke("AskForPoData", PoCreateDelayInSeconds);
   }

   /// <summary>
   /// Add positions and rotations to use for the POs, this is where POs are layed out
   /// and behaviours are added. The keys of the POs are already in the list _posToCreate, this is 
   /// adding to that.
   /// </summary>
   private void CreateMassPos_AddBehaviours()
   {
      // Pyramid
      CalcPyramidPositions();
      int pyramidCount = _pyramidPosns.Count;
      int pyramidPosPlacedSoFar = 0;

      foreach (PoKeyWithGameAttrs pok in _posToCreate)
      {
         // First few Pos placed might be in a pyramid
         if (pyramidCount > pyramidPosPlacedSoFar)
         {
            pok.Pos = _pyramidPosns[pyramidPosPlacedSoFar];
            pok.Rot = Quaternion.identity;
            pok.MoveType = EpmPoMovementType.Static;
            pyramidPosPlacedSoFar++;
         }
         else
         {
            // Locations are +/- variance from a random generator
            Transform t = _generatorLocations[UnityEngine.Random.Range(0, _generatorLocations.Count)].transform;
            pok.Pos = new Vector3(t.position.x + UnityEngine.Random.Range(-SpawnVariance.x, SpawnVariance.x),
                               t.position.y + UnityEngine.Random.Range(-SpawnVariance.y, SpawnVariance.y),
                               t.position.z + UnityEngine.Random.Range(-SpawnVariance.z, SpawnVariance.z));
            pok.Rot = Quaternion.identity;

            // Behaviour, movement type
            if (IsToCreatePyramids)
            {
               float f = UnityEngine.Random.Range(-2f, 1f);
               if (f > 0)
                  pok.MoveType = EpmPoMovementType.Static;
               else
                  pok.MoveType = EpmPoMovementType.MoveRandom;
            }
            else
            {
               pok.MoveType = EpmPoMovementType.MoveRandom;
            }
         }
      }
   }

   /// <summary>
   /// Calculates the pyramid positions & store in _pyramidPosns
   /// </summary>
   void CalcPyramidPositions()
   {
      // first 6 are pyramid run along z axis
      _pyramidPosns = new List<Vector3>(6);
   
      if (!IsToCreatePyramids)
      {
         return;
      }

      // Base Offset from factory
      Vector3 bo = new Vector3(3f, 1f, 2f);
      // Start position of pyramid
      Vector3 sp = this.transform.position + bo;

      // Horizontal & vertical spacing
      float hs = 1.3f;
      float hhs = hs / 2f; //half horizonatal offset
      float vs = 1.1f;

      //3
      _pyramidPosns.Add(new Vector3(sp.x, sp.y, sp.z));
      _pyramidPosns.Add(new Vector3(sp.x, sp.y, sp.z + hs));
      _pyramidPosns.Add(new Vector3(sp.x, sp.y, sp.z + hs + hs));
      //2
      _pyramidPosns.Add(new Vector3(sp.x, sp.y + vs, sp.z + hhs));
      _pyramidPosns.Add(new Vector3(sp.x, sp.y + vs, sp.z + hhs + hs));
      //1
      _pyramidPosns.Add(new Vector3(sp.x, sp.y + vs + vs, sp.z + hhs + hhs));
   }

   private void CreatePoWithItems()
   {
      //-------------------------------------------------------------------------
      // Instatiate PO gameobject
      //-------------------------------------------------------------------------
      GameObject gParent = GameObject.FindWithTag("PoBucket");
      Vector3 spawnPosition = _posToCreate[_posCreatedSoFar].Pos; 
      Quaternion spawnRotation = _posToCreate[_posCreatedSoFar].Rot; 
      spawnRotation.SetLookRotation(new Vector3(0, 270, 0));
      GameObject g = (GameObject)Instantiate(PoTemplate, spawnPosition, spawnRotation);
      if (gParent != null)
      {
         g.transform.parent = gParent.transform;
      }

      // Scale
      float f = UnityEngine.Random.Range(0.85f, 1.15f);
      Vector3 adjustedScale = new Vector3(f, f, f);
      g.transform.localScale = adjustedScale;
      
      //-------------------------------------------------------------------------
      // Fill PO with its PO business data
      //-------------------------------------------------------------------------
      EpmPoDataWithItems powi = GameManager.Instance.GetEpmResponse_SinglePOWithItems();
      //powi.PrintToConsole();
      PoManager pom = g.GetComponent<PoManager>();
      pom.PoBusinessDataWithItems = powi;

      //-------------------------------------------------------------------------
      // Change its texture
      //-------------------------------------------------------------------------
      Texture2D tex = PoTexturesMaster.GetPoTextureForProductId(powi.PoItems[0].ProductId);
      if (tex == null)
      {
         print("WARNING could not find PO texture called " + powi.PoItems[0].ProductId);
         tex = PoTexturesMaster.HT_1000; // just default to something
      }
      g.renderer.material.mainTexture = tex;
      // Set the tint
      Color tint = PoProdCategoriesMaster.GetTintForProductCategory(powi.PoItems[0].ProductCategory);
      g.renderer.material.color = tint;

      //-------------------------------------------------------------------------
      // PO behaviour
      //-------------------------------------------------------------------------
      EpmPoBehaviour pob = new EpmPoBehaviour()
      {
         movementType = _posToCreate[_posCreatedSoFar].MoveType,
         PreRocketWarmUpTime = 2f
      };
      pom.PoBehaviour = pob;
      
      // Tell Gui
      if (_gameGui != null)
      {
         _gameGui.PoCreated();
      }

      // Have we created all the POs?
      _posCreatedSoFar++;
      if (_posCreatedSoFar < _actualPoHeaderCount)
      {
         Invoke("AskForPoData", PoCreateDelayInSeconds);
      }
      else
      {
         // factory is idle
         _factoryState = FactoryState.Idle;
      }
   }
}
