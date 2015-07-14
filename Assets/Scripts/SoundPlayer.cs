using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
	[SerializeField]
	private AudioClip clip;

	private AudioSource audioSource;

	void Start ()
	{
		audioSource = gameObject.GetComponent<AudioSource>();
		audioSource.clip = clip;
		audioSource.loop = true;
	}

	public void Play()
	{
		if (audioSource.isPlaying == false)
		{
			audioSource.Play();
		}
	}

	public void Stop()
	{
		audioSource.Stop();
	}
}
