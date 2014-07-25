using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Spout2Receiver))]
[CanEditMultipleObjects]
public class Spout2ReceiverEditor : Editor {

	Spout2Receiver receiver;
	int _popupIndex = 0;
	string[] options;
	
	void OnEnable()
	{
		if(receiver == null)
		{
			receiver = target as Spout2Receiver;
			Spout2.addListener(texShared,senderStopped);
			updateOptions ();
		}
	}
	
	public void texShared(TextureInfo texInfo)
	{
		updateOptions();
	}
	
	public void senderStopped(TextureInfo texInfo)
	{
		Debug.Log ("Editor : senderStopped");
		updateOptions();
	}
	
	void updateOptions()
	{
		Debug.Log ("updateOptions");
		options = new string[Spout2.activeSenders.Count+1];
		options[0] = "Any";
		
		
		for(int i=0;i<Spout2.activeSenders.Count;i++)
		{
			options[i+1] = Spout2.activeSenders[i].name;
		}
		
		updateReceiver();
	}
	
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		popupIndex = EditorGUILayout.Popup("Select texture",popupIndex,options);
	}
	
	void updateReceiver()
	{
		receiver.sharingName = options[popupIndex];
	}
	
	int popupIndex
	{
		get { return _popupIndex;}
		set {
			if(popupIndex == value) return;
			_popupIndex = value;
			updateReceiver();
		}
	}
}
