# AutoPOE - Simulacrum Farming Bot for Exile API

This is an updated (but far from perfect) version of my simualcrum farming bot. 
Based on initial feedback on the Chieftain version, the main changes is that this bot is much more stable and configurable. 

You can define which skills should be used, when and how (including targeting mercenaries for link skills, only casting if buff is not present on player, only on enemy rarities, etc). 
The general logic flow is easier to read (but still not great, I just kinda slapped it together and tested overnight) and modify to add new functionality and debug failure conditions. 

There is also now support to automatically apply incubators to your gear which is a pretty big boost to profit/hour especially in earlier league sitautions. 


# Setup & Skill Usage

Once the plugin is installed and showing up in your ExileAPI, **you need to configure skill usage**.   
At least 1 skill must be defined as a movement skill. These will be selected at random and used to try to move around the map  
**Please ensure you have at least 1 blink skill such as frostblink/flame dash to help the bot get unstuck on corners of terrain**  

Movement skills should be set to 'Do not Use' unless you also want them to be used to attack enemy monsters. 

For skills like righteous fire, you can set the buff name. The skill will NOT be re-cast as long as that buff is active on player, regardless of what internal cooldown you've defined. 
The same could be done for things like mercenary link skills where you set the cast frequency very low but don't have it re-cast as long as a buff is already active. 

Minimum delay is just that. A minimum time between casting the skill. This is great for things like curse skills that COULD be spammed repeatedly but really should onlybe cast every X seconds.


# How To use

Make sure you have your dump tab selected in your stash (this is also where incubators should be stored)
Start in your hideout with simulacrums added to the map device storage.
Press the bot start key (default insert)
