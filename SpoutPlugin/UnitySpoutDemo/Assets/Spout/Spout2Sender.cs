using UnityEngine;
using System.Collections;

public class Spout2Sender : MonoBehaviour {

	public string sharingName = "UnitySender";
	public Texture texture;
	
	public bool debugConsole = false;
	
	
	// Use this for initialization
	void Awake () {
		if(debugConsole) Spout2.initDebugConsole();
	}
	
	void Start()
	{
		if(texture != null) Spout2.CreateSender(sharingName,texture);
	}
	
	void Update()
	{
		if(texture != null) Spout2.UpdateSender(sharingName,texture);	
	}
	
	void OnDestroy()
	{
		Spout2.CloseSender(sharingName);
	}
	
}
