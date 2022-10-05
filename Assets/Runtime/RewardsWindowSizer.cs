using UnityEngine;

namespace Squaretwo {

  public class RewardsWindowSizer : MonoBehaviour {

    private static readonly Vector3[] worldCorners = new Vector3[4];
    private readonly int[] margins = new int[6];

    private RectTransform _rectTransform;

    void Start() {
      _rectTransform = gameObject.GetComponent<RectTransform>();
    }

    void Update() {
      _rectTransform.GetWorldCorners(worldCorners);


      int top = (int)(Screen.height - worldCorners[2].y);
      int right = (int)(Screen.width - worldCorners[2].x);
      int bottom = (int)(worldCorners[0].y);
      int left = (int)(worldCorners[0].x);

      bool needsUpdate = Screen.height != margins[4] || Screen.width != margins[5] || top != margins[0] || right != margins[1] || bottom != margins[2] || left != margins[3];

      if (needsUpdate) {
        margins[0] = top;
        margins[1] = right;
        margins[2] = bottom;
        margins[3] = left;
        margins[4] = Screen.height;
        margins[5] = Screen.width;

        S2RewardsSdk.SetMargins(left, top, right, bottom);

        //Debug.Log(string.Format("Margins {0} {1} {2}, {3}", margins[0], margins[1], margins[2], margins[3]));
      }

    }
  }

}