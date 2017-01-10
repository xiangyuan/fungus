﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Fungus
{
    /// <summary>
    /// A singleton game object which displays a simple UI for the save system.
    /// </summary>
    public class SaveMenu : MonoBehaviour 
    {
        const string SaveDataKey = "save_data";

        const string NewGameSavePointKey = "new_game";

        [Tooltip("On scene start, execute any Save Point Loaded event handlers which have the 'new_game' key")]
        [SerializeField] protected bool autoStartGame = true;

        [Tooltip("Delete the save game data from disk when player restarts the game. Useful for testing, but best switched off for release builds.")]
        [SerializeField] protected bool restartDeletesSave = false;

        [Tooltip("The CanvasGroup containing the save menu buttons")]
        [SerializeField] protected CanvasGroup saveMenuGroup;

        [Tooltip("The button which hides / displays the save menu")]
        [SerializeField] protected Button saveMenuButton;

        [Tooltip("The button which saves the save history to disk")]
        [SerializeField] protected Button saveButton;

        [Tooltip("The button which loads the save history from disk")]
        [SerializeField] protected Button loadButton;

        [Tooltip("The button which rewinds the save history to the previous save point.")]
        [SerializeField] protected Button rewindButton;

        [Tooltip("The button which fast forwards the save history to the next save point.")]
        [SerializeField] protected Button forwardButton;

        [Tooltip("The button which restarts the game.")]
        [SerializeField] protected Button restartButton;

        protected static bool saveMenuActive = false;

        protected AudioSource clickAudioSource;

        protected LTDescr fadeTween;

        protected static SaveMenu instance;

        protected string startScene = "";

        protected virtual void Awake()
        {
            // Only one instance of SaveMenu may exist
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            GameObject.DontDestroyOnLoad(this);

            clickAudioSource = GetComponent<AudioSource>();
        }

        protected virtual void Start()
        {
            // Assume that the first scene that contains the SaveMenu is also the scene to load on restart.
            startScene = SceneManager.GetActiveScene().name;


            if (!saveMenuActive)
            {
                saveMenuGroup.alpha = 0f;
            }

            var saveManager = FungusManager.Instance.SaveManager;

            if (autoStartGame &&
                saveManager.NumSavePoints == 0)
            {
                StartNewGame();
            }

            CheckSavePointKeys();
        }

        protected virtual void StartNewGame()
        {
            var saveManager = FungusManager.Instance.SaveManager;

            // Create an initial save point
            saveManager.AddSavePoint(NewGameSavePointKey, "");

            // Start game execution
            SaveManager.ExecuteBlocks(NewGameSavePointKey);
        }

        protected virtual void Update()
        {
            var saveManager = FungusManager.Instance.SaveManager;

            if (saveButton != null)
            {
                // Don't allow saving unless there's at least one save point in the history,
                // This avoids the case where you could try to load a save data with 0 save points.
                saveButton.interactable = saveManager.NumSavePoints > 0;
            }
            if (loadButton != null)
            {
                loadButton.interactable = saveManager.SaveDataExists(SaveDataKey);
            }
            if (rewindButton != null)
            {
                rewindButton.interactable = saveManager.NumSavePoints > 0;
            }
            if (forwardButton != null)
            {
                forwardButton.interactable = saveManager.NumRewoundSavePoints > 0;
            }
        }

        /// <summary>
        /// Warns if duplicate SavePointKeys are found.
        /// </summary>
        protected void CheckSavePointKeys()
        {
            List<string> keys = new List<string>();

            var savePoints = GameObject.FindObjectsOfType<SavePoint>();

            foreach (var savePoint in savePoints)
            {
                if (string.IsNullOrEmpty(savePoint.SavePointKey))
                {
                    continue;
                }

                if (keys.Contains(savePoint.SavePointKey))
                {
                    Debug.LogError("Save Point Key " + savePoint.SavePointKey + " is defined multiple times.");
                }
                else
                {
                    keys.Add(savePoint.SavePointKey);
                }
            }
        }

        protected void PlayClickSound()
        {
            if (clickAudioSource != null)
            {
                clickAudioSource.Play();
            }
        }

        /// <summary>
        /// Callback for the restart scene load
        /// </summary>
        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != startScene)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        
            StartNewGame();
        }

        #region Public methods

        /// <summary>
        /// Toggles the expanded / collapsed state of the save menu.
        /// Uses a tween to fade the menu UI in and out.
        /// </summary>
        public virtual void ToggleSaveMenu()
        {
            if (fadeTween != null)
            {
                LeanTween.cancel(fadeTween.id, true);
                fadeTween = null;
            }

            if (saveMenuActive)
            {
                // Switch menu off
                LeanTween.value(saveMenuGroup.gameObject, saveMenuGroup.alpha, 0f, 0.5f).setOnUpdate( (t) => { 
                    saveMenuGroup.alpha = t;
                }).setOnComplete( () => {
                    saveMenuGroup.alpha = 0f;
                });
            }
            else
            {
                // Switch menu on
                LeanTween.value(saveMenuGroup.gameObject, saveMenuGroup.alpha, 1f, 0.5f).setOnUpdate( (t) => { 
                    saveMenuGroup.alpha = t;
                }).setOnComplete( () => {
                    saveMenuGroup.alpha = 1f;
                });
            }

            saveMenuActive = !saveMenuActive;
        }

        /// <summary>
        /// Handler function called when the Save button is pressed.
        /// </summary>
        public virtual void Save()
        {
            var saveManager = FungusManager.Instance.SaveManager;

            if (saveManager.NumSavePoints > 0)
            {
                PlayClickSound();
                saveManager.Save(SaveDataKey);
            }
        }

        /// <summary>
        /// Handler function called when the Load button is pressed.
        /// </summary>
        public virtual void Load()
        {
            var saveManager = FungusManager.Instance.SaveManager;

            if (saveManager.SaveDataExists(SaveDataKey))
            {
                PlayClickSound();
                saveManager.Load(SaveDataKey);
            }
        }

        /// <summary>
        /// Handler function called when the Rewind button is pressed.
        /// </summary>
        public virtual void Rewind()
        {
            PlayClickSound();

            var saveManager = FungusManager.Instance.SaveManager;
            if (saveManager.NumSavePoints > 0)
            {
                saveManager.Rewind();
            }
        }

        /// <summary>
        /// Handler function called when the Fast Forward button is pressed.
        /// </summary>
        public virtual void FastForward()
        {
            PlayClickSound();

            var saveManager = FungusManager.Instance.SaveManager;
            if (saveManager.NumRewoundSavePoints > 0)
            {
                saveManager.FastForward();
            }
        }

        /// <summary>
        /// Handler function called when the Restart button is pressed.
        /// </summary>
        public virtual void Restart()
        {
            if (string.IsNullOrEmpty(startScene))
            {
                Debug.LogError("No start scene specified");
                return;
            }

            var saveManager = FungusManager.Instance.SaveManager;

            PlayClickSound();

            saveManager.ClearHistory();
            if (restartDeletesSave)
            {
                saveManager.Delete(SaveDataKey);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(startScene);
        }

        #endregion
    }
}