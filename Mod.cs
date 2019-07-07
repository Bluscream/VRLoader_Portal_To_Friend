using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;
using VRCSDK2;
using VRCModLoader;
using VRCTools;

namespace PortalToFriend
{
    [VRCModInfo("Drop portal to friend (VRCML)", "1.1", "bay, yoshifan, Bluscream")]
    public class Mod : VRCMod {
        private static VRCModInfoAttribute ModInfo = System.Attribute.GetCustomAttribute(typeof(Mod), typeof(VRCModInfoAttribute)) as VRCModInfoAttribute;
        public const string userPanelString = "MenuContent/Screens/UserInfo/User Panel/";
        public const string prefSection = "portaltofriend";
        private bool initialized = false;
        
        void OnGUI() {
            if (initialized) return;
            var userPanel = GameObject.Find(userPanelString);
            if (userPanel is null) return;
            initialized = true;
            Init();
        }

        private void Init() {
            Utils.Log($"Initializing {ModInfo.Name} v{ModInfo.Version} by {ModInfo.Author}");
            VRCTools.ModPrefs.RegisterCategory(prefSection, "Drop Portal to friend");
            VRCTools.ModPrefs.RegisterPrefBool(prefSection, "dropanywhere", false, "Allow dropping the portal anywhere");
            new GameObject("DropPortalToFriend").AddComponent<ModComponent>();
        }
    }

    class ModComponent : MonoBehaviour
    {
        public GameObject UserDropPortalButton { get; private set; }
        void Awake()
        {
            Utils.Log("ModComponent.Awake");
            DontDestroyOnLoad(this);
            var userPanel = GameObject.Find(Mod.userPanelString);
            var playlistButton = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists/PlaylistsButton");
            var playlists = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists");
            UserDropPortalButton = Instantiate(playlistButton, playlists.transform);
            UserDropPortalButton.transform.SetParent(userPanel.transform);
            UserDropPortalButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 75);
            UserDropPortalButton.GetComponentInChildren<Text>().text = "Drop Portal";
            UserDropPortalButton.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            UserDropPortalButton.GetComponentInChildren<Button>().onClick.AddListener(DropPortalToUserClicked);
        }

        private void DropPortalToUserClicked()
        {
            var player = VRCUiManagerUtils.GetVRCUiManager().menuContent.GetComponentInChildren<PageUserInfo>().user;
            Utils.Log("Selected User:", player.displayName, player.id.Enclose());
            Utils.Log("Location:", player.location);
            if (player.id == APIUser.CurrentUser.id)
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().ShowAlert("Error", "You cannot drop a portal to yourself!");
                return;
            }
            else if (player.location.IsNullOrEmpty())
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} has no valid location!");
                return;
            }
            else if (player.location == "private")
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} is in a private instance!");
                return;
            }
            else if (player.location == "local")
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} is in a local test world!");
                return;
            }
            string[] locationArray = player.location.Split(':');
            Action<ApiContainer> onSuccess = new Action<ApiContainer>(ApiWorldRecieved);
            Action<ApiContainer> onFailure = new Action<ApiContainer>(ApiWorldFailed);
            var world = API.Fetch<ApiWorld>(locationArray[0], onSuccess, onFailure, false);
            var instance = new ApiWorldInstance(world, locationArray[1], 0);
            Utils.Log("World: ", world.name, world.id.Enclose());
            var instancesstr = string.Join(";", world.instances.Select(x => $"{x.Key}={x.Value}").ToArray());
            Utils.Log("Instances:", instancesstr);
            if (world.instances.ContainsKey(instance.idOnly))
                instance.count = world.instances[instance.idOnly];
            else instance.count = -1;
            Utils.Log("Instance:", instance.idWithTags, $"({instance.count}/{world.capacity})");
            if (VRCTools.ModPrefs.GetBool(Mod.prefSection, "dropanywhere"))
            {
                DropPortalToLocation(locationArray, instance.count);
            } else {
                PortalInternal.CreatePortal(world, instance, VRCPlayer.Instance.transform.position, VRCPlayer.Instance.transform.forward, true);
            }
        }
        
        internal static void ApiWorldRecieved(ApiContainer apiContainer) { }

        internal static void ApiWorldFailed(ApiContainer apiContainer) { }

        public static void DropPortalToLocation(string[] location, int count=0)
        {
            Utils.Log("Dropping Portal to :", location);
            var gameObject = Networking.Instantiate(VRC_EventHandler.VrcBroadcastType.Always, "Portals/PortalInternalDynamic", VRCPlayer.Instance.transform.position + VRCPlayer.Instance.transform.forward, VRCPlayer.Instance.transform.rotation);
            Networking.RPC(VRC_EventHandler.VrcTargetType.AllBufferOne, gameObject, "ConfigurePortal", new object[]
            {
                    location[0],
                    location[1],
                    count
            });
        }
    }
}
