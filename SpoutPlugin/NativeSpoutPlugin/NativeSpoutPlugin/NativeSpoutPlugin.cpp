#include "UnityPluginInterface.h"
#include <string>

using namespace std;

extern "C" int EXPORT_API shareTexture(void* texturePtr)
{
	DebugLog("[SpoutNative] Share texture !");
	// A script calls this at initialization time; just remember the texture pointer here.
	// Will update texture pixels each frame from the plugin rendering event (texture update
	// needs to happen on the rendering thread).
	ID3D11Texture2D * tex = (ID3D11Texture2D *)texturePtr;
	D3D11_TEXTURE2D_DESC desc;
	tex->GetDesc(&desc);

	return desc.Format;

	//string s = std::to_string(desc->Width);
	//DebugLog(s.c_str());


}