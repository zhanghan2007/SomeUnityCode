using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 适配全屏背景UI，宁可UI元素超出边框也要填满整个屏幕
/// </summary>
public class FixScale : MonoBehaviour {
    public RectTransform baseBg;
    public bool justSetBaseBG = false;
    public float defaultWidth = 1560f;
    public float defalutHeight = 720f;
    public static float fAspect = 0f;

    void Start() {
        SetScale();
    }

    void SetScale() {
        if (!IsNeedFix()) {
            return;
        }
        if (fAspect < 0.001) {
            if (Camera.main == null) {
                return;
            }
            fAspect = Camera.main.aspect;
        }
        
        float currdefaultWidth = defaultWidth;
        float currdefalutHeight = defalutHeight;
        
        if (baseBg != null) {
            Vector3 bgScale = baseBg.localScale;
            currdefaultWidth = baseBg.rect.width * bgScale.x;
            if (currdefaultWidth - Mathf.Epsilon <= 0 && currdefaultWidth + Mathf.Epsilon >= 0) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(baseBg);
                currdefaultWidth = baseBg.rect.width * bgScale.x;
            }

            if (!(currdefaultWidth - Mathf.Epsilon <= 0 && currdefaultWidth + Mathf.Epsilon >= 0)) {
                currdefalutHeight = baseBg.rect.height * bgScale.y;
            }
        }
        
        float targetWidth, targetHeight;
        
        bool blandScape = ScreenMatch.LandScape;
        if (blandScape) {
            targetWidth = 720 * fAspect;
            targetHeight = 720;
        }
        else {
            targetWidth = 1280;
            targetHeight = 1280 / fAspect;
        }

        // Debug.Log(currdefaultWidth +" " +targetWidth +" " + currdefalutHeight +" " + targetHeight);
        if (currdefaultWidth < targetWidth || currdefalutHeight < targetHeight) {
            float scalex = currdefaultWidth / targetWidth;
            float scaley = currdefalutHeight / targetHeight;
            float min = Mathf.Min(scalex, scaley);
            float to = 1 / min;
            // Debug.Log("to:"+to);
            Vector3 scale;
            GameObject o;
            if (justSetBaseBG && (baseBg != null)) {
                scale = baseBg.localScale;
                o = baseBg.gameObject;
            }
            else {
                scale = gameObject.transform.localScale;
                o = gameObject;
            }
            
            scale *= to;
            o.transform.localScale = scale;
        }

    }

    public bool IsNeedFix() {
        RectTransform rt = baseBg != null ? baseBg : transform.GetComponent<RectTransform>();
        return IsMiddleAnichors(rt);
    }

    bool IsMiddleAnichors(RectTransform rt) {
        if (rt == null) {
            return false;
        }
        return IsSameFloat(rt.anchorMin.x, 0.5f) && IsSameFloat(rt.anchorMin.y, 0.5f) && 
               IsSameFloat(rt.anchorMax.x, 0.5f) && IsSameFloat(rt.anchorMax.y, 0.5f);
    }

    bool IsSameFloat(float f1, float f2) {
        return Mathf.Abs(f1 - f2) < float.Epsilon;
    }
}