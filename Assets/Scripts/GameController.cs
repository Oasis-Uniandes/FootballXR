//----------------------------------------------------------------
//  GameController.cs
//  Oasis Developments - Simulador de Penaltis
//
//  Versión 11.0 - Lógica Invertida Corregida
//----------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Scene Management")]
    public string gameSceneName = "Football";
    public string registrationSceneName = "Intro";
    public string statsRankingSceneName = "StatsRanking";
    public string tutorialSceneName = "Tutorial";
    [Tooltip("Nombre de la escena que se muestra cuando la prueba ha expirado")]
    public string freeTrialSceneName = "FreeTrial";
    [Tooltip("Nombre de la escena que se muestra cuando no hay conexión a internet")]
    public string noConnectionSceneName = "NoConection";

    // --- Variables de estado del juego ---
    private int lastSessionGolesAtajados = 0;
    private int lastSessionGolesRecibidos = 0;
    private bool isTransitioningToGame = false;
    private bool isTransitioningToStats = false;
    private bool isTransitioningToTutorial = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        await CheckAppStatus();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    //----------------------------------------------------------------
    // --- SECCIÓN DE FIREBASE (LÓGICA DE VERIFICACIÓN CORREGIDA) ---
    //----------------------------------------------------------------

    private async Task CheckAppStatus()
    {
        Debug.Log("Iniciando verificación de estado...");

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("VERIFICACIÓN INICIAL FALLIDA: No hay conexión a ninguna red.");
            HandleOfflineStatus();
            return;
        }

        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError("No se pudieron resolver las dependencias de Firebase: " + dependencyStatus);
            HandleOfflineStatus();
            return;
        }

        Debug.Log("Dependencias de Firebase OK. Intentando obtener configuración remota...");
        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;

        var configSettings = new ConfigSettings();
        configSettings.MinimumFetchIntervalInMilliseconds = 0;
        await remoteConfig.SetConfigSettingsAsync(configSettings);

        await remoteConfig.FetchAsync(TimeSpan.Zero);

        // --- CORRECCIÓN: Se invierte la lógica para que coincida con el comportamiento observado ---
        if (remoteConfig.Info.LastFetchStatus != LastFetchStatus.Success)
        {
            // Si el fetch NO fue exitoso, lo tratamos como offline.
            Debug.LogError("El último Fetch no fue exitoso. Estado: " + remoteConfig.Info.LastFetchStatus);
            HandleOfflineStatus();
        }
        else
        {
            // Si el fetch SÍ fue exitoso, activamos y leemos los valores.
            await remoteConfig.ActivateAsync();
            Debug.Log("Configuración remota obtenida y activada exitosamente.");

            bool appIsEnabled = remoteConfig.GetValue("isAppEnabled").BooleanValue;

            if (appIsEnabled)
            {
                EnableApp();
            }
            else
            {
                // Conexión exitosa, pero la app está deshabilitada remotamente.
                DisableApp();
            }
        }
    }

    private void HandleOfflineStatus()
    {
        Debug.LogWarning("No hay conexión a internet. Cargando escena de 'Sin Conexión'.");
        SceneManager.LoadScene(freeTrialSceneName); 
    }

    private void DisableApp()
    {
        Debug.LogWarning("Aplicación DESHABILITADA remotamente. Cargando escena de fin de prueba...");
        SceneManager.LoadScene(noConnectionSceneName);
    }

    private void EnableApp()
    {
        Debug.Log("Aplicación HABILITADA. El juego continuará normalmente.");
    }

    //----------------------------------------------------------------
    // --- SECCIÓN ORIGINAL: LÓGICA DE ESCENAS Y JUEGO ---
    //----------------------------------------------------------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == freeTrialSceneName || scene.name == noConnectionSceneName)
        {
            return;
        }

        if (scene.name == registrationSceneName)
        {
            return;
        }

        Debug.Log("Escena cargada: " + scene.name);

        if (scene.name == gameSceneName && isTransitioningToGame)
        {
            isTransitioningToGame = false;
            Shoot shootComponent = FindObjectOfType<Shoot>();
            if (shootComponent != null)
            {
                if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.GetCurrentPlayer() != null)
                {
                    shootComponent.RestartCycle();
                }
                else
                {
                    Debug.LogError("No hay un jugador activo en PlayerDataManager");
                    LoadRegistrationScene();
                }
            }
            else { Debug.LogError("No se encontró el componente Shoot en la escena del juego"); }
        }
        else if (scene.name == tutorialSceneName && isTransitioningToTutorial)
        {
            isTransitioningToTutorial = false;
            TutorialShoot tutorialShootComponent = FindObjectOfType<TutorialShoot>();
            if (tutorialShootComponent != null)
            {
                tutorialShootComponent.RestartCycle();
            }
            else { Debug.LogError("No se encontró el componente TutorialShoot en la escena tutorial"); }
        }
        else if (scene.name == statsRankingSceneName && isTransitioningToStats)
        {
            isTransitioningToStats = false;
            StatsRankingManager statsManager = FindObjectOfType<StatsRankingManager>();
            if (statsManager != null)
            {
                statsManager.Initialize(lastSessionGolesAtajados, lastSessionGolesRecibidos);
            }
            else { Debug.LogError("No se encontró el StatsRankingManager en la escena de estadísticas"); }
        }
    }

    public void LoadGameScene()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.GetCurrentPlayer() == null)
        {
            Debug.LogError("Se intentó cargar la escena del juego sin un jugador activo");
            return;
        }
        isTransitioningToGame = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadTutorialScene()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.GetCurrentPlayer() == null)
        {
            Debug.LogError("Se intentó cargar la escena tutorial sin un jugador activo");
            return;
        }
        isTransitioningToTutorial = true;
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void LoadRegistrationScene()
    {
        SceneManager.LoadScene(registrationSceneName);
    }

    public void LoadStatsRankingScene(int golesAtajados, int golesRecibidos)
    {
        lastSessionGolesAtajados = golesAtajados;
        lastSessionGolesRecibidos = golesRecibidos;

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UpdateCurrentSessionStats(golesAtajados, golesRecibidos);
        }

        isTransitioningToStats = true;
        SceneManager.LoadScene(statsRankingSceneName);
    }

    public void FinishGame(int golesAtajados, int golesRecibidos)
    {
        Shoot shootComponent = FindObjectOfType<Shoot>();
        if (shootComponent != null)
        {
            shootComponent.StopCycle();
        }
        LoadStatsRankingScene(golesAtajados, golesRecibidos);
    }
}
