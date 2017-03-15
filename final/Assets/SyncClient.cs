using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoSync;
using Amazon.CognitoSync.SyncManager;

public class SyncClient : MonoBehaviour {
	public GameObject ScoreUI;
	bool sync = false;
	string name;
	int health;
	int exp;
	Dataset playerInfo;
	CognitoSyncManager syncManager;
	CognitoAWSCredentials credentials;

	void Start () {
		UnityInitializer.AttachToGameObject (this.gameObject);
		//Remove if you want to build on an IOS device.
		AWSConfigs.LoggingConfig.LogTo = LoggingOptions.UnityLogger;
		credentials = new CognitoAWSCredentials (""//Put your own identity pool id here.
			, RegionEndpoint.USEast1);
		syncManager = new CognitoSyncManager (credentials, RegionEndpoint.USEast1);
		playerInfo = syncManager.OpenOrCreateDataset ("playerInfo");
		playerInfo.OnSyncSuccess += SyncSuccessCallback;
		UpdateInformation ();
	}

	public void ChangeName(string newName){
		name = newName;
		playerInfo.Put ("name", newName);
	}

	public void ChangeHealth(string newHealth){
		try{
			health = int.Parse(newHealth);
			playerInfo.Put("health", newHealth);
		} catch{
		}
	}
	
	public void ChangeExp(string newExp){
		try{
			exp = int.Parse(newExp);
			playerInfo.Put("exp", newExp);
		} catch{
		}
	}

	public void Synchronize(){
		if (!string.IsNullOrEmpty (playerInfo.Get ("FacebookId")) && !this.GetComponent<FacebookClient> ().isLoggedIn) {
			Debug.Log ("You must logged in to synchronize.");
		} else {
			sync = true;
			playerInfo.SynchronizeOnConnectivity ();
		}
	}

	void UpdateInformation(){
		if (!string.IsNullOrEmpty (playerInfo.Get ("name"))) {
			name = playerInfo.Get ("name");
			ScoreUI.transform.FindChild ("NameInputField").GetComponent<InputField> ().text = name;
		} else
			ScoreUI.transform.FindChild ("NameInputField").GetComponent<InputField> ().text = "";
		if (!string.IsNullOrEmpty (playerInfo.Get ("health"))) {
			health = int.Parse(playerInfo.Get ("health"));
			ScoreUI.transform.FindChild ("HealthInputField").GetComponent<InputField> ().text = health.ToString();
		} else
			ScoreUI.transform.FindChild ("HealthInputField").GetComponent<InputField> ().text = "";
		if (!string.IsNullOrEmpty (playerInfo.Get ("exp"))) {
			exp = int.Parse (playerInfo.Get ("exp"));
			ScoreUI.transform.FindChild ("ExpInputField").GetComponent<InputField> ().text = exp.ToString ();
		} else
			ScoreUI.transform.FindChild ("ExpInputField").GetComponent<InputField> ().text = "";
	}

	void SyncSuccessCallback(object sender, SyncSuccessEventArgs e){
		List<Record> newRecords = e.UpdatedRecords;
		for (int k = 0; k < newRecords.Count; k++) {
			Debug.Log (newRecords [k].Key + " was updated: " + newRecords [k].Value);
		}
		UpdateInformation ();
		sync = false;
	}

	public void FBHasLoggedIn(string token, string id){
		string oldFacebookId = playerInfo.Get ("FacebookId");
		if (string.IsNullOrEmpty (oldFacebookId) || id.Equals (oldFacebookId)) {
			playerInfo.Put ("FacebookId", id);
			credentials.AddLogin ("graph.facebook.com", token);
		} else {
			Debug.Log ("New user detected.");
			credentials.Clear ();
			playerInfo.Delete ();
			credentials.AddLogin ("graph.facebook.com", token);
			Synchronize ();
			StartCoroutine (WaitForEndOfSync (id));
		}
	}

	IEnumerator WaitForEndOfSync(string id){
		while (sync)
			yield return null;
		playerInfo.Put ("FacebookId", id);
	}

}
