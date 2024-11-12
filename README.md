# Anywhen installation instructions
## Prerequisites
- Make sure you have git installed
## Installing the Anywhen package in Unity Package Manager
**Installing the Anywhen system**
- Open the Package Manager window (Window → Package Manager)
- Click the plus (+) icon in the top left corner
- Select “Add package from git URL….”
- Paste in this URL https://github.com/asger60/PackageAnywhen.git
- Click add (to the right of the address) and wait for it to install
**Installing the song packs**
- Again in the package manager, go through the ass process, and basic Anywhen songpack from here: https://github.com/asger60/AnywhenTrackPack1
- wait, it will take some time to install..

## AnywhenPlayer
The AnywhenPlayer is the main component for selecting and controlling playback of Anywhen songs. It has a built in song browser, and settings for handling triggering and transitions between songs, as well as customisation options for the current song.
Add a AnywhenPlayer to your scene:
- In the top menu, select “GameObject” → “Anywhen” → “Create Anywhen Player”
You should see an “AnywhenPlayer” gameobject in your scene.
### Selecting songs
Use the “Browse songs” button in the top right corner of the component, to find a song for the player. This player will now play that song whenever it receives a trigger signal
### Triggering the song playback
The player needs a trigger component in order to control playback. You can either make your own or use the one that comes with the package.
### Starting a song with the AnywhenTrigger
If the AnywhenPlayer does not have a reference to a trigger, a “Create trigger” button will be present in the inspector. Clicking this button creates an AnywhenTrigger component and link it to the player. It is also possible to create a AnywhenTrigger through the Unity “Add component” menu.
### Starting a song through code
To start a AnywhenPlayer through code simply call the AnywhenPlayer.Play() method.
### Controlling song transitions
There can never be more than one AnywhenPlayer playing at the same time. So whenever an AnywhenPlayer is started it will stop the current active player. The system can handle several different types of transitions between players.
- **Instant** This will stop the current player and start the new one instantaneously. Some notes from the previous player might play out on top of the new song.
- **Next Bar** The new player will wait until the next bar in the music before starting
- **Cross Fade** The new player will do an intensity fade in, while the old player does an intensity fade out. After the fade the old player stops. 
## AnywhenTrigger
This component will trigger playback of an AnywhenPlayer if the “TriggerType” condition is met. You can select trigger type from the dropdown. Some options require additional settings.
The AnywhenTrigger does not need to be on the same gameobject as the connected AnywhenPlayer.
### The music intensity parameter
The music can be manipulated in runtime by changing the music intensity value. This is a value from 0-1 where 0 is considered low intensity which generally will make the music be more subtle. This could for instance be mapped to the speed of a race car in a racing game, or the proximity to an enemy in an adventure game.
Note that the way music map to the general intensity is mapped per song, so some songs might react differently than others.
Controlling music intensity
The intensity of the playing music can be controlled either from custom code, or by using the AnywhenIntensitySetter component that comes with the package. 
## AnywhenIntensitySetter
The AnywhenIntensitySetter can be added to any gameobject. It uses the AnywhenTrigger for triggering.
There is two intensity update modes:
- **Set** In this mode the intensity value is hard set to the value specified in the inspector.
- **Modify** In this mode the value is modified by the value set in the inspector every time the trigger is triggered. The value can be both positive and negative.
### Setting intensity through code
Setting the music intensity through your own code is pretty easy. 
Just call: AnysongPlayerBrain.SetGlobalIntensity(_newIntensity_); - where “newIntensity” is your desired value
## Customising the music
Every AnywhenPlayer has a fold out section called “Customize”. Expanding this will give you access to two options.
- **Randomise instruments** This will basically randomise all sounds used in the song to give it a completely new vibe. The system will try to select sounds that fit with the ones that were before to keep a little bit of the structure from the original song. You can always use the restore button to return to the original song.
- **Root note** Here you can change the root note of the song. The whole song will be transposed to match the new root.
