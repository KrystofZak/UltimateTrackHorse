using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIController : MonoBehaviour
    {
        [Header("Drag and Drop from Hierarchy!")]
        [Tooltip("Drag the MapGenerator GameObject here")]
        public MapGeneration.MapGenerator mapGenerator;
        
        [Tooltip("Drag the GameManager GameObject here")]
        public GameLogic.GameManager gameManager;
        
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

        // Track history for 'Back' buttons or resume states
        private VisualElement previousView;
        private VisualElement currentView;

        private void EnsureHUDComponents()
        {
            // The old GUI Canvas likely contained the original Timer and Spedometer scripts. 
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

            // Initialize default state. (If this runs before background is grabbed, it clears it!)
            ReturnToMainMenu();
        }

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

        public void ShowView(VisualElement newView)
        {
            if (newView == null) return;
            
            // Allow tracking of last view for proper settings return behavior
            if (currentView != null && currentView != newView && currentView != obstacleChoiceView)
                previousView = currentView;

            HideAllViews();
            newView.RemoveFromClassList("hidden");
            currentView = newView;

            // Get exactly the element you put your image on
            VisualElement targetContainer = root.Q<VisualElement>("RootContainer");

            if (targetContainer != null)
            {
                // The easiest and safest way to hide the background without deleting its texture 
                // is to simply make its Tint Color fully transparent!
                if (newView == gameView || newView == obstacleChoiceView)
                {
                    targetContainer.style.unityBackgroundImageTintColor = new StyleColor(Color.clear);
                }
                else
                {
                    // Restore to full white (visible) for all menus
                    targetContainer.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                }
            }
        }

        public bool IsObstacleChoiceViewActive => obstacleChoiceView != null && !obstacleChoiceView.ClassListContains("hidden");

        public void ShowObstacleChoiceView()
        {
            ShowView(obstacleChoiceView);
        }

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

        private void ShowSettingsFrom(VisualElement sourceView)
        {
            previousView = sourceView;
            ShowView(settingsView);
        }

        private void RestorePreviousView()
        {
            if (previousView != null)
                ShowView(previousView);
            else
                ShowView(mainMenuView);
        }
    }
}
