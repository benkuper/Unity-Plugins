using UnityEngine;
using System.Collections;

public class Spout2Sender : MonoBehaviour {

	public string sharingName = "UnitySender";
	public Texture texture;

	public bool debugConsole = false;
	
	bool senderIsCreated;

	// Use this for initialization
	void Awake () {
		if(debugConsole) Spout2.initDebugConsole();
	}
	
	void Start()
	{
		if(texture != null) senderIsCreated = Spout2.CreateSender(sharingName,texture);
	}
	
	void Update()
	{
		if(texture != null && senderIsCreated) Spout2.UpdateSender(sharingName,texture);	
	}
	
	void OnDestroy()
	{
		if(senderIsCreated) Spout2.CloseSender(sharingName);
	}
	
}
