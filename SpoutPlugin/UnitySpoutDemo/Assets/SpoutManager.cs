using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

using UnityEngine.UI;

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
	
	public Text t;
	
	bool sharing;
	
	public string sharingName = "UnityDemo 1";
	
	// Use this for initialization
	void Start () {
		setNativeDebug();
		
		tex = new Texture2D(rt.width,rt.height,TextureFormat.ARGB32,false);
		texColors = new Color[tex.width*tex.height];
		
		if(useRenderTexture)
		{
			targetTex = rt;
		}else
		{
			targetTex = tex;
		}
		
		go.renderer.material.mainTexture = tex;
		
		updateTexColors();
		
		
	}
	
	
	void updateTexColors()
	{
		/*if(useRenderTexture)
		{
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();
			RenderTexture.active = null;
			return;
			
			
		}
		*/
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
	
	// Update is called once per frame
	void Update () {
		updateTexColors();
		if(!sharing && Input.GetKeyDown(KeyCode.Space))
		{
			shareTexture(sharingName, targetTex);
			sharing = true;
			
			t.text = t.text + " [Sharing now]";
			Debug.Log(t.text);
		}else if(Input.GetKeyDown(KeyCode.R))
		{
			Texture2D receiveTex = receiveDemoTexture();
			go.renderer.material.mainTexture = receiveTex;
			Debug.Log ("Format :"+receiveTex.format);
		}else if(Input.GetKeyDown(KeyCode.N))
		{
			Debug.Log (getNumSenders());
		}
		
		if(sharing)
		{
			updateTexture(sharingName, targetTex);
		}
	}
	 
	void OnGUI()
	{
		Event e = Event.current;
		if(e.type == EventType.KeyDown)
		{
			switch(e.keyCode)
			{
			case KeyCode.G:
				getSenderNames();
			break;
			}
		}
	}
	
	void OnApplicationQuit()
	{
		Debug.Log("Cleanup !");
		//SpoutCleanup();
	}
	
	//Spout Native Plugin Exports
	[DllImport ("NativeSpoutPlugin", EntryPoint="shareDX11")]
	private static extern int shareTextureNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="updateTexture")]
	private static extern int updateTextureNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="receiveDX11")]
	private static extern bool receiveTextureNative (string sharingName, IntPtr texture, out int rWidth, out int rHeight, bool getActive);
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern IntPtr getTest();
	
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int getNumSenders ();
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="getSenderNames")]
    private static extern void getSenderNamesNative (IntPtr namesArray);
	        
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int SpoutCleanup();
	
	
	
	//Helpers
	public static void shareTexture(string sharingName, Texture texture)
	{
		Debug.Log ("[SpoutManager :: shareTexture]");
		int result = shareTextureNative(sharingName, texture.GetNativeTexturePtr());
		Debug.Log ("share result = "+result);
	}
	
	public static void updateTexture(string sharingName, Texture texture)
	{
		updateTextureNative(sharingName, texture.GetNativeTexturePtr());
	}
	
	public string[] getSenderNames()
	{
		Debug.Log ("get SenderNames");
		string[] names = new string[32];
		for(int i=0;i<32;i++)
		{
			names[i] = "Super long string"+i;
		}
		
		GCHandle h = GCHandle.Alloc(names,GCHandleType.Pinned);
		getSenderNamesNative(h.AddrOfPinnedObject());
		h.Free();
		foreach(string s in names)
		{
			Debug.Log (" > "+s);
		}
		return names;
	}
	
	public Texture2D receiveTexture(string sharingName)
	{
		return receiveTextureToNative(sharingName,false);
	}
	
	public Texture2D receiveDemoTexture()
	{
		return receiveTextureToNative("active-texture",true); 
	}
	
	public Texture2D receiveTextureToNative(string sharingName, bool getActive)
	{
		int rWidth = 0;
		int rHeight = 0;
		IntPtr rTex = IntPtr.Zero;
		GCHandle handle = GCHandle.Alloc(rTex,GCHandleType.Pinned);
		bool result = receiveTextureNative(sharingName, handle.AddrOfPinnedObject(), out rWidth, out rHeight, getActive);
		handle.Free();
		Debug.Log ("ReceiveTextureNative result :"+result);
		Debug.Log ("Receive Texture from Native :"+rWidth+"/"+rHeight);
		if(result)
		{
			return Texture2D.CreateExternalTexture(rWidth,rHeight,TextureFormat.BGRA32,false,false,rTex);
		}else
		{
			return null;
		}
	}
	
	
	//Debug
	[DllImport ("NativeSpoutPlugin")]
	public static extern void SetDebugFunction( IntPtr fp );
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void DebugDelegate(string str);
	
	void setNativeDebug()
	{
		DebugDelegate callback_delegate = new DebugDelegate( CallBackFunction );
		IntPtr intptr_delegate = 
			Marshal.GetFunctionPointerForDelegate(callback_delegate);
		
		// Call the API passing along the function pointer.
		SetDebugFunction( intptr_delegate );
	}
	
	
	static void CallBackFunction(string str)
	{
		Debug.Log("[Plugin] " + str);
	}
}
