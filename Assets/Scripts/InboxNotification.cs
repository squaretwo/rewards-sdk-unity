using UnityEngine;
using Squaretwo;

public class InboxNotification : MonoBehaviour {

  public TMPro.TextMeshProUGUI inboxCountText;

  void Start() {
    S2RewardsSdk.onUserUpdated += OnUserUpdated;

    if (S2RewardsSdk.User == null) {
      UpdateCount(0);
    } else {
      UpdateCount(S2RewardsSdk.User.inboxCount);
    }
  }

  void OnDestroy() {
    S2RewardsSdk.onUserUpdated -= OnUserUpdated;
  }

  void OnUserUpdated (UserData user) {
    UpdateCount(user.inboxCount);
  }

  void UpdateCount (int count) {
    gameObject.SetActive(count > 0);
    inboxCountText.text = count.ToString();
  }

}
