using UnityEngine;
using System.Collections;
using System;
using Epm3d;

/// <summary>
/// Manages a single PO.  Calls GameManager to update PO approval status, waits for response.
/// Listens to highlight events, does other highlight stuff like PO display (via script PoGui), halting
/// movement, everything not related to the highlight visual change on the object (which is done by
/// Highlight_IsHighlightable).
/// </summary>
[RequireComponent (typeof (PoGui))]
public class PoManager : MonoBehaviour
{
   public GameObject PoToDisplayWhenDead;

   public GameObject RocketAttachedWhenApproved;
   
   /// <summary>
   /// PO with Items business data.
   /// </summary>
   public EpmPoDataWithItems PoBusinessDataWithItems;

   /// <summary>
   /// PO gameplay behaviour data (eg movement type)
   /// NB This is assigned values in code, not from inspector, it is left in inspector to help debugging
   /// </summary>
   public EpmPoBehaviour PoBehaviour;

   /// <summary>
   /// Occurs when on this PO approved, when we've heard genuine positive response from gamemanager.
   /// </summary>
   public event EventHandler<EventArgs> OnThisPOApproved = delegate {};

   /// <summary>
   /// Occurs when on this PO rejected, when we've heard genuine positive response from gamemanager.
   /// </summary>
   public event EventHandler<EventArgs> OnThisPORejected = delegate {};

   // refs to others components and scripts on the same game object
   private Highlight_IsHighlightable _scriptIsHighlightable;
   private PoMover _poMover;

   /// <summary>
   /// The _po GUI, set to active when PO highlighted
   /// </summary>
   private PoGui _poGui;

   private GameGui _gameGui;

   private enum PoManagerState
   {
      Normal,
      BusyRequestingApproved,
      BusyChangingStateApproved,
      BusyRequestingRejected,
      BusyChangingStateRejected
   }

   /// <summary>
   /// State machine for this PO manager
   /// Used eg to ensure PO cannot be fiddled with whilst it is being approved or rejected (ie whilst effects are happening)
   /// </summary>
   private PoManagerState _poManagerState;

   private float _shakingTimeLeft;

   void Start()
   {
      _poManagerState = PoManagerState.Normal;
      _shakingTimeLeft = PoBehaviour.PreRocketWarmUpTime;

      // Listen to global approve reject events, which we'll filter to make sure it's only the
      // one that applies to this PO
      GameManager.Instance.OnSinglePOApproved += OnSinglePOApproved;
      GameManager.Instance.OnSinglePORejected += OnSinglePORejected;

      // Highlighting - start stop movement when highlighted
      _scriptIsHighlightable = GetComponent<Highlight_IsHighlightable>();
      if (_scriptIsHighlightable != null)
      {
         _scriptIsHighlightable.OnHighlighted += OnHighlighted;
         _scriptIsHighlightable.OnUnhighlighted += OnUnhighlighted;
      }

      // Po GUI to display PO details
      _poGui = GetComponent<PoGui>();
      _poGui.enabled = false; // always start switched off

      // Game Gui, to tell it when we approve/reject
      GameObject go = GameObject.Find("GameGui");
      if (go != null)
      {
         _gameGui = go.GetComponent<GameGui>();
      }

      // Movement - get reference
      _poMover = GetComponent<PoMover>();

      // Delete mover for static POs
      if (_poMover != null && PoBehaviour.movementType == EpmPoMovementType.Static)
      {
         Component.Destroy(_poMover);
      }
   }

   private void OnSinglePOApproved(object sender, PoApprovedEventArgs e)
   {
      if (e.PurchaseOrderId == this.PoBusinessDataWithItems.PurchaseOrderId)
      {
         // It's us!  Changing this state triggers the shaking effect, which itself is applied in FixedUpdate()
         _poManagerState = PoManagerState.BusyChangingStateApproved;
         // Visuals and audio for the immediate act of approval are done in PoSfx, which listens to this event
         OnThisPOApproved(null, null);
      
         // Stop any other movement
         if (_poMover != null)
         {
            Component.Destroy(_poMover);
         }

         // Set rocket attachment
         Invoke("AttachRocket", PoBehaviour.PreRocketWarmUpTime);
         Invoke("UpdateApprovedPoScore", 4);

         // Self destruct
         // TODO add a destructor if object outside game world box
         Destroy(this.gameObject, 10f);
      }
   }

   private void AttachRocket()
   {
      // allow po to rocket through any obstacles
      GetComponent<Collider>().enabled = false;

      // Attach rocket gameobject to this game object
      // Visuals and audio of the rocket are done in this new gameobject RocketAttachedWhenApproved
      GameObject gParent = this.gameObject;
      Vector3 spawnPosition = this.transform.position; 
      Quaternion spawnRotation = this.transform.rotation; 
      GameObject g = (GameObject)Instantiate(RocketAttachedWhenApproved, spawnPosition, spawnRotation);
      g.transform.parent = gParent.transform;
   }

   private void OnSinglePORejected(object sender, PoApprovedEventArgs e)
   {
      if (e.PurchaseOrderId == this.PoBusinessDataWithItems.PurchaseOrderId)
      {
         // It's us! 
         _poManagerState = PoManagerState.BusyChangingStateRejected;
         // Visuals and audio are done in PoSfx, which listens to this event
         OnThisPORejected(null, null);

         // Hide self
         GetComponent<Renderer>().enabled = false;
         GetComponent<Collider>().enabled = false;

         // Self destruct
         Destroy(this.gameObject, 5f); // delay needed to allow audio and explosion particles to finish

         // Create smoke gameobject as separate gameobject in the PO bucket folder
         // This gameobject PoToDisplayWhenDead prefab could have some models too
         GameObject gParent = GameObject.FindWithTag("PoBucket");
         Vector3 spawnPosition = this.transform.position; 
         Quaternion spawnRotation = this.transform.rotation; 
         GameObject g = (GameObject)Instantiate(PoToDisplayWhenDead, spawnPosition, spawnRotation);
         g.transform.parent = gParent.transform;
      
         // Tell Gui
         if (_gameGui != null)
         {
            _gameGui.PoRejected();
         }
      }
   }

   private void UpdateApprovedPoScore()
   {
      // Tell Gui
      if (_gameGui != null)
      {
         _gameGui.PoApproved();
      }
   }

   private void OnHighlighted(object sender, EventArgs e)
   {
      if (_poMover != null)
      {
         _poMover.enabled = false;
      }
      // po gui script is a required cpt, dont need to check existence
      _poGui.enabled = true;
   }

   private void OnUnhighlighted(object sender, EventArgs e)
   {
      if (_poMover != null)
      {
         _poMover.enabled = true;
      }
      // po gui script is a required cpt, dont need to check existence
      _poGui.enabled = false;
   }

   // Update is called once per frame
   void Update()
   {
      switch (_poManagerState)
      {
         case PoManagerState.Normal:
            if (_poGui.enabled)
            {
               if (Input.GetButtonUp("Approve"))
               {
                  GameManager.Instance.GetEpmData_ApprovePO(PoBusinessDataWithItems.PurchaseOrderId);
                  _poManagerState = PoManagerState.BusyRequestingApproved;
               }
               
               if (Input.GetButtonUp("Reject"))
               {
                  GameManager.Instance.GetEpmData_RejectPO(PoBusinessDataWithItems.PurchaseOrderId);
                  _poManagerState = PoManagerState.BusyRequestingRejected;                  
               }
            }
            break;

         default:
            break;
      }
   }

   void FixedUpdate()
   {
      switch (_poManagerState)
      {
         case PoManagerState.BusyChangingStateApproved:
            _shakingTimeLeft -= Time.deltaTime;
            if(_shakingTimeLeft > 0f)
            {
               // shaking effect
               float mag = UnityEngine.Random.Range(10f, 15f);
               //Vector3 dir = UnityEngine.Random.onUnitSphere;
               Vector2 circ = UnityEngine.Random.insideUnitCircle;
               Vector3 diralt = new Vector3(circ.x, 0.05f, circ.y);
               //rigidbody.AddForce(dir * mag);
               //rigidbody.AddRelativeTorque(diralt * mag);
               GetComponent<Rigidbody>().AddTorque(diralt * mag);
            }
            break;
            
         default:
            break;
      }
   }
}