# Limitations / Known Issues
* The default skeleton/armature generated is almost unusable for animation 
	To my understanding this is a limitation of the file format I choose (`glTF`) combined with unity IBM's lacking critical information for skeletal reconstruction.
	
	Refer to [This](#replacing-the-skeleton-with-a-usable-one) for instructions on replacing the skeleton with a more usable one in Blender
	(I'm sorry for anyone who uses other 3d programs but the information might still work with some adaptations);
	that is the best solution I could come up with if anybody has a better idea that works please contact me, I'm in the Rumble Modding discord `@blankochan`

* My face and hair is red!!!
	This is because by default some 3d programs automatically render vertex color, which is used by rumble for cutting out your hair in first person and eye rendering, and I include this information incase it ever becomes handy to someone.
	
	A quick fix would be in your program of choice to just disable vertex color.

* The model is flipped
	as far as I can tell this is a quirk of how unity renders/stores the model, its a very easy fix, just set the rig's X axis scale to -1, I am looking into automatically flipping it for you but this mods been delayed enough I'd rather get a buggy version out than delay another 3 months


# Replacing the skeleton with a usable one
### Some quick notes about this section
I only have access to blender so I can only provide blender specific instructions but this should be possible in other software.

And you're not locked into using the rig I provide (if your software of choice allows for skeletal replacements), in theory you can use any rig as long as its bone names match the ones in the original GLB file

Importing blender animations back into unity is untested but if you do try it please send me a photo of your results `@blankochan` on discord
### Replacing the skeleton
Using blender you can use [this file](https://github.com/blankochan/glTF-Exporter/raw/refs/heads/main/Tutorial/rig.blend) to import a manually reconstructed rig

<img alt="Video Tutorial" src="https://github.com/blankochan/glTF-Exporter/raw/refs/heads/main/Tutorial/exporter.webp">

#### Text Instructions
Assuming you already have a model imported you can
1. Delete all the bones in the original rig 
	select the rig thats a bunch of Icosphere's or the one that looks like an eldritch horror and press tab
	I want you to delete the bones in edit mode not the actual `Armature` object

2. Import the new skeleton
	You can do this by just dragging and dropping `rig.blend` from the link above or [this link](https://github.com/blankochan/glTF-Exporter/raw/refs/heads/main/Tutorial/rig.blend)
	And appending `Rig` from the `Armature` folder

3. Select "`Rig`" (from the blend file you just imported)

4. Select the "`Armature`" that's inside "`CC_Result`" (ctrl click)

5. And Press `Ctrl` + `J` to Combine the new rigs
#### And you're done, small issue that you may notice is; the model is mirrored you can fix this by scaling the X Axis on the `Armature` or the entire object to -1