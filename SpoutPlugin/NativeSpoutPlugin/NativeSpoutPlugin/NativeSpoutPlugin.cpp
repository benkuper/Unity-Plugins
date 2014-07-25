#define _CRT_SECURE_NO_WARNINGS

#include "UnityPluginInterface.h"
#include "pthread.h"

#include "spoutDirectX.h"
#include "spoutGLDXinterop.h"
#include "spoutSenderNames.h"


HWND hWnd;

spoutSenderNames sender;
spoutGLDXinterop interop;
spoutDirectX sdx;

ID3D11Device * d3d11;
ID3D11DeviceContext * context;

//sending
ID3D11Texture2D * sendingTexture; //todo : find a way to be able to share more than one texture
DXGI_FORMAT texFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
HANDLE sharedSendingHandle;

using namespace std;


typedef void (*SpoutStartPtr)(char * senderName,ID3D11ShaderResourceView * resourceView ,int width,int height);
typedef void (*SpoutStopPtr)(char * senderName);
SpoutStartPtr UnitySharingStarted;
SpoutStopPtr UnitySharingStopped;


extern "C"  void EXPORT_API SetSpoutHandlers( SpoutStartPtr sharingStartedHandler, SpoutStopPtr sharingStoppedHandler )
{
        UnitySharingStarted = sharingStartedHandler;
		UnitySharingStopped = sharingStoppedHandler;
}


extern "C" void EXPORT_API initDebugConsole()
{
	
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
	
}


// ************** UTILS ********************* //

HANDLE getSharedHandleForTexture(ID3D11Texture2D * texToShare)
{
	HANDLE sharedHandle;

	IDXGIResource* pOtherResource(NULL);
	texToShare->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );
	pOtherResource->GetSharedHandle(&sharedHandle);
	pOtherResource->Release();

	return sharedHandle;
}


BOOL CALLBACK EnumProc(HWND hwnd, LPARAM lParam)
	{
		DWORD windowID;
		GetWindowThreadProcessId(hwnd, &windowID);

		if (windowID == lParam)
		{
			printf("Found HWND !\n");
			hWnd = hwnd;

			return false;
		}

		return true;
	}

// INIT //

extern "C" bool EXPORT_API init()
{

	DWORD processID = GetCurrentProcessId();
	EnumWindows(EnumProc, processID);
	
	if(hWnd == NULL)
	{
		printf("SpoutNative :: HWND NULL\n");
		return false;
	}

	//sdx.g_pImmediateContext = g_pImmediateContext;

	/*
	d3d11 = sdx.CreateDX11device(hWnd);
	if(d3d11 == NULL)
	{
		printf("SpoutNative :: D3D11Device NULL\n");
		return false;
	}

	d3d11->GetImmediateContext(&context);
	*/

	return true;
}


// ************** SENDING ******************* //
extern "C" bool EXPORT_API createSender(char * senderName, ID3D11Texture2D * texturePointer)
{
	printf("SpoutNative :: create Sender %s\n",senderName);
	
	
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);

	/*
	td.BindFlags |=  D3D11_BIND_RENDER_TARGET;
	td.MiscFlags =  D3D11_RESOURCE_MISC_SHARED;


	td.Format = texFormat; //force format
	printf("texFormat %i %i\n",texFormat,td.Format);

	*/

	/*
	g_D3D11Device->CreateTexture2D(&td, NULL, &sendingTexture);
	sharedSendingHandle = getSharedHandleForTexture(sendingTexture);
	*/

	//sender.RegisterSenderName(senderName);
	bool senderResult = sender.CreateSender(senderName,td.Width,td.Height,sharedSendingHandle);//td.Format);
	printf(">> Create sender with sender names : %i\n",senderResult);	

	//sharedSendingHandle = NULL;
	bool texResult = sdx.CreateSharedDX11Texture(g_D3D11Device,td.Width,td.Height,texFormat,&sendingTexture,sharedSendingHandle);
	printf(">> Create shared Texture with SDX : %i\n",texResult);
	
	

	g_pImmediateContext->CopyResource(sendingTexture,texturePointer);
	g_pImmediateContext->Flush();

	printf("SendSharingHandle after DX11TexCreation : %i\n",sharedSendingHandle);
	sender.UpdateSender(senderName,td.Width,td.Height,sharedSendingHandle);
	/*
	// Get the description of the passed texture
	

	// Create a new shared texture with the same properties
	
	
	*/

	//bool result = spout.CreateSender(senderName,td.Width,td.Height,td.Format);

	//Loopback test
	/*
	ID3D11ShaderResourceView * resourceView;
	ID3D11Resource * tempResource11;
	g_D3D11Device->OpenSharedResource(sharedHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &resourceView);
	*/
	//UnitySharingStarted(0,resourceView,td.Width,td.Height);

	return texResult;
}

extern "C" bool EXPORT_API updateSender(char* senderName, ID3D11Texture2D * texturePointer)
{
	
	g_pImmediateContext->CopyResource(sendingTexture,texturePointer);
	g_pImmediateContext->Flush();
	
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);
	//printf("update texFormat %i %i\n",texFormat,td.Format);

	bool result = sender.UpdateSender(senderName,td.Width,td.Height,sharedSendingHandle);
	//printf("updateSender result : %i\n",result);
	
	return result;
}




extern "C" void EXPORT_API closeSender(char * senderName)
{
	printf("Close Sender : %s\n",senderName);
	sender.CloseSender(senderName);
	sender.ReleaseSenderName(senderName);
}


// *************** RECEIVING ************************ //

typedef void (*SpoutSenderUpdatePtr)(int numSenders);
SpoutSenderUpdatePtr UnitySenderUpdate;
typedef void (*SpoutSenderStartedPtr)(char * senderName,ID3D11ShaderResourceView * resourceView ,int width,int height );
SpoutSenderStartedPtr UnitySenderStarted;
typedef void (*SpoutSenderStoppedPtr)(char * senderName);
SpoutSenderStoppedPtr UnitySenderStopped;


extern "C" int EXPORT_API getNumSenders()
{
	return sender.GetSenderCount();
}


pthread_t receiveThread;
bool doReceive;
int lastSendersCount = 0;

void * receiveThreadLoop(void * data)
{
	//UnityLog("receive Thread Loop start !\n");
	printf("Unity Thread loop start !\n");

	
	char senderNames[32][256];

	unsigned int w;
	unsigned int h;
	HANDLE sHandle;


	while(doReceive)
	{
		
		int numSenders = sender.GetSenderCount();

		if(numSenders != lastSendersCount)
		{
			printf("Num Senders changed : %i\n",numSenders);
			
			UnitySenderUpdate(numSenders);

			char newNames[32][256];
			int i,j;
			bool found;
			
			//printf("\n\n################ SENDER UPDATE ###############\n\n");
			//printf("> Old senders : ");

			/*
			for(i=0;i<lastSendersCount;i++)
			{
				printf("%s | ",senderNames[i]);
			}
			*/

			
			//printf("\n");
			//printf("> New senders : ");
			for(i=0;i<numSenders;i++)
			{
				
				sender.GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);
				//spout.getSenderNameForIndex(i,newNames[i]);

				//printf("%s | ",newNames[i]);
			}

			//printf("\n");

			//NEW SENDERS DETECTION
			//printf("\n** Detecting new senders **\n");
			for(i=0;i<numSenders;i++)
			{
				//printf("Check for : %s  >>> ",newNames[i]);
				found = false;
				for(j = 0;j<lastSendersCount;j++)
				{
					//printf(" | %s ",senderNames[j]);
					if(!found && strcmp(newNames[i],senderNames[j]) == 0) 
					{
							found = true;
							printf("(found !) ");
					}
				}

				//printf("\nFound ? %i\n",found);

				if(!found) 
				{
					sender.GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);

					ID3D11Resource * tempResource11;
					ID3D11ShaderResourceView * rView;

					HRESULT openResult = g_D3D11Device->OpenSharedResource(sHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
					g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &rView);

					//UnitySharingStarted(newNames[i],rView,w,h);

					UnitySenderStarted(newNames[i],rView,w,h);
				}
			}
			
			//SENDER STOP DETECTION
			//printf("\n** Detecting leaving senders **\n");

			for(int i=0;i<lastSendersCount;i++)
			{
				found = false;
				//printf("Check for : %s  >>> ",senderNames[i]);
				for(j = 0;j<numSenders;j++)
				{
					//printf(" | %s  ",newNames[j]);
					if(!found && strcmp(senderNames[i],newNames[j]) == 0) 
					{
							found = true;
							//printf("(found !) ");
					}
				}

				//printf("\nFound ? %i\n",found);
				if(!found) UnitySenderStopped(senderNames[i]);
			}
			
			

			memcpy(senderNames,newNames,sizeof(newNames));
			
		}

		

		lastSendersCount = numSenders;
		
		Sleep(50);
	}
	

	
	return 0;
}

extern "C" void EXPORT_API stopReceiving()
{
	doReceive = false;
}

extern "C" bool EXPORT_API startReceiving(SpoutSenderUpdatePtr senderUpdateHandler,SpoutSenderStartedPtr senderStartedHandler,SpoutSenderStoppedPtr senderStoppedHandler) 
{
	printf("SpoutNative :: Start Receiving");
	//UnityLog("Start Receiving");
	UnitySenderUpdate = senderUpdateHandler;
	UnitySenderStarted = senderStartedHandler;
	UnitySenderStopped = senderStoppedHandler;
	

	if(doReceive)
	{
		pthread_join(receiveThread, NULL);
		doReceive = false;
	}

	doReceive = true;
	int ret = pthread_create(&receiveThread,NULL, receiveThreadLoop,NULL);

	lastSendersCount = 0;
	
	

	return true;//ret == 0; //success
}





