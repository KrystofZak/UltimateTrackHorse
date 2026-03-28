using System.Collections;
using System.Collections.Generic;
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
        public GameLogic.Obstacles.SpawnObstacle spawnObstacle;

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
            if (spawnObstacle == null) spawnObstacle = FindObjectOfType<GameLogic.Obstacles.SpawnObstacle>();

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

            // 2. Setup Button Callbacks

            // Main Menu
            root.Q<Button>("PlayButton")?.RegisterCallback<ClickEvent>(evt => 
            {
                ShowView(mapSelectionView);
                Time.timeScale = 1f;
            });
            root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(evt => ShowSettingsFrom(mainMenuView));
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
                ReturnToMainMenu();
            });

            // Settings Menu
            root.Q<Button>("SettingsBackButton")?.RegisterCallback<ClickEvent>(evt => RestorePreviousView());

            // Initialize default state.
            ReturnToMainMenu();
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

            // Specifically destroy the map instantly so it doesn't run in the background
            if (mapGenerator != null)
            {
                mapGenerator.ClearScene();
            }

            // Immediately disable player input if passing through to main menu
            if (gameManager != null && gameManager.playerCar != null)
            {
                CarController carController = gameManager.playerCar.GetComponent<CarController>();
                if (carController != null)
                {
                    carController.isInputEnabled = false;
                }
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

            // Target the specific background wrapper element so we don't accidentally style the invisible UI Document root.
            VisualElement targetContainer = root.Q<VisualElement>("RootContainer");

            if (targetContainer != null)
            {
                // The easiest and safest way to hide the background without destroying UI Builder's inline texture reference 
                // is to simply make its Tint Color fully transparent!
                if (newView == gameView || newView == obstacleChoiceView)
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
                spawnObstacle.SpawnNewObstacles(obstacleCount);
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
