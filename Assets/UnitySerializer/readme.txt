UnitySerializer by Mike Talbot - 2012
http://whydoidoit.com/unity-serializer-v0-2/



Installation
_____________

Install the package and you will find:

    Plugins\Editor\whydoidoit.com       > Editor extensions
    Plugins\whydoidoit.com		 		> Libraries for the serialization and support functions
    Plugins\whydoidoit.com\AddOns		> Packages for iTween and PlayMaker
    
	
	Standard Assets 					> Just stuff for the demo scene
	UnitySerializer						> Project directory for the demonstration
	
		Example 				> The example scene
		[Materials]				> Supporting materials
		[Prefabs]				> Demo cube prefab
		readme					> This file
		Getting Started Guide	> PDF instructions for setting up the system
		Room Management         > PDF instructions for the room management system
		[Scripts]				> Demo scripts
			ChangeColor			> Demo of saving a private variable to change the color of a cube
			Movement			> Basic movement for the camera in the scene
			Spawner				> Randomly creates cubes
			TestSerialization	> Example (simple) UI for saving and loading games
			CreateTexture		> Example dynamic texture generation
			PauseMenu.js		> Example pause menu written in JavaScript
			



Playmaker & iTween Support
__________________________

Please visit the web site for instructions and to download the add-on packages





			
Features
________

Unity Serializer will save the positions of game objects, their parent child relationships and all of their public fields and properties (including complex classes of your own creation).

It will also store the state of all of the animations running on your object, the physics of the rigidbody and a host of other Unity components.

It will store all of your game object or component references, even if those are stored deep inside your own classes.

Using Radical routines to start Coroutines will also ensure that all of your coroutines recommence from where they left off with local variables intact.
(See the website for more details)





			
Setting up a game for Unity Serializer
______________________________________

You will find all of the scripts for Unity Serializer under the Components>Storage menu

Save Game Manager - You need one of these attached to a scene component in every scene in your game.


PrefabIdentifier - stores information for an object that will be created at run time

StoreInformation - stored information for an object that is in the scene already

UniqueIdentifier - stores a reference to an object so it can be found later






Using the Unity Serializer Wizard
_________________________________

You can show the wizard window from the Unity Serializer menu on the main menu bar, or by pressing Ctrl(Cmd) Alt 0.  The wizard
allows you to quickly setup a scene for your save game feature.  You can set an item, or the item and all of its children to be stored 
at the click of a button - you can also add a Save Game Manager, capture your stored objects and delete old save game data in seconds.

Unity Serializer now also shows you which items are stored in the hierarchy view and gives you short cut menus to quickly add one of the scripts,
using the keyboard.

You can still just highlight the items you want and add the script directly from the Components>Storage menu.






Basic Steps
___________

* Create an empty game object in each scene and attach a Save Game Manager to it

* Add a PrefabIdentifier to anything that will be created at runtime

* Add a StoreInformation to anything whose values, position or existence you wish to store that is NOT marked with a PrefabIdentifier

* If your prefabs are created within another GameObject then that GameObject MUST have one of the scripts attached

* Any GameObject that will be referenced by another MUST have a script attached

* You must run the scene once to capture the initial elements to the Save Game Manager and then save the scene.

* You can make as many references to your own objects as you like – these will be re-established on deserialization.

If you have code that shouldn’t run when the level reloads (probably because all of the intialization is over and the stored state is what you want) then skip you initialization when LevelSerializer.IsDeserializing == true by adding:

 	if(LevelSerializer.IsDeserializing) {
		return;
	}
	





Saving A Game
_____________

If you want to save games for different players you can change LevelSerializer.PlayerName at some point early in play.  Leave it blank if you don't care.

	LevelSerializer.PlayerName = "Mike";
	
Saved games are keyed off player name, so Mike and Tim will see different saved games.	

You can save a game in a number of ways:

* Call LevelSerializer.SaveGame("GameNameHere"); and a new entry will made for the current player in PlayerPrefs.

* Call LevelSerializer.Checkpoint(); and a special check point will be added for the current player - this can be resumed with LevelSerializer.Resume();

* Call LevelSerializer.SerializeLevel(); which returns a string that you can save anywhere and load using LevelSerializer.LoadLevel(theStringYouGotBack);






Loading A Game
______________

You can get the list of saved games for a player by calling LevelSerializer.SavedGames[aPlayerNameHere] which returns you a list of games that you can iterate through.

Calling .Load() on one of the entries causes that game to be reloaded.  Each entry has a .Caption() which is a nice name to display about the saved information.

If you have resume information (which you can check with LevelSerializer.CanResume) you can call LevelSerializer.Resume() to resume for the current player.

If you saved data using LevelSerializer.SerializeLevel() you can reload it using LevelSerializer.LoadLevel()



			
									
					
User Interface
______________

I provide a simple example of using LevelSerializer to produce save game info in UnitySerializer>Scripts>Test Serialization. Please don't use this in your game, you should find it pretty easy to understand though.	
	
	
	
	
	
Controlling what is saved
_________________________

If you import the Serialization namespace:

     JS
			import Serialization;
			
	 C# 
			using Serialization;

You can decorate any variable with a [DoNotSerialize] attribute to say that its value should not be saved:

     JS
			@DoNotSerialize
			var myObject : MyClass;
     C#
			[DoNotSerialize]
			public MyClass myObject;
			
You can also specify that all private variables should be stored using the SerializeAll attribute:

    JS
			@script SerializeAll
			
	C#
			[SerializeAll]
			public MyScript : MonoBehaviour 

You can also use SerializeThis to make a single private member be stored

StoreInformation and PrefabIdentifier both have custom inspectors that let you choose which components will be saved.  By default all components are serialized.








Example scene
_____________
	
The example scene demonstrates just a few examples of serializing a game.  You can find more information online at whydoidoit.com.

A FPC moves the camera around and a number of random boxes with random colours are spawned.  You can save the game by clicking on the Save button in the top left corner of the screen.  You will see that saving the game causes the list of the last saved games to appear beneath it.  Clicking on one of the buttons will reload the level from that point.

The ChangeColor script allocates a random colour to a private variable which then gets increased towards white.  This is there so you can see how a SerializeThis attribute can be used to serialize private members of classes and scripts.  By default only public members are saved.

When you reload the cubes will continue from where they left off, with the right colors and rigidbody dynamics.

* The "Capsule" has a Store Information script on it, so its position is restored on load

* The "Manager" game object is an empty one with the "Scripts>Test Serialization" script attached for the basic test UI and the all important "Storage>Save Game Manager" script which stores all of the objects in the scene that need saving.

* The Cube prefab "UnitySerializer>Prefabs>Cube" has a "Storage>Prefab Identifier" attached to it so that it saves all of its data and can be created at runtime.

* The Cube also has a ChangeColor script attached to show very simple serialization of custom scfripts.

		
A more complex example is available on the web site.
			




****************************************************************************
Unity Serializer
Licensing Terms:  MIT LICENSE

http://www.whydoidoit.com
Copyright (C) 2012 Mike Talbot

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
