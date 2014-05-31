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
//
extern "C" int EXPORT_API shareDX11(ID3D11Texture2D * texturePointer)
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

		createSenderFromSharedHandle(sharedHandle,td);
	}
	else {
		//UnityLog("error in shareDX11");
	}

	return 2;
}
