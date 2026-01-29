using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Clips")]
    public AudioClip shootSfx;
    public AudioClip hitSfx;
    public AudioClip groundHitSfx;
    public AudioClip playerHurtSfx;
    public AudioClip playerFemaleHurtSfx;
    public AudioClip powerCollectSfx;
    public AudioClip birdHitSfx;


    private void Awake()
    {
        instance = this;
    }

    public void PlayShootSfx()
    {
        PlayClipAtPointCustom(shootSfx, transform.position, .1f);
    }

    public void PlayHitSfx()
    {
        PlayClipAtPointCustom(hitSfx, transform.position, .1f);
    }

    public void PlayGroundHitSfx()
    {
        PlayClipAtPointCustom(groundHitSfx, transform.position, .1f);
    }

    public void PlayHurtSfx()
    {
        PlayClipAtPointCustom(playerHurtSfx, transform.position, .5f);

    }

    public void PlayPowerCollectSfx()
    {
        PlayClipAtPointCustom(powerCollectSfx, transform.position, .1f);

    }

    public void PlayBirdHitSfx()
    {
        PlayClipAtPointCustom(birdHitSfx, transform.position, .1f);

    }

    private void PlayClipAtPointCustom(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume, string audioName = "One shot audio", float spatialBlend = 0f)
    {
        GameObject gameObject = new GameObject(audioName);
        gameObject.transform.position = position;
        AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
        audioSource.clip = clip;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
        audioSource.Play();
        Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

}
