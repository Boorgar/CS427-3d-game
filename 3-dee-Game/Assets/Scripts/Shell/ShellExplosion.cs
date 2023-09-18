using Unity.Netcode;
using UnityEngine;

public class ShellExplosion : NetworkBehaviour
{
    public LayerMask m_TankMask;
    public GameObject m_particlesPrefab;

    private GameObject explosion;
    private ParticleSystem m_ExplosionParticles;       
    private AudioSource m_ExplosionAudio;              
    
    public float m_MaxDamage = 100f;                  
    public float m_ExplosionForce = 1000f;            
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;              


    private void Start()
    {
        Destroy(gameObject, m_MaxLifeTime);
    }

    [ServerRpc]
    private void explodeServerRpc()
    {
        explosion = Instantiate(m_particlesPrefab, transform.position, Quaternion.Euler(-90, 0, 0));
        explosion.GetComponent<NetworkObject>().Spawn();
        m_ExplosionParticles = explosion.GetComponent<ParticleSystem>();
        m_ExplosionAudio = explosion.GetComponent<AudioSource>();
        explode_ClientRpc();
    }

    [ClientRpc]
    private void explode_ClientRpc()
    {
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        Destroy(explosion, m_ExplosionParticles.main.duration);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Find all the tanks in an area around the shell and damage them.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        for(int i=0; i<colliders.Length; i++)
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
        explosion = Instantiate(m_particlesPrefab, transform.position, Quaternion.Euler(-90, 0, 0));
        //explosion.GetComponent<NetworkObject>().Spawn();
        m_ExplosionParticles = explosion.GetComponent<ParticleSystem>();
        m_ExplosionAudio = explosion.GetComponent<AudioSource>();
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        Destroy(explosion, m_ExplosionParticles.main.duration);
        Destroy(gameObject);

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