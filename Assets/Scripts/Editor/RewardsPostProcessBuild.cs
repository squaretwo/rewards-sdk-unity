#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Squaretwo {

  public static class RewardsPostProcessBuild {

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
      var srcFolder = "Packages/co.squaretwo.rewards-sdk-unity/www~";
      var targetFolder = GetWwwPath(target, pathToBuiltProject);

      try {
        if (!Directory.Exists(targetFolder)) {
          Directory.CreateDirectory(targetFolder);
        }

      } catch (IOException ex) {
        Debug.LogError(ex.Message);
      }

      targetFolder += "/www";

      Debug.LogFormat("Copy www folder from '{0}' to: {1}", srcFolder, targetFolder);
      FileUtil.CopyFileOrDirectory(srcFolder, targetFolder);
    }

    static string GetWwwPath(BuildTarget target, string pathToBuiltProject) {
      if (target.ToString().Contains("OSX")) {
        return pathToBuiltProject + "/Contents/Resources/Data/StreamingAssets";
      } else if (target == BuildTarget.iOS) {
        return pathToBuiltProject + "/Data/Raw";
      }

      throw new UnityException("Platform not implemented");
    }

  }

}
#endif
