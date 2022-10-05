using UnityEngine;
using UnityEngine.Events;
using Squaretwo;

public class InitializeRewardsSdk : MonoBehaviour {

  public string vendorId;
  public string appId;

  public UnityEvent onClickInboxButton;
  public UnityEvent onClickTokensButton;
  public UnityEvent onClickTicketsButton;

  public UnityEvent<System.Exception> onInitializeFailure;
  public UnityEvent onInitializeSuccess;

  private TMPro.TextMeshProUGUI displayedTickets;
  private TMPro.TextMeshProUGUI displayedTokens;

  void Awake() {
    displayedTickets = gameObject.transform.Find("Rewards Canvas/Button Row/Tickets Button/TicketQuantityText").GetComponent<TMPro.TextMeshProUGUI>();
    displayedTokens = gameObject.transform.Find("Rewards Canvas/Button Row/Tokens Button/TokenQuantityText").GetComponent<TMPro.TextMeshProUGUI>();

    DontDestroyOnLoad(this.gameObject);

    S2RewardsSdk.onUserUpdated += OnUserUpdated;

    Initialize();
  }

  public async void Initialize() {
    try {
      if (appId == null || appId == "") {
        throw new UnityException("Missing app id");
      }

      if (vendorId == null || vendorId == "") {
        throw new UnityException("Missing vendor id");
      }

      await S2RewardsSdk.Initialize(vendorId, appId, true);

      onInitializeSuccess?.Invoke();
    } catch (System.Exception exception) {
      Debug.Log("INVOKING");
      onInitializeFailure?.Invoke(exception);
    }
  }

  public void OnUserUpdated(UserData user) {
    // Update displayed tickets and tokens
    displayedTickets.text = user.tickets.ToString("n0");
    displayedTokens.text = user.tokens.ToString("n0");
  }

  public void OnClickInbox() {
    if (S2RewardsSdk.IsInboxOpen) {
      S2RewardsSdk.HideInbox();
    } else {
      S2RewardsSdk.ShowInbox();
    }

    onClickInboxButton?.Invoke();
  }

  public void OnClickTickets() {
    if (S2RewardsSdk.IsRewardsCatalogOpen) {
      S2RewardsSdk.HideRewardsCatalog();
    } else {
      S2RewardsSdk.ShowRewardsCatalog();
    }

    onClickTicketsButton?.Invoke();
  }

  public void OnClickTokens() {
    S2RewardsSdk.HideRewardsCatalog();
    S2RewardsSdk.HideInbox();
    onClickTokensButton?.Invoke();
  }

}
