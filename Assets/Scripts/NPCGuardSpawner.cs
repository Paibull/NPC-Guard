using UnityEngine;

public static class NPCGuardSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnAdditionalGuards()
    {
        NPCBrain original = Object.FindFirstObjectByType<NPCBrain>();
        NPCBrain[] guards = Object.FindObjectsByType<NPCBrain>(FindObjectsSortMode.None);

        if (original == null || guards.Length > 1)
            return;

        original.name = "NPC_Guard_A";

        Vector3[] spawnPositions =
        {
            new Vector3(-6f, 1f, 6f),
            new Vector3(6f, 1f, 6f)
        };

        for (int i = 1; i <= 2; i++)
        {
            NPCBrain clone = Object.Instantiate(
                original,
                spawnPositions[i - 1],
                original.transform.rotation);
            clone.enabled = false;
            clone.name = $"NPC_Guard_{(char)('A' + i)}";
            clone.SetPatrolStartIndex(i);
            clone.enabled = true;
        }
    }
}
