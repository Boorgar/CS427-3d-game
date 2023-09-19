using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExplosionParticles : NetworkBehaviour
{
    private ParticleSystem m_ExplosionParticles;
    private AudioSource m_ExplosionAudio;

    public LayerMask m_TankMask;
    public float m_MaxDamage = 100f;
    public float m_ExplosionForce = 1000f;
    public float m_MaxLifeTime = 2f;
    public float m_ExplosionRadius = 5f;
    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        m_ExplosionParticles = gameObject.GetComponent<ParticleSystem>();
        m_ExplosionAudio = gameObject.GetComponent<AudioSource>();
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        checkup_ClientRpc();
        destroy_ServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void destroy_ServerRpc()
    {
        Destroy(gameObject, m_ExplosionParticles.main.duration);
    }

    [ClientRpc]
    private void checkup_ClientRpc()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody target = colliders[i].GetComponent<Rigidbody>();
            if (!target)
                continue;
            target.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            TankHealth targetHealth = target.GetComponent<TankHealth>();
            if (!targetHealth)
                continue;
            float damage = CalculateDamage(target.position);
            targetHealth.TakeDamage(damage);
        }
    }
    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate the amount of damage a target should take based on it's position.
        Vector3 explosionToTarget = (targetPosition - transform.position);
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDmg = (m_ExplosionRadius - explosionDistance) / (m_ExplosionRadius);
        float damage = m_MaxDamage * relativeDmg;
        return Mathf.Max(0f, damage);
    }
}
