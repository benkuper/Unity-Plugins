using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class SpoutDX11 : MonoBehaviour {

	public string sharingName = "UnityDX9Test";
	public Color fill = Color.red;
	
	Texture2D sendTex;
	Texture2D receiveTex;
	
	Color[] colors;
	
	bool sharing;
	public bool updateSpout;
	
	// Use this for initialization
	void Start () {
		
		Spout.init();
		Spout.addListener(textureShared,textureStopped);
		
		sendTex = new Texture2D(300,200,TextureFormat.RGBA32,false);
		colors = sendTex.GetPixels();
		fillTexture();
	}
	
	
	
	// Update is called once per frame
	void Update () {
		if(updateSpout) Spout.update();
		
		if(sharing)
		{
			fillTexture();
			Spout.updateTexture(sharingName,sendTex);
		}
		
	}
	
	void fillTexture()
	{
		for(int i=0;i<colors.Length;i++)
		{
			colors[i] = fill;
		}
		
		sendTex.SetPixels(colors);
		sendTex.Apply();
	}
	
	void OnGUI()
	{
		Event e = Event.current;
		if(e.type != EventType.KeyDown) return;
		
		switch(e.keyCode)
		{
			case KeyCode.T:
				if(!sharing)
				{
					Spout.shareTexture(sharingName,sendTex);
					//renderer.material.mainTexture = sendTex;
					sharing = true;
				}
				break;
				
			case KeyCode.R:
				Spout.receiveTexture(sharingName);
				break;
			
			case KeyCode.S:
				Spout.startReceiving();
				break;
		}
		
	}
	
	void OnApplicationQuit()
	{
		Debug.Log("Cleanup !");
		Spout.clean();
	}
	
	public void textureShared(string senderName, Texture2D sharedTexture)
	{
		Debug.Log ("texture shared, "+senderName+"< >"+sharingName+" = "+(senderName == sharingName));
		
		if(sharingName != senderName) return;
		receiveTex = sharedTexture;
		renderer.material.mainTexture = receiveTex;
	}
	
	public void textureStopped(string senderName)
	{
		if(sharingName != senderName) return;
		renderer.material.mainTexture = null;
	}
}
