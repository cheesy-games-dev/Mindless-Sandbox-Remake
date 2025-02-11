using Steamworks.Data;
using UnityEngine;

namespace MindlessSandbox
{
    public class LobbySaver : MonoBehaviour
    {
        public Lobby currentLobby;

        public static LobbySaver Instance { get; private set; }

        private void Start()
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
}