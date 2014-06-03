#define _CRT_SECURE_NO_WARNINGS

#include "UnityPluginInterface.h"
#include "SpoutHelpers.h"

using namespace std;


//#####################
// WYPHON FUNCTIONS
//#####################

//--------------------------------------------------------------------------------------
// Wyphon Callback functions
//--------------------------------------------------------------------------------------
typedef void (*SpoutStartPtr)(int sendingParnterId,ID3D11ShaderResourceView * resourceView ,int width,int height);
typedef void (*SpoutStopPtr)(int sendingPartnerId);
SpoutStartPtr UnitySharingStarted;
SpoutStopPtr UnitySharingStopped;

extern "C"  void EXPORT_API SetSpoutHandlers( SpoutStartPtr sharingStartedHandler, SpoutStopPtr sharingStoppedHandler )
{
        UnitySharingStarted = sharingStartedHandler;
		UnitySharingStopped = sharingStoppedHandler;
}


//
// Spout
//
//	This creates a shared texture with the same size, format etc of the passed texture
//	The info of this texture is saved inshared memory so that
//	receivers can access the shared texture by way of the share handle
//	Once this shared memory is set up, all that is necessary is that the shared texture is updated
//	If the size of the texture changes - then the shared memory needs to be updated.

HANDLE getSharedHandleForTexture(ID3D11Texture2D * texToShare)
{
	HANDLE sharedHandle;

	IDXGIResource* pOtherResource(NULL);
	g_pSharedTexture->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );
	pOtherResource->GetSharedHandle(&sharedHandle);
	pOtherResource->Release();

	return sharedHandle;
}



extern "C" int EXPORT_API shareDX11(char * senderName, ID3D11Texture2D * texturePointer)
{
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);


	// Get the description of the passed texture
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);
	td.BindFlags |=  D3D11_BIND_RENDER_TARGET;
	td.MiscFlags =  D3D11_RESOURCE_MISC_SHARED;
	//td.Format = DXGI_FORMAT_R8G8B8A8_UNORM;

	// Create a new shared texture with the same properties
	g_D3D11Device->CreateTexture2D(&td, NULL, &g_pSharedTexture);


	HANDLE sharedHandle = getSharedHandleForTexture(g_pSharedTexture);
	createSenderFromSharedHandle(senderName, sharedHandle,td);
	
	//Loopback test
	ID3D11ShaderResourceView * resourceView;
	ID3D11Resource * tempResource11;
	g_D3D11Device->OpenSharedResource(sharedHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &resourceView);
	UnitySharingStarted(0,resourceView,td.Width,td.Height);

	return 2;
}




extern "C" void EXPORT_API stopSharing(char * senderName)
{
	spout.CloseSender(senderName);
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
extern "C" bool EXPORT_API receiveDX11(char * senderName)
{
	
	//UnityLog("Receive Texture !");

	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);

	

	DWORD wFormat = 0;
	HANDLE hShareHandle = NULL;

	spoutSenders senders;	
	unsigned int w = 0;
	unsigned int h = 0;

	bool result = false;
	//senders.GetFirstSenderName(0,senderName);
	
	printf("Sender name ? %s\n",senderName);

	result = spout.ReceiveActiveDXTexture(senderName,w,h,hShareHandle,wFormat);
	//result = spout.ReceiveDXTexture(senderName,w,h,hShareHandle,wFormat);

	printf("Get sender name (%s) info result : %i\n",senderName, result);

	if(!result)
	{
		printf("No sender info, stopping here\n");
		return false;
	}

	printf("There is a sender : %s ! Texture width / height /handle : %i %i %p  format = %i\n",senderName,w,h,hShareHandle,wFormat);

	//if(wFormat == 0) return false;
	
	ID3D11Resource * tempResource11;
	ID3D11ShaderResourceView * rView;

	HRESULT openResult = g_D3D11Device->OpenSharedResource(hShareHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &rView);

	UnitySharingStarted(0,rView,w,h);


	return true;
}



