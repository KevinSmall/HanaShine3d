using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Audio and visuals for PO events like creation, approval, rejection
/// </summary>
public class PoSfx : MonoBehaviour
{
   // Tried to make particle systems public and assign them in inspector, but could not then get them
   // to display in-game
   public AudioClip AudioAppear;
   public List<AudioClip> AudioExplodes;
   public AudioClip AudioApproved;

   // refs to others scripts & object on the same game object
   private PoManager _pom;
   private ParticleSystem _psAppear;
   private ParticleSystem _psExplode;
   private ParticleSystem _psPreRocketFlash;
   private ParticleSystem _psSteam; 

   void Awake()
   {
      // get handles to necessary particle systems
      ParticleSystem[] psAll = GetComponentsInChildren<ParticleSystem>();
      foreach (ParticleSystem ps in psAll)
      {
         switch (ps.name)
         {
            case "Shockwave":
               _psAppear = ps;
               break;

            case "Explosion05":
               _psExplode = ps;
               break;

            case "Flash02":
               _psPreRocketFlash = ps;
               break;

            case "Steam":
               _psSteam = ps;
               break;

            default:
               break;
         }
      }
   }

   // Use this for initialization
   void Start()
   {
      // local ref to PO manager
      _pom = GetComponent<PoManager>();      

      _pom.OnThisPOApproved += OnThisPOApproved;
      _pom.OnThisPORejected += OnThisPORejected;

      // creation noise
      audio.clip = AudioAppear;
      audio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
      audio.volume = 0.1f;
      audio.Play();

      // creation particles
      _psAppear.Play(true);
   }

   private void OnThisPOApproved(object sender, EventArgs e)
   {
      //print("PoSfx for " + _pom.PoBusinessDataWithItems.PurchaseOrderId + " has received event OnThisPOApproved");

      // approval noise
      audio.clip = AudioApproved;
      audio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
      audio.volume = 1f;
      audio.Play();

      // po steam
      _psSteam.Play(true);

      // pre-rocket flash appears after short delay to watch steam
      float preRccketDelay = _pom.PoBehaviour.PreRocketWarmUpTime;
      Invoke("PreRocketFlash", preRccketDelay);

      // hissing fuse sound? would it need separate game object and audio cot to play concurrnetly with other
      // noises above?
      // TODO
   }

   private void PreRocketFlash()
   {
      _psPreRocketFlash.Play(true);
   }

   private void OnThisPORejected(object sender, EventArgs e)
   {
      //print("PoSfx for " + _pom.PoBusinessDataWithItems.PurchaseOrderId + " has received event OnThisPORejected");

      // explode noise
      audio.clip = AudioExplodes[UnityEngine.Random.Range(0, AudioExplodes.Count)];
      audio.pitch = UnityEngine.Random.Range(0.5f, 1.5f);
      audio.volume = 1f;
      audio.Play();

      // explode particles
      _psExplode.Play(true);
      //_psSmoke.Play(true);
   }
   
   // Update is called once per frame
   void Update()
   {
   
   }
}
