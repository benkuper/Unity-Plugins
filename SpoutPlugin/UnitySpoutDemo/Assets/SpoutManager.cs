using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


public class SpoutManager : MonoBehaviour {

	Texture2D tex;
	public GameObject go;
	Color[] texColors;
	
	public Color color1;
	public Color color2;
	public Color color3;
	
	public bool autoAnimate;
	public bool useRenderTexture;
	
	public RenderTexture rt;
	
	Texture targetTex;
	
	// Use this for initialization
	void Start () {
		tex = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
		texColors = new Color[tex.width*tex.height];
		
		if(useRenderTexture)
		{
			targetTex = rt;
		}else
		{
			targetTex = tex;
		}
		
		go.renderer.material.mainTexture = targetTex;
		
		updateTexColors();
		
		shareTexture(tex);
	}
	
	void updateTexColors()
	{
		if(useRenderTexture)
		{
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();
			RenderTexture.active = null;
			
			
			
		}else
		{
			if(autoAnimate)
			{
				color1.r = (color1.r+.01f)%1;
				color2.g = (color2.g+.007f)%1;
				color3.b = (color3.b+.023f)%1;
			}
			
			for(int i=0;i<tex.width;i++)
			{
				for(int j=0;j<tex.height;j++)
				{
					Color c = Color.Lerp(color1,Color.Lerp(color2,color3,i*1.00f/tex.width),j*1.00f/tex.height);
					texColors[j*tex.width+i] = c;
					//tex.SetPixel(i,j,c);
				}
			}
			tex.SetPixels(texColors);
			tex.Apply();
		}
	}
	
	// Update is called once per frame
	void Update () {
		updateTexColors();
		updateTexture(tex);
	}
	
	 
	
	void OnApplicationQuit()
	{
		Debug.Log("Cleanup !");
		//SpoutCleanup();
	}
	
	//Spout Native Plugin Exports
	[DllImport ("NativeSpoutPlugin", EntryPoint="shareDX11")]
	private static extern int shareTextureNative (IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="updateTexture")]
	private static extern int updateTextureNative (IntPtr texture);
	
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int SpoutCleanup();
	
	
	
	//Helpers
	public static void shareTexture(Texture texture)
	{
		Debug.Log ("[SpoutManager :: shareTexture]");
		int result = shareTextureNative(texture.GetNativeTexturePtr());
		Debug.Log ("share result = "+result);
	}
	
	public static void updateTexture(Texture texture)
	{
		updateTextureNative(texture.GetNativeTexturePtr());
	}
}
