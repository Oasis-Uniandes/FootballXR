﻿using System.Collections;
using UnityEngine;
using TMPro;

public class Shoot : MonoBehaviour
{
    [Header("Referencias a las Manos")]
    [Tooltip("Referencia directa al objeto de la mano derecha")]
    public GameObject rightHand;

    [Tooltip("Referencia directa al objeto de la mano izquierda")]
    public GameObject leftHand;

    [Header("Indicadores de Tiro")]
    [Tooltip("GameObject indicador para el tiro 1")]
    public GameObject indicadorTiro1;

    [Tooltip("GameObject indicador para el tiro 2")]
    public GameObject indicadorTiro2;

    [Tooltip("GameObject indicador para el tiro 3")]
    public GameObject indicadorTiro3;

    [Tooltip("GameObject indicador para el tiro 4")]
    public GameObject indicadorTiro4;

    [Tooltip("GameObject indicador para el tiro 5")]
    public GameObject indicadorTiro5;

    [Header("Materiales para los Indicadores")]
    [Tooltip("Material inicial para los indicadores")]
    public Material materialNormal;

    [Tooltip("Material rojo para indicar gol")]
    public Material materialRojo;

    [Tooltip("Material verde para indicar atajada")]
    public Material materialVerde;

    [Header("Basic Shooting Parameters")]
    [Tooltip("Time multiplier for the ball trajectory (higher = slower ball)")]
    [Range(0.5f, 5.0f)]
    public float timeScale = 1.0f;

    [Tooltip("Delay in seconds before shooting after spawning")]
    public float shootDelay = 5f;

    [Tooltip("Tiempo fijo en segundos antes de que la pelota desaparezca tras ser lanzada")]
    public float fixedDisappearDelay = 5f;

    [Header("Target Area Settings")]
    [Tooltip("The minimum bounds of the target area")]
    public Vector3 targetAreaMin = new Vector3(-3.48f, 0.261f, 0f);

    [Tooltip("The maximum bounds of the target area")]
    public Vector3 targetAreaMax = new Vector3(3.54f, 2.274f, 0f);

    [Tooltip("If true, draws the target area as a wireframe in the editor")]
    public bool showTargetArea = true;

    [Header("Respawn Settings")]
    [Tooltip("The position to respawn the ball at")]
    public Vector3 respawnPosition = new Vector3(0f, 0.258f, 11f);

    [Tooltip("Posición donde se esconde la pelota cuando no está en uso")]
    public Vector3 hidingPosition = new Vector3(0f, -100f, 0f);

    [Tooltip("Total number of shots before cycle ends")]
    public int totalShots = 5;

    [Header("UI References")]
    [Tooltip("Reference to the TextMeshPro component that will display the countdown")]
    public TextMeshProUGUI countdownText;

    [Header("Goal Settings")]
    [Tooltip("Tag used for the goal trigger")]
    public string goalTag = "Goal";

    [Header("End Game Settings")]
    [Tooltip("Tiempo de espera en segundos antes de cargar la escena de estadísticas")]
    public float delayBeforeStats = 2f;

    [Header("Diálogo Final")]
    [Tooltip("GameObject con el diálogo que aparece al terminar la partida")]
    public GameObject DialogoFinalPartida;

    [Tooltip("Tiempo de espera en segundos para mostrar el diálogo antes de cargar estadísticas")]
    public float tiempoMostrarDialogo = 5f;

    [Header("Debug Options")]
    [Tooltip("Enable verbose logging for collision detection")]
    public bool verboseLogging = true;

    // Reference to components
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private Vector3 currentTargetPoint;
    private bool shotInProgress = false;
    public int shotsFired = 0;
    private bool cycleActive = true;

    // Conteo de goles
    private int golesAnotados = 0;
    private int golesAtajados = 0;
    // Flag para evitar contar goles múltiples veces en la misma jugada
    private bool goalScoredThisShot = false;
    // Flag para controlar si hubo contacto con las manos en este tiro
    private bool handContactThisShot = false;
    // Flag para controlar si ya se registró una atajada para este tiro
    private bool savedRegisteredThisShot = false;

    // Flag para evitar mostrar las estadísticas múltiples veces
    private bool statsShown = false;

    // Flag para controlar si el diálogo ya se mostró
    private bool dialogoMostrado = false;

    // Flag para indicar si la pelota está "visible" en juego
    private bool ballVisible = true;

    // Debug variables to visualize the trajectory
    private Vector3 calculatedVelocity;
    private float debugFlightTime;

    // Método para obtener el GameObject indicador correspondiente al número de tiro
    private GameObject GetIndicadorForShot(int shotNumber)
    {
        switch (shotNumber)
        {
            case 1:
                return indicadorTiro1;
            case 2:
                return indicadorTiro2;
            case 3:
                return indicadorTiro3;
            case 4:
                return indicadorTiro4;
            case 5:
                return indicadorTiro5;
            default:
                if (verboseLogging) Debug.LogWarning("No hay indicador para el tiro número " + shotNumber);
                return null;
        }
    }

    // Método para actualizar el material del indicador según el resultado del tiro
    private void ActualizarIndicadorTiro(int shotNumber, bool esGol)
    {
        GameObject indicador = GetIndicadorForShot(shotNumber);

        if (indicador != null)
        {
            Renderer renderer = indicador.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (esGol)
                {
                    renderer.material = materialRojo;
                    if (verboseLogging) Debug.Log("Tiro " + shotNumber + ": Indicador cambiado a ROJO (gol)");
                }
                else
                {
                    renderer.material = materialVerde;
                    if (verboseLogging) Debug.Log("Tiro " + shotNumber + ": Indicador cambiado a VERDE (atajada)");
                }
            }
            else
            {
                Debug.LogWarning("El indicador para el tiro " + shotNumber + " no tiene componente Renderer");
            }
        }
    }

    void Start()
    {
        // Verificar si se han asignado las manos
        if (rightHand == null || leftHand == null)
        {
            Debug.LogWarning("¡Las referencias a las manos no están asignadas! Por favor, asígnalas en el inspector para una mejor detección de colisiones.");
        }

        // Inicializar todos los indicadores con el material normal
        for (int i = 1; i <= 5; i++)
        {
            GameObject indicador = GetIndicadorForShot(i);
            if (indicador != null)
            {
                Renderer renderer = indicador.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = materialNormal;
                }
                else
                {
                    Debug.LogWarning("El indicador para el tiro " + i + " no tiene componente Renderer");
                }
            }
        }

        // Find all audio listeners in the scene
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

        // If there's more than one, disable all except the first one
        if (listeners.Length > 1)
        {
            Debug.Log("Found " + listeners.Length + " audio listeners. Keeping only one.");
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }

        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found! Please add a Rigidbody to this GameObject.");
            return;
        }

        // Get the MeshRenderer component (para ocultar la pelota visualmente)
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            // Intentar encontrar en hijos
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogWarning("MeshRenderer no encontrado en el objeto o sus hijos. No se podrá ocultar visualmente la pelota.");
            }
        }

        // Check if countdown text is assigned
        if (countdownText == null)
        {
            Debug.LogWarning("Countdown TextMeshPro is not assigned! Please assign it in the inspector.");
        }
        else
        {
            // Clear the text initially
            countdownText.text = " ";
        }

        // Verificar si el diálogo final está asignado
        if (DialogoFinalPartida == null)
        {
            Debug.LogWarning("DialogoFinalPartida no está asignado. Asígnalo en el inspector.");
        }
        else
        {
            // Desactivar el diálogo al inicio
            DialogoFinalPartida.SetActive(false);
        }

        // Make sure the Rigidbody has appropriate settings
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Store initial position as respawn position if not specified
        if (respawnPosition == Vector3.zero)
        {
            respawnPosition = transform.position;
        }

        // Inicializar el hiding position si no se especificó
        if (hidingPosition == Vector3.zero)
        {
            hidingPosition = new Vector3(0, -100, 0); // Muy por debajo de la escena
        }

        // Inicializar flags
        statsShown = false;
        dialogoMostrado = false;

        // Asegurarnos que la pelota es visible al inicio
        ShowBall();

        // Start the first shot
        StartCoroutine(ShootAfterDelay());
    }

    void OnDrawGizmos()
    {
        if (showTargetArea)
        {
            // Draw target area as a wireframe box
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                (targetAreaMin + targetAreaMax) / 2,
                new Vector3(
                    Mathf.Abs(targetAreaMax.x - targetAreaMin.x),
                    Mathf.Abs(targetAreaMax.y - targetAreaMin.y),
                    Mathf.Abs(targetAreaMax.z - targetAreaMin.z)
                )
            );

            // Draw the current target point if available
            if (currentTargetPoint != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentTargetPoint, 0.2f);

                // Draw predicted trajectory if we have velocity
                if (calculatedVelocity != Vector3.zero && Application.isEditor)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 start = respawnPosition;
                    Vector3 velocity = calculatedVelocity;

                    const int STEPS = 50;
                    const float TIME_STEP = 0.1f; // 0.1 second intervals

                    for (int i = 0; i < STEPS; i++)
                    {
                        float t = i * TIME_STEP;
                        Vector3 nextPos = start + velocity * t + 0.5f * Physics.gravity * t * t;

                        if (i > 0)
                        {
                            Vector3 prevPos = start + velocity * (t - TIME_STEP) +
                                0.5f * Physics.gravity * (t - TIME_STEP) * (t - TIME_STEP);
                            Gizmos.DrawLine(prevPos, nextPos);
                        }
                    }
                }
            }
        }

        // Draw respawn position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(respawnPosition, 0.3f);

        // Draw hiding position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hidingPosition, 0.3f);
    }

    // Método para ocultar visualmente la pelota (sin desactivar el GameObject)
    private void HideBall()
    {
        if (!ballVisible) return; // Ya está oculta

        ballVisible = false;

        // Desactivar renderer para hacerla invisible
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Mover la pelota fuera de la vista y desactivar físicas
        transform.position = hidingPosition;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Desactivar colisiones
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.enabled = false;
        }

        if (verboseLogging) Debug.Log("Pelota ocultada (sin desactivar GameObject)");
    }

    // Método para mostrar visualmente la pelota
    private void ShowBall()
    {
        if (ballVisible) return; // Ya está visible

        ballVisible = true;

        // Activar renderer para hacerla visible
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        // Activar colisiones
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.enabled = true;
        }

        if (verboseLogging) Debug.Log("Pelota mostrada");
    }

    // Método para verificar si un objeto es una mano o parte de una mano
    private bool IsHandObject(GameObject obj)
    {
        // Verificar si el objeto es directamente una mano
        if (obj == rightHand || obj == leftHand)
            return true;

        // Verificar si el objeto es hijo de una mano
        if (rightHand != null && obj.transform.IsChildOf(rightHand.transform))
            return true;

        if (leftHand != null && obj.transform.IsChildOf(leftHand.transform))
            return true;

        // Verificar por tag
        if (obj.CompareTag("hands"))
            return true;

        // No es una mano
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (verboseLogging) Debug.Log("Colisión detectada con: " + collision.gameObject.name);

        // Detectar colisión con las manos usando la referencia directa
        if (!goalScoredThisShot && !handContactThisShot && IsHandObject(collision.gameObject))
        {
            handContactThisShot = true;
            Debug.Log("Tiro número " + shotsFired + ": ¡Balón tocó las manos del arquero! GameObject: " + collision.gameObject.name);

            // Reproducir sonido de atajada
            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlaySaveSound();
                Debug.Log("Reproduciendo sonido de atajada por colisión con las manos");
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // También detectar colisiones continuas con las manos
        if (!goalScoredThisShot && !handContactThisShot && IsHandObject(collision.gameObject))
        {
            handContactThisShot = true;
            Debug.Log("Tiro número " + shotsFired + ": ¡Balón en contacto continuo con las manos! GameObject: " + collision.gameObject.name);

            // Reproducir sonido de atajada
            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlaySaveSound();
                Debug.Log("Reproduciendo sonido de atajada por colisión continua con las manos");
            }
        }
    }

    // Método para detectar cuando la pelota atraviesa el trigger del gol
    void OnTriggerEnter(Collider other)
    {
        if (verboseLogging) Debug.Log("Trigger entrado: " + other.gameObject.name + " con tag: " + other.tag);

        // Verificar si atravesó el collider con tag "Goal" y que no haya anotado gol en este tiro aún
        if (!goalScoredThisShot && other.CompareTag(goalTag))
        {
            // Incrementar contador de goles
            golesAnotados++;
            goalScoredThisShot = true;

            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlayGoalSound();
            }

            // Incrementar goles recibidos en la base de datos
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.IncrementGolesRecibidos();
                Debug.Log("Tiro número " + shotsFired + ": Gol registrado en la base de datos para el jugador: " +
                         PlayerDataManager.Instance.GetCurrentPlayerName());
            }
            else
            {
                Debug.LogWarning("PlayerDataManager no encontrado. No se pudo registrar el gol en la base de datos.");
            }

            // Actualizar el indicador correspondiente a este tiro
            ActualizarIndicadorTiro(shotsFired, true); // true = es gol

            // Mostrar mensaje en debug log
            Debug.Log("Tiro número " + shotsFired + ": ¡GOL DEL USUARIO! El usuario lleva " + golesAnotados + " goles");
        }

        // Cuando se detecta un golpe en el poste:
        if (!goalScoredThisShot && other.CompareTag("poste"))
        {
            Debug.Log("Tiro número " + shotsFired + ": ¡Balón golpeó el poste!");

            // Reproducir sonido de poste
            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlayPostSound();
            }
        }
    }

    // Método para registrar gol atajado
    private void RegisterGoalSaved()
    {
        // Solo registrar una vez por tiro y solo si no hubo gol
        if (!goalScoredThisShot && !savedRegisteredThisShot)
        {
            savedRegisteredThisShot = true;
            golesAtajados++;

            // Solo reproducir sonido si no hubo contacto previo con las manos
            //if (!handContactThisShot && FootballAudioManager.Instance != null)
            //{
            //    FootballAudioManager.Instance.PlaySaveSound();
            //    Debug.Log("Reproduciendo sonido de atajada al registrar gol atajado");
            //}

            // Incrementar goles atajados en la base de datos
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.IncrementGolesAtajados();
                Debug.Log("Tiro número " + shotsFired + ": Gol atajado registrado en la base de datos para el jugador: " +
                         PlayerDataManager.Instance.GetCurrentPlayerName());
            }
            else
            {
                Debug.LogWarning("PlayerDataManager no encontrado. No se pudo registrar el gol atajado en la base de datos.");
            }

            // Actualizar el indicador correspondiente a este tiro
            ActualizarIndicadorTiro(shotsFired, false); // false = no es gol, es atajada

            Debug.Log("Tiro número " + shotsFired + ": ¡GOL ATAJADO! El usuario lleva " + golesAtajados + " goles atajados");
        }
    }

    // Método para desaparecer la pelota después de un tiempo fijo tras el lanzamiento
    IEnumerator DisappearAfterFixedDelay()
    {
        // Esperar el tiempo fijo
        yield return new WaitForSeconds(fixedDisappearDelay);

        // Verificar si ha marcado un gol, si no ha marcado, se considera atajado
        if (!goalScoredThisShot)
        {
            RegisterGoalSaved();
        }

        // Si no hemos llegado al límite de tiros, respawnear
        if (shotsFired < totalShots && cycleActive)
        {
            yield return new WaitForSeconds(0.5f); // Breve pausa antes de respawnear
            Respawn();
        }
        else
        {
            // Ciclo completado
            Debug.Log("Ciclo completo. Total tiros: " + shotsFired +
                      ", Goles recibidos: " + golesAnotados +
                      ", Goles atajados: " + golesAtajados);

            // Ocultar la pelota (sin desactivar el GameObject)
            HideBall();

            // Mostrar el diálogo final y luego cargar estadísticas
            MostrarDialogoFinal();
        }
    }

    // Método para mostrar el diálogo final y programar la carga de estadísticas
    private void MostrarDialogoFinal()
    {
        // Si ya se mostró el diálogo o se están mostrando las estadísticas, salir
        if (dialogoMostrado || statsShown) return;

        dialogoMostrado = true;

        // Activar el diálogo
        if (DialogoFinalPartida != null)
        {
            DialogoFinalPartida.SetActive(true);
            Debug.Log("Diálogo de finalización de partida activado");

            // Programar la carga de estadísticas después del tiempo especificado
            StartCoroutine(LoadStatsAfterDialog());
        }
        else
        {
            Debug.LogWarning("DialogoFinalPartida no está asignado. Cargando directamente las estadísticas.");
            // Cargar directamente las estadísticas si no hay diálogo
            StartCoroutine(LoadStatsScene());
        }
    }

    // Método para cargar estadísticas después del diálogo
    IEnumerator LoadStatsAfterDialog()
    {
        Debug.Log("Esperando " + tiempoMostrarDialogo + " segundos antes de cargar las estadísticas...");

        // Esperar el tiempo configurado para el diálogo
        yield return new WaitForSeconds(tiempoMostrarDialogo);

        // Cargar la escena de estadísticas
        if (!statsShown)
        {
            statsShown = true;
            StartCoroutine(LoadStatsScene());
        }
    }

    // Método para cargar la escena de estadísticas
    IEnumerator LoadStatsScene()
    {
        Debug.Log("Esperando " + delayBeforeStats + " segundos antes de cargar escena de estadísticas...");

        // Esperar el tiempo configurado
        yield return new WaitForSeconds(delayBeforeStats);

        // Imprimir información de las estadísticas finales antes de cargar la escena
        Debug.Log("ESTADÍSTICAS FINALES ANTES DE CARGAR ESCENA:");
        Debug.Log("- Total de tiros: " + shotsFired);
        Debug.Log("- Goles anotados: " + golesAnotados);
        Debug.Log("- Goles atajados: " + golesAtajados);

        // Verificar que la suma de goles + atajadas = total de tiros
        if (golesAnotados + golesAtajados != shotsFired)
        {
            Debug.LogWarning("¡ALERTA! La suma de goles (" + golesAnotados + ") y atajadas (" +
                             golesAtajados + ") no coincide con el total de tiros (" + shotsFired + ")");
        }

        // Cargar la escena de estadísticas
        if (GameController.Instance != null)
        {
            Debug.Log("Cargando escena de estadísticas con: " + golesAtajados + " goles atajados, " + golesAnotados + " goles recibidos");
            GameController.Instance.LoadStatsRankingScene(golesAtajados, golesAnotados);
        }
        else
        {
            Debug.LogError("No se pudo cargar la escena de estadísticas. GameController no encontrado.");
        }
    }

    void Respawn()
    {
        // Reset the ball's physics state
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Asegurarse que la pelota esté visible
        ShowBall();

        // Move to respawn position
        transform.position = respawnPosition;
        transform.rotation = Quaternion.identity;

        // Reset collision state
        shotInProgress = false;
        goalScoredThisShot = false;
        handContactThisShot = false;
        savedRegisteredThisShot = false;

        Debug.Log("Ball respawned at " + respawnPosition);

        // Start the countdown to shoot again
        StartCoroutine(ShootAfterDelay());
    }

    IEnumerator ShootAfterDelay()
    {
        Debug.Log("Ball will shoot in " + shootDelay + " seconds... (Shot " + (shotsFired + 1) + " of " + totalShots + ")");

        // Generate a random target point
        GenerateTargetPoint();

        // Restaurar volumen estándar al iniciar cuenta regresiva
        if (FootballAudioManager.Instance != null)
        {
            FootballAudioManager.Instance.RestoreStandardCrowdVolume();
        }

        // Wait for the specified delay with countdown
        for (int i = (int)shootDelay; i > 0; i--)
        {
            Debug.Log("Shooting in " + i + " seconds...");

            // Update the TextMeshPro with the countdown number
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }

            yield return new WaitForSeconds(1f);
        }

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Increment shot counter
        shotsFired++;
        shotInProgress = true;

        // Shoot the ball
        Debug.Log("Shooting ball now! (Shot " + shotsFired + " of " + totalShots + ")");

        // Make sure the ball isn't kinematic
        rb.isKinematic = false;

        // Calculate the base velocity needed to hit the target in the standard time
        Vector3 baseVelocity = CalculateBaseVelocity();

        // Apply the time scale to slow down the ball while preserving trajectory
        Vector3 scaledVelocity = baseVelocity / timeScale;

        // Apply the velocity to the ball
        rb.velocity = scaledVelocity;
        calculatedVelocity = scaledVelocity; // For debugging visualization

        if (FootballAudioManager.Instance != null)
        {
            FootballAudioManager.Instance.PlayKickSound();
        }

        Debug.Log("Tiro número " + shotsFired + ": Applied initial velocity: " + scaledVelocity + " with magnitude: " + scaledVelocity.magnitude);
        Debug.Log("Tiro número " + shotsFired + ": Time scale: " + timeScale + " (higher = slower ball)");
        Debug.Log("Tiro número " + shotsFired + ": Estimated time to target: " + (CalculateBaseTime() * timeScale) + " seconds");
        Debug.Log("Tiro número " + shotsFired + ": Ball shot towards target: " + currentTargetPoint);

        // Iniciar el temporizador para que la pelota desaparezca después de un tiempo fijo
        StartCoroutine(DisappearAfterFixedDelay());
    }

    void GenerateTargetPoint()
    {
        // Generate a random point inside the target area
        currentTargetPoint = new Vector3(
            Random.Range(targetAreaMin.x, targetAreaMax.x),
            Random.Range(targetAreaMin.y, targetAreaMax.y),
            Random.Range(targetAreaMin.z, targetAreaMax.z)
        );

        Debug.Log("Target point generated: " + currentTargetPoint);
    }

    float CalculateBaseTime()
    {
        // Calculate the horizontal distance to target
        Vector3 displacement = currentTargetPoint - respawnPosition;
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;

        // Calculate a base time using the original formula
        float baseTime = Mathf.Sqrt(horizontalDistance) * 0.3f;
        baseTime = Mathf.Clamp(baseTime, 0.5f, 3.0f);

        return baseTime;
    }

    Vector3 CalculateBaseVelocity()
    {
        // Get the displacement vector from start to target
        Vector3 displacement = currentTargetPoint - respawnPosition;

        // Extract the horizontal and vertical displacements
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
        float verticalDistance = displacement.y;

        // Calculate the direction in the horizontal plane
        Vector3 horizontalDirection = new Vector3(displacement.x, 0, displacement.z).normalized;

        // Calculate the base time to reach the target (without timeScale applied)
        float baseTime = CalculateBaseTime();
        debugFlightTime = baseTime * timeScale; // Store the actual flight time

        // Calculate the gravity (positive value)
        float g = Mathf.Abs(Physics.gravity.y);

        // Calculate the vertical velocity component using the formula:
        // Δy = v₀y × t + 0.5 × (-g) × t²
        // Solving for v₀y: v₀y = (Δy + 0.5 × g × t²) / t
        float initialVerticalVelocity = (verticalDistance + 0.5f * g * baseTime * baseTime) / baseTime;

        // Calculate the horizontal velocity components
        float horizontalSpeed = horizontalDistance / baseTime;
        Vector3 horizontalVelocity = horizontalDirection * horizontalSpeed;

        // Combine to get the final velocity vector
        Vector3 velocity = new Vector3(horizontalVelocity.x, initialVerticalVelocity, horizontalVelocity.z);

        return velocity;
    }

    // Public method to restart the cycle
    public void RestartCycle()
    {
        // Stop all running coroutines
        StopAllCoroutines();

        // Reset state
        shotsFired = 0;
        shotInProgress = false;
        goalScoredThisShot = false;
        handContactThisShot = false;
        savedRegisteredThisShot = false;
        cycleActive = true;
        golesAnotados = 0;
        golesAtajados = 0;
        statsShown = false; // Reiniciar el flag de estadísticas
        dialogoMostrado = false; // Reiniciar el flag de diálogo mostrado

        // Ocultar el diálogo si está visible
        if (DialogoFinalPartida != null)
        {
            DialogoFinalPartida.SetActive(false);
        }

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Reiniciar todos los indicadores al material normal
        for (int i = 1; i <= 5; i++)
        {
            GameObject indicador = GetIndicadorForShot(i);
            if (indicador != null)
            {
                Renderer renderer = indicador.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = materialNormal;
                    if (verboseLogging) Debug.Log("Reiniciando indicador " + i + " a material normal");
                }
            }
        }

        // Respawn the ball
        Respawn();
    }

    // Public method to stop the cycle
    public void StopCycle()
    {
        cycleActive = false;
        StopAllCoroutines();

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Ocultar la pelota (sin desactivar el GameObject)
        HideBall();

        // Mostrar el diálogo final y programar la carga de estadísticas
        MostrarDialogoFinal();
    }

    // Función pública para obtener el número de goles
    public int GetGolesAnotados()
    {
        return golesAnotados;
    }

    // Función pública para obtener el número de goles atajados
    public int GetGolesAtajados()
    {
        return golesAtajados;
    }

    // Función pública para resetear los goles (por si la necesitas)
    public void ResetearGoles()
    {
        golesAnotados = 0;
        golesAtajados = 0;
        Debug.Log("Contadores de goles reseteados a 0");
    }
}