using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;

public class Spout {

	public static bool isInit;
	static bool isReceiving;
	
	public delegate void TextureSharedDelegate(string senderName, Texture2D sharedTexture);
	public static TextureSharedDelegate texSharedDelegate;
	public delegate void SenderStoppedDelegate(string senderName);
	public static SenderStoppedDelegate senderStoppedDelegate;
	
	
	private static List<String> newSenders;
	private static List<String> stoppedSenders;
	public static List<String> activeSenders;
	
	//temp
	private static string currentSender;
	
	
	public Renderer[] cubes;
	
	public static void init()
	{
		if(isInit) return;
		setHandlers();
		setNativeDebug();
		newSenders = new List<String>();
		stoppedSenders = new List<String>();
		activeSenders = new List<String>();
	}
	
	public static void addListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback )
	{
		texSharedDelegate += sharedCallback;
		senderStoppedDelegate += stoppedCallback;
	}	
	
	
	public static void update()
	{
		foreach(String s in newSenders)
		{
			Debug.Log ("new Sender !"+s);
			currentSender = s;
			receiveTexture(s);
		}
		newSenders.Clear();
		
		foreach(String s in stoppedSenders)
		{
			Debug.Log ("stoppedSender !"+s);
			if(senderStoppedDelegate != null) senderStoppedDelegate(s);
			if(s == currentSender && activeSenders.Count > 0) receiveTexture(activeSenders[0]);
		}
		
		
		stoppedSenders.Clear ();
		
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
	[DllImport ("NativeSpoutPlugin", EntryPoint="receiveTexture")]
	private static extern bool receiveTextureNative (string sharingName);
	public static void receiveTexture(string sharingName)
	{
		receiveTextureNative(sharingName);
	}
	
	//Texture Sharing Callbacks
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStartedDelegate(string senderName, IntPtr resourceView,int textureWidth, int textureHeight);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSharingStoppedDelegate(string senderName);
	
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern void SetSpoutHandlers(IntPtr startedHandler, IntPtr stoppedHandler);
	
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
	
	public static void TextureSharingStarted(string senderName, IntPtr resourceView, int width, int height)
	{
		//Debug.Log ("Texture Sharing Started : "+partnerId+"/"+width+":"+height);
		Texture2D tex = Texture2D.CreateExternalTexture(width,height,TextureFormat.BGRA32,false,true,resourceView);
		
		if(texSharedDelegate != null) texSharedDelegate(senderName,tex);
	}
	
	public static void TextureSharingStopped(string senderName)
	{
		Debug.Log ("Texture Sharing Stopped : "+senderName);
	}
	
	
	//Receiving Thread init
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderUpdateDelegate(int numSenders);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderStartedDelegate(string senderName);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderStoppedDelegate(string senderName);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="startReceiving")]
	private static extern bool startReceivingNative(IntPtr senderUpdateHandler,IntPtr senderStartedHandler,IntPtr senderStoppedHandler);
	[DllImport ("NativeSpoutPlugin")]
	private static extern void stopReceiving();
	
	public static void startReceiving()
	{
		if(isReceiving)return;
		
		SpoutSenderUpdateDelegate senderUpdate_delegate = new SpoutSenderUpdateDelegate(SenderUpdate);
		IntPtr intptr_senderUpdate_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderUpdate_delegate);
			
		SpoutSenderStartedDelegate senderStarted_delegate = new SpoutSenderStartedDelegate(SenderStarted);
		IntPtr intptr_senderStarted_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderStarted_delegate);
			
		SpoutSenderStoppedDelegate senderStopped_delegate = new SpoutSenderStoppedDelegate(SenderStopped);
		IntPtr intptr_senderStopped_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderStopped_delegate);
		
		isReceiving = startReceivingNative(intptr_senderUpdate_delegate, intptr_senderStarted_delegate, intptr_senderStopped_delegate);
	}
	
	public static void SenderUpdate(int numSenders)
	{
		//Debug.Log("Sender update, numSenders : "+numSenders);
	}
	
	public static void SenderStarted(string senderName)
	{
		Debug.Log("Sender started, sender name : "+senderName);
		newSenders.Add(senderName);
		activeSenders.Add (senderName);
	}
	public static void SenderStopped(string senderName)
	{
		Debug.Log("Sender stopped, sender name : "+senderName);
		stoppedSenders.Add (senderName);
		activeSenders.Remove(senderName);
		
		
	}
	
	
	//cleaning
	
	[DllImport ("NativeSpoutPlugin")]
	private static extern int SpoutCleanup();
	
	public static void clean()
	{
		//SpoutCleanup();
		stopReceiving();
	}
	
	
	//Debug
	[DllImport ("NativeSpoutPlugin")]
	public static extern void SetDebugFunction( IntPtr fp );
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void DebugDelegate(string str);
	
	static void setNativeDebug()
	{
		DebugDelegate callback_delegate = new DebugDelegate( DebugCallback );
		IntPtr intptr_delegate = 
			Marshal.GetFunctionPointerForDelegate(callback_delegate);
		
		// Call the API passing along the function pointer.
		SetDebugFunction( intptr_delegate );
	}
	
	
	static void DebugCallback(string str)
	{
		Debug.Log("[Plugin] " + str);
	}
}
