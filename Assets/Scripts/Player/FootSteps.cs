using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootSteps : MonoBehaviour
{
    public AudioClip[] footstepClips;
    private AudioSource audioSource;
    private Rigidbody rb;
    public float footstepThreshold;
    public float footstepRate;
    private float footstepTime;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(rb.velocity.y) < 0.1f);
        {
            if (rb.velocity.magnitude > footstepThreshold)
            {
                if(Time.time - footstepTime > footstepRate)
                {
                    footstepTime = Time.time;
                    audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)]);
                }
            }
        }
    }
}
