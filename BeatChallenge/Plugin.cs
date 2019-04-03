using IllusionPlugin;
using UnityEngine.SceneManagement;
using System;
using _PlayerController = BeatChallenge.Controllers.PlayerController;
using BeatChallenge.Utils;
namespace BeatChallenge
{
    public class Plugin : IPlugin
    {
        public static Plugin instance;
        public string Name => "BeatChallenge";
        public string Version => "1.0.0";
        public string UpdatedVersion { get; set; }
        public string CurrentScene { get; set; }

        public void OnApplicationStart()
        {
            Init();
        }

        private void Init()
        {
            try
            {

                instance = this;
                Logger.Init();

                SceneManager.sceneLoaded += SceneLoaded;
            } catch(Exception e)
            {
                Logger.Error(e);
            }
        }

        public void OnApplicationQuit()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        private void SceneLoaded(Scene to, LoadSceneMode mode)
        {
            try
            {
                if (to.name == "MenuCore")
                {
                    _PlayerController.Init(to);
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
