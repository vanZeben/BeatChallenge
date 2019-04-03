using BeatChallenge.Packets;
using CustomAvatar;
using System.Collections.Generic;
using BeatChallenge.Utils;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
using Logger = BeatChallenge.Utils.Logger;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace BeatChallenge.Controllers
{
    class ReplayController : MonoBehaviour, IAvatarInput
    {
        public static float UPDATE_INTERVAL = 1f / 25f;
        public static ReplayController Instance;
        public static Dictionary<string, CustomAvatar.CustomAvatar> cachedAvatars = new Dictionary<string, CustomAvatar.CustomAvatar>();
        private static CustomAvatar.CustomAvatar defaultAvatar;

        private List<ReplayPacket> _replay = new List<ReplayPacket>();
        private SpawnedAvatar avatar;
        private CharacterPosition position;
        private CharacterPosition interpPosition;
        private CharacterPosition targetPosition;
        VRCenterAdjust _centerAdjust;

        public PosRot HeadPosRot => new PosRot(interpPosition.position_head, interpPosition.rotation_head);
        public PosRot LeftPosRot => new PosRot(interpPosition.position_leftHand, interpPosition.rotation_leftHand);
        public PosRot RightPosRot => new PosRot(interpPosition.position_rightHand, interpPosition.rotation_rightHand);
        private float update = 0f;
        private bool _playingReplay = false;
        public bool Replay
        {
            get { return _playingReplay; }
            set
            {
                if (value == _playingReplay) { return; }

                _playingReplay = value;
                if (_playingReplay)
                {
                    StartCoroutine(InitializeReplayManager());
                }
                else
                {
                    Destroy(Instance);
                }
            }
        }



        public static void LoadAvatars()
        {
            if (defaultAvatar == null)
            {
                defaultAvatar = CustomAvatar.Plugin.Instance.AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath.ToLower().Contains("template.avatar"));
            }
            Logger.Debug($"Found avatar, isLoaded={defaultAvatar.IsLoaded}");
            if (!defaultAvatar.IsLoaded)
            {
                defaultAvatar.Load(null);
            }

            foreach (CustomAvatar.CustomAvatar avatar in CustomAvatar.Plugin.Instance.AvatarLoader.Avatars)
            {
                Task.Run(() =>
                {
                    string hash;
                    if (CreateMD5FromFile(avatar.FullPath, out hash))
                    {
                        cachedAvatars.Add(hash, avatar);
                        Logger.Debug("Hashed avatar " + avatar.FullPath + "! Hash: " + hash);
                    }
                }).ConfigureAwait(false);
            }
        }

        public ReplayController()
        {
            Instance = this;
        }

        public static ReplayController Create(string name)
        {
            FileInfo fileLocation = new FileInfo($"UserData/Replays/{name}.replay");
            if (!fileLocation.Exists)
            {
                return null;
            }
            GameObject go = new GameObject("ReplayController");
            ReplayController _controller = go.AddComponent<ReplayController>();
            Logger.Info($"Loading Replay for {name}");
            string[] replay = File.ReadAllText(fileLocation.FullName).Split('|');
            foreach (string position in replay)
            {
                _controller._replay.Add(new ReplayPacket(position));
            }
            if (_controller._replay.Count == replay.Length)
            {
                Logger.Info($"Loaded Replay for {name}");
            } else
            {
                Logger.Info($"Failed to load replay for {name}");
            }
            _controller.Replay = true;
            return _controller;
        }

        public void Awake()
        {
            if (Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        IEnumerator InitializeReplayManager()
        {
            if (!defaultAvatar.IsLoaded)
            {
                Logger.Debug("Waiting for avatar to load");
                yield return new WaitWhile(delegate () { return !defaultAvatar.IsLoaded; });
            }
            else
            {
                yield return null;
            }

            Logger.Debug("Spawning avatar");
            _centerAdjust = FindObjectOfType<VRCenterAdjust>();

            avatar = AvatarSpawner.SpawnAvatar(defaultAvatar, this);
            
            avatar.GameObject.transform.SetParent(_centerAdjust.transform, false);
            transform.SetParent(_centerAdjust.transform, false);

            InvokeRepeating("MovementPlay", 0f, UPDATE_INTERVAL);
        }

        void OnDestroy()
        {
            CancelInvoke("MovementPlay");
            Destroy(avatar.GameObject);
        }
        

        private void SetRendererInChilds(Transform origin, bool enabled)
        {
            Renderer[] rends = origin.gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in rends)
            {
                rend.enabled = enabled;
            }
        }

        void Update()
        {
            update += Time.deltaTime * UPDATE_INTERVAL;
            if (update > 1.0f)
            {
                update = 1f;
            }
            interpPosition = new CharacterPosition
            {
                position_head = Vector3.Lerp(position.position_head, targetPosition.position_head, update),
                position_leftHand = Vector3.Lerp(position.position_leftHand, targetPosition.position_leftHand, update),
                position_rightHand = Vector3.Lerp(position.position_rightHand, targetPosition.position_rightHand, update),
                rotation_head = Quaternion.Lerp(position.rotation_head, targetPosition.rotation_head, update),
                rotation_leftHand = Quaternion.Lerp(position.rotation_leftHand, targetPosition.rotation_leftHand, update),
                rotation_rightHand = Quaternion.Lerp(position.rotation_rightHand, targetPosition.rotation_rightHand, update),
            };
        }
        void MovementPlay()
        {
            if (_replay.Count == 0) { Logger.Info($"Movement is 0"); Replay = false; return; }
            position = _replay[0].CharacterPosition;
            if (_replay.Count >= 2)
            {
                targetPosition = _replay[1].CharacterPosition;
            }
            update = 0f;
            transform.position = new Vector3(2f, 0f, 2.5f);
            _replay.RemoveAt(0);
        }


        public static T CreateInstance<T>(params object[] args)
        {
            var type = typeof(T);
            var instance = type.Assembly.CreateInstance(
                type.FullName, false,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, args, null, null);
            return (T)instance;
        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }
    }
}
