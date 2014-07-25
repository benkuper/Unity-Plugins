using UnityEngine;
using System.Collections;

public class Spout2Sender : MonoBehaviour {

	public string sharingName;
	public Texture texture;
	
	public bool sending;
	// Use this for initialization
	void Start () {
		
	}
	
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space))
		{
			if(!sending)
			{
				Spout2.CreateSender(sharingName,texture);
				sending = true;
			}
		}
		
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			if(sending)
			{
				Spout2.CloseSender(sharingName);
				sending = false;
			}
		}
		
		//Debug.Log ("update !");
		if(sending) Spout2.UpdateSender(sharingName,texture);	
	}
	
	void OnApplicationQuit()
	{
		Spout2.CloseSender(sharingName);
	}
	
}
