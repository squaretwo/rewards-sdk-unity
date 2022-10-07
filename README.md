# Squaretwo Rewards SDK for Unity

rewards-sdk-unity is a plugin for Unity 2019.4 and greater that enables Unity games to integrate real world rewards for their games.
Currently, only iOS is suppported, with Android support slated for the future.

## Rewards Example Scene

A minimalistic example scene is placed under `Packages/co.squaretwo.rewardssdk/Assets/Scenes/`. You can open and run it to see how your code can interact with the Rewards SDK.

## Package Manager

This plugin can be imported using the Package Manager, by adding the following entry in your `Packages/manifest.json`:

```json
{
  "dependencies": {
    ...
    "co.squaretwo.rewards-sdk-unity": "https://github.com/squaretwo/rewards-sdk-unity.git",
    ...
  }
}
```

## Rewards Prefab

For a quick start with a prebuilt rewards canvas, you will find a prefab under `Packages/co.squaretwo.rewards-sdk-unity/Assets/Prefabs/`. Drag and drop this into your desired scene. Click on the prefab instance, and provide the following in the inspector: 

`Vendor Id` - This will be the vendor identifier provided to you by Squaretwo

`App Id` - The ID for the current app you are building (found in the admin panel)

`On Click Inbox Button` - Add any callbacks here if your game needs to be notified that the inbox button was clicked, displaying the inbox.

`On Click Tokens Button` - Add any callbacks here if your game needs to be notified that the tokens button was clicked. This button has no default behavior.

`On Click Tickets Button` - Add any callbacks here if your game needs to be notified that the tickets button was clicked, displaying the redemption catalog.

`On Initialize Failure` - If there is an issue with SDK initialization, this callback will be called with an error. Use this for handling any failure state or retry logic.

`On Initialize Success` - Add any callbacks here if your game needs to be notified when the SDK initializes successfully.

On game start, this prefab will auto-initialize the rewards system with a minimalistic UI for showing token and ticket balances, as well as providing buttons for showing the inbox and rewards catalog.

