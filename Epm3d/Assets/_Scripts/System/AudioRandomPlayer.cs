﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AudioRandomPlayer : MonoBehaviour
{
   public List<AudioClip> AudioClipList;
   public float DelayBetweenClipsPlaying;
   public float DelayVariance;
   public float PitchVariance;
   public float VolumeBase;
   public float VolumeVariance;
   public float ProbabilityOfPlaying;

   private float _timeSinceLastCheck;

   // Use this for initialization
   void Start()
   {
      _timeSinceLastCheck = 0f;
   }
   
   // Update is called once per frame
   void Update()
   {
      _timeSinceLastCheck += Time.deltaTime;

      if (_timeSinceLastCheck > DelayBetweenClipsPlaying)
      {
         if (UnityEngine.Random.Range(0f, 1f) > (1f - ProbabilityOfPlaying))
         {
            GetComponent<AudioSource>().clip = AudioClipList[UnityEngine.Random.Range(0, AudioClipList.Count)];
            GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(1f - PitchVariance, 1f + PitchVariance);
            GetComponent<AudioSource>().volume = UnityEngine.Random.Range(VolumeBase - VolumeVariance, VolumeBase + VolumeVariance);
            GetComponent<AudioSource>().Play();
         }

         _timeSinceLastCheck = 0f;
      }
   }
}
