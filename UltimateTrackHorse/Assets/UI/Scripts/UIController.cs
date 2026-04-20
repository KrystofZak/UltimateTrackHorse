using System.Collections;
using System.Collections.Generic;
using GameLogic.Traps.Core;
using GameLogic.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// Master controller for the UI Toolkit menu system.
    /// Handles switching between different views, grabbing references to game managers,
    /// passing user input down to the game logic, and managing background visibility.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIController : MonoBehaviour
    {
        [Header("Drag and Drop from Hierarchy!")]
        
        /// <summary>
        /// Reference to the map generator responsible for building the track.
        /// </summary>
        [Tooltip("Drag the MapGenerator GameObject here")]
        public MapGeneration.MapGenerator mapGenerator;
        
        /// <summary>
        /// Reference to the main game logic controller.
        /// </summary>
        [Tooltip("Drag the GameManager GameObject here")]
        public GameLogic.GameManager gameManager;
        
        /// <summary>
        /// Reference to the script that handles placing obstacles on the generated track.
        /// </summary>
        [Tooltip("Drag the GameManager GameObject here (it has the SpawnObstacle script)")]
        public TrapSpawner spawnObstacle;

        [Header("Menu 3D Background")]
        [Tooltip("Drag the Virtual Camera looking at your custom Menu Diorama here")]
        public GameObject menuCamera;

        [Header("UI Audio")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private AudioCue hoverCue;
        [SerializeField] private AudioCue clickCue;

        private UIDocument document;
        private VisualElement root;

        [Header("Views")]
        private VisualElement mainMenuView;
        private VisualElement mapSelectionView;
        private VisualElement randomSelectionView;
        private VisualElement seededSelectionView;
        private VisualElement gameView;
        private VisualElement obstacleChoiceView;
        private VisualElement pauseView;
        private VisualElement settingsView;
        private VisualElement aboutUsView;
        private VisualElement victoryView;
        private VisualElement defeatView;

        // References for Lap History UI
        private VisualElement lapHistoryBox;
        private VisualElement lapListContainer;

        // References for Countdown UI
        private VisualElement countdownBox;
        private Label countdownText;

        // Reference for displaying the Map Seed in the Pause Menu
        private Label mapSeedLabel;

        /// <summary>
        /// Stores the last active view so that the "Back" button functions properly (e.g., from Settings back to Pause or Main Menu).
        /// </summary>
        private VisualElement previousView;
        
        /// <summary>
        /// Tracks the currently active visual element in the UI.
        /// </summary>
        private VisualElement currentView;

        /// <summary>
        /// Restores essential HUD components (Timer and Spedometer) that were originally tied to Legacy Canvas UI.
        /// Automatically attaches them to this GameObject if they are missing in the current scene to prevent NullReference errors.
        /// </summary>
        private void EnsureHUDComponents()
        {
            // Since that Canvas was deleted, we need to guarantee they still exist in the current scene by auto-adding them!
            Timer timerScript = FindObjectOfType<Timer>();
            if (timerScript == null) 
            {
                timerScript = gameObject.AddComponent<Timer>();
                Debug.Log("UIController: Auto-restored missing Timer script to the scene!");
            }

            Spedometer speedScript = FindObjectOfType<Spedometer>();
            if (speedScript == null)
            {
                speedScript = gameObject.AddComponent<Spedometer>();
                Debug.Log("UIController: Auto-restored missing Spedometer script to the scene!");
            }

            // Restore the Spedometer's car rigidbody variable
            if (speedScript.car == null && gameManager != null && gameManager.playerCar != null)
            {
                speedScript.car = gameManager.playerCar.GetComponent<Rigidbody>();
            }
        }

        /// <summary>
        /// Automatically called when the UI GameObject is initialized.
        /// Finds missing references, caches all UI elements, registers button click events, 
        /// and enforces the default view state (Main Menu).
        /// </summary>
        private void OnEnable()
        {
            // Auto-assign references if they were left blank in the Inspector, 
            // but preferring your manual Drag-and-Drop assignments in the Inspector to prevent any weird Unity bugs!
            if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGeneration.MapGenerator>();
            if (gameManager == null) gameManager = FindObjectOfType<GameLogic.GameManager>();
            if (spawnObstacle == null) spawnObstacle = FindObjectOfType<TrapSpawner>();
            if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
            
            if (menuCamera == null) 
            {
                var camObj = GameObject.Find("MenuCamera");
                if (camObj != null) menuCamera = camObj;
            }

            EnsureHUDComponents();

            document = GetComponent<UIDocument>();
            root = document.rootVisualElement;

            // 1. Query Views
            mainMenuView = root.Q<VisualElement>("mainMenuView");
            mapSelectionView = root.Q<VisualElement>("mapSelectionView");
            randomSelectionView = root.Q<VisualElement>("randomSelectionView");
            seededSelectionView = root.Q<VisualElement>("seededSelectionView");
            gameView = root.Q<VisualElement>("gameView");
            obstacleChoiceView = root.Q<VisualElement>("obstacleChoiceView");
            pauseView = root.Q<VisualElement>("pauseView");
            settingsView = root.Q<VisualElement>("settingsView");
            aboutUsView = root.Q<VisualElement>("aboutUsView");
            victoryView = root.Q<VisualElement>("victoryView");
            defeatView = root.Q<VisualElement>("defeatView");
            
            lapHistoryBox = root.Q<VisualElement>("LapHistoryBox");
            lapListContainer = root.Q<VisualElement>("LapList");

            countdownBox = root.Q<VisualElement>("CountdownBox");
            countdownText = root.Q<Label>("CountdownText");

            mapSeedLabel = root.Q<Label>("MapSeedLabel");

            // --- Register Global Audio Callbacks for all buttons ---
            var allButtons = root.Query<Button>().ToList();
            foreach (var button in allButtons)
            {
                button.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    audioManager?.PlayUI(hoverCue);
                });

                button.RegisterCallback<ClickEvent>(evt =>
                {
                    audioManager?.PlayUI(clickCue);
                });
            }

            // 2. Setup Button Callbacks

            // Copy Seed
            root.Q<Button>("CopySeedButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                if (mapSeedLabel != null)
                {
                    GUIUtility.systemCopyBuffer = mapSeedLabel.text;
                    Debug.Log("Copied seed to clipboard: " + mapSeedLabel.text);
                }
            });

            // Main Menu
            root.Q<Button>("PlayButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                ShowView(mapSelectionView);
                Time.timeScale = 1f;
            });
            root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(evt => ShowSettingsFrom(mainMenuView));
            root.Q<Button>("AboutButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                previousView = mainMenuView;
                ShowView(aboutUsView);
            });
            root.Q<Button>("QuitButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                Debug.Log("Quit Game!");
                Application.Quit();
            });

            // Map Selection
            root.Q<Button>("Random")?.RegisterCallback<ClickEvent>(evt => ShowView(randomSelectionView));
            root.Q<Button>("Seeded")?.RegisterCallback<ClickEvent>(evt => ShowView(seededSelectionView));
            root.Q<Button>("MapBackButton")?.RegisterCallback<ClickEvent>(evt => ShowView(mainMenuView));

            // Random Selection
            root.Q<Button>("Length5")?.RegisterCallback<ClickEvent>(evt => StartRandomRun(5));
            root.Q<Button>("Length10")?.RegisterCallback<ClickEvent>(evt => StartRandomRun(10));
            root.Q<Button>("Length15")?.RegisterCallback<ClickEvent>(evt => StartRandomRun(15));
            root.Q<Button>("LengthCustomPlay")?.RegisterCallback<ClickEvent>(evt => {
                var input = root.Q<TextField>("LengthCustomInput");
                if (input != null && !string.IsNullOrEmpty(input.value))
                {
                    if (int.TryParse(input.value, out int length))
                        StartRandomRun(length, input.value);
                    else
                        StartRandomRun(0, input.value);
                }
            });
            root.Q<Button>("RandomBackButton")?.RegisterCallback<ClickEvent>(evt => ShowView(mapSelectionView));

            // Seeded Selection
            root.Q<Button>("SeededPlayButton")?.RegisterCallback<ClickEvent>(evt => StartSeededRun());
            root.Q<Button>("SeededBackButton")?.RegisterCallback<ClickEvent>(evt => ShowView(mapSelectionView));

            // Obstacles
            root.Q<Button>("Zero")?.RegisterCallback<ClickEvent>(evt => OnObstacleSelected(0));
            root.Q<Button>("One")?.RegisterCallback<ClickEvent>(evt => OnObstacleSelected(1));
            root.Q<Button>("Two")?.RegisterCallback<ClickEvent>(evt => OnObstacleSelected(2));
            root.Q<Button>("ObstaclesBackButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                OnObstacleSelected(0);
            });

            // Game View / HUD
            root.Q<Button>("Pause")?.RegisterCallback<ClickEvent>(evt => 
            {
                ShowView(pauseView);
                Time.timeScale = 0f;
            });

            // Pause Menu
            root.Q<Button>("ResumeButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                ShowView(gameView);
                Time.timeScale = 1f;
            });
            root.Q<Button>("PauseSettingsButton")?.RegisterCallback<ClickEvent>(evt => ShowSettingsFrom(pauseView));
            root.Q<Button>("BackToMainButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                mapGenerator.ResetSeed();
                gameManager.isGameActive = false;
                ReturnToMainMenu();
            });

            // Settings Menu
            root.Q<Button>("SettingsBackButton")?.RegisterCallback<ClickEvent>(evt => RestorePreviousView());

            // About Us Menu
            root.Q<Button>("AboutBackButton")?.RegisterCallback<ClickEvent>(evt => RestorePreviousView());

            // Post-Game Menus
            root.Q<Button>("VictoryMainMenuButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                mapGenerator.ResetSeed();
                gameManager.isGameActive = false;
                ReturnToMainMenu();
            });
            root.Q<Button>("DefeatMainMenuButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                mapGenerator.ResetSeed();
                gameManager.isGameActive = false;
                ReturnToMainMenu();
            });
            root.Q<Button>("DefeatRetryButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                // Simple retry by going map selection
                mapGenerator.ResetSeed();
                gameManager.isGameActive = false;
                ShowView(mapSelectionView);
                Time.timeScale = 1f;
            });
            root.Q<Button>("VictoryRetryButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                mapGenerator.ResetSeed();
                gameManager.isGameActive = false;
                ShowView(mapSelectionView);
                Time.timeScale = 1f;
            });

            // Initialize default state.
            ReturnToMainMenu();
            
            // Kickstart the menu camera to instantly be active upon booting the game
            if (menuCamera != null)
            {
                TriggerCameraCut();
                menuCamera.SetActive(true);
                var vcam = menuCamera.GetComponent<Cinemachine.CinemachineVirtualCameraBase>();
                if (vcam != null) vcam.Priority = 100;
            }
        }

        /// <summary>
        /// Checks for input every frame, specifically handling the ESC key to toggle the pause menu.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentView == gameView)
                {
                    ShowView(pauseView);
                    Time.timeScale = 0f;
                }
                else if (currentView == pauseView)
                {
                    ShowView(gameView);
                    Time.timeScale = 1f;
                }
            }
        }

        /// <summary>
        /// Resets the game to its very beginning state.
        /// Displays the main menu, resumes normal time, clears any actively generated map, 
        /// and forcefully disables the car input so the player doesn't drive around in the background.
        /// </summary>
        private void ReturnToMainMenu()
        {
            ShowView(mainMenuView);
            Time.timeScale = 1f;

            // Safety wipe: ensure lap history is hidden if returning to main menu
            HideLapHistory();

            // Specifically destroy the map instantly so it doesn't run in the background
            if (mapGenerator != null)
            {
                mapGenerator.ClearScene();
            }

            // Immediately disable player input if passing through to main menu
            if (gameManager != null)
            {
                gameManager.StopRaceCountdown();
                
                if (gameManager.playerCar != null)
                {
                    CarController carController = gameManager.playerCar.GetComponent<CarController>();
                    if (carController != null)
                    {
                        carController.isInputEnabled = false;
                    }
                }
            }
            
            Timer timerScript = FindObjectOfType<Timer>();
            if (timerScript != null)
            {
                timerScript.StopTimer();
            }
            
        }

        /// <summary>
        /// Applies the 'hidden' CSS class to every single view element in the document.
        /// This creates a blank slate before un-hiding the specifically requested view.
        /// </summary>
        private void HideAllViews()
        {
            mainMenuView?.AddToClassList("hidden");
            mapSelectionView?.AddToClassList("hidden");
            randomSelectionView?.AddToClassList("hidden");
            seededSelectionView?.AddToClassList("hidden");
            gameView?.AddToClassList("hidden");
            obstacleChoiceView?.AddToClassList("hidden");
            pauseView?.AddToClassList("hidden");
            settingsView?.AddToClassList("hidden");
            aboutUsView?.AddToClassList("hidden");
            victoryView?.AddToClassList("hidden");
            defeatView?.AddToClassList("hidden");
        }

        /// <summary>
        /// The primary method for transitioning between UI screens. 
        /// Hides all other views, handles the tracking of "previous views" for the back button,
        /// and dynamically turns the UI background image transparent if the gameplay view is entered.
        /// </summary>
        /// <param name="newView">The specific VisualElement screen you want to show (e.g., gameView, mainMenuView).</param>
        public void ShowView(VisualElement newView)
        {
            if (newView == null) return;
            
            if (currentView != null && currentView != newView && currentView != obstacleChoiceView)
                previousView = currentView;

            HideAllViews();
            newView.RemoveFromClassList("hidden");
            currentView = newView;
            if (gameManager != null && gameManager.playerCar != null)
            {
                CarController carController = gameManager.playerCar.GetComponent<CarController>();
                if (carController != null)
                {
                    // Hra b   pouze pokud je aktivn  hern  obrazovka (gameView)
                    carController.isPlaying = (newView == gameView);
                }
            }

            // If entering the Pause View, instantly ping the MapGenerator for the active seed and display it.
            if (newView == pauseView && mapSeedLabel != null && mapGenerator != null)
            {
                mapSeedLabel.text = mapGenerator.GetSeedDisplayValue();
            }

            bool shouldShowMenuCam = !(newView == gameView || newView == obstacleChoiceView || newView == pauseView);

            if (menuCamera != null && menuCamera.activeSelf != shouldShowMenuCam)
            {
                TriggerCameraCut();
                menuCamera.SetActive(shouldShowMenuCam);

                // Force the menu camera to have extremely high priority so it always overpowers the game camera on boot!
                var vcam = menuCamera.GetComponent<Cinemachine.CinemachineVirtualCameraBase>();
                if (vcam != null)
                {
                    vcam.Priority = shouldShowMenuCam ? 100 : 0;
                }
            }

            // Target the specific background wrapper element so we don't accidentally style the invisible UI Document root.
            VisualElement targetContainer = root.Q<VisualElement>("RootContainer");

            if (targetContainer != null)
            {
                if (!shouldShowMenuCam)
                {
                    // Map becomes visible underneath the UI overlay
                    targetContainer.style.unityBackgroundImageTintColor = new StyleColor(Color.clear);
                }
                else
                {
                    // Restore to full white opacity (fully visible menu background)
                    targetContainer.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                }
            }
        }

        /// <summary>
        /// Hacks into the global camera brain to instantly execute a teleporting cut, 
        /// preventing the camera from sweeping 5000 units across the map between menus.
        /// </summary>
        private void TriggerCameraCut()
        {
            if (Camera.main != null)
            {
                Cinemachine.CinemachineBrain brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null)
                {
                    Cinemachine.CinemachineBlendDefinition oldDef = brain.m_DefaultBlend;
                    brain.m_DefaultBlend = new Cinemachine.CinemachineBlendDefinition(Cinemachine.CinemachineBlendDefinition.Style.Cut, 0f);
                    StartCoroutine(RestoreCinemachineBlend(brain, oldDef));
                }
            }
        }

        private IEnumerator RestoreCinemachineBlend(Cinemachine.CinemachineBrain brain, Cinemachine.CinemachineBlendDefinition oldDef)
        {
            // Wait strictly two frames so the Cut is consumed by Cinemachine's LateUpdate cycle completely
            yield return null;
            yield return null; 
            if (brain != null)
            {
                brain.m_DefaultBlend = oldDef;
            }
        }

        /// <summary>
        /// Safely checks if the Obstacle Choice UI overlay is currently active and visible to the player.
        /// Used primarily by game logic managers to know if they should halt background behavior.
        /// </summary>
        public bool IsObstacleChoiceViewActive => obstacleChoiceView != null && !obstacleChoiceView.ClassListContains("hidden");

        /// <summary>
        /// Directly triggers the UI transition to the Obstacle Chooser screen.
        /// </summary>
        public void ShowObstacleChoiceView()
        {
            ShowView(obstacleChoiceView);
        }

        /// <summary>
        /// Directly triggers the UI transition to the Victory screen.
        /// </summary>
        public void ShowVictoryView()
        {
            ShowView(victoryView);
        }

        /// <summary>
        /// Directly triggers the UI transition to the Defeat screen.
        /// </summary>
        public void ShowDefeatView()
        {
            ShowView(defeatView);
        }

        /// <summary>
        /// Toggles the massive full-screen countdown sequence graphics.
        /// </summary>
        public void ShowCountdown(bool show)
        {
            if (show)
                countdownBox?.RemoveFromClassList("hidden");
            else
                countdownBox?.AddToClassList("hidden");
        }

        /// <summary>
        /// Updates the text inside the countdown (3, 2, 1, GO!) and triggers a bouncy heartbeat physical scale effect.
        /// </summary>
        public void UpdateCountdownText(string text)
        {
            if (countdownText != null)
            {
                countdownText.text = text;
                
                // Add huge popping scale
                countdownText.AddToClassList("countdown-pop");
                
                // Remove pop naturally after a tiny delay so it shrinks smoothly back
                countdownText.schedule.Execute(() => 
                {
                    countdownText.RemoveFromClassList("countdown-pop");
                }).StartingIn(200); 
            }
        }

        /// <summary>
        /// Instantly hides the Lap history panel and erases the visually rendered child texts inside.
        /// </summary>
        public void HideLapHistory()
        {
            lapHistoryBox?.AddToClassList("hidden");
            lapListContainer?.Clear();
        }

        /// <summary>
        /// Populates the text labels on the Victory and Defeat screens before showing them.
        /// </summary>
        public void UpdatePostGameStats(bool isVictory, float bestLapTime, int obstaclesCount, int lapsCompleted = 0)
        {
            // Format time string
            string timeStr = "--:--";
            if (bestLapTime > 0 && bestLapTime < float.MaxValue)
            {
                float seconds = Mathf.FloorToInt(bestLapTime);
                float milliseconds = Mathf.FloorToInt((bestLapTime - seconds) * 100);
                timeStr = string.Format("{0:00}:{1:00}", seconds, milliseconds);
            }

            if (isVictory)
            {
                var bestLapLabel = root.Q<Label>("VictoryBestLap");
                if (bestLapLabel != null) bestLapLabel.text = $"Best Lap: {timeStr}";

                var obsLabel = root.Q<Label>("VictoryObstacles");
                if (obsLabel != null) obsLabel.text = $"Obstacles Count: {obstaclesCount}";

                var lapsLabel = root.Q<Label>("VictoryLaps");
                if (lapsLabel != null) lapsLabel.text = $"Laps Completed: {lapsCompleted}";
            }
            else
            {
                var bestLapLabel = root.Q<Label>("DefeatBestLap");
                if (bestLapLabel != null) bestLapLabel.text = $"Best Lap: {timeStr}";

                var obsLabel = root.Q<Label>("DefeatObstacles");
                if (obsLabel != null) obsLabel.text = $"Obstacles Count: {obstaclesCount}";
            }
        }

        /// <summary>
        /// Takes a list of raw lap times in seconds, formats them beautifully, calculates the fastest one, 
        /// and dynamically builds UI Labels to draw the History Board on the screen!
        /// </summary>
        public void UpdateLapHistoryUI(List<float> lapTimes)
        {
            if (lapHistoryBox == null || lapListContainer == null) return;

            // If we have 0 laps somehow, just stay hidden
            if (lapTimes == null || lapTimes.Count == 0)
            {
                HideLapHistory();
                return;
            }

            // Bring the box onto the screen!
            lapHistoryBox.RemoveFromClassList("hidden");
            lapListContainer.Clear();

            // First, find the absolute fastest time (lowest number)
            float bestTime = float.MaxValue;
            int bestLapIndex = -1;
            for (int i = 0; i < lapTimes.Count; i++)
            {
                if (lapTimes[i] < bestTime)
                {
                    bestTime = lapTimes[i];
                    bestLapIndex = i;
                }
            }

            // Always display the best lap at the top
            if (bestLapIndex != -1)
            {
                float bSeconds = Mathf.FloorToInt(bestTime);
                float bMilliseconds = Mathf.FloorToInt((bestTime - bSeconds) * 100);
                string bFormatted = string.Format("{0:00}:{1:00}", bSeconds, bMilliseconds);

                Label bestLabel = new Label($"Best (Lap {bestLapIndex + 1}): {bFormatted}");
                bestLabel.AddToClassList("history-label");
                bestLabel.AddToClassList("best-time-label");
                lapListContainer.Add(bestLabel);

                // Add a divider below the best lap
                VisualElement divider = new VisualElement();
                divider.AddToClassList("history-divider");
                lapListContainer.Add(divider);
            }

            // Figure out the window of the last 5 laps 
            int startIndex = Mathf.Max(0, lapTimes.Count - 5);

            // Loop and build the UI rows!
            for (int i = startIndex; i < lapTimes.Count; i++)
            {
                float timeInSecs = lapTimes[i];
                
                // Format matching exactly how the Timer.cs renders! (e.g. 01:25)
                float seconds = Mathf.FloorToInt(timeInSecs);
                float milliseconds = Mathf.FloorToInt((timeInSecs - seconds) * 100);
                string formattedTime = string.Format("{0:00}:{1:00}", seconds, milliseconds);

                // Build a literal label via code
                Label rowLabel = new Label($"Lap {i + 1}: {formattedTime}");
                rowLabel.AddToClassList("history-label");

                // Append the label dynamically into the visual layout
                lapListContainer.Add(rowLabel);
            }
        }

        /// <summary>
        /// Reads input from the Random UI selection and invokes the MapGenerator.
        /// Clears any manual seeds, sets the required track length, and informs the generator to begin.
        /// </summary>
        /// <param name="length">The predefined track length (5, 10, or 15).</param>
        /// <param name="lengthStr">The custom input string from the TextField for variable track lengths.</param>
        private void StartRandomRun(int length, string lengthStr = "")
        {
            Debug.Log($"UIController: StartRandomRun requested with length {length}");

            if (mapGenerator != null)
            {
                mapGenerator.SetSeed(""); // clear forced manual seed

                if (length == 5) mapGenerator.SetTrackLengthFive();
                else if (length == 10) mapGenerator.SetTrackLengthTen();
                else if (length == 15) mapGenerator.SetTrackLengthFifteen();
                else if (!string.IsNullOrEmpty(lengthStr)) mapGenerator.SetCustomTrackLengthFromString(lengthStr);
                
                Debug.Log("UIController: Telling MapGenerator to play...");
                mapGenerator.OnPlayClicked();
            }
            else
            {
                Debug.LogError("UIController: MapGenerator reference is MISSING! Did you drag and drop it into the Inspector?");
            }

            ShowView(gameView);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Reads input from the Seeded UI Selection and attempts to generate the map deterministically.
        /// Parses the string via MapGenerator.SetSeed and immediately launches gameplay.
        /// </summary>
        private void StartSeededRun()
        {
            var input = root.Q<TextField>("SeedInput");
            if (input != null && !string.IsNullOrEmpty(input.value))
            {
                if (mapGenerator != null)
                {
                    mapGenerator.SetSeed(input.value);
                    Debug.Log("UIController: Telling MapGenerator to play seeded...");
                    mapGenerator.OnPlayClicked();
                }
                else
                {
                    Debug.LogError("UIController: MapGenerator reference is MISSING! Did you drag and drop it into the Inspector?");
                }
            }
            ShowView(gameView);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Handled when the user clicks an obstacle amount button (e.g., 0, 1, 2) in the obstacle UI.
        /// Spawns the required number of obstacles, informs GameManager the choice is locked in, and jumps straight into gameplay.
        /// </summary>
        /// <param name="obstacleCount">Amount of obstacles requested.</param>
        private void OnObstacleSelected(int obstacleCount)
        {
            if (spawnObstacle != null)
            {
                spawnObstacle.SpawnNewTraps(obstacleCount);
            }
            else
            {
                Debug.LogWarning("UIController: SpawnObstacle missing!");
            }
            
            // Adjust the timer based on the player's obstacle choice.
            // Since this runs EVERY time they pick obstacles (every lap),
            // we ADD/SUBTRACT from the pile of adjustments so it stacks as intended!
            Timer timerScript = FindObjectOfType<Timer>();
            if (timerScript != null)
            {
                if (obstacleCount == 0)
                {
                    timerScript.SubtractSecondsFromIncrement(2f);
                }
                else if (obstacleCount == 2)
                {
                    timerScript.AddSecondsToIncrement(2f);
                }
            }
            else
            {
                Debug.LogWarning("UIController: Timer missing! Could not adjust time.");
            }
            
            if (gameManager != null)
            {
                gameManager.OnChoiceClicked();
            }
            else
            {
                Debug.LogWarning("UIController: GameManager missing!");
            }

            ShowView(gameView);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Stores the view that the user was currently on so the 'Back' button on the settings panel knows where to return.
        /// </summary>
        /// <param name="sourceView">The menu we are leaving to enter Settings.</param>
        private void ShowSettingsFrom(VisualElement sourceView)
        {
            previousView = sourceView;
            ShowView(settingsView);
        }

        /// <summary>
        /// Restores the view that the user was on before entering Settings.
        /// Defaults back to the Main Menu if tracking somehow failed.
        /// </summary>
        private void RestorePreviousView()
        {
            if (previousView != null)
                ShowView(previousView);
            else
                ShowView(mainMenuView);
        }
    }
}
