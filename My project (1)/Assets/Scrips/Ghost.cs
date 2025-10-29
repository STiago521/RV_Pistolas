using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ghost : MonoBehaviour
{
    [Header("Refs (desde la escena)")]
    public GameObject ghostPrefab;      // prefab SIN scripts
    public Transform target;            // objetivo
    public Transform[] spawnPoints;     // puntos de spawn
    public GameObject canvasSusto;      // UI opcional

    [Header("Juego")]
    public float hitRadius = 0.6f;      // “golpe” al llegar
    public int maxVivos = 50;           // límite opcional

    public enum Difficulty { Easy, Normal, Hard, Insane }
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Respawn por dificultad (seg)")]
    public float easyInterval = 4f, normalInterval = 2.5f, hardInterval = 1.5f, insaneInterval = 0.8f;

    [Header("Velocidad por dificultad (u/s)")]
    public float easySpeed = 2f, normalSpeed = 3.2f, hardSpeed = 4.2f, insaneSpeed = 5.2f;

    readonly List<Transform> vivos = new List<Transform>();
    Coroutine loop;

    void OnEnable() { loop = StartCoroutine(RespawnLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); vivos.Clear(); }

    // --- API para cambiar dificultad en runtime ---
    public void SetDifficulty(Difficulty d)
    {
        difficulty = d;
        if (loop != null) { StopCoroutine(loop); loop = StartCoroutine(RespawnLoop()); }
    }

    // --- Spawner principal ---
    IEnumerator RespawnLoop()
    {
        while (true)
        {
            if (vivos.Count < maxVivos && spawnPoints.Length > 0)
            {
                Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                var g = Instantiate(ghostPrefab, sp.position, sp.rotation);
                vivos.Add(g.transform);
            }
            yield return new WaitForSeconds(IntervalForDiff());
        }
    }

    // --- Movimiento directo de TODOS los fantasmas ---
    void Update()
    {
        if (!target) return;

        float speed = SpeedForDiff();
        Vector3 targetPos = target.position;

        // recorrer al revés por si destruimos
        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            var t = vivos[i];
            if (!t) { vivos.RemoveAt(i); continue; }

            Vector3 dir = (targetPos - t.position).normalized;
            t.position += dir * speed * Time.deltaTime;
            if (dir.sqrMagnitude > 0.0001f) t.forward = dir; // opcional mirar al objetivo

            if (Vector3.Distance(t.position, targetPos) <= hitRadius)
            {
                if (canvasSusto) canvasSusto.SetActive(true);
                Destroy(t.gameObject);
                vivos.RemoveAt(i);
            }
        }
    }

    // --- Helpers de dificultad ---
    float IntervalForDiff() => difficulty switch
    {
        Difficulty.Easy => easyInterval,
        Difficulty.Normal => normalInterval,
        Difficulty.Hard => hardInterval,
        _ => insaneInterval
    };

    float SpeedForDiff() => difficulty switch
    {
        Difficulty.Easy => easySpeed,
        Difficulty.Normal => normalSpeed,
        Difficulty.Hard => hardSpeed,
        _ => insaneSpeed
    };
}
