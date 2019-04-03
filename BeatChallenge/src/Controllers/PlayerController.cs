
using UnityEngine;
using UnityEngine.SceneManagement;
using BeatChallenge.Packets;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Logger = BeatChallenge.Utils.Logger;
using System.IO;
using BeatChallenge.Utils;
using System.Collections;

namespace BeatChallenge.Controllers
{
    class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;

        private List<string> _replayPackets = new List<string>();
        private string songName = "";
        private Int32 startTime = 0;
        private bool _isRecording = false;
        public bool isRecording
        {
            get { return _isRecording; }
            set
            {
                if (value == _isRecording) { return; }

                _isRecording = value;
                if (_isRecording)
                {
                    _replayPackets.Clear();
                    startTime = GetCurrentSeconds();
                    InvokeRepeating("MovementRecord", 0f, ReplayController.UPDATE_INTERVAL);
                }
                else
                {
                    Write();
                    CancelInvoke("MovementRecord");
                }
            }
        }

        public static void Init(Scene to)
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("PlayerController").AddComponent<PlayerController>();
            SceneManager.activeSceneChanged += Instance.ActiveSceneChanged;
        }

        public void Awake()
        {
            if (Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ReplayController.LoadAvatars();
            }
        }

        private Int32 GetCurrentSeconds()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        void MovementRecord()
        {
            try
            {
                _replayPackets.Add(GetReplayPacket().Serialize());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void Write()
        {
            FileInfo fileLocation = new FileInfo($"UserData/Replays/{songName}_{startTime}.replay");
            fileLocation?.Directory?.Create();
            StreamWriter writer = new StreamWriter(fileLocation.FullName) { AutoFlush = true };
            int index = 0;
            foreach (string pos in _replayPackets.ToArray())
            {
                if (index > 0)
                {
                    writer.Write("|");
                }
                writer.Write(pos);
                index++;
            }
            _replayPackets.Clear();
        }

        private ReplayPacket GetReplayPacket()
        {
            return new ReplayPacket(WorldController.GetCharacterPosition());
        }

        public void ActiveSceneChanged(Scene from, Scene to)
        {
            if (to.name == "GameCore" || to.name == "MenuCore")
            {
                if (to.name == "GameCore")
                {
                    StartCoroutine(UpdatePresenceAfterFrame());
                    isRecording = true;
                }
                else
                {
                    isRecording = false;
                }
            }
        }
        private IEnumerator UpdatePresenceAfterFrame()
        {
            yield return true;
            MainFlowCoordinator _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault();
            GameplayCoreSceneSetup _gameplaySetup = Resources.FindObjectsOfTypeAll<GameplayCoreSceneSetup>().FirstOrDefault();
            if (_gameplaySetup == null)
            {
                yield break;
            }
            FieldInfo _gameplayCoreSceneSetupDataField = typeof(SceneSetup<GameplayCoreSceneSetupData>).GetField("_sceneSetupData", BindingFlags.NonPublic | BindingFlags.Instance);
            GameplayCoreSceneSetupData _mainSetupData = _gameplayCoreSceneSetupDataField.GetValue(_gameplaySetup) as GameplayCoreSceneSetupData;
            if (_mainSetupData == null || _gameplaySetup == null || _mainFlowCoordinator == null)
            {
                yield break;
            }
            IDifficultyBeatmap diff = _mainSetupData.difficultyBeatmap;
            this.songName = $"{diff.level.songName}";
            ReplayController.Create(songName);
        }
    }
}
