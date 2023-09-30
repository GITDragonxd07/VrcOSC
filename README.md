
# VrcOSC

A free tool for spotify integration into the VRChat OSC system

## About
The VRChat OSC system allows 3rd party programs to send data to the game.

This program uses OSC along with a Spotify Api to send the currently playing song and some other information to the game
## How to install

To install this program, please follow the steps below

```
- Create a spotify application at https://developer.spotify.com/dashboard

- Download the entire program from github

- Place this progtam in a seperate folder

- Run VrcOSCWorking.exe

- Open the settings.json file and paste in your spotify Client ID and Secret

- Make sure all the settings match up to your spotify application (USEBROWSERTOOL is not part of spotify, read about it below)

- Launch VrcOSCWorking.exe

- Login to spotify and accept the application permissions

- Enjoy!
```
## What is BrowseTool (BT)?

BrowseTool is a special brower made by me to allow for easier spotify authentication

You are not required to use BrowseTool when using VrcOSC

Read more about BrowseTool here 
    https://github.com/GITDragonxd07/BrowseTool
```
What if I disable BrowseTool?

    If you do not have BrowseTool enabled, your default brower will be used for authentication

Whats the benifit of using BrowseTool?

    The brower window is opened in the background and closed after authentication completes

How do I clear BrowseTool cashe?

    Launch BrowseTool with these arguments: ["https://www.google.com" true "BT-clear"]
```
