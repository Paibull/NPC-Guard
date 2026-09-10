using System;
using UnityEngine;

public static class NPCNoiseSystem
{
    public static event Action<Vector3, float> NoiseReported;

    public static void ReportNoise(Vector3 position, float radius)
    {
        NoiseReported?.Invoke(position, radius);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePlayerNoise()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<PlayerNoise>() == null)
            player.AddComponent<PlayerNoise>();
    }
}
