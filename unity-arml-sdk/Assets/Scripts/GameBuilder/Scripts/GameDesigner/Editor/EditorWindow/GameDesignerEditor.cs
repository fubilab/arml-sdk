using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;

namespace ARML.GameBuilder
{
    public class GameDesignerEditor : EditorWindow
    {
        VisualElement container;
        ObjectField loadGameObjectField;
        Label gameNameLabel;
        Button loadGameButton;

        ARMLGameSO gameSO;

        GameController gameManager;

        [MenuItem("ARML/Game Designer")]
        public static void ShowWindow()
        {
            GameDesignerEditor window = GetWindow<GameDesignerEditor>();
            window.titleContent = new GUIContent("ARML Game Designer");
            window.minSize = new Vector2(500, 500);
        }

        public void CreateGUI()
        {
            string filePath = FranUtils.GetScriptableObjectFilePath(this);

            container = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(filePath.Substring(0, filePath.Length - 2) + "uxml");
            container.Add(visualTree.Instantiate());

            loadGameObjectField = container.Q<ObjectField>("loadGameObjectField");
            loadGameObjectField.objectType = typeof(ARMLGameSO);
            //Try to find a way to check when object is chosen to automatically call LoadGame
            //Meanwhile we can use a button

            loadGameButton = container.Q<Button>("loadGameButton");
            loadGameButton.clicked += LoadGame;

            gameNameLabel = container.Q<Label>("gameNameLabel");

            //Find GameManager
            gameManager = GameObject.FindObjectOfType<GameController>();
        }

        void LoadGame()
        {
            gameSO = loadGameObjectField.value as ARMLGameSO;

            if (gameSO == null)
                return;

            gameNameLabel.text = gameSO.GetGameName();

            gameManager.LoadGame(gameSO);
        }

    }
}