# CameraPlus
CameraPlus is a Beat Saber mod that allows for multiple wide FOV cameras with smoothed movement, which makes for a much more pleasant overall spectator experience.

ModAssistant has released [Camera2](https://github.com/kinsi55/CS_BeatSaber_Camera2), which is newly designed and lighter.  
Therefore, CameraPlus is no longer registered in ModAssistant.  
This is the version where I will add the features I want without permission.  

# Supported game versions
BeatSaber 1.39.0  

# Requirements
CameraPlus is an external plugin.  
You need BSIPA to load the plug-in, so install BSIPA first.  
Please install BSIPA using ModAssistant or obtain it from the following page.  
Please note that it may not be supported when updating the game version.  
[BSIPA Link](https://bsmg.github.io/BeatSaber-IPA-Reloaded/)

# Latest version Download
The latest version can be downloaded from the following.  
[Release Page](https://github.com/Snow1226/CameraPlus/releases)
### To install manually:
As a prerequisite, please install BSIPA from ModAssistant.　　

	1. Make sure that Beat Saber is not running.
	2. Extract the contents of the zip into Beat Saber's installation folder.
		For Oculus Home: \Oculus Apps\Software\hyperbolic-magnetism-beat-saber\
		For Steam: \steamapps\common\Beat Saber\
		(The folder that contains Beat Saber.exe)
	3. Done! You've installed the CameraPlus Plugin.

### When using CameraPlus, "SmoothCamera" is disabled in the base game.
The latest version will automatically force SmoothCamera to be turned off, ignoring the game's settings.

# Usage
To edit the settings of any camera in real time, right click on the Beat Saber game window! A context menu will appear with options specific to the camera that you right clicked on!

Press <kbd>F1</kbd> to toggle the main camera between first and third person.

# Doesn't it work?
- Do you have BSIPA in your system?  
  Check read reuirements.  
- Camera settings are corrupted, or unrelated files are mixed in with the CameraPlus settings file.  
  ```\Beat Saber\UserData\CameraPlus```  

  Do not put anything other than the Camera Plus configuration file in the above.  
  ```\Beat Saber\UserData\CameraPlus\Scripts```  
  MovementScript is put all above. Do not put it in the profile folder or any other folder.  

  Please try moving UserData/CameraPlus to a different location before contacting us.  
  If it starts normally, there are corrupted/unrelated files mixed in.
  
# Wiki
[CameraPlus wiki](https://github.com/Snow1226/CameraPlus/wiki)

# Configuration file description
## UserData/CameraPlus.json
[CameraPlus.json in wiki](https://github.com/Snow1226/CameraPlus/wiki/Configuration-file-description-CameraPlus.json)

## CameraConfig
[CameraConfig in wiki](https://github.com/Snow1226/CameraPlus/wiki/Configuration-file-description-*.json)

## Movement Script
[Movement Script in wiki](https://github.com/Snow1226/CameraPlus/wiki/MovementScript)  
### Tools to support the creation of MovementScript  
[ScriptMapper(https://github.com/hibit-at/Scriptmapper)](https://github.com/hibit-at/Scriptmapper)  

## Convert to Camera2 Setting
The setting conversion with Camera2 Mod is tentatively implemented.  
Please note the version at that time as the configuration file may change due to mutual updates.  
[Convert to Camera2 Setting](https://github.com/Snow1226/CameraPlus/wiki/Convert-to-Camera2-Setting)

## If you need help, ask us at the Beat Saber Mod Group Discord Server:  
https://discord.gg/BeatSaberMods

## For developers

### Contributing to CameraPlus
In order to build this project, please create the file `CameraPlus.csproj.user` and add your Beat Saber directory path to it in the project directory.
This file should not be uploaded to GitHub and is in the .gitignore.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Set "YOUR OWN" Beat Saber folder here to resolve most of the dependency paths! -->
    <BeatSaberDir>E:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

If you plan on adding any new dependencies which are located in the Beat Saber directory, it would be nice if you edited the paths to use `$(BeatSaberDir)` in `CameraPlus.csproj`

```xml
...
<Reference Include="BS_Utils">
  <HintPath>$(BeatSaberDir)\Plugins\BS_Utils.dll</HintPath>
</Reference>
<Reference Include="IPA.Loader">
  <HintPath>$(BeatSaberDir)\Beat Saber_Data\Managed\IPA.Loader.dll</HintPath>
</Reference>
...
```
### VMCAvatar-BS Mod is required to build with full functionality.  
[VMCAvatar-BS](https://github.com/nagatsuki/VMCAvatar-BS)
Full builds currently require the Pre-release version.
[https://github.com/nagatsuki/VMCAvatar-BS/releases/tag/v1.0.0-dev2](https://github.com/nagatsuki/VMCAvatar-BS/releases/tag/v1.0.0-dev2)
  
### Modify build-in shader
To create customshader, you need to open the UnityProject folder from UnityEditor and create an AssetBundle.  
This shader is Unity built-in shader modified by [Reiya1013](https://github.com/Reiya1013).

### I borrowed the following shaders for Chroma Key.  
[hecomi/uChromaKey](https://github.com/hecomi/uChromaKey)
