using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState { Patrol, Chase, Search }

    [Header("References")]
    [SerializeField] private NPCSensor sensor;
    [SerializeField] private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointTolerance = 0.7f;
    [SerializeField] private float waypointWaitDuration = 2f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchTolerance = 0.8f;
    [SerializeField] private float searchRotationAngle = 60f;
    [SerializeField] private float searchRotationSpeed = 120f;

    [Header("Debug")]
    [SerializeField] private NPCState currentState;

    private NPCState previousState;
    private int patrolIndex;
    private float waypointWaitTimer;
    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition;
    private bool lastPositionWasHeard;
    private float searchTimer;
    private bool searchingLastKnownPosition;
    private int searchRotationPhase;
    private Quaternion searchStartRotation;
    private GameObject alertChase;
    private GameObject alertSearch;

    private void Awake()
    {
        if (sensor == null)
            sensor = GetComponent<NPCSensor>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            GameObject patrolRoot = GameObject.Find("PatrolPoints");
            if (patrolRoot != null)
            {
                Transform[] points = patrolRoot.GetComponentsInChildren<Transform>();
                patrolPoints = new Transform[points.Length - 1];
                for (int i = 1; i < points.Length; i++)
                    patrolPoints[i - 1] = points[i];
            }
        }
    }

    private void OnEnable() => NPCNoiseSystem.NoiseReported += OnNoiseReported;
    private void OnDisable() => NPCNoiseSystem.NoiseReported -= OnNoiseReported;

    private void Start()
    {
        currentState = NPCState.Patrol;
        previousState = currentState;
        CreateAlertIndicators();
        GoToCurrentPatrolPoint();
        UpdateAlertIndicator();
    }

    private void Update()
    {
        if (sensor == null || agent == null)
            return;

        UpdateMemory();
        MakeDecision();
        ExecuteCurrentState();
        UpdateAlertIndicator();
    }

    private void UpdateMemory()
    {
        if (!sensor.CanSeePlayer || sensor.Player == null)
            return;

        lastKnownPosition = sensor.Player.position;
        hasLastKnownPosition = true;
        lastPositionWasHeard = false;
    }

    private void MakeDecision()
    {
        if (sensor.CanSeePlayer)
        {
            ChangeState(NPCState.Chase);
            return;
        }

        if (currentState == NPCState.Chase && hasLastKnownPosition)
        {
            searchTimer = searchDuration;
            ChangeState(NPCState.Search);
            return;
        }

        if (currentState == NPCState.Patrol && hasLastKnownPosition)
        {
            searchTimer = searchDuration;
            ChangeState(NPCState.Search);
            return;
        }

        if (currentState == NPCState.Search && searchTimer <= 0f)
        {
            hasLastKnownPosition = false;
            lastPositionWasHeard = false;
            searchingLastKnownPosition = false;
            ChangeState(NPCState.Patrol);
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol: Patrol(); break;
            case NPCState.Chase: Chase(); break;
            case NPCState.Search: Search(); break;
        }
    }

    private void Patrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null || patrolPoints.Length == 0 || agent.pathPending)
            return;

        if (waypointWaitTimer > 0f)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
                GoToNextPatrolPoint();
            return;
        }

        if (agent.remainingDistance <= waypointTolerance)
        {
            waypointWaitTimer = waypointWaitDuration;
            agent.ResetPath();
        }
    }

    private void GoToNextPatrolPoint()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        GoToCurrentPatrolPoint();
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        if (sensor.Player != null)
            agent.SetDestination(sensor.Player.position);
    }

    private void Search()
    {
        agent.speed = patrolSpeed;

        if (searchingLastKnownPosition)
        {
            if (agent.pathPending || agent.remainingDistance > searchTolerance)
                return;

            searchingLastKnownPosition = false;
            agent.ResetPath();
        }

        if (!SearchRotation())
            return;
        searchTimer -= Time.deltaTime;
    }

    private bool SearchRotation()
    {
        float targetAngle = searchRotationPhase == 0
            ? -searchRotationAngle
            : searchRotationPhase == 1 ? searchRotationAngle : 0f;
        Quaternion targetRotation = searchStartRotation * Quaternion.Euler(0f, targetAngle, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, searchRotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
            return false;

        searchRotationPhase++;
        return searchRotationPhase > 2;
    }

    private void ChangeState(NPCState newState)
    {
        if (currentState == newState)
            return;

        previousState = currentState;
        currentState = newState;

        if (newState == NPCState.Search)
        {
            searchRotationPhase = 0;
            searchStartRotation = transform.rotation;
            searchingLastKnownPosition = true;
            agent.SetDestination(lastKnownPosition);
        }
        else if (newState == NPCState.Patrol)
        {
            GoToCurrentPatrolPoint();
        }

        Debug.Log($"{name}: {previousState} -> {currentState}");
    }

    private void OnNoiseReported(Vector3 position, float radius)
    {
        if (Vector3.Distance(transform.position, position) <= radius)
        {
            lastKnownPosition = position;
            hasLastKnownPosition = true;
            lastPositionWasHeard = true;
            if (currentState == NPCState.Search)
            {
                searchingLastKnownPosition = true;
                agent.SetDestination(lastKnownPosition);
            }
            Debug.Log($"{name}: HEARD Player at {position}");
        }
    }

    private void CreateAlertIndicators()
    {
        alertChase = CreateAlert("AlertChase", "!", Color.red);
        alertSearch = CreateAlert("AlertSearch", "?", Color.yellow);
    }

    private GameObject CreateAlert(string objectName, string text, Color color)
    {
        GameObject alert = new GameObject(objectName);
        alert.transform.SetParent(transform);
        alert.transform.localPosition = Vector3.up * 2.7f;
        TextMesh mesh = alert.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.color = color;
        mesh.fontSize = 64;
        mesh.fontStyle = FontStyle.Bold;
        mesh.characterSize = 0.22f;
        mesh.anchor = TextAnchor.MiddleCenter;
        return alert;
    }

    private void UpdateAlertIndicator()
    {
        if (alertChase != null)
            alertChase.SetActive(currentState == NPCState.Chase);
        if (alertSearch != null)
            alertSearch.SetActive(currentState == NPCState.Search);
    }

    public void SetPatrolStartIndex(int index)
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            patrolIndex = Mathf.Abs(index) % patrolPoints.Length;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = currentState == NPCState.Chase ? Color.red :
            currentState == NPCState.Search ? Color.blue : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.8f);

        if (hasLastKnownPosition)
        {
            Gizmos.color = lastPositionWasHeard ? Color.cyan : Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }
    }
}
