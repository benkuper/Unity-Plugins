using UnityEngine;
using System.Collections;

public class Spout2Sender : MonoBehaviour {

	public string sharingName;
	public Texture texture;
	// Use this for initialization
	void Awake () {
		Spout2.CreateSender(sharingName,texture);
	}
	
	
	// Update is called once per frame
	void Update () {
		//Debug.Log ("update !");
		Spout2.UpdateSender(sharingName,texture);	
	}
	
	void OnApplicationQuit()
	{
		Spout2.CloseSender(sharingName);
	}
	
}
