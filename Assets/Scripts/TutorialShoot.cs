﻿using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialShoot : MonoBehaviour
{
    [Header("Indicadores de Tiro")]
    [Tooltip("GameObject indicador para el tiro 1")]
    public GameObject indicadorTiro1;

    [Tooltip("GameObject indicador para el tiro 2")]
    public GameObject indicadorTiro2;

    [Tooltip("GameObject indicador para el tiro 3")]
    public GameObject indicadorTiro3;

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
    public int totalShots = 3; // Cambiado a 3 disparos para el tutorial

    [Header("UI References")]
    [Tooltip("Reference to the TextMeshPro component that will display the countdown")]
    public TextMeshProUGUI countdownText;

    [Header("Diálogo Final Tutorial")]
    [Tooltip("GameObject con el diálogo que aparece al terminar el tutorial")]
    public GameObject DialogoTerminadoTutorial;

    [Tooltip("Tiempo de espera en segundos antes de cargar la escena Football (después del diálogo)")]
    public float tiempoEsperaTrasDiálogo = 10f;

    [Header("Goal Settings")]
    [Tooltip("Tag used for the goal trigger")]
    public string goalTag = "Goal";

    // Reference to components
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private Vector3 currentTargetPoint;
    private bool shotInProgress = false;
    public int shotsFired = 0;
    private bool cycleActive = true;

    // Conteo de goles (solo para mostrar en el tutorial, no se guardan)
    private int golesAnotados = 0;
    private int golesAtajados = 0;
    // Flag para evitar contar goles múltiples veces en la misma jugada
    private bool goalScoredThisShot = false;

    // Flag para evitar cargar la escena Football múltiples veces
    private bool footballSceneLoading = false;

    // Flag para saber si se mostró el diálogo final
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
            default:
                Debug.LogWarning("No hay indicador para el tiro número " + shotNumber);
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
                    Debug.Log("Tiro " + shotNumber + ": Indicador cambiado a ROJO (gol)");
                }
                else
                {
                    renderer.material = materialVerde;
                    Debug.Log("Tiro " + shotNumber + ": Indicador cambiado a VERDE (atajada)");
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
        Debug.Log("TutorialShoot - Start iniciado. totalShots configurado a: " + totalShots);

        // Inicializar todos los indicadores con el material normal
        for (int i = 1; i <= 3; i++)
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
        if (DialogoTerminadoTutorial == null)
        {
            Debug.LogWarning("DialogoTerminadoTutorial no está asignado. Asígnalo en el inspector.");
        }
        else
        {
            // Desactivar el diálogo al inicio
            DialogoTerminadoTutorial.SetActive(false);
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
        footballSceneLoading = false;
        dialogoMostrado = false;

        // Reiniciar contador de disparos
        shotsFired = 0;

        // Asegurarnos que la pelota es visible al inicio
        ShowBall();

        // Start the first shot
        Debug.Log("Iniciando primer disparo en TutorialShoot");
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

        Debug.Log("Pelota ocultada (sin desactivar GameObject)");
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

        Debug.Log("Pelota mostrada");
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detectar colisión con las manos
        if (!goalScoredThisShot && collision.gameObject.CompareTag("hands"))
        {
            Debug.Log("Tiro número " + shotsFired + ": ¡Balón tocó las manos del arquero!");

            // Reproducir sonido de atajada
            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlaySaveSound();
            }
        }
    }

    // Método para detectar cuando la pelota atraviesa el trigger del gol
    void OnTriggerEnter(Collider other)
    {
        // Verificar si atravesó el collider con tag "Goal" y que no haya anotado gol en este tiro aún
        if (!goalScoredThisShot && other.CompareTag(goalTag))
        {
            // Incrementar contador de goles (solo para el tutorial, no se guarda en estadísticas)
            golesAnotados++;
            goalScoredThisShot = true;

            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlayGoalSound();
            }

            // Actualizar el indicador correspondiente a este tiro
            ActualizarIndicadorTiro(shotsFired, true); // true = es gol

            // Mostrar mensaje en debug log
            Debug.Log("Tiro número " + shotsFired + ": ¡GOL DEL USUARIO! Tutorial: " + golesAnotados + " goles");
        }

        // Cuando se detecta un golpe en el poste:
        else if (!goalScoredThisShot && other.CompareTag("poste"))
        {
            Debug.Log("Tiro número " + shotsFired + ": ¡Balón golpeó el poste!");

            // Reproducir sonido de poste
            if (FootballAudioManager.Instance != null)
            {
                FootballAudioManager.Instance.PlayPostSound();
            }
        }
    }

    // Método para registrar gol atajado (solo para mostrar en tutorial, no se guarda en estadísticas)
    private void RegisterGoalSaved()
    {
        if (!goalScoredThisShot)
        {
            golesAtajados++;

            

            // Actualizar el indicador correspondiente a este tiro
            ActualizarIndicadorTiro(shotsFired, false); // false = no es gol, es atajada

            Debug.Log("Tiro número " + shotsFired + ": ¡GOL ATAJADO! Tutorial: " + golesAtajados + " goles atajados");
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

        // DEBUG: Verificar estado antes de decidir respawn
        Debug.Log("DisappearAfterFixedDelay - disparos realizados: " + shotsFired +
                  ", total disparos configurados: " + totalShots +
                  ", ciclo activo: " + cycleActive);
        Debug.Log("Evaluación condición respawn: " + (shotsFired < totalShots) + " && " + cycleActive);

        // Si no hemos llegado al límite de tiros, respawnear
        if (shotsFired < totalShots && cycleActive)
        {
            Debug.Log("Preparando respawn para siguiente disparo...");
            yield return new WaitForSeconds(0.5f); // Breve pausa antes de respawnear
            Respawn();
        }
        else
        {
            // Ciclo completado
            Debug.Log("Tutorial completado. Total tiros: " + shotsFired +
                      ", Goles recibidos: " + golesAnotados +
                      ", Goles atajados: " + golesAtajados);

            // Ocultar la pelota (sin desactivar el GameObject)
            HideBall();

            // Mostrar el diálogo de finalización del tutorial
            MostrarDialogoFinal();
        }
    }

    // Método para mostrar el diálogo final y programar la carga de la escena
    private void MostrarDialogoFinal()
    {
        // Si ya se mostró el diálogo o se está cargando la escena, salir
        if (dialogoMostrado || footballSceneLoading)
        {
            Debug.Log("MostrarDialogoFinal: Diálogo ya mostrado o escena ya cargándose");
            return;
        }

        dialogoMostrado = true;

        // Activar el diálogo
        if (DialogoTerminadoTutorial != null)
        {
            DialogoTerminadoTutorial.SetActive(true);
            Debug.Log("Diálogo de finalización del tutorial activado");

            // Programar la carga de la escena después del tiempo especificado
            StartCoroutine(LoadFootballAfterDialog());
        }
        else
        {
            Debug.LogError("DialogoTerminadoTutorial no está asignado. No se puede mostrar.");
            // Cargar directamente la escena si no hay diálogo
            StartCoroutine(LoadFootballScene());
        }
    }

    // Método para cargar Football después del diálogo
    IEnumerator LoadFootballAfterDialog()
    {
        Debug.Log("Esperando " + tiempoEsperaTrasDiálogo + " segundos antes de cargar la escena Football...");

        // Esperar el tiempo configurado para el diálogo
        yield return new WaitForSeconds(tiempoEsperaTrasDiálogo);

        // Cargar la escena Football
        StartCoroutine(LoadFootballScene());
    }

    // Método para cargar la escena Football
    IEnumerator LoadFootballScene()
    {
        // Si ya se está cargando la escena, salir
        if (footballSceneLoading)
        {
            Debug.Log("LoadFootballScene: Ya se está cargando la escena");
            yield break;
        }

        footballSceneLoading = true;
        Debug.Log("Cargando escena Football después del tutorial");

        // Cargar la escena Football
        if (GameController.Instance != null)
        {
            Debug.Log("Usando GameController para cargar escena de juego principal");
            GameController.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("No se pudo cargar la escena Football. GameController no encontrado.");
        }
    }

    void Respawn()
    {
        Debug.Log("Respawn() ejecutándose...");

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
        Debug.Log("RestartCycle() llamado - Reiniciando tutorial");

        // Stop all running coroutines
        StopAllCoroutines();

        // Reset state
        shotsFired = 0;
        shotInProgress = false;
        goalScoredThisShot = false;
        cycleActive = true;
        golesAnotados = 0;
        golesAtajados = 0;
        footballSceneLoading = false; // Reiniciar el flag de carga de escena
        dialogoMostrado = false; // Reiniciar el flag de diálogo mostrado

        // Ocultar el diálogo si está visible
        if (DialogoTerminadoTutorial != null)
        {
            DialogoTerminadoTutorial.SetActive(false);
        }

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Reiniciar todos los indicadores al material normal
        for (int i = 1; i <= 3; i++)
        {
            GameObject indicador = GetIndicadorForShot(i);
            if (indicador != null)
            {
                Renderer renderer = indicador.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = materialNormal;
                    Debug.Log("Reiniciando indicador " + i + " a material normal");
                }
            }
        }

        // Respawn the ball
        Respawn();
    }

    // Public method to stop the cycle
    public void StopCycle()
    {
        Debug.Log("StopCycle() llamado - Deteniendo tutorial");

        cycleActive = false;
        StopAllCoroutines();

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Ocultar la pelota (sin desactivar el GameObject)
        HideBall();

        // Mostrar diálogo final y programar la carga de la escena
        MostrarDialogoFinal();
    }

    // Función pública para obtener el número de goles (solo para el tutorial)
    public int GetGolesAnotados()
    {
        return golesAnotados;
    }

    // Función pública para obtener el número de goles atajados (solo para el tutorial)
    public int GetGolesAtajados()
    {
        return golesAtajados;
    }

    // Función pública para skip tutorial y cargar escena Football inmediatamente
    public void SkipTutorial()
    {
        Debug.Log("SkipTutorial() llamado - Saltando directamente a Football");

        // Detener todos los coroutines actuales
        StopAllCoroutines();

        // Asegurarse de cargar la escena Football solo una vez
        if (!footballSceneLoading)
        {
            footballSceneLoading = true;

            // Cargar inmediatamente la escena Football
            if (GameController.Instance != null)
            {
                Debug.Log("Saltando tutorial y cargando escena Football inmediatamente");
                GameController.Instance.LoadGameScene();
            }
            else
            {
                Debug.LogError("No se pudo cargar la escena Football. GameController no encontrado.");
            }
        }
    }
}