using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI;
using VRCSDK2;
using UnityEngine.Events;
using VRC.Core;
using VRLoader.Attributes;
using VRLoader.Modules;
using System.Linq;

namespace PortalToFriend
{
    [ModuleInfo("Drop portal to instance", "1.0", "bay, yoshifan, plu")]
    public class Mod : VRModule
    {
        public const string userPanelString = "MenuContent/Screens/UserInfo/User Panel/";
        private bool initialized = false;
        
        void OnGUI() {
            if (initialized) return;
            var userPanel = GameObject.Find(userPanelString);
            Utils.Log("Mod.OnGUI and not initialized");
            if (userPanel is null) return;
            initialized = true;
            Init();
        }

        private void Init() {
            Utils.Log("Mod.Init");
            new GameObject("mod").AddComponent<ModComponent>();
        }
    }

    class ModComponent : MonoBehaviour
    {
        public GameObject UserDropPortal { get; private set; }
        public static VRCUiManager VrcuimInstance { get; private set; }
        public static VRCUiPopupManager PopupManagerInstance;
        void Awake()
        {
            Utils.Log("ModComponent.Awake");
            DontDestroyOnLoad(this);
            var userPanel = GameObject.Find(Mod.userPanelString);
            var playlistButton = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists/PlaylistsButton");
            var playlists = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists");
            UserDropPortal = Instantiate(playlistButton, playlists.transform);
            UserDropPortal.transform.SetParent(userPanel.transform);
            UserDropPortal.transform.position += new Vector3(0, 0.065f, 0);
            UserDropPortal.GetComponentInChildren<Text>().text = "Drop Portal to Instance";
            UserDropPortal.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            UserDropPortal.GetComponentInChildren<Button>().onClick.AddListener(DropPortalToUserClicked);
        }

        private void DropPortalToUserClicked()
        {
            var player = GetVRCUiMInstance().menuContent.GetComponentInChildren<PageUserInfo>().user;
            Utils.Log("Selected User:", player.displayName, player.id.Enclose());
            Utils.Log("Location:", player.location);
            if (player.id == APIUser.CurrentUser.id)
            {
                GetVRCUiPopupManager().ShowAlert("Error", "You cannot drop a portal to yourself!");
                return;
            }
            else if (player.location.IsNullOrEmpty())
            {
                GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} has no valid location!");
                return;
            }
            else if (player.location == "private")
            {
                GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} location is private!");
                return;
            }
            var location = player.location;
            string[] array = location.Split(':');
            Utils.Log("Dropping Portal to instance: ", player.displayName);
            DropPortalToLocation(array);
        }

        public static void DropPortalToLocation(string[] location)
        {
            Utils.Log("ModComponent.DropPortalToLocation");
            var gameObject = Networking.Instantiate(VRC_EventHandler.VrcBroadcastType.Always, "Portals/PortalInternalDynamic", VRCPlayer.Instance.transform.position + VRCPlayer.Instance.transform.forward, VRCPlayer.Instance.transform.rotation);
            Networking.RPC(VRC_EventHandler.VrcTargetType.AllBufferOne, gameObject, "ConfigurePortal", new object[]
            {
                    location[0],
                    location[1],
                    0
            });
        }

        public static VRCUiManager GetVRCUiMInstance()
        {
            if (ModComponent.VrcuimInstance == null)
            {
                var method = typeof(VRCUiManager).GetMethod("get_Instance", BindingFlags.Static | BindingFlags.Public);
                if (method == null)
                {
                    return null;
                }
                ModComponent.VrcuimInstance = (VRCUiManager)method.Invoke(null, new object[0]);
            }
            return VrcuimInstance;
        }

        public static VRCUiPopupManager GetVRCUiPopupManager()
        {
            if (PopupManagerInstance == null)
            {
                FieldInfo[] nonpublicStaticPopupFields = typeof(VRCUiPopupManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
                if (nonpublicStaticPopupFields.Length == 0)
                {
                    return null;
                }
                FieldInfo uiPopupManagerInstanceField = nonpublicStaticPopupFields.First(field => field.FieldType == typeof(VRCUiPopupManager));
                if (uiPopupManagerInstanceField == null)
                {
                    return null;
                }
                PopupManagerInstance = uiPopupManagerInstanceField.GetValue(null) as VRCUiPopupManager;
            }

            return PopupManagerInstance;
        }
    }
}