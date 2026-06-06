using UnityEngine;
using Robots.Capabilities.Flight;

[RequireComponent(typeof(AudioSource))]
public class RobotRealAudio : MonoBehaviour
{
    private AudioSource audioSource;
    private RobotOperator[] operators;
    private AgroBotFlight drone;

    [Header("Pitch Settings")]
    public float idlePitch = 0.8f;
    public float movingPitch = 1.2f;
    public float workingPitch = 1.4f;

    private float targetPitch = 1.0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; // Full 3D Audio
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 40f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        
        // VOLUM REDUS pentru a nu acoperi restul sunetelor din joc
        audioSource.volume = 0.15f; 

        audioSource.loop = true;
        audioSource.playOnAwake = true;
    }

    void Start()
    {
        operators = GetComponents<RobotOperator>();
        drone = GetComponent<AgroBotFlight>();

        LoadAudioClip();
    }

    private void LoadAudioClip()
    {
        // Încărcăm automat fișierele audio din folderul Assets/Resources/Audio/
        string fileName = (drone != null) ? "drone" : "tractor";
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + fileName);

        if (clip != null)
        {
            audioSource.clip = clip;
            
            // Drona poate fi chiar și mai încet, deoarece zboară sus
            if (drone != null) audioSource.volume = 0.10f; 

            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[Audio] Nu am găsit sunetul '{fileName}' în folderul Assets/Resources/Audio/");
        }
    }

    void Update()
    {
        if (audioSource.clip == null) return;

        targetPitch = idlePitch;
        float targetVolume = 0f; // Implicit sunetul e OPRIT (volum 0) când robotul stă pe loc

        // Dacă e dronă
        if (drone != null)
        {
            string status = drone.GetStatus();
            if (status.StartsWith("Zbor") || status.StartsWith("Scanare")) 
            {
                targetPitch = movingPitch;
                targetVolume = 0.10f; // Fade IN la volum 10%
            }
            else if (status.StartsWith("Tratează")) 
            {
                targetPitch = workingPitch;
                targetVolume = 0.10f;
            }
        }
        // Dacă e robot de sol (tractor)
        else if (operators != null)
        {
            foreach (var op in operators)
            {
                if (op.CurrentState == RobotOperator.OperatorState.Working) 
                {
                    targetPitch = workingPitch;
                    targetVolume = 0.15f; // Fade IN la volum 15%
                }
                else if (op.CurrentState == RobotOperator.OperatorState.MovingToParcel || 
                         (op.CurrentState == RobotOperator.OperatorState.Charging && op.GetStatus() == "Going to Charger")) 
                {
                    targetPitch = movingPitch;
                    targetVolume = 0.15f;
                }
            }
        }

        // Turația motorului se adaptează fin
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * 2.5f);
        
        // Efect de Fade IN / Fade OUT (pornește fin când se mișcă, se oprește complet când stă)
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * 5f);
    }
}
