using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ClientStartUp : NetworkBehaviour
{
	public Configuration configuration;
	public ServerStartUp serverStartUp;
	public NetworkManager networkManager;
	


    private void Start()
    {
		OnLoginUserButtonClick();
    }

    public void OnLoginUserButtonClick()
	{
		if (configuration.buildType == BuildType.REMOTE_CLIENT)
		{
			if (configuration.buildId == "")
			{
				throw new Exception("A remote client build must have a buildId. Add it to the Configuration. Get this from your Multiplayer Game Manager in the PlayFab web console.");
			}
			else
			{
				LoginRemoteUser();
			}
		}
		else if (configuration.buildType == BuildType.LOCAL_CLIENT)
		{
			SceneManager.LoadScene("Login");
		}
	}

	public void LoginRemoteUser()
	{
		Debug.Log("[ClientStartUp].LoginRemoteUser");

		//We need to login a user to get at PlayFab API's. 
		LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
		{
			TitleId = PlayFabSettings.TitleId,
			CreateAccount = true,
			CustomId = GUIDUtility.getUniqueID()
		};

		PlayFabClientAPI.LoginWithCustomID(request, OnPlayFabLoginSuccess, OnLoginError);
	}

	private void OnLoginError(PlayFabError response)
	{
		Debug.Log(response.ToString());
	}

	private void OnPlayFabLoginSuccess(LoginResult response)
	{
		Debug.Log(response.ToString());
		if (configuration.ipAddress == "")
		{   //We need to grab an IP and Port from a server based on the buildId. Copy this and add it to your Configuration.
			RequestMultiplayerServer();
		}
		else
		{
			ConnectRemoteClient();
		}
	}

	private void RequestMultiplayerServer()
	{
		Debug.Log("[ClientStartUp].RequestMultiplayerServer");
		RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
		requestData.BuildId = configuration.buildId;
		requestData.SessionId = System.Guid.NewGuid().ToString();
		requestData.PreferredRegions = new List<AzureRegion>() { AzureRegion.NorthEurope };
		PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
	}

	private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
	{
		Debug.Log(response.ToString());
		ConnectRemoteClient(response);
	}

	private void ConnectRemoteClient(RequestMultiplayerServerResponse response = null)
	{
		if (response == null)
		{
			networkManager.GetComponent<NetworkSettings>().ConnectAddress = configuration.ipAddress;
			networkManager.GetComponent<NetworkSettings>().ConnectPort = configuration.port;
			
			
		}
		else
		{
			Debug.Log("**** ADD THIS TO YOUR CONFIGURATION **** -- IP: " + response.IPV4Address + " Port: " + (ushort)response.Ports[0].Num);
			networkManager.GetComponent<NetworkSettings>().ConnectAddress = response.IPV4Address;
			networkManager.GetComponent<NetworkSettings>().ConnectPort = (ushort)response.Ports[0].Num;
			
		}

		SceneManager.LoadScene("Login");
	}

	private void OnRequestMultiplayerServerError(PlayFabError error)
	{
		Debug.Log(error.ErrorDetails);
	}
}
