using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TankShooting : NetworkBehaviour
{
    public int m_PlayerNumber = 1;       
    public Rigidbody m_Shell;            
    public Transform m_FireTransform;    
    public Slider m_AimSlider;           
    public AudioSource m_ShootingAudio;  
    public AudioClip m_ChargingClip;     
    public AudioClip m_FireClip;         
    public float m_MinLaunchForce = 15f; 
    public float m_MaxLaunchForce = 30f; 
    public float m_MaxChargeTime = 0.75f;

    private string m_FireButton; 
    private bool m_Fired;
    private float m_ChargeSpeed;

    private NetworkVariable<float> m_CurrentLaunchForce = new NetworkVariable<float>(15f, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    //private float m_CurrentLaunchForce;  


    private void OnEnable()
    {
        m_CurrentLaunchForce.Value = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }


    private void Start()
    {
        m_FireButton = "Fire" + m_PlayerNumber;

        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    private void Update()
    {
        // Track the current state of the fire button and make decisions based on the current launch force.
        if (!IsOwner) return;
        m_AimSlider.value = m_MinLaunchForce;
        if (m_CurrentLaunchForce.Value >= m_MaxLaunchForce && !m_Fired)
        {
            m_Fired = true;
            m_CurrentLaunchForce.Value = m_MaxLaunchForce;
            FireServerRpc();
            m_CurrentLaunchForce.Value = m_MinLaunchForce;
        }
        else if(Input.GetButtonDown(m_FireButton))
        {
            m_Fired = false;
            m_CurrentLaunchForce.Value = m_MinLaunchForce;
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }
        else if (Input.GetButton(m_FireButton) && !m_Fired)
        {
            m_CurrentLaunchForce.Value += m_ChargeSpeed * Time.deltaTime;
            m_AimSlider.value = m_CurrentLaunchForce.Value;
        }
        else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
        {
            FireServerRpc();
            m_CurrentLaunchForce.Value = m_MinLaunchForce;
        }
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        // Instantiate and launch the shell.
        m_Fired = true;
        Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.GetComponent<NetworkObject>().Spawn();
        shellInstance.velocity = m_CurrentLaunchForce.Value * m_FireTransform.forward;
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();
    }
}