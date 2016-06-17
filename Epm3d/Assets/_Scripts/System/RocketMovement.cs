using UnityEngine;
using System.Collections;

public class RocketMovement : MonoBehaviour
{
   public AudioClip RocketNoise;
   
   // Use this for initialization
   void Start()
   {
      // creation noise
      GetComponent<AudioSource>().clip = RocketNoise;
      GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(0.8f, 1.2f);
      GetComponent<AudioSource>().volume = 1f;
      GetComponent<AudioSource>().Play();
      //print("playing rocket");
   }
   
   // Update is called once per frame
   void Update()
   {
   }

   void FixedUpdate()
   {
      // Force mode
      // Option for how to apply a force using Rigidbody.AddForce.
      //   Force Add a continuous force to the rigidbody, using its mass. 
      //   Acceleration Add a continuous acceleration to the rigidbody, ignoring its mass. 
      //   Impulse Add an instant force impulse to the rigidbody, using its mass. 
      //   VelocityChange Add an instant velocity change to the rigidbody, ignoring its mass. 
      
      //rigidbody.AddForce and rigidbody.AddTorque,
      float mag = 200f; //Random.Range(0f, 40f);
      Vector3 dir = Vector3.up; //Random.onUnitSphere;
      this.gameObject.transform.parent.GetComponent<Rigidbody>().AddForce(dir * mag);

   }
}
