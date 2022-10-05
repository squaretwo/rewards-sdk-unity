using System;
using System.Threading.Tasks;
using UnityEngine;
using Squaretwo;

public class DemoGame : MonoBehaviour {

  public GameObject storePanel;

  public GameObject gameStartPanel;
  public GameObject gamePlayPanel;
  public GameObject gameOverPanel;
  public GameObject messagePanel;

  public TMPro.TextMeshProUGUI messageText;
  public TMPro.TextMeshProUGUI wonTickets;

  void Awake() {
    DontDestroyOnLoad(this.gameObject);
  }

  public void OnCloseGameOver() {
    gameStartPanel.SetActive(true);
    gameOverPanel.SetActive(false);
  }

  public async void OnStartGame(bool useTokens) {
    gameStartPanel.SetActive(false);
    try {
      await S2RewardsSdk.StartGameWithTokens(1);
      gamePlayPanel.SetActive(true);
    } catch (Exception e) {
      gameStartPanel.SetActive(true);
      ShowMessage(e);
    }
  }

  public async void OnCompleteGame() {
    gamePlayPanel.SetActive(false);

    var tickets = await S2RewardsSdk.CollectGameTickets(100);

    wonTickets.text = tickets.ToString("n0");
    gameOverPanel.SetActive(true);

    await Task.Delay(2000);

    gameOverPanel.SetActive(false);
    gameStartPanel.SetActive(true);
  }

  public void OnClickStore() {
    storePanel.SetActive(!storePanel.activeSelf);
  }

  public void ShowMessage(System.Exception exception) {
    Debug.Log("RECEIVING EXCEPTION");
    ShowMessage(exception.Message);
  }

  public void ShowMessage(string message) {
    Debug.Log("SHOWING MESSAGE");
    messageText.text = message;
    messagePanel.SetActive(true);
  }

  public void HideMessage() {
    messagePanel.SetActive(false);
  }

}
