using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class Spout2Receiver : MonoBehaviour {
	
	[SerializeField]
	private string _sharingName;
	
	private Texture2D _texture;
	
	public bool debugConsole = false;
	
	// Use this for initialization
	void Awake () {
		if(debugConsole) Spout2.initDebugConsole();
	}
	
	void Start()
	{
		Spout2.addListener(texShared,texStopped);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	public void texShared(TextureInfo texInfo)
	{
		if(sharingName == "" || sharingName == texInfo.name || sharingName == "Any")
		{
			texture = texInfo.getTexture();
		}
	}
	
	public void  texStopped(TextureInfo texInfo)
	{
		if(texInfo.name == _sharingName)
		{
			
			texture = null;
			
		}else if(sharingName == "Any" && Spout2.activeSenders.Count > 0)
		{
			texture = Spout2.activeSenders[Spout2.activeSenders.Count-1].getTexture();
		}
	}
	
	public Texture2D texture
	{
		get { return _texture; }
		set {
			_texture = value;
			if(renderer != null && Application.isPlaying) 
			{
				renderer.material.mainTexture = _texture;
			}
		}
	}
	
	public string sharingName
	{
		get { return _sharingName; }
		set {
			if(_sharingName == value && sharingName != "Any") return;
			_sharingName = value;
			if(sharingName == "Any")
			{ 
				if(Spout2.activeSenders != null && Spout2.activeSenders.Count > 0)
				{
					texture = Spout2.activeSenders[Spout2.activeSenders.Count-1].getTexture();
				}
			}else
			{
				Debug.Log ("Set sharing name :"+sharingName);
				TextureInfo texInfo = Spout2.getTextureInfo(sharingName);
				if(texInfo != null) texture = texInfo.getTexture ();
				else
				{
					Debug.LogWarning ("Sender "+sharingName+" does not exist");
					texture = new Texture2D(10,10);
				}
				
			}
		}
	}
}
