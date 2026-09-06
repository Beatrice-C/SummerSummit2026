using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Sfx : MonoBehaviour
{
    public static Sfx instance;
    public AudioSource source;

    public AudioClip cardDeal, brushStroke, caught, win;

    private void Awake()
    {
        instance = this;

        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip != null) source.PlayOneShot(clip);
    }
}
