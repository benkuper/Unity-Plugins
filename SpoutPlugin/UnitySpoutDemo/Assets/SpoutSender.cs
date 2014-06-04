using UnityEngine;
using System.Collections;

public class SpoutSender : MonoBehaviour {

	public string sendingName = "unity";
	public RenderTexture rt;
	public Texture2D tex;
	
	bool sharing;
	// Use this for initialization
	void Start () {
		Spout.init();
		tex = new Texture2D(rt.width,rt.height);
		
		
	}
	
	// Update is called once per frame
	void Update () {
		//Spout.update();
		/*
		RenderTexture.active = rt;
		tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
		tex.Apply();
		RenderTexture.active = null;
		*/
		if(sharing)
		{
			Spout.updateTexture(sendingName,rt);
		}
	}
	
	void OnGUI()
	{
		Event e = Event.current;
		if(e.type != EventType.KeyDown) return;
		switch(e.keyCode)
		{
		case KeyCode.Space:
			Spout.shareTexture(sendingName,rt);
			sharing = true;
			break;
		}
	}
	
	void OnApplicationQuit()
	{
		Spout.stopSharing(sendingName);
	}
}
