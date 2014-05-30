using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Controls PO movement, works out where to go based on PoBehaviour and does the movement. 
/// </summary>
[RequireComponent (typeof(PoManager))]
public class PoMover : MonoBehaviour
{
   /// <summary>
   /// From 50 to 60 is good
   /// </summary>
   public Vector2 HoppingForceRangeFromTo;

   /// <summary>
   ///  Radius of circle around currnet pos where next waypoint will be
   /// </summary>
   public float NextWaypointRadius;

   /// <summary>
   /// If this close to waypoint, then deemed to be AT the waypoint
   /// </summary>
   public float CloseEnoughToWaypoint;

   /// <summary>
   /// Minimum time delay between hops (PO will sit at rest waiting if it lands early)
   /// Actual time could be longer, because PO will only hop again if it is stationary
   /// </summary>
   public float HoppingTime;

   /// <summary>
   /// Max time allowed to reach destination. If this is exceeded a new WP is chosen
   /// </summary>
   public float MaxTimeToDestination;

   public List<AudioClip> AudioHopping;
   
   private float _timeToDestinationTimer;
   //private PoManager _pom;
   private Vector3 _nextWaypoint;
   private float _hopTimer;

   /// <summary>
   /// Internal record of movement state to control hopping
   /// </summary>
   private MovementState _movementState = MovementState.Idle;
   private enum MovementState
   {
      Idle,
      /// <summary>
      /// First hop is used to rotate PO to look at its destination
      /// </summary>
      FirstHop,
      Hopping
   }

   void Awake()
   {
      // Refs
      //_pom = GetComponent<PoManager>();

      // Randomise a bit
      HoppingTime += HoppingTime * UnityEngine.Random.Range(-0.5f, 0.5f);
      HoppingForceRangeFromTo += HoppingForceRangeFromTo * (UnityEngine.Random.Range(-0.5f, 0.5f));

      // Ready to go
      _movementState = MovementState.Idle;
      _hopTimer = HoppingTime;
      _timeToDestinationTimer = MaxTimeToDestination;
   }

   private void GetNewWaypoint()
   {
      // make up a waypoint
      Vector2 nextWPvector = UnityEngine.Random.insideUnitCircle.normalized * NextWaypointRadius;
      Vector3 nextWPfloor = new Vector3(this.transform.position.x + nextWPvector.x, this.transform.position.y, this.transform.position.z + nextWPvector.y);
      // TODO sample height to get a beter targer waypoint, in case its up a hill?
      _nextWaypoint = nextWPfloor;
   }
   
   void FixedUpdate()
   {
      // Force mode
      // Option for how to apply a force using Rigidbody.AddForce.
      //   Force Add a continuous force to the rigidbody, using its mass. 
      //   Acceleration Add a continuous acceleration to the rigidbody, ignoring its mass. 
      //   Impulse Add an instant force impulse to the rigidbody, using its mass. 
      //   VelocityChange Add an instant velocity change to the rigidbody, ignoring its mass. 

      //------------------------------------------------------------------------------------------
      // Execute every tick
      //------------------------------------------------------------------------------------------
      _timeToDestinationTimer -= Time.fixedDeltaTime;
      switch (_movementState)
      {
         case MovementState.Idle:
            break;
               
         case MovementState.FirstHop:
            // smoothly look at new destination
            var newRotation = Quaternion.LookRotation(_nextWaypoint - transform.position).eulerAngles;
            newRotation.x = 0;
            newRotation.z = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(newRotation), 3f * Time.deltaTime);
            break;
               
         case MovementState.Hopping:
            // If it has taken too long to get to destination, then lets choose a new WP
            if (_timeToDestinationTimer < 0)
            {
               _movementState = MovementState.Idle;
               _timeToDestinationTimer = MaxTimeToDestination;
            }

            break;
               
         default:
            break;
      }

      //------------------------------------------------------------------------------------------
      // Only execute once per hop, and only when stationary
      //------------------------------------------------------------------------------------------
      _hopTimer -= Time.fixedDeltaTime;
      if (_hopTimer < 0 && rigidbody.velocity.magnitude < 0.1f)
      {
         switch (_movementState)
         {
            case MovementState.Idle:
               GetNewWaypoint();
               // next state
               // First hop is bounce and rotate to face destination when roughly at top of bounce
               // Give a little bounce here, then other code does the rotation (see "Execute every tick" section above)
               rigidbody.AddForce(new Vector3(0f, 190f, 0f));
               _movementState = MovementState.FirstHop;
              
               MakeHopNoise();

               break;
         
            case MovementState.FirstHop:
               // next state
               _movementState = MovementState.Hopping;
               break;

            case MovementState.Hopping:
               // if close enough to WP, make Idle so that a new WP is chosen next tick
               Vector3 distVect3 = this.transform.position - _nextWaypoint;
               Vector2 distVect2 = new Vector2(distVect3.x, distVect3.z);
               float dist = distVect2.magnitude;
               if (dist < CloseEnoughToWaypoint)
               {
                  //print("reached destination");
                  _movementState = MovementState.Idle;
               }

               // Hop!
               float mag = UnityEngine.Random.Range(HoppingForceRangeFromTo.x, HoppingForceRangeFromTo.y);
               // direction
               Vector3 desiredTerrainDir = _nextWaypoint - this.transform.position;
               desiredTerrainDir.Normalize();
               // force needs to kick up a bit
               Vector3 actualForceDir = new Vector3(desiredTerrainDir.x, 2f, desiredTerrainDir.z);
               rigidbody.AddForce(actualForceDir * mag);

               MakeHopNoise();

               break;
            
            default:
               break;
         }
         _hopTimer = HoppingTime;
      }
   }

   private void MakeHopNoise()
   {
      // hop noise
      audio.clip = AudioHopping[UnityEngine.Random.Range(0, AudioHopping.Count)];
      audio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
      audio.volume = 0.55f;      
      audio.Play();
   }
}
