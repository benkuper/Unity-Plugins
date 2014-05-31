#include <Windows.h>

// SPOUT
// #include "spoutInterop.h"

// Other spout files might be needed due to dependency of spoutInterop
// but the functions for DirectX initialization etc. will not be used.
// A single sender or receiver is assumed. Support for multiple senders / receivers not yet established.

// Spout shared texture info structure used for communication
// between a sender and receiver


#include "spoutSenders.h"	// Spout sender management support
#include "spoutMap.h"		// memory map creation and management
#include "spoutInterop.h"

// SPOUT
bool					bInitialized = false;
char					g_SenderName[256];	// Spout sender and shared memory name
HANDLE					g_shareHandle;		// Global texture share handle
unsigned int			g_width;			// Global texture width
unsigned int			g_height;			// Global texture height
SharedTextureInfo		g_info;				// Global texture info structre
spoutInterop			spout;				// Spout setup functions


// ############# SPOUT FUNCTIONS #################
extern "C"  int EXPORT_API initSpout()
{
	UnityLog("initSpout !");
	// Not needed
	return 1;
}


extern "C" void EXPORT_API SpoutCleanup()
{
	UnityLog("clean Spout !");

	spout.CloseSender(g_SenderName);
	bInitialized = false; // prevent access to sender info in update

	if( g_pImmediateContext ) g_pImmediateContext->ClearState();
	if( g_pImmediateContext ) g_pImmediateContext->Release();
	
	if(g_D3D9Device) g_D3D9Device->Release();
	if(g_pDeviceD3D9ex) g_pDeviceD3D9ex->Release();

}



extern "C" void createSenderFromSharedHandle(HANDLE sharedHandle, D3D11_TEXTURE2D_DESC td)
{
	// 1) Set global variables to check for texture changes
		g_shareHandle	= sharedHandle;
		g_width			= td.Width;
		g_height		= td.Height;

		// 2) Set up the texture info structure
		g_info.shareHandle		= (unsigned __int32)sharedHandle; // used
		g_info.width			= (unsigned __int32)g_width; // used
		g_info.height			= (unsigned __int32)g_height; // used
		g_info.format			= (DWORD)td.Format; // can be used for DX11 - but needs work
		g_info.usage			= (DWORD)td.Usage; // unused

		// 3) Create a sender name
		strcpy_s(g_SenderName, 256, "Unity");	// Sender name 

		// 4) Create a Spout sender
		spout.CreateSender(g_SenderName,(DX9SharedTextureInfo *)&g_info);

		bInitialized = true; // flag that a sender has been created for update
}

extern "C" void EXPORT_API updateTexture(ID3D11Texture2D * texturePointer)
{
	
	if(g_pImmediateContext == NULL) UnityLog("Immediate context is null");
	if((ID3D11Resource *)texturePointer == NULL) UnityLog("Resource is null");

	//UnityLog("Update in plugin");
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);

	// SPOUT
	// Check the texture against the global size used when the sender was created
	// and update the sender info if it has changed
	if(bInitialized) {
		if(td.Width != g_width || td.Height != g_height) {
			g_width			= td.Width;
			g_height		= td.Height;
			g_info.width	= (unsigned __int32)g_width;
			g_info.height	= (unsigned __int32)g_height;
			// This assumes that the sharehandle in the info structure remains 
			// the same as the texture this could be checked as well
			spout.UpdateSender(g_SenderName, (DX9SharedTextureInfo *)&g_info);
		}
	}


	//make it a shared texture, so add a flag
	/*
	td.MiscFlags |= D3D11_RESOURCE_MISC_SHARED;
	td.MipLevels = 1;
	td.Usage = D3D11_USAGE_DEFAULT;
	td.ArraySize = 1;

	// Note to Frederik : Why 0 ? From Unity, BindFlags is set to 8 (SHADER_RESOURCE)
	td.BindFlags = 0; //D3D11_BIND_RENDER_TARGET;
	td.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	*/
	g_pImmediateContext->CopyResource(g_pSharedTexture,texturePointer);
	g_pImmediateContext->Flush();
}