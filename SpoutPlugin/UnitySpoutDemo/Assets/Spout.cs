using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Spout {

	public static bool isInit;
	
	public delegate void TextureSharedDelegate(Texture2D sharedTexture);
	public static TextureSharedDelegate texSharedDelegate;
	
	
	public static void init()
	{
		if(isInit) return;
		setHandlers();
	}
	
	public static void addListener(TextureSharedDelegate callback)
	{
		texSharedDelegate += callback;
	}	
	
	//Texture sharing and updating
	[DllImport ("NativeSpoutPlugin", EntryPoint="shareDX11")]
	private static extern int shareTextureNative (string sharingName, IntPtr texture);
	public static void shareTexture(string sharingName, Texture texture)
	{
		Debug.Log ("[Spout :: shareTexture]");
		int result = shareTextureNative(sharingName, texture.GetNativeTexturePtr());
		Debug.Log ("share result = "+result);
	}
	
	[DllImport ("NativeSpoutPlugin")]
	public static extern int stopSharing(string sharingName);   
		
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="updateTexture")]
	private static extern int updateTextureNative (string sharingName, IntPtr texture);
	
	public static void updateTexture(string sharingName, Texture texture)
	{
		updateTextureNative(sharingName, texture.GetNativeTexturePtr());
	}
	
	
	//Texture Receiving
	[DllImport ("NativeSpoutPlugin", EntryPoint="receiveDX11")]
	private static extern bool receiveTextureNative (string sharingName);
	public static void receiveTexture(string sharingName)
	{
		receiveTextureNative(sharingName);
	}
	
	//Texture Sharing Callbacks
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStartedDelegate(int partnerId, IntPtr resourceView,int textureWidth, int textureHeight);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStoppedDelegate(int partnerId);
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern void SetSpoutHandlers(IntPtr startedHandler, IntPtr stoppedHandler);
	
	
	public static void TextureSharingStarted(int partnerId, IntPtr resourceView, int width, int height)
	{
		//Debug.Log ("Texture Sharing Started : "+partnerId+"/"+width+":"+height);
		Texture2D tex = Texture2D.CreateExternalTexture(width,height,TextureFormat.BGRA32,false,true,resourceView);
		
		if(texSharedDelegate != null) texSharedDelegate(tex);
	}
	
	public static void TextureSharingStopped(int partnerId)
	{
		Debug.Log ("Texture Sharing Stopped : "+partnerId);
	}
	
	public static void setHandlers()
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
	
	//cleaning
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int SpoutCleanup();
	
	public static void clean()
	{
		SpoutCleanup();
	}
}
