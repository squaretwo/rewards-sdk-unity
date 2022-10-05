using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MiniJSON;
using UniWebServer;

namespace Squaretwo
{

  public delegate void OnUserUpdated(UserData user);
  public delegate void OnMessageReceived(RemoteMessage message);

  public class UserData
  {
    public string id;
    public int tickets;
    public int tokens;
    public int inboxCount;
  }

  [Serializable]
  class ProductVo
  {
    public string productId;
    public string transactionId;
    public string receiptData;
    public string type;
  }

  public class RemoteMessage
  {
    public int messageId;
    public string messageType;
    public object messageBody;
    public string messageError;
  }

  public class SdkException : System.Exception
  {
    public SdkException()
    {
    }

    public SdkException(string message) : base(message)
    {
    }
  }

  public class S2RewardsSdk
  {
    const string NATIVE_APP_NAMESPACE = "co.squaretwo.nativeapp";
    const string WEB_CLIENT_NAMESPACE = "co.squaretwo.webclient";

    const int DEFAULT_NETWORK_TIMEOUT_MS = 20000;

    public static OnUserUpdated onUserUpdated;
    private static OnMessageReceived _onMessageReceived;

    private static WebViewObject _webViewObject;
    private static WebServer _localWebServer;
    private static bool _isLocalServerRunning = false;
    private static string _appId;
    private static string _vendorId;
    private static string _path = "/";
    private static UserData _user = null;
    private static readonly int[] _margins = new int[4];
    private static bool _isFullScreenForced = false;
    private static int _nextRemoteMessageId = 0;

    private static bool _isInitialized = false;
    private static bool _isInitializing = false;
    private static string _sdkUrl = "https://localhost:3000";

#if UNITY_EDITOR
    static readonly string FOLDER_ROOT = Path.GetFullPath("Packages/co.squaretwo.rewardssdk/www~");
#else
    static readonly string FOLDER_ROOT = Application.streamingAssetsPath + "/www";
#endif

    public static async Task Initialize(string vendorId, string appId, bool isTestMode = true)
    {
      if (_isInitialized)
      {
        throw new SdkException("Already initialized");
      }

      if (_isInitializing)
      {
        throw new SdkException("Already being initialized");
      }

      _isInitializing = true;

      _appId = appId;
      _vendorId = vendorId;

      StartLocalServer();

      if (_webViewObject == null)
      {
        InitializeWebView();
      }

      _webViewObject.LoadURL(_sdkUrl);

      var initResponse = await WaitForRemoteMessage((message) =>
      {
        return message.messageType == "unityClientInitialized" || message.messageType == "WebviewError";
      }, DEFAULT_NETWORK_TIMEOUT_MS);

      if (initResponse.messageError == null)
      {
        var body = $"{{ \"vendorId\": \"{ _vendorId }\", \"appId\": \"{ _appId }\", \"isTestMode\": { isTestMode.ToString().ToLower() } }}";
        initResponse = await SendMessageWithResponse("initialize", body, DEFAULT_NETWORK_TIMEOUT_MS);
      }

      _isInitializing = false;

      if (initResponse.messageError != null)
      {
        Debug.Log(initResponse.messageType);
        Debug.LogError(initResponse.messageError);
        // TODO: Populate this with real error info
        throw new SdkException("An error has occurred while initializing the SDK: " + initResponse.messageError);
      }

      _isInitialized = true;
    }

    private static void StartLocalServer() {

      if (_isLocalServerRunning) {
        return;
      }


      if (_localWebServer == null) {
        _localWebServer = new WebServer(0, 2, false);
        _localWebServer.logRequests = true;
        _localWebServer.HandleRequest += HandleRequest;
      }

      Application.quitting += StopLocalServer;

      var root = _localWebServer.Start();
      _sdkUrl = $"http://{ root.Replace("0.0.0.0", "localhost") }/index.html";

      Debug.Log("SERVER START: " + _sdkUrl);

      _isLocalServerRunning = true;
    }

    private static void StopLocalServer() {
      if (_isLocalServerRunning) {
        Application.quitting -= StopLocalServer;
        _isLocalServerRunning = false;
        _localWebServer.Stop();
      }
    }

    private static void HandleRequest(Request request, Response response) {
      string fullPath = FOLDER_ROOT + Uri.UnescapeDataString(request.uri.LocalPath);
      string fileExt = Path.GetExtension(fullPath);

      if (!File.Exists(fullPath)) {
        response.statusCode = 404;
        response.message = "Not Found";
        response.Write(fullPath + " not found.");
        return;
      }

      response.statusCode = 200;
      response.message = "OK";
      response.headers.Add("Content-Type", MimeTypeMap.GetMimeType(fileExt));

      using FileStream fs = File.OpenRead(fullPath);

      int length = (int)fs.Length;
      byte[] buffer;

      response.headers.Add("Content-Length", length.ToString());

      using (BinaryReader br = new(fs)) {
        buffer = br.ReadBytes(length);
      }

      response.SetBytes(buffer);
    }

    private static void InitializeWebView()
    {
      _webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();

      var disableTransitions = false;

#if UNITY_EDITOR
      disableTransitions = true;
#endif

      _webViewObject.Init(

        cb: (msg) =>
        {
          string messageResponse = HandleMessage(msg);

          if (messageResponse != null)
          {
            Debug.Log(messageResponse);
            _webViewObject.EvaluateJS($"window.postMessage('{ messageResponse }');");
          }
        },

        err: (msg) =>
        {
          Debug.Log(string.Format("CallOnError[{0}]", msg));
          _onMessageReceived?.Invoke(new RemoteMessage
          {
            messageId = -1,
            messageType = "WebviewError",
            messageBody = null,
            messageError = msg
          });
        },

        httpErr: (msg) =>
        {
          Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
          _onMessageReceived?.Invoke(new RemoteMessage
          {
            messageId = -1,
            messageType = "WebviewError",
            messageBody = null,
            messageError = msg
          });
        },

        hooked: (msg) =>
        {
          Debug.Log(string.Format("CallOnHooked[{0}]", msg));
          if (msg != "about:blank")
          {
            Application.OpenURL(msg);
          }
        },

        ld: (msg) =>
        {
          Debug.Log(string.Format("CallOnLoaded[{0}]", msg));

          var js = @"
            if (window.Unity) {
              window.ReactNativeWebView = {
                postMessage: function(message) {
                  window.Unity.call(message);
                }
              }
            } else if (window.parent && window.parent.unityWebView) {
              window.ReactNativeWebView = {
                postMessage: function (msg) {
                  window.parent.unityWebView.sendMessage('WebViewObject', msg);
                }
              };
            } else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
              window.ReactNativeWebView = {
                postMessage: function (msg) {
                  window.webkit.messageHandlers.unityControl.postMessage(msg);
                }
              }
            } else {
              window.ReactNativeWebView = {
                postMessage: function (msg) {
                  window.location = 'unity:' + msg;
                }
              }
            }

            window.redemptionSdk.initializeUnityClient();
          ";

          if (disableTransitions)
          {
            js += "window.redemptionSdk.disableTransitions();";
          }

          _webViewObject.EvaluateJS(js);
          _webViewObject.SetURLPattern("", "", $"^(?:(?!recaptcha|{ Regex.Escape(_sdkUrl) }).)*$");
        },

        //separated: true,

        enableWKWebView: true,

        transparent: true,

        zoom: false

      );

      _webViewObject.SetTextZoom(100);
      //_webViewObject.SetVisibility(true);

      UnityEngine.Object.DontDestroyOnLoad(_webViewObject.gameObject);
    }

    public static async void ShowInbox()
    {
      if (!IsInboxOpen)
      {
        await SendMessageWithResponse("showInbox");
      }
    }

    public static async void HideInbox()
    {
      if (IsInboxOpen)
      {
        await SendMessageWithResponse("hideInbox");
      }
    }

    public static async void ShowRewardsCatalog()
    {
      if (!IsRewardsCatalogOpen)
      {
        await SendMessageWithResponse("showRewardsCatalog");
      }
    }

    public static async void HideRewardsCatalog()
    {
      if (IsRewardsCatalogOpen)
      {
        await SendMessageWithResponse("hideRewardsCatalog");
      }
    }

    public static void SetMargins(int left, int top, int right, int bottom)
    {
      _margins[0] = left;
      _margins[1] = top;
      _margins[2] = right;
      _margins[3] = bottom;

      if (_webViewObject != null && !_isFullScreenForced)
      {
        // Hacky workaround for an issue with the webview not resizing properly
        _webViewObject.SetMargins(left, top, right, bottom - 1);
        _webViewObject.SetMargins(left, top, right, bottom);
      }
    }

    public static async Task<bool> ConfirmIap(string productId, string transactionId, string receiptData)
    {

      // productId: product.definition.storeSpecificId
      // transactionId: product.transactionID
      // receiptData: product.receipt

      var receipt = Json.Deserialize(receiptData) as Dictionary<string, object>;

      var productVo = new ProductVo
      {
        productId = productId,
        transactionId = transactionId,
        receiptData = receipt["Payload"] as string,
        type = Application.platform == RuntimePlatform.Android ? "PlayStorePurchase" : "AppStorePurchase"
      };

      var json = JsonUtility.ToJson(productVo);

      var response = await SendMessageWithResponse("savePurchase", json, DEFAULT_NETWORK_TIMEOUT_MS);

      return response.messageError == null;
    }

    public static async Task StartGameWithTokens(int tokenCount)
    {
      var result = await SendMessageWithResponse("startGameWithTokens", tokenCount.ToString(), DEFAULT_NETWORK_TIMEOUT_MS);

      if (result.messageError != null)
      {
        throw new SdkException(result.messageError);
      }
    }

    public static async Task<int> CollectGameTickets(int scorePercent)
    {
      var result = await SendMessageWithResponse("collectGameTickets", scorePercent.ToString(), DEFAULT_NETWORK_TIMEOUT_MS);

      if (result.messageError != null)
      {
        throw new SdkException(result.messageError);
      }

      var tickets = Convert.ToInt32(result.messageBody);

      return tickets;
    }

    public static async Task<bool> VerifyUser()
    {
      var result = await SendMessageWithResponse("verifyUser", "null", 120000);

      if (result.messageError != null)
      {
        throw new SdkException(result.messageError);
      }

      return (bool)result.messageBody;
    }

    private static string HandleMessage(string message)
    {

      Debug.Log(message);

      var parsedMessage = Json.Deserialize(message) as Dictionary<string, object>;

      var isValidMessage = parsedMessage != null
        && parsedMessage.ContainsKey("namespace")
        && parsedMessage["namespace"] is string
        && parsedMessage["namespace"] as string == WEB_CLIENT_NAMESPACE;

      if (!isValidMessage)
      {
        return null;
      }

      var messageType = parsedMessage["type"] as string;
      var messageId = Convert.ToInt32(parsedMessage["id"]);
      var messageBody = parsedMessage.ContainsKey("data") ? parsedMessage["data"] : null;
      var messageError = (parsedMessage.ContainsKey("error") && parsedMessage["error"] != null) ? parsedMessage["error"] as string : null;

      _onMessageReceived?.Invoke(new RemoteMessage
      {
        messageId = messageId,
        messageType = messageType,
        messageBody = messageBody,
        messageError = messageError
      });

      var response = "null";

      switch (messageType) {

        case "navigationRouteUpdated": {
          _path = messageBody as string;
          _webViewObject.SetVisibility(_path != "/");
          ForceFullScreen(_path.StartsWith("/AuthScreen"));
          break;
        }

        case "storageGetItem": {
          var key = messageBody as string;

          if (PlayerPrefs.HasKey(key)) {
            response = $"\"{ PlayerPrefs.GetString(key).Replace("\"", "\\\\\"") }\"";
          }
          break;
        }

        case "storageSetItem": {
          var dict = messageBody as Dictionary<string, object>;
          var key = dict["key"] as string;
          var value = dict["value"] as string;

          PlayerPrefs.SetString(key, value);

          response = $"\"{ value.Replace("\"", "\\\\\"") }\"";
          break;
        }

        case "userUpdated": {
          var dict = messageBody as Dictionary<string, object>;

          _user = new UserData {
            id = dict["id"] as string,
            tickets = Convert.ToInt32(dict["tickets"]),
            tokens = Convert.ToInt32(dict["tokens"]),
            inboxCount = Convert.ToInt32(dict["inboxCount"])
          };

          onUserUpdated?.Invoke(_user);
          break;
        }
      }

      return $"{{ \"namespace\": \"{ NATIVE_APP_NAMESPACE }\", \"id\": { messageId }, \"type\": \"{ messageType }Response\", \"data\": { response }, \"error\": null }}";
    }

    private static async Task<RemoteMessage> SendMessageWithResponse(string messageType, string messageBody = "null", int timeoutMs = 0)
    {
      var messageId = _nextRemoteMessageId++;
      var remoteMessageString = $"{{ \"namespace\": \"{ NATIVE_APP_NAMESPACE }\", \"id\": { messageId }, \"type\": \"{ messageType }\", \"data\": { messageBody } }}";

      Debug.Log("SENDING MESSAGE: " + remoteMessageString);

      _webViewObject.EvaluateJS($"window.postMessage('{ remoteMessageString }');");

      var response = await WaitForRemoteMessage((message) =>
      {
        return message.messageId == messageId && message.messageType.EndsWith("Response");
      }, timeoutMs);

      return response;
    }

    private static async Task<RemoteMessage> WaitForRemoteMessage(Func<RemoteMessage, bool> checkMethod, int timeoutMs = 0)
    {

      var waitTaskSource = new TaskCompletionSource<RemoteMessage>();

      CancellationTokenSource cancellationTokenSource = null;

      void handler(RemoteMessage message)
      {
        if (checkMethod(message))
        {
          _onMessageReceived -= handler;
          waitTaskSource.TrySetResult(message);
          cancellationTokenSource?.Cancel();
        }
      }

      _onMessageReceived += handler;

      if (timeoutMs == 0)
      {
        // Basic wait without timeout, just return the result when done
        return await waitTaskSource.Task;
      }

      // Need to create cancelable timeout task if timeoutMs is specified
      cancellationTokenSource = new CancellationTokenSource();

      var delayTask = Task.Delay(timeoutMs, cancellationTokenSource.Token);

      if (waitTaskSource.Task == await Task.WhenAny(waitTaskSource.Task, delayTask))
      {
        return await waitTaskSource.Task;
      }

      // Timed out. Clean up.
      _onMessageReceived -= handler;
      waitTaskSource.SetCanceled();

      return new RemoteMessage
      {
        messageId = -1,
        messageType = "TimedOut",
        messageBody = null,
        messageError = "Request Timed Out"
      };
    }

    public static bool IsInboxOpen
    {
      get => _path.StartsWith("/InboxScreen");
    }

    public static bool IsRewardsCatalogOpen
    {
      get => _path.StartsWith("/RedeemScreen");
    }

    public static UserData User
    {
      get => _user;
    }

    private static void ForceFullScreen(bool isFullScreenForced)
    {

      if (_isFullScreenForced == isFullScreenForced)
      {
        return;
      }

      _isFullScreenForced = isFullScreenForced;

      if (_isFullScreenForced)
      {
        _webViewObject.SetMargins(0, 0, 0, 0);
      }
      else
      {
        _webViewObject.SetMargins(_margins[0], _margins[1], _margins[2], _margins[3]);
      }
    }

  }

}