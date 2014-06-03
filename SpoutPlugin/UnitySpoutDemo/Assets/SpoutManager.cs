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
		setHandlers();
		
		tex = new Texture2D(useRenderTexture?rt.width:500,useRenderTexture?rt.height:200,TextureFormat.RGBA32,false);
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
		if(autoAnimate && !useRenderTexture)
		{
			color1.r = (color1.r+.01f)%1;
			color2.g = (color2.g+.007f)%1;
			color3.b = (color3.b+.023f)%1;
			
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
		if(!sharing && Input.GetKeyDown(KeyCode.Space))
		{
			shareTexture(sharingName, targetTex);
			sharing = true;
			
			t.text = t.text + " [Sharing now]";
			Debug.Log(t.text);
		}else if(Input.GetKeyDown(KeyCode.R))
		{
			receiveTexture("active");
			//if(receiveTex == null) return;
			//go.renderer.material.mainTexture = receiveTex;
			//Debug.Log ("Format :"+receiveTex.format);
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
		stopSharing(sharingName);
		SpoutCleanup();
	}
	
	//Spout Native Plugin Exports
	[DllImport ("NativeSpoutPlugin", EntryPoint="shareDX11")]
	private static extern int shareTextureNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="updateTexture")]
	private static extern int updateTextureNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="receiveDX11")]
	private static extern bool receiveTextureNative (string sharingName);
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern void SetSpoutHandlers(IntPtr startedHandler, IntPtr stoppedHandler);
	
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int getNumSenders ();
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="getSenderNames")]
    private static extern void getSenderNamesNative (IntPtr namesArray);
	   
	[DllImport ("NativeSpoutPlugin")]
	private static extern int stopSharing(string sharingName);     
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int SpoutCleanup();
	
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStartedDelegate(int partnerId, IntPtr resourceView,int textureWidth, int textureHeight);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStoppedDelegate(int partnerId);
	//Helpers
	
	
	void TextureSharingStarted(int partnerId, IntPtr resourceView, int width, int height)
	{
		Debug.Log ("Texture Sharing Started : "+partnerId+"/"+width+":"+height);
		/*
		rw = width;
		rh = height;
		resourcePtr = resourceView;
		newShare = true;
		*/
		if(width > 0 && height > 0)
		{
			go.renderer.material.mainTexture = Texture2D.CreateExternalTexture(width,height,TextureFormat.RGBA32,false,false,resourceView);
		}
	}
	
	void TextureSharingStopped(int partnerId)
	{
		Debug.Log ("Texture Sharing Stopped : "+partnerId);
	}
	
	void setHandlers()
	{
		SpoutSharingStartedDelegate started_delegate = new SpoutSharingStartedDelegate (TextureSharingStarted);
		IntPtr intptr_started_delegate = 
			Marshal.GetFunctionPointerForDelegate (started_delegate);
		
		SpoutSharingStoppedDelegate stopped_delegate = new SpoutSharingStoppedDelegate (TextureSharingStopped);
		IntPtr intptr_stopped_delegate = 
			Marshal.GetFunctionPointerForDelegate (stopped_delegate);
		// Call the API passing along the function pointer.
		SetSpoutHandlers (intptr_started_delegate,intptr_stopped_delegate);
	}
	
	
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
	
	
	public void receiveTexture(string sharingName)
	{
		receiveTextureNative(sharingName);
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
