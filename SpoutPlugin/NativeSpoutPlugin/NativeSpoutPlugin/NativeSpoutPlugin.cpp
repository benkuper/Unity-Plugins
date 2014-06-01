#include "UnityPluginInterface.h"
#include "SpoutHelpers.h"

using namespace std;

//
// Spout
//
//	This creates a shared texture with the same size, format etc of the passed texture
//	The info of this texture is saved inshared memory so that
//	receivers can access the shared texture by way of the share handle
//	Once this shared memory is set up, all that is necessary is that the shared texture is updated
//	If the size of the texture changes - then the shared memory needs to be updated.






extern "C" int EXPORT_API shareDX11(char * senderName, ID3D11Texture2D * texturePointer)
{
	//UnityLog("Share DX11 From Plugin !");
	HANDLE sharedHandle;

	// LJ NOTE
	// For sharing a DX11 texture with DX9 receivers 
	// the only format that seems to work is  DXGI_FORMAT_B8G8R8A8_UNORM

	// Get the description of the passed texture
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);
	td.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
	td.MiscFlags =  D3D11_RESOURCE_MISC_SHARED;
	
	td.Format = DXGI_FORMAT_R8G8B8A8_UNORM; //test


	//UnityLog(std::to_string(td.Format).c_str());

	// Create a new shared texture with the same properties
	g_D3D11Device->CreateTexture2D(&td, NULL, &g_pSharedTexture);

	IDXGIResource* pOtherResource(NULL);
	HRESULT hr = g_pSharedTexture->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );

	// Derive the sharehandle from the texture just created
	if(hr == 0)	{
		pOtherResource->GetSharedHandle(&sharedHandle);
		g_pImmediateContext->CopyResource(g_pSharedTexture,texturePointer);
		pOtherResource->Release();

		createSenderFromSharedHandle(senderName, sharedHandle,td);
	}
	else {
		//UnityLog("error in shareDX11");
	}

	return 2;
}


extern "C" int EXPORT_API getNumSenders()
{
	spoutSenders senders;
	return senders.GetSenderCount();
}

extern "C" void EXPORT_API getSenderNames(char** names)
{
	UnityLog("get sender names");
	spoutSenders senders;
	char * sn = "Super long string to host a super long sender name";
	
	std::set<string> Senders;
	std::set<string>::iterator iter;
	senders.GetSenderNames(&Senders);
	UnityLog("senders :");

	const  int numSenders = Senders.size();
	//names = new const char * [numSenders];

	int i=0;
	if(numSenders) {
		for(iter = Senders.begin(); iter != Senders.end(); iter++) {
			string namestring = *iter; // the Sender name string
			char * name = new char[256];
			strcpy_s(name, 256, namestring.c_str());
			
			names[i] = (char *)namestring.c_str();
			UnityLog(names[i]);
			// we have the name already, so look for it's info
			DX9SharedTextureInfo info;
			if(!spout.getSharedInfo(name, &info)) {
				// Sender does not exist any more
				senders.ReleaseSenderName(name); // release from the shared memory list
			}
			i++;
		}
	}
}

// Receive a spout texture from a sender
// The global sender name has been set up already in initSpout
extern "C" bool EXPORT_API receiveDX11(char * senderName, ID3D11ShaderResourceView * texturePointer, unsigned int & w, unsigned int & h, boolean getActive = true)
{
	
	UnityLog("Receive Texture !");

	DWORD wFormat;
	HANDLE hShareHandle;
	//unsigned int width, height;

	// pass the current width, height and sendername
	//width = g_width;
	//height = g_height;

	unsigned int receivedWidth = 0;
	unsigned int receivedHeight = 0;

	spoutSenders senders;	

	/*
	if(getActive)
	{
		
		//std::set<string> Senders;
		std::set<string>::iterator iter;
		senders.GetSenderNames(&Senders);

		UnityLog(std::to_string(Senders.size()).c_str());

		if(Senders.size() > 0) {
			for(iter = Senders.begin(); iter != Senders.end(); iter++) {
				string namestring = *iter; // the Sender name string
			
				strcpy_s(senderName, 256, namestring.c_str());
				UnityLog("found !");
				// we have the name already, so look for it's info
				
				DX9SharedTextureInfo info;
				if(!spout.getSharedInfo(senderName, &info)) {
					// Sender does not exist any more
					senders.ReleaseSenderName(senderName); // release from the shared memory list
				}
				
				break;
			}
		}
	}
	*/
	senders.GetSenderNameInfo(0,senderName,256,w,h,hShareHandle);

	UnityLog("Sender name :");
	UnityLog(senderName);


	ID3D11Resource * tempResource11;
	ID3D11Texture2D * receivingTexture;
	//ID3D11ShaderResourceView * resourceView;
	
	HRESULT openResult = g_D3D11Device->OpenSharedResource(hShareHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &texturePointer);

	D3D11_SHADER_RESOURCE_VIEW_DESC rd;
	texturePointer->GetDesc(&rd);

	UnityLog(std::to_string(rd.Format).c_str());

	return true;
	

	/*
	if(spout.m_Senders.FindSenderName(senderName)) {
		UnityLog("Sender found with mSenders");
		
		
		DX9SharedTextureInfo info;
		if(spout.getSharedInfo(senderName, &info)) {
			UnityLog("got shared info !");
			// Get the size to check it

			w  = (unsigned int)info.width;
			h = (unsigned int)info.height;

			ID3D11Resource * tempResource11;
			ID3D11Texture2D * receivingTexture;
			ID3D11ShaderResourceView * resourceView;
			HRESULT openResult = g_D3D11Device->OpenSharedResource((HANDLE)info.shareHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
			g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &resourceView);
			

			UnityLog(std::to_string(info.format).c_str());
			texturePointer = resourceView;
			return true;
		}
		
	}

	*/
	
	/*
	// Currently Spout senders will be returning DX9 textures
	// Is there any way to detect where a DX9 or DX11 texture sharehandle is returned ??
	if(spout.ReceiveDXTexture(senderName, receivedWidth, receivedHeight, hShareHandle, wFormat)) {
		// Retrieved the sender info OK
		// This includes the sharehandle of the shared texture which we can use
		//
		// Todo - given the sharehandle returned, update the local texture
		//

		w = receivedWidth;
		h = receivedHeight;

		UnityLog("Texture well received !");
		UnityLog(std::to_string(receivedWidth).c_str());

		return true;
	}
	else {
		//	Returned false - sender not found or size changed
		//			1) width and height are returned zero for sender not found
		//			2) width and height are returned changed for sender size change
		if(receivedWidth != 0 &&  receivedHeight != 0) {

			// Width and height are changed, so the local texture has to be reset
			//
			//g_width  = width;
			//g_height = height;
			//
			// Todo - put texture update code here
			// 
			w = receivedWidth;
			h = receivedHeight;
			UnityLog("Sender size changed");
			return true;
		}

		UnityLog("Sender not found");
	}
	*/

}