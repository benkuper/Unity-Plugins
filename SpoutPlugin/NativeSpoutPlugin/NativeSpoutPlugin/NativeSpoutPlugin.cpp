#define _CRT_SECURE_NO_WARNINGS

#include "UnityPluginInterface.h"
//#include "pthread.h"

#include "spoutDirectX.h"
#include "spoutGLDXinterop.h"
#include "spoutSenderNames.h"

using namespace std;

HWND hWnd;

spoutSenderNames * sender;
spoutGLDXinterop * interop;
spoutDirectX * sdx;

ID3D11Device * d3d11;
ID3D11DeviceContext * context;

//sending
ID3D11Texture2D * sendingTexture; //todo : find a way to be able to share more than one texture
DXGI_FORMAT texFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
HANDLE sharedSendingHandle;

vector<ID3D11Texture2D *> activeTextures;
vector<HANDLE> activeHandles;
vector<string> activeNames;
int  numActiveSenders;

extern "C" void EXPORT_API initDebugConsole()
{
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
	
}


// ************** UTILS ********************* //
/*
HANDLE getSharedHandleForTexture(ID3D11Texture2D * texToShare)
{
	HANDLE sharedHandle;

	IDXGIResource* pOtherResource(NULL);
	texToShare->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );
	pOtherResource->GetSharedHandle(&sharedHandle);
	pOtherResource->Release();

	delete pOtherResource;

	return sharedHandle;
}
*/


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

	printf("!! Init !!\n");
	DWORD processID = GetCurrentProcessId();
	EnumWindows(EnumProc, processID);


	
	if(hWnd == NULL)
	{
		printf("SpoutNative :: HWND NULL\n");
		return false;
	}

	sender		= new spoutSenderNames;
	interop		= new spoutGLDXinterop;
	sdx			= new spoutDirectX;

	numActiveSenders = 0;

	return true;
}

int getIndexForSenderName(char * senderName)
{
	//printf("Get Index For Name %s  (%i active Senders) :\n",senderName,numActiveSenders);
	for(int i=0;i<numActiveSenders;i++)
	{
		//printf("\t> %s\n",activeNames[i].c_str());
		if(strcmp(senderName,activeNames[i].c_str()) == 0) return i;
	}
	//printf("\t....Not found\n");
	return -1;
}


// ************** SENDING ******************* //
extern "C" bool EXPORT_API createSender(char * senderName, ID3D11Texture2D * texturePointer)
{
	printf("SpoutNative :: create Sender %s\n",senderName);
	
	printf("Check TexturePointer : %i\n",texturePointer);
	if(texturePointer == nullptr) 
	{
		printf("## Texture Pointer null, create Sender fail");
		return false;
	}

	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);

	//sender.RegisterSenderName(senderName);
	bool senderResult = sender->CreateSender(senderName,td.Width,td.Height,sharedSendingHandle);//td.Format);
	printf(">> Create sender with sender names : %i\n",senderResult);	

	//sharedSendingHandle = NULL;
	bool texResult = sdx->CreateSharedDX11Texture(g_D3D11Device,td.Width,td.Height,texFormat,&sendingTexture,sharedSendingHandle);
	printf(">> Create shared Texture with SDX : %i\n",texResult);
	

	g_pImmediateContext->CopyResource(sendingTexture,texturePointer);
	g_pImmediateContext->Flush();


	printf("SendSharingHandle after DX11TexCreation : %i\n",sharedSendingHandle);
	sender->UpdateSender(senderName,td.Width,td.Height,sharedSendingHandle);

	string sName=  string(senderName);
	//printf("Registered at index : %i -> %s\n",numActiveSenders,sName.c_str());
	activeNames.push_back(sName);
	activeHandles.push_back(sharedSendingHandle);
	activeTextures.push_back(sendingTexture);
	numActiveSenders++;

	
	int senderIndex = getIndexForSenderName(senderName);
	//printf("Index search test > %i",senderIndex);

	return texResult;
}

extern "C" bool EXPORT_API updateSender(char* senderName, ID3D11Texture2D * texturePointer)
{
	int senderIndex = getIndexForSenderName(senderName);

	//printf("Sender index is %i\n",senderIndex);

	if(senderIndex == -1 || sendingTexture == nullptr)
	{
		printf("Sender is not known or badly created, creating one\n");
		createSender(senderName,texturePointer);
		return false;
	}

	ID3D11Texture2D * targetTex = activeTextures[senderIndex];
	HANDLE targetHandle = activeHandles[senderIndex];

	g_pImmediateContext->CopyResource(targetTex,texturePointer);
	g_pImmediateContext->Flush();
	
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);
	//printf("update texFormat %i %i\n",texFormat,td.Format);

	bool result = sender->UpdateSender(senderName,td.Width,td.Height,targetHandle);
	//printf("updateSender result : %i\n",result);
	
	return result;
}




extern "C" void EXPORT_API closeSender(char * senderName)
{
	int senderIndex = getIndexForSenderName(senderName);

	printf("Close Sender : %s\n",senderName);
	sender->CloseSender(senderName);
	sender->ReleaseSenderName(senderName);

	if(senderIndex != -1)
	{
		activeNames.erase(activeNames.begin()+senderIndex);
		activeHandles.erase(activeHandles.begin()+senderIndex);
		activeTextures.erase(activeTextures.begin()+senderIndex);
		numActiveSenders--;
	}

	printf("There are now %i senders remaining\n",numActiveSenders,activeNames.size());
	
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
	printf("Get Num Senders\n");
	return sender->GetSenderCount();
}



int lastSendersCount = 0;

char (*senderNames)[256];
char (*newNames)[256];

unsigned int w;
unsigned int h;
HANDLE sHandle;

extern "C" void EXPORT_API checkReceivers()
{

	if(sender == nullptr) return;
	

	int numSenders = sender->GetSenderCount();
	

	if(numSenders != lastSendersCount)
	{
		printf("Num Senders changed : %i\n",numSenders);
			
		UnitySenderUpdate(numSenders);

		int i,j;
		bool found;
		
		printf("Old Sender List :\n");
		for(i=0;i<lastSendersCount;i++)
		{
			printf("\t> %s\n",senderNames[i]);
		}
			
		printf("\nUpdated Sender List :\n");
		for(i=0;i<numSenders;i++)
		{
			sender->GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);
			printf("\t> %s\n",newNames[i]);
		}

		//NEW SENDERS DETECTION
		printf("\nNew Sender Detection, checking against previous sender list :\n");
		for(i=0;i<numSenders;i++)
		{
			printf("\t> %s .... ",newNames[i]);

			found = false;
			for(j = 0;j<lastSendersCount;j++)
			{
				if(!found && strcmp(newNames[i],senderNames[j]) == 0) 
				{
						found = true;
						printf("found in previous list.\n");
						break;
				}
			}

			

			if(!found) 
			{
				printf("not found [New]\n");

				sender->GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);

				ID3D11Resource * tempResource11;
				ID3D11ShaderResourceView * rView;

				HRESULT openResult = g_D3D11Device->OpenSharedResource(sHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
				g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &rView);

				printf("\t => Send Started Event with name : %s\n",newNames[i]);
				UnitySenderStarted(newNames[i],rView,w,h);
			}
		}
			
		//SENDER STOP DETECTION
		for(int i=0;i<lastSendersCount;i++)
		{
			found = false;
			for(j = 0;j<numSenders;j++)
			{
				if(!found && strcmp(senderNames[i],newNames[j]) == 0) 
				{
						found = true;
				}
			}


			if(!found) 
			{
				printf("Send Stopped Event with name : %s\n",senderNames[i]);
				UnitySenderStopped(senderNames[i]);
			}
		}

		for(int i=0;i<numSenders;i++)
		{
			memcpy(senderNames[i],newNames[i],sizeof(newNames[i]));
		}
	}

	lastSendersCount = numSenders;
}


extern "C" bool EXPORT_API startReceiving(SpoutSenderUpdatePtr senderUpdateHandler,SpoutSenderStartedPtr senderStartedHandler,SpoutSenderStoppedPtr senderStoppedHandler) 
{
	printf("SpoutNative :: Start Receiving\n");

	//UnityLog("Start Receiving");
	UnitySenderUpdate = senderUpdateHandler;
	UnitySenderStarted = senderStartedHandler;
	UnitySenderStopped = senderStoppedHandler;
	
	lastSendersCount = 0;

	senderNames = new char[32][256];
	newNames = new char[32][256];

	return true;//ret == 0; //success
}

bool isCleaned = false;

extern "C" void EXPORT_API clean()
{
	printf("*** clean, already cleaned ? %i ***\n",isCleaned);
	
	if(isCleaned) return;
	
	
	delete[] senderNames;
	delete[] newNames;
	

	delete d3d11;
	delete context;
	
	
	delete sender;
	delete sdx;
	delete interop;


	FreeConsole();

	isCleaned = true;
}




