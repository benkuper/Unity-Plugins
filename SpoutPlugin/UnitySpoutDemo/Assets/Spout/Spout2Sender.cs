using UnityEngine;
using System.Collections;

public class Spout2Sender : MonoBehaviour {

	public string sharingName;
	public Texture texture;
	
	public bool debugConsole = true;
	
	// Use this for initialization
	void Awake () {
		if(debugConsole) Spout2.initDebugConsole();
	}
	
	void Start()
	{
		Spout2.CreateSender(sharingName,texture);
	}
	
	void Update()
	{
		Spout2.UpdateSender(sharingName,texture);	
	}
	
	void OnDestroy()
	{
		Spout2.CloseSender(sharingName);
	}
	
}
