# SquareTwo Rewards SDK for Unity

rewards-sdk-unity is a plugin for Unity 2019.4 and greater that enables Unity games to integrate real world rewards for their games.
Currently, only iOS is suppported, with Android support slated for the future.

## Install the Plugin

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

## Rewards Example Scene

A minimalistic example scene is placed under `Packages/co.squaretwo.rewardssdk/Assets/Scenes/`. To view it, copy the scene by dragging it from the package directory to your game's Assets folder. You can now open and run it to see how your code can interact with the Rewards SDK. Since the SDK Example Scene and Rewards Prefab rely upon Text Mesh Pro, the scene may require you to install TMP Essentials on first open.

## Rewards Prefab

For a quick start with a prebuilt rewards canvas, you will find a prefab under `Packages/co.squaretwo.rewards-sdk-unity/Assets/Prefabs/`. Drag and drop this into your desired scene, and install TMP Essentials (if needed). Click on the prefab instance, and provide the following in the inspector: 

| Property | Description |
| -------- | ----------- |
| Vendor Id | This will be the vendor identifier provided to you by Squaretwo        |
| App Id | The ID for the current app you are building (found in the admin panel) |
| On Click Inbox Button | Add any callbacks here if your game needs to be notified that the inbox button was clicked, displaying the inbox. |
| On Click Tokens Button | Add any callbacks here if your game needs to be notified that the tokens button was clicked. This button has no default behavior. |
| On Click Tickets Button | Add any callbacks here if your game needs to be notified that the tickets button was clicked, displaying the redemption catalog. |
| On Initialize Failure | If there is an issue with SDK initialization, this callback will be called with an error. Use this for handling any failure state or retry logic. |
| On Initialize Success | Add any callbacks here if your game needs to be notified when the SDK initializes successfully. |

On game start, this prefab will auto-initialize the rewards system with a minimalistic UI for showing token and ticket balances, as well as providing buttons for showing the inbox and rewards catalog.

## Spending Tokens to Start a Game and Reward Tickets

```cs
using Squaretwo;

...

async void PlayGame() {
  try {
    // Start game with single token
    await S2RewardsSdk.StartGameWithTokens(1);

    ... // Insert awaitable gameplay logic here
    
    // End the open game with 100% of max reward
    var tickets = await S2RewardsSdk.CollectGameTickets(100);
    
    Debug.Log("Awarded " + tickets + " tickets.");
} catch (Exception e) {
    // Handle errors here
    Debug.LogError(e.Message);
  }
}
```

## Saving User In-app Purchases

When a user makes an in-app purchase for tokens, you will need to pass the receipt data to the SDK for validation. Once validation is complete, the tokens will appear in the user's inbox.

```cs
using Squaretwo;

...

// Use this in your IStoreListener for listening to purchases
public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e) {
  S2RewardsSdk.ConfirmIap(
    e.purchasedProduct.definition.storeSpecificId,
    e.purchasedProduct.transactionID,
    e.purchasedProduct.receipt
  ).ContinueWith((task) => {
    myIapController.ConfirmPendingPurchase(e.purchasedProduct);
  });

  return PurchaseProcessingResult.Pending;
}
```

## API

If you'd like more fine-grained customization for your Rewards SDK integration, you can call the API directly. All of the following can be called off of the `Squaretwo.S2RewardsSdk`:

### Delegates

| Name          | Param         | Description |
| ------------- | ------------- | ----------- |
| onUserUpdated | <code>UserData user</code> | Called whenever a user is logged in or updated. |

### Methods

| Method | Description |
| ------ | ----------- |
| <code>static Task Initialize(string vendorId, string appId, bool isTestMode = true)</code> | Returns a task that completes when the SDK initializes successfully |
| <code>static void ShowInbox()</code> | Shows the inbox |
| <code>static void HideInbox()</code> | Hides the inbox |
| <code>static void ShowRewardsCatalog()</code> | Shows the rewards catalog |
| <code>static void HideRewardsCatalog()</code> | Hides the rewards catalog |
| <code>static async Task<bool> ConfirmIap(string productId, string transactionId, string receiptData)</code> | Returns a task that returns a bool indicating whether the product was successfully saved and an inbox item granted. This will return true if the product had already been saved in the past. |
| <code>static Task StartGameWithTokens(int tokenCount)</code> | Spends `{tokenCount}` tokens to begin a game |
| <code>static Task<int> CollectGameTickets(int scorePercent) | Collects game tickets from the current active game and returns a task containing the ticket count. `scorePercent` is an integer between 0 and 100 representing the percentage of maximum reward to grant. |
| <code>static Task VerifyUser()</code> | If the user is not sms verified, this will show the sms authentication flow. Call this before allowing users to make in-app purchases. |
| <code>static Task SignOut()</code> | Signs the active user out of the rewards SDK. |





























