# Tutorial Challenges NPC Guard

Dokumen ini mengikuti code yang saat ini aktif pada project `NPC Guard`. Semua challenge 1 sampai 5 sudah diimplementasikan.

## Struktur aktif

```text
Assets/Scripts/
├── NPCBrain.cs
├── NPCSensor.cs
├── PlayerController.cs
├── PlayerNoise.cs
├── NPCNoiseSystem.cs
└── NPCGuardSpawner.cs
```

Scene yang digunakan adalah `Assets/Scenes/Praktikum02_NPCGuard.unity`.

## Cara membaca perubahan code

Viewer Markdown tertentu tidak memberi warna pada fenced block `diff`. Karena itu, tutorial memakai legend visual berikut:

<div style="background-color:#d9ead3; padding:8px; border-left:4px solid #38761d;">
🟩 <strong>Baris hijau</strong> = code yang ditambahkan atau hasil perubahan.
</div>

Pada code diff, penanda tetap ditulis seperti ini:

```diff
+ baris code baru
- baris code lama yang dihapus atau diganti
```

Tanda `+` dan `-` hanya penanda dokumentasi. Jangan ikut menyalin tandanya ke script Unity.

Jika viewer tidak merender warna hijau, baca semua baris yang diawali tanda `+` sebagai baris 🟩 tambahan. Tanda `-` berarti code lama yang diganti atau dihapus.

`NPC_Guard` pada scene memiliki `NPCSensor`, `NavMeshAgent`, dan `NPCBrain`. Player memiliki `PlayerController` dan `PlayerNoise`. `NPCBrain` memiliki reference sensor, agent, serta `Point1` sampai `Point4`.

## Reference dan auto-binding

Reference utama sudah tersimpan di scene. Sebagai fallback, `NPCBrain.Awake()` otomatis mencari `NPCSensor`, `NavMeshAgent`, dan child dari object `PatrolPoints` jika reference Inspector kosong. `NPCSensor.Awake()` mencari object dengan tag `Player` jika reference Player kosong.

Tidak perlu drag and drop tambahan untuk menjalankan challenge yang sudah ada.

## Challenge 1 NPC berhenti di waypoint

### Tujuan

NPC berhenti selama dua detik ketika tiba di waypoint sebelum melanjutkan patrol.

### Parameter aktif

**File:** `Assets/Scripts/NPCBrain.cs`

Pada `NPCBrain`:

```csharp
[SerializeField] private float waypointTolerance = 0.7f;
[SerializeField] private float waypointWaitDuration = 2f;
[SerializeField] private float patrolSpeed = 2f;
```

### Logic aktif

**File:** `Assets/Scripts/NPCBrain.cs`

```csharp
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
```

Urutan action:

```text
Menuju waypoint
→ Tiba dalam waypoint tolerance
→ ResetPath
→ Menunggu 2 detik
→ GoToNextPatrolPoint
```

### Demo

Jauhkan Player dari NPC. NPC harus bergerak ke `Point1`, diam dua detik, lanjut ke `Point2`, `Point3`, `Point4`, lalu kembali ke `Point1`.

## Challenge 2 Search rotation

### Tujuan

Setelah NPC tiba di `lastKnownPosition`, NPC melihat ke kiri, melihat ke kanan, lalu mengembalikan arah hadap sebelum kembali patrol.

### Parameter aktif

**File:** `Assets/Scripts/NPCBrain.cs`

```csharp
[SerializeField] private float searchDuration = 4f;
[SerializeField] private float searchTolerance = 0.8f;
[SerializeField] private float searchRotationAngle = 60f;
[SerializeField] private float searchRotationSpeed = 120f;
```

### Flow aktif

**File:** `Assets/Scripts/NPCBrain.cs`

Saat state berubah ke `SEARCH`, NPC langsung diberi destination:

```csharp
if (newState == NPCState.Search)
{
    searchRotationPhase = 0;
    searchStartRotation = transform.rotation;
    searchingLastKnownPosition = true;
    agent.SetDestination(lastKnownPosition);
}
```

NPC tidak melakukan rotasi sebelum tiba di lokasi:

```csharp
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
```

Target rotasi ditentukan oleh `searchRotationPhase`: `-60` derajat, `+60` derajat, lalu `0` derajat.

### Demo

1. Biarkan NPC melihat Player.
2. Sembunyikan Player di balik Wall.
3. NPC berubah dari `CHASE` ke `SEARCH`.
4. NPC menuju `lastKnownPosition`.
5. Setelah tiba, NPC melakukan search rotation.
6. Setelah `Search Duration` habis, NPC kembali `PATROL`.

## Challenge 3 Alert indicator

### Tujuan

NPC menampilkan tanda `!` saat `CHASE` dan tanda `?` saat `SEARCH`.

### Implementasi aktif

**File:** `Assets/Scripts/NPCBrain.cs`

Indicator dibuat otomatis sebagai child runtime dari NPC. Tidak ada object indicator yang harus dibuat manual di Hierarchy.

```csharp
private void CreateAlertIndicators()
{
    alertChase = CreateAlert("AlertChase", "!", Color.red);
    alertSearch = CreateAlert("AlertSearch", "?", Color.yellow);
}
```

Text dibuat bold dan cukup besar untuk Game view:

```csharp
TextMesh mesh = alert.AddComponent<TextMesh>();
mesh.text = text;
mesh.color = color;
mesh.fontSize = 64;
mesh.fontStyle = FontStyle.Bold;
mesh.characterSize = 0.22f;
mesh.anchor = TextAnchor.MiddleCenter;
```

Visibility indicator diperbarui setiap frame:

```csharp
private void UpdateAlertIndicator()
{
    if (alertChase != null)
        alertChase.SetActive(currentState == NPCState.Chase);
    if (alertSearch != null)
        alertSearch.SetActive(currentState == NPCState.Search);
}
```

### Demo

- Player terlihat: `!` muncul.
- Player hilang dari sensor: `?` muncul.
- NPC kembali patrol: kedua indicator disembunyikan.

## Challenge 4 Hearing

### Tujuan

NPC menerima posisi suara Player walaupun Player tidak terlihat oleh FOV atau terhalang Wall.

### PlayerNoise.cs

**File:** `Assets/Scripts/PlayerNoise.cs`

`PlayerNoise` membaca input gerak Player. Setiap `0.5` detik saat Player bergerak, script mengirim noise dengan radius `12`:

```csharp
private void Update()
{
    timer -= Time.deltaTime;
    if (timer > 0f || !IsMoving())
        return;

    timer = noiseInterval;
    Debug.Log($"{name}: NOISE at {transform.position}");
    NPCNoiseSystem.ReportNoise(transform.position, noiseRadius);
}
```

### NPCNoiseSystem.cs

**File:** `Assets/Scripts/NPCNoiseSystem.cs`

```csharp
public static event Action<Vector3, float> NoiseReported;

public static void ReportNoise(Vector3 position, float radius)
{
    NoiseReported?.Invoke(position, radius);
}
```

`NPCBrain` subscribe saat aktif dan unsubscribe saat nonaktif:

```csharp
private void OnEnable() => NPCNoiseSystem.NoiseReported += OnNoiseReported;
private void OnDisable() => NPCNoiseSystem.NoiseReported -= OnNoiseReported;
```

Saat mendengar suara dalam radius, NPC menyimpan posisi suara sebagai `lastKnownPosition`:

```csharp
private void OnNoiseReported(Vector3 position, float radius)
{
    if (Vector3.Distance(transform.position, position) <= radius)
    {
        lastKnownPosition = position;
        hasLastKnownPosition = true;
        lastPositionWasHeard = true;
        Debug.Log($"{name}: HEARD Player at {position}");
    }
}
```

Karena hearing menggunakan state yang sudah ada, hasilnya adalah:

```text
PATROL
→ Player bergerak dan terdengar
→ SEARCH
→ Menuju posisi suara
→ Search rotation
→ PATROL
```

Tidak ada state `HEARING` baru.

### Cara memastikan berhasil

1. Pastikan Player tidak pernah masuk FOV NPC.
2. Letakkan Player dalam radius noise `12`.
3. Gerakkan Player.
4. Console harus menampilkan `NOISE at (...)`.
5. NPC yang mendengar harus menampilkan `HEARD Player at (...)`.
6. Gizmos `lastKnownPosition` berwarna cyan berarti sumbernya hearing. Warna magenta berarti sumbernya visual.

Jika hanya muncul `SEARCH -> PATROL` tanpa log `NOISE` dan `HEARD`, itu adalah search dari visual memory, bukan Challenge 4.

## Challenge 5 Multiple guards

### Tujuan

Scene memiliki tiga guard yang dapat mengambil keputusan secara mandiri:

```text
NPC_Guard_A
NPC_Guard_B
NPC_Guard_C
```

### Implementasi aktif

**File:** `Assets/Scripts/NPCGuardSpawner.cs`

`NPCGuardSpawner.cs` berjalan setelah scene selesai dimuat. Guard utama diubah namanya menjadi `NPC_Guard_A`, kemudian dibuat dua clone pada posisi berbeda:

```csharp
Vector3[] spawnPositions =
{
    new Vector3(-6f, 1f, 6f),
    new Vector3(6f, 1f, 6f)
};
```

Setiap clone diberi nama dan start waypoint berbeda:

```csharp
clone.name = $"NPC_Guard_{(char)('A' + i)}";
clone.SetPatrolStartIndex(i);
```

Tidak ada drag and drop tambahan. Setiap guard hasil clone membawa `NPCSensor`, `NavMeshAgent`, `NPCBrain`, parameter patrol, dan reference Player dari guard utama.

### Demo

1. Tekan Play.
2. Pastikan terlihat tiga guard pada posisi berbeda.
3. Masukkan Player ke area salah satu guard.
4. Guard tersebut berubah state secara mandiri.
5. Guard lain tetap mengikuti state dan patrol masing-masing.

## Debugging state dan Gizmos

Perubahan state ditampilkan di Console, misalnya:

```text
NPC_Guard_A: Patrol -> Chase
NPC_Guard_A: Chase -> Search
NPC_Guard_A: Search -> Patrol
```

Warna Gizmos brain:

- Hijau: `PATROL`
- Merah: `CHASE`
- Biru: `SEARCH`
- Magenta: `lastKnownPosition` dari visual
- Cyan: `lastKnownPosition` dari hearing

## Checklist demo final

- NPC patrol melalui `Point1` sampai `Point4`.
- NPC berhenti dua detik di setiap waypoint.
- Radius, FOV, dan Raycast sensor bekerja.
- Player di belakang Wall tidak terlihat.
- NPC masuk `CHASE` ketika Player terlihat.
- NPC menuju `lastKnownPosition` ketika Player hilang.
- NPC melakukan search rotation setelah tiba di lokasi.
- NPC kembali `PATROL` setelah timeout.
- `!` muncul saat `CHASE`.
- `?` muncul saat `SEARCH`.
- Hearing menghasilkan log `NOISE` dan `HEARD`.
- Posisi hearing terlihat sebagai Gizmos cyan.
- Tiga guard berjalan dan mengambil keputusan secara independen.

## Ringkasan perubahan code

Bagian ini menunjukkan code apa yang ditambahkan atau diganti dari implementasi awal praktikum.

### Challenge 1 waypoint wait

Menambahkan timer agar NPC tidak langsung pindah waypoint:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
 [SerializeField] private float waypointTolerance = 0.7f;
+[SerializeField] private float waypointWaitDuration = 2f;
 [SerializeField] private float patrolSpeed = 2f;

+private float waypointWaitTimer;
```

Logic patrol lama yang langsung pindah waypoint diganti dengan timer:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
-if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
-{
-    patrolIndex++;
-    GoToCurrentPatrolPoint();
-}
+if (waypointWaitTimer > 0f)
+{
+    waypointWaitTimer -= Time.deltaTime;
+    if (waypointWaitTimer <= 0f)
+        GoToNextPatrolPoint();
+    return;
+}
+
+if (agent.remainingDistance <= waypointTolerance)
+{
+    waypointWaitTimer = waypointWaitDuration;
+    agent.ResetPath();
+}
```

Tujuannya adalah memberi jeda dua detik yang terlihat jelas saat demo.

### Challenge 2 search rotation

Menambahkan parameter dan state internal untuk rotasi pencarian:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
 [SerializeField] private float searchTolerance = 0.8f;
+[SerializeField] private float searchRotationAngle = 60f;
+[SerializeField] private float searchRotationSpeed = 120f;

+private bool searchingLastKnownPosition;
+private int searchRotationPhase;
+private Quaternion searchStartRotation;
```

Saat masuk `SEARCH`, NPC sekarang menuju lokasi terlebih dahulu:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
 if (newState == NPCState.Search)
 {
     searchRotationPhase = 0;
     searchStartRotation = transform.rotation;
+    searchingLastKnownPosition = true;
+    agent.SetDestination(lastKnownPosition);
 }
```

Rotasi tidak boleh dimulai sebelum NPC tiba:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
-agent.ResetPath();
-SearchRotation();
+if (searchingLastKnownPosition)
+{
+    if (agent.pathPending || agent.remainingDistance > searchTolerance)
+        return;
+
+    searchingLastKnownPosition = false;
+    agent.ResetPath();
+}
+
+if (!SearchRotation())
+    return;
```

### Challenge 3 alert indicator

Menambahkan reference runtime:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+private GameObject alertChase;
+private GameObject alertSearch;
```

Object indicator dibuat tanpa drag and drop:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+alertChase = CreateAlert("AlertChase", "!", Color.red);
+alertSearch = CreateAlert("AlertSearch", "?", Color.yellow);
```

Text dibuat tebal dan besar agar terlihat di Game view:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+mesh.fontSize = 64;
+mesh.fontStyle = FontStyle.Bold;
+mesh.characterSize = 0.22f;
```

Indicator diaktifkan berdasarkan state:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+alertChase.SetActive(currentState == NPCState.Chase);
+alertSearch.SetActive(currentState == NPCState.Search);
```

### Challenge 4 hearing

Menambahkan dua file baru:

**File:** `Assets/Scripts/PlayerNoise.cs` dan `Assets/Scripts/NPCNoiseSystem.cs`

```diff
+Assets/Scripts/PlayerNoise.cs
+Assets/Scripts/NPCNoiseSystem.cs
```

Player mengirim noise ketika bergerak:

**File:** `Assets/Scripts/PlayerNoise.cs`

```diff
+timer = noiseInterval;
+Debug.Log($"{name}: NOISE at {transform.position}");
+NPCNoiseSystem.ReportNoise(transform.position, noiseRadius);
```

NPC menerima noise melalui event:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+private void OnEnable() => NPCNoiseSystem.NoiseReported += OnNoiseReported;
+private void OnDisable() => NPCNoiseSystem.NoiseReported -= OnNoiseReported;
```

Posisi suara disimpan sebagai `lastKnownPosition`:

**File:** `Assets/Scripts/NPCBrain.cs`

```diff
+lastKnownPosition = position;
+hasLastKnownPosition = true;
+lastPositionWasHeard = true;
+Debug.Log($"{name}: HEARD Player at {position}");
```

`lastPositionWasHeard` membedakan Gizmos cyan dari memory visual yang berwarna magenta.

### Challenge 5 multiple guards

Menambahkan file baru:

**File:** `Assets/Scripts/NPCGuardSpawner.cs`

```diff
+Assets/Scripts/NPCGuardSpawner.cs
```

Spawner membuat dua clone dari guard utama:

**File:** `Assets/Scripts/NPCGuardSpawner.cs`

```diff
+original.name = "NPC_Guard_A";
+clone.name = $"NPC_Guard_{(char)('A' + i)}";
+clone.SetPatrolStartIndex(i);
```

Hasil runtime mengikuti nama challenge:

```text
NPC_Guard_A
NPC_Guard_B
NPC_Guard_C
```

Setiap clone memiliki instance `NPCBrain` sendiri, sehingga `currentState`, memory, dan `patrolIndex` tidak dibagi dengan guard lain.
