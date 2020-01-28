using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickSound : MonoBehaviour
{
    public AudioSource mySource;
	public AudioClip sound;

    public void PlaySound()
    {
		mySource.PlayOneShot(sound);
    }

    public static void PlaySoundBySource(string source)
    {
        AudioSource audioSource = GameObject.Find(source).GetComponent<AudioSource>();
        audioSource.Play();
    }

    public static void StopSoundBySource(string source)
    {
        AudioSource audioSource = GameObject.Find(source).GetComponent<AudioSource>();
        audioSource.Stop();
    }
}
