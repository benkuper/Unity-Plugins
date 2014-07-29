using UnityEngine;
using System.Collections;
using System;

public class TextureInfo  {
	
	public string name;
	private int w;
	private int h;
	private IntPtr resourceView;
	
	private Texture2D tex;
	
	// Use this for initialization
	public TextureInfo (string name) {
		this.name = name;
		
	}
	
	public void setInfos(int width, int height, IntPtr resourceView){
		this.w = width;
		this.h = height;
		this.resourceView = resourceView;
	}
	
	public Texture2D getTexture()
	{
		if(resourceView == IntPtr.Zero)
		{
			Debug.LogWarning("ResourceView is null, returning empty texture");
			return new Texture2D(10,10);
		}
		
		
		if(tex == null) tex = Texture2D.CreateExternalTexture(w,h,TextureFormat.RGBA32,false,true,resourceView);
		
		return tex;
	}
}
