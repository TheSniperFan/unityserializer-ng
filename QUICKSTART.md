## Using unityserializer-ng

### Installation
Install US-NG by importing it as a package, or by dragging the folder into the project window. Unity should import all of the assets.

### Basic Usage
US-NG has a configuration window under `Window/Unity Serialiser/Wizard`. Dock that or leave it floating. Once you've done that, it'll ask you to create a 'Save Game Manager'. Do so by clicking on the create button.
Once done, select a GameObject from your Hierarchy. To store variables and other data (such as position), click on 'Store Information'. You can also save the material or mesh by clicking on the respective buttons afterwards.
You can now implement an example script or write your own.

### Quick Start
Go to the UnitySerializerExamples folder, and go to Scripts. Add the 'PauseMenu' script to a GameObject (use your camera for now). Enter play mode. When you press P, it'll show a save/load menu. This will allow you to save or load a game.
As long as you have followed the instructions above, you can adjust anything (position, rotation, scale) on the cube or give it any variable, and save the game.
Once you have done that, restart play mode. You should notice that your cube has returned to normal, as usual. Press P again, and click on the new menu option. You should notice that it has restored your parameters from before.
If it does not, please make sure you have **followed all the instructions up to here**. This tutorial was followed as it was being written and the author had no issue with the steps.

### Writing your own save/load script
If you need more functionality than the example scripts provide, this is a basic list of commands you can execute to intergrate unityserializer-ng functionality into your own scripts.

`LevelSerializer.SaveGame(Name of save file);` - Save your game.

`LevelSerializer.LoadNow(Data);` - Load a save file. Requests savefile data, you will have to use a foreach with the type LevelSerializer.SaveEntry.

`LevelSerializer.SavedGames[LevelSerializer.PlayerName]` - Returns a list of saved games. Best used in a foreach loop.

`LevelSerializer.IsDeserializing` - Returns true when a level is loading. Best used when waiting for a level to finish loading, so that a function can continue.

### Additional Notes
It's been a while since I [(lobby417)](https://github.com/lobby417) have used this, so excuse any problems. If requested, I will add more detail to this tutorial, including more advanced functionality.
