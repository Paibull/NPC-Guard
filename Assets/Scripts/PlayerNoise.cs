using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [SerializeField] private float noiseRadius = 12f;
    [SerializeField] private float noiseInterval = 0.5f;
    private float timer;

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f || !IsMoving())
            return;

        timer = noiseInterval;
        Debug.Log($"{name}: NOISE at {transform.position}");
        NPCNoiseSystem.ReportNoise(transform.position, noiseRadius);
    }

    private bool IsMoving()
    {
        return Input.GetAxisRaw("Horizontal") != 0f ||
               Input.GetAxisRaw("Vertical") != 0f;
    }
}
