using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExplosionParticles : NetworkBehaviour
{
    private ParticleSystem m_ExplosionParticles;
    private AudioSource m_ExplosionAudio;

    // Start is called before the first frame update
    void Start()
    {
        m_ExplosionParticles = gameObject.GetComponent<ParticleSystem>();
        m_ExplosionAudio = gameObject.GetComponent<AudioSource>();
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        Destroy(gameObject, m_ExplosionParticles.main.duration);
    }
}
