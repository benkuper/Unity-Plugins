using UnityEngine;
using System.Collections;

public class Spout2Receiver : MonoBehaviour {

	private string _sharingName = "Any";
	private Texture2D _texture;
	public string sName = "Any";
	
	// Use this for initialization
	void Start () {
		Spout2.initDebugConsole();
		Spout2.addListener(texShared,texStopped);
	}
	
	// Update is called once per frame
	void Update () {
		//sharingName = sName;
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
		if(texInfo.name == _sharingName || sharingName == "Any") texture = null;
	}
	
	public Texture2D texture
	{
		get { return _texture; }
		set {
			_texture = value;
			if(renderer != null) 
			{
				renderer.material.mainTexture = _texture;
			}
		}
	}
	
	public string sharingName
	{
		get { return _sharingName; }
		set {
			if(_sharingName == value) return;
			_sharingName = value;
			if(sharingName == "Any")
			{ 
				texture = Spout2.activeSenders[Spout2.activeSenders.Count-1].getTexture();
			}else
			{
				TextureInfo texInfo = Spout2.getTextureInfo(sharingName);
				if(texInfo != null) texture = texInfo.getTexture ();
				texture = null;
			}
		}
	}
}
