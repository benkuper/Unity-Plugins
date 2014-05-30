using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


public class SpoutManager : MonoBehaviour {

	Texture2D tex;
	
	// Use this for initialization
	void Start () {
		tex = new Texture2D(256,256,TextureFormat.RGBA32,false);
		shareTexture(tex);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	
	
	
	//Spout Native Plugin Exports
	[DllImport ("NativeSpoutPlugin", EntryPoint="shareTexture")]
	private static extern int shareTextureNative (IntPtr texture);
	
	
	
	//Helpers
	public static void shareTexture(Texture texture)
	{
		Debug.Log ("[SpoutManager :: shareTexture]");
		int result = shareTextureNative(texture.GetNativeTexturePtr());
		Debug.Log ("share result = "+result);
	}
}
