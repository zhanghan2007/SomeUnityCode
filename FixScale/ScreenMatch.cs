using UnityEngine;
using UnityEngine.UI;

public class ScreenMatch : MonoBehaviour {
    private const float aspectRatio = 1920f / 1080f;
    private static int SrcResolutionWidth;
    private static int SrcResolutionHeight;
    
    static float Ratio {
        get {
            if (ratio < 0) {
                if (SrcResolutionWidth == 0)
                    SrcResolutionWidth = Screen.width;
                if (SrcResolutionHeight == 0)
                    SrcResolutionHeight = Screen.height;
                ratio = (float) SrcResolutionWidth / SrcResolutionHeight;
            }

            return ratio;
        }
    }

    public static bool LandScape => Ratio >= aspectRatio - 0.1f;

    public static bool MatchLiuHai => Ratio >= 2.0f;
    
    public CanvasScaler canvasScaler;

    static float ratio = -1f;

    void Awake() {
        Execute();
    }

    void Start() {
        Execute();
    }

    void Execute() {
        canvasScaler.matchWidthOrHeight = LandScape ? 1f : 0f;
    }


}