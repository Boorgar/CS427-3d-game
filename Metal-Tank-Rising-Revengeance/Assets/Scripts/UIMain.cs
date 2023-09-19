/*  This file is part of the "Tanks Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TanksMP
{
    /// <summary>
    /// UI script for all elements, settings and user interactions in the menu scene.
    /// </summary>
    public class UIMain : MonoBehaviour
    {
        /// <summary>
        /// Window object for input ip address of server to connect to.
        /// </summary>
        public GameObject ipaddress_input;

        /// <summary>
        /// Window object for loading screen between connecting and scene switch.
        /// </summary>
        public GameObject loadingWindow;

        /// <summary>
        /// Window object for displaying errors with the connection or timeouts.
        /// </summary>
        public GameObject connectionErrorWindow;
        
        /// <summary>
        /// Window object for displaying errors with the billing actions.
        /// </summary>
        public GameObject billingErrorWindow;
        
        /// <summary>
		/// Settings: input field for the player name.
		/// </summary>
		public InputField nameField;

        /// <summary>
        /// Settings: dropdown selection for network mode.
        /// </summary>
        public Dropdown networkDrop;

        /// <summary>
        /// Dropdown selection for preferred game mode.
        /// </summary>
        public Dropdown gameModeDrop;

        /// <summary>
        /// Settings: input field for manual server address,
        /// hosting a server in a private network (Photon only).
        /// </summary>
        public InputField serverField;

		/// <summary>
		/// Settings: checkbox for playing background music.
		/// </summary>
		public Toggle musicToggle;

		/// <summary>
		/// Settings: slider for adjusting game sound volume.
		/// </summary>
		public Slider volumeSlider;
			
		//how many times the shop has been opened
		//private int shopOpened = 0;

		//how many times the settings have been opened
		//private int settingsOpened = 0;
        

        //initialize player selection in Settings window
        //if this is the first time launching the game, set initial values
        void Start()
        {      
            //set initial values for all settings         
            if (!PlayerPrefs.HasKey(PrefsKeys.playerName)) PlayerPrefs.SetString(PrefsKeys.playerName, "User" + System.String.Format("{0:0000}", Random.Range(1, 9999)));
            if (!PlayerPrefs.HasKey(PrefsKeys.networkMode)) PlayerPrefs.SetInt(PrefsKeys.networkMode, 0);
            if (!PlayerPrefs.HasKey(PrefsKeys.gameMode)) PlayerPrefs.SetInt(PrefsKeys.gameMode, 0);
            if (!PlayerPrefs.HasKey(PrefsKeys.serverAddress)) PlayerPrefs.SetString(PrefsKeys.serverAddress, "127.0.0.1");
            if (!PlayerPrefs.HasKey(PrefsKeys.playMusic)) PlayerPrefs.SetString(PrefsKeys.playMusic, "true");
            if (!PlayerPrefs.HasKey(PrefsKeys.appVolume)) PlayerPrefs.SetFloat(PrefsKeys.appVolume, 1f);
            if (!PlayerPrefs.HasKey(PrefsKeys.activeTank)) PlayerPrefs.SetString(PrefsKeys.activeTank, Encryptor.Encrypt("0"));
            PlayerPrefs.Save();
            
            //read the selections and set them in the corresponding UI elements
            nameField.text = PlayerPrefs.GetString(PrefsKeys.playerName);
            networkDrop.value = PlayerPrefs.GetInt(PrefsKeys.networkMode);
            gameModeDrop.value = PlayerPrefs.GetInt(PrefsKeys.gameMode);
            serverField.text = PlayerPrefs.GetString(PrefsKeys.serverAddress);
            musicToggle.isOn = bool.Parse(PlayerPrefs.GetString(PrefsKeys.playMusic));
            volumeSlider.value = PlayerPrefs.GetFloat(PrefsKeys.appVolume);

            //call the onValueChanged callbacks once with their saved values
            OnMusicChanged(musicToggle.isOn);
            OnVolumeChanged(volumeSlider.value);
            
            //listen to IAP billing errors
            UnityIAPManager.purchaseFailedEvent += OnBillingError;
        }

        public void getIpAddress()
        {
            string ipaddress = ipaddress_input.GetComponentInChildren<InputField>().text;
            NetworkManagerCustom.singleton.networkAddress = ipaddress;
            ipaddress_input.SetActive(false);
            loadingWindow.SetActive(true);
            StartCoroutine(NetworkManagerCustom.StartMatch((NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode)));
            StartCoroutine(HandleTimeout());
        }


        /// <summary>
        /// Tries to enter the game scene. Sets the loading screen active while connecting to the
        /// Matchmaker and starts the timeout coroutine at the same time.
        /// </summary>
        public void Play()
        {
            //UnityAnalyticsManager.MainSceneClosed(shopOpened, settingsOpened, musicToggle.isOn,
            //                      Encryptor.Decrypt(PlayerPrefs.GetString(PrefsKeys.activeTank)));
            if (NetworkMode.Online == (NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode) || NetworkMode.LAN == (NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode))
            {
                ipaddress_input.SetActive(true);
                return;
            }
            loadingWindow.SetActive(true);
            StartCoroutine(NetworkManagerCustom.StartMatch((NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode)));
            StartCoroutine(HandleTimeout());
        }
        
        
        //coroutine that waits x seconds before cancelling joining a match
        IEnumerator HandleTimeout()
        {
            //timeout by transport layer usually happens after 10 seconds (change in Mirror transport class)
            //in case we are still not connected after the timeout happened, display the error window
            yield return new WaitForSeconds(11);

            if (Mirror.NetworkClient.active)
                yield break;

            //timeout has passed, we would like to stop joining a game now
            NetworkManagerCustom.singleton.StopHost();

            //display connection issue window
            OnConnectionError();
        }


        //activates the connection error window to be visible
        void OnConnectionError()
        {
            StopCoroutine(HandleTimeout());
            loadingWindow.SetActive(false);
            connectionErrorWindow.SetActive(true);
        }
        
        
        //activates the billing error window to be visible
        void OnBillingError(string error)
        {
            //get text label to display billing failed reason
            Text errorLabel = billingErrorWindow.GetComponentInChildren<Text>();
            if(errorLabel)
                errorLabel.text = "Purchase failed.\n" + error;
            
            billingErrorWindow.SetActive(true);
        }


        /// <summary>
        /// Increase counter when opening the shop.
        /// Used for Unity Analytics purposes.
        /// </summary>
        public void OpenShop()
        {
            //shopOpened++;
        }


        /// <summary>
        /// Increase counter when opening settings.
        /// Used for Unity Analytics purposes.
        /// </summary>
        public void OpenSettings()
        {
            //settingsOpened++;
        }
		
		
		/// <summary>
        /// Allow additional input of server address only in network mode LAN.
        /// Otherwise, the input field will be hidden in the settings (Photon only).
        /// </summary>
        public void OnNetworkChanged(int value)
        {
        }


        /// <summary>
        /// Save newly selected GameMode value to PlayerPrefs in order to check it later.
        /// Called by DropDown onValueChanged event.
        /// </summary>
        public void OnGameModeChanged(int value)
        {
            PlayerPrefs.SetInt(PrefsKeys.gameMode, value);
            PlayerPrefs.Save();
        }


        /// <summary>
        /// Modify music AudioSource based on player selection.
        /// Called by Toggle onValueChanged event.
        /// </summary>
        public void OnMusicChanged(bool value)
        {
			AudioManager.GetInstance().musicSource.enabled = musicToggle.isOn;
            AudioManager.PlayMusic(0);
        }


        /// <summary>
        /// Modify global game volume based on player selection.
        /// Called by Slider onValueChanged event.
        /// </summary>
        public void OnVolumeChanged(float value)
        {
            volumeSlider.value = value;
            AudioListener.volume = value;
        }
			

        /// <summary>
        /// Saves all player selections chosen in the Settings window on the device.
        /// </summary>
        public void CloseSettings()
        {
            PlayerPrefs.SetString(PrefsKeys.playerName, nameField.text);
            PlayerPrefs.SetInt(PrefsKeys.networkMode, networkDrop.value);
			PlayerPrefs.SetString(PrefsKeys.serverAddress, serverField.text);
            PlayerPrefs.SetString(PrefsKeys.playMusic, musicToggle.isOn.ToString());
            PlayerPrefs.SetFloat(PrefsKeys.appVolume, volumeSlider.value);
            PlayerPrefs.Save();
        }

			
        /// <summary>
        /// Opens a browser window to the App Store entry for this app.
        /// </summary>
        public void RateApp()
        {
            //UnityAnalyticsManager.RateStart();
            
            //default app url on non-mobile platforms
            //replace with your website, for example
			string url = "";
			
			#if UNITY_ANDROID
				url = "http://play.google.com/store/apps/details?id=" + Application.identifier;
			#elif UNITY_IPHONE
				url = "https://itunes.apple.com/app/idXXXXXXXXX";
			#endif
			
			if(string.IsNullOrEmpty(url) || url.EndsWith("XXXXXX"))
            {
                Debug.LogWarning("UIMain: You didn't replace your app links!");
                return;
            }
			
			Application.OpenURL(url);
        }
    }
}
