## Spout Plugin
This plugin allows Unity to receive and share textures from and to external softwares like Resolume, Adobe AIR, After Effects...
It has been updated to fully supports the Spout2SDK available here : https://github.com/leadedge/Spout2

### Installation and usage
Just put the "Spout" script folder inside your Asset folder, and the "NativeSpoutPlugin.dll" in the Assets/Plugins folder.
Next thing is drop a "Spout2Sender" or "Spout2Receiver" onto objects.
You can look at those files to see how the texture is retrieved and do your custom behaviors.

### Notes
Unity is a 32-bit editor, so to test inside the editor your must use the 32-bit dll.
If you wish to export a 64bit application, you must first export from Unity and then copy the NativeSpoutPlugin.dll from bin/x64 into your [projectName]_data/Assets/Plugins folder.


This project is done on my spare time, if you feel like it you can donate whatever you want !
<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=bkuperberg%40hotmail%2ecom&lc=US&item_name=Ben%20Kuper&item_number=open_paypal_donate&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"><img src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif" /></a>
