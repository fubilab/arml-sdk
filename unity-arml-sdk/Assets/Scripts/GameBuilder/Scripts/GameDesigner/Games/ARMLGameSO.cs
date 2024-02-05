using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARML.GameBuilder
{
    [CreateAssetMenu(menuName = "ARML/Create New Game", fileName = "ARML Game")]
    public class ARMLGameSO : ScriptableObject
    {
        [SerializeField] string gameName;
        public SceneField gameScene;
        [SerializeField] List<Level> levels;
        [SerializeField] List<ScoreEntry> highScores;
        public bool usesScores;
        public bool isEncrypted;

        private IDataService DataService = new JsonDataService();

        /// <summary>
        /// Returns name of ARML Game
        /// </summary>
        public string GetGameName()
        {
            return gameName;
        }

        public void AddHighScore(ScoreEntry sc)
        {
            highScores.Add(sc);

            if (DataService.SaveData(string.Format("/{0}.json", gameName), highScores, isEncrypted))
            {
                Debug.Log("Succesfully saved score data");
            }
        }
        public void LoadScores(string path)
        {
            List<ScoreEntry> loadedScores = DataService.LoadData<List<ScoreEntry>>(path, isEncrypted);

            highScores = loadedScores;
        }
    }

    [Serializable]
    public struct ScoreEntry
    {
        public int score;
        public float timeToComplete;
        public string name;
        public string dateTime;

        public ScoreEntry(int _score, float _timeToComplete, string _name)
        {
            score = _score;
            timeToComplete = _timeToComplete;
            name = _name;
            dateTime = DateTime.Now.ToString();
        }
    }
}
