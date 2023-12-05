using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLua;
using ZXing;
using ZXing.QrCode;
using System.IO;
using System.Runtime.InteropServices;

[CustomLuaClass]
public class ThirdChannel : MonoBehaviour {
    public class Config {
        public string name;
        public int id;

        public Config(string name, int id) {
            this.name = name;
            this.id = id;
        }
    }

    //分享数据块
    public struct ShareData {
        public string url;
        public string tile;
        public string description;
        public int qrCodeX;
        public int qrCodeY;
        public int scene;

        //0-->wx 1-->qq
        public int channel;

        public ShareData(string url, int qrCodeX, int qrCodeY, string tile, string description, int scene, int channel) {
            this.url = url;
            this.qrCodeX = qrCodeX;
            this.qrCodeY = qrCodeY;
            this.tile = tile;
            this.description = description;
            this.scene = scene;
            this.channel = channel;
        }
    }


    //苹果登录 sdk返回结构体
    public enum Apple_UserDetectionStatus {
        LikelyReal,
        Unknown,
        Unsupported
    }

    //苹果登录 sdk返回结构体
    public enum Apple_UserCredentialState {
        Revoked,
        Authorized,
        NotFound
    }

    //苹果登录 sdk返回结构体
    public struct Apple_UserInfo {
        public string userId;
        public string email;
        public string displayName;

        public string idToken;
        public string error;

        public Apple_UserDetectionStatus userDetectionStatus;
    }

    public static string config = "{\"name\":\"官方渠道\", \"id\":1, \"channel\":2, \"loginMode\":7}";

    //"{\"name\":\"官方渠道\",\"id\":1,\"officialPay\":true,\"loginMode\":7,\"openInviteRoom\":true,\"payMode\":\"AppStorePay\"}";
    private static Config _config = new Config("官方渠道", 1);

    static ThirdChannel tc;
    static AndroidJavaObject _currentActivity = null;

    private delegate void Apple_OnLoginCompleted(int result, Apple_UserInfo info);

    private delegate void Apple_OnCredentialState(Apple_UserCredentialState state);

    public static void LoadConfig(string text) {
        config = text;
        _config = JsonUtility.FromJson<Config>(text);
    }

    private static bool IsUseWXShare() {
        return PlayerPrefs.GetInt("LoginType") != 2;
    }

    [DoNotToLua]
    public static AndroidJavaObject currentActivity {
        get {
            if (_currentActivity == null) {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _currentActivity = activity.GetStatic<AndroidJavaObject>("currentActivity");
            }

            return _currentActivity;
        }
    }

    [DoNotToLua]
    public static AndroidJavaObject currentALiActivity {
        get { return new AndroidJavaObject("com.saiyun.cat.AliSDKActivity"); }
    }

    public static void Init() {
        // Debug.Log("ThirdChannel:Init");
        GameObject go = GameObject.Find("ThirdChannel");
        if (go == null) {
            go = new GameObject("ThirdChannel");
            tc = go.AddComponent<ThirdChannel>();
            DontDestroyOnLoad(go);
        }
        else {
            tc = go.GetComponent<ThirdChannel>();
        }
    }

#if UNITY_IOS
    //苹果登录
    [DllImport("__Internal")]
    private static extern void UnitySignInWithApple_Login();

    //苹果登录
    [DllImport("__Internal")]
    private static extern void UnitySignInWithApple_GetCredentialState(string userID);

    [DllImport("__Internal")]
    private static extern void U3D_Login();

    //    [DllImport("__Internal")]
    //    private static extern void U3D_ShareImage(string url, string tile, string description, int scene);
    //    [DllImport("__Internal")]
    //    private static extern void U3D_ShareWebpage(string url, string tile, string description, int scene);
    [DllImport("__Internal")]
    private static extern bool U3D_IsWXAppInstalled();

    [DllImport("__Internal")]
    private static extern bool U3D_EnableSignInWithApple();

    [DllImport("__Internal")]
    private static extern void qqLogin();

    //    [DllImport("__Internal")]
    //    private static extern void qqShareImage(string url, string tile, string description, int scene);
    //    [DllImport("__Internal")]
    //    private static extern void qqShareWebpage(string url, string tile, string description, int scene);
    [DllImport("__Internal")]
    private static extern bool qqIsInstalled();

    [DllImport("__Internal")]
    private static extern void U3D_InitAppStorePay();

    [DllImport("__Internal")]
    private static extern void U3D_AppStorePay(string transactionId, string productId, string extData);

    [DllImport("__Internal")]
    private static extern void U3D_FinishAppStorePay(string transactionId);

    [DllImport("__Internal")]
    private static extern void U3D_RestorePay();

#endif

    public static void BuglyInit() {
#if UNITY_EDITOR
        return;
#endif
#if UNITY_ANDROID
        BuglyAgent.InitWithAppId("b731fd48cb");
#elif UNITY_IOS
        BuglyAgent.InitWithAppId("f02d598c77");
#endif
        BuglyAgent.EnableExceptionHandler();
    }

    public static void Apple_Login() {
#if UNITY_IOS && !UNITY_EDITOR
        UnitySignInWithApple_Login();
#endif
    }

    public static void Apple_GetCredentialStateInternal(string userID) {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        UnitySignInWithApple_GetCredentialState(userID);
#endif
    }

    void Apple_GetCredentialStateCallback(string value) {
        Apple_UserCredentialState state = (Apple_UserCredentialState) int.Parse(value);
        Debug.Log("Apple_GetCredentialStateCallback: " + state);
    }

    void Apple_OnLoginSucceed(string result) {
        Debug.Log("Apple_OnLoginSucceed: " + result);
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["Apple_onLoginSucceed"];
            fun.call(result);
        }
        else {
            Debug.LogError("QQ_OnLoginSucceed no find lua Channel");
        }
    }

    void Apple_OnLoginFailed(string result) {
        Debug.Log("Apple_OnLoginFailed: " + result);
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["Apple_onLoginFailed"];
            Debug.Log("call lua: " + result);
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_OnLoginFailed no find lua Channel");
        }
    }

    public static void Login() {
#if UNITY_IOS
        U3D_Login();
#else
        currentActivity.Call("Login");
#endif
    }

    void U3D_OnLoginSucceed(string result) {
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onLoginSucceed"];
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_LoginSucceed no find lua Channel");
        }
    }

    void U3D_OnLoginFailed(string result) {
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onLoginFailed"];
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_OnLoginFailed no find lua Channel");
        }
    }

    void U3D_OnLogout() {
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onLogout"];
            fun.call();
        }
        else {
            Debug.LogError("U3D_OnLogout no find lua Channel");
        }
    }

    void U3D_OnExtraDataSuncced(string jsonStr) {
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onExtraDataSuncced"];
            fun.call(jsonStr);
        }
        else {
            Debug.LogError("U3D_OnExtraDataSuncced no find lua Channel");
        }
    }

    public static void GetVerifiedInfo() {
#if UNITY_ANDROID
        currentActivity.Call("GetVerifiedInfo");
#endif
    }

    void U3D_OnVerifiedSucceed(string age) {
        Debug.Log("U3D_OnVerifiedSucceed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onVerifiedSucceed"];
            fun.call(age);
        }
        else {
            Debug.LogError("U3D_OnVerifiedSucceed no find lua Channel");
        }
    }

    void U3D_OnVerifiedFailed(string age) {
        Debug.Log("U3D_OnVerifiedFailed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onVerifiedFailed"];
            fun.call(age);
        }
        else {
            Debug.LogError("U3D_OnVerifiedFailed no find lua Channel");
        }
    }

    private static Color32[] QRCode(string textForEncoding, int width, int height) {
        var writer = new BarcodeWriter {Format = BarcodeFormat.QR_CODE, Options = new QrCodeEncodingOptions {Height = height, Width = width}};
        return writer.Write(textForEncoding);
    }

    private IEnumerator _ShareImage(ShareData sd) {
        //等待渲染线程结束
        yield return new WaitForEndOfFrame();
        //初始化Texture2D, 大小可以根据需求更改
        var mRect = new Rect(0, 0, UnityUtil.SrcResolutionWidth, UnityUtil.SrcResolutionHeight);
        var mTexture = new Texture2D(Mathf.FloorToInt(mRect.width), Mathf.FloorToInt(mRect.height),
            TextureFormat.RGB24, false);
        //读取屏幕像素信息并存储为纹理数据
        mTexture.ReadPixels(mRect, 0, 0, false);

        //二维码
        Texture2D texQRCode = new Texture2D(256, 256);
        texQRCode.SetPixels32(QRCode(sd.url, texQRCode.width, texQRCode.height));
        texQRCode.Apply();

        for (int x = 20; x < texQRCode.width - 20; ++x) {
            for (int y = 20; y < texQRCode.height - 20; ++y) {
                mTexture.SetPixel(x + sd.qrCodeX, y + sd.qrCodeY, texQRCode.GetPixel(x, y));
            }
        }

        //应用
        mTexture.Apply();
        //将图片信息编码为字节信息
        byte[] bytes = mTexture.EncodeToJPG();
        string path = Path.Combine(Application.persistentDataPath, "share.jpg");
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log("save 'share.jpg' to: " + path);


#if UNITY_IOS
        if (IsUseWXShare()) {
            //U3D_ShareImage(url, tile, description, scene);
        }
        else {
            //qqShareImage(url, tile, description, scene);
        }
#else
        if (sd.channel == 0) //IsUseWXShare())
        {
            currentActivity.Call("WXShareImage", sd.url, sd.tile, sd.description, sd.scene);
        }
        else {
            currentActivity.Call("QQShareImage", sd.url, sd.tile, sd.description, sd.scene);
        }
#endif
    }

    // 分享图片
    public static void ShareImage(string url, int qrCodeX, int qrCodeY,
        string tile, string description, int scene, int channel) {
        ShareData sd = new ShareData(url, qrCodeX, qrCodeY, tile, description, scene, channel);
        tc.StopCoroutine("_ShareImage");
        tc.StartCoroutine("_ShareImage", sd);
    }

    // 分享链接
    public static void ShareWebpage(string url, string tile, string description, int scene) {
#if UNITY_IOS
        if (IsUseWXShare())
        {
            //U3D_ShareWebpage(url, tile, description, scene);
        }
        else
        {
            //qqShareWebpage(url, tile, description, scene);
        }
#else
        if (IsUseWXShare()) {
            currentActivity.Call("ShareWebpage", url, tile, description, scene);
        }
        else {
            currentActivity.Call("QQShareWebpage", url, tile, description, scene);
        }
#endif
    }

    public static bool EnableSignInWithApple() {
#if UNITY_EDITOR
        return false;
#elif UNITY_IOS
        return U3D_EnableSignInWithApple();
#else
        return false;
#endif
    }

    public static bool isWXAppInstalled() {
#if UNITY_EDITOR
        return true;
#elif UNITY_IOS
        return U3D_IsWXAppInstalled();
#elif UNITY_ANDROID
        return currentActivity.Call<bool>("IsWechatInstalled");
#else
        return false;
#endif
    }

    void U3D_OnInviteRoom(string args) {
        Debug.Log("U3D_OnJoinRoom room=" + args);
        PlayerPrefs.SetString("InviteRoom", args);

        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onJoinRoom"];
            fun.call(args);
        }
    }

    public static void WXBuy(string transactionId, string partnerid, string prepayId, string nonceStr, string timeStamp,
        string sign) {
        Debug.Log("WXBuy:transactionId" + transactionId);
        //不需要订单号
#if UNITY_ANDROID
        //currentActivity.Call("WXPay", transactionId, partnerid, prepayId, nonceStr, timeStamp, sign);
        currentActivity.Call("WXPay", transactionId, partnerid, prepayId, nonceStr, timeStamp, sign);
#endif
    }

    void U3D_OnWXPaySucceed(string prepayId) {
        //新版sdk没有订单号
        Debug.Log("U3D_OnWXPaySucceed" + prepayId);
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onWXPaySucceed"];
            fun.call(prepayId);
        }
        else {
            Debug.LogError("U3D_OnWXPaySucceed no find lua Channel");
        }
    }

    void U3D_OnHuaweiPaySucceed(string result) {
        Debug.Log("U3D_OnHuaweiPaySucceed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onHuaweiPaySucceed"];
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_OnHuaweiPaySucceed no find lua Channel");
        }
    }

    //已发货未消耗 掉单轮询服务器查询订单成功
    void U3D_OnProductOwned(string errorCode) {
        Debug.Log("U3D_OnProductOwned");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onProductOwned"];
            fun.call();
        }
        else {
            Debug.LogError("U3D_OnProductOwned no find lua Channel");
        }
    }

    //调起sdk查询掉单字段
    public static void CheckUncomptedOrders() {
#if !UNITY_EDITOR&& UNITY_ANDROID
        currentActivity.Call("CheckUncomptedOrders");
#endif
    }

    //收到sdk查询到的所有掉单信息
    void U3D_OnGetUncompletedOrders(string orders) {
        Debug.Log("U3D_OnGetUncompletedOrders");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Ex_Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["OnGetUncompletedOrders"];
            fun.call(orders);
        }
        else {
            Debug.LogError("U3D_OnGetUncompletedOrders no find lua Channel");
        }
    }

    void U3D_OnAppStorePaySucceed(string result) {
        Debug.Log("U3D_OnAppStorePaySucceed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onAppStorePaySucceed"];
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_OnAppStorePaySucceed no find lua Channel");
        }
    }

    void U3D_OnWXPayFailed(string prepayId) {
        Debug.Log("U3D_OnWXPayFailed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onWXPayFailed"];
            fun.call(prepayId);
        }
        else {
            Debug.LogError("U3D_OnWXPayFailed no find lua Channel");
        }
    }

    void U3D_OnWXShareSucceed(string args) {
        Debug.Log("U3D_OnWXShareSucceed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onWXShareSucceed"];
            fun.call(args);
        }
        else {
            Debug.LogError("U3D_OnWXShareSucceed no find lua Channel");
        }
    }

    void U3D_OnWXShareFailed(string args) {
        Debug.Log("U3D_OnWXShareFailed");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onWXShareFailed"];
            fun.call(args);
        }
        else {
            Debug.LogError("U3D_OnWXShareFailed no find lua Channel");
        }
    }

    public static bool isQQInstalled() {
#if UNITY_EDITOR
        return true;
#elif UNITY_IOS
        return qqIsInstalled();
        // return false;
#else
        return currentActivity.Call<bool>("isQQInstalled");
#endif
    }

    public static void QQLogin() {
#if UNITY_IOS
        qqLogin();
#else
        currentActivity.Call("QQLogin");
#endif
    }

    public static void JoinQQGroup(string key) {
#if UNITY_IOS
        //qqJoinGroup(key);
#else
        currentActivity.Call("JoinQQGroup", key);
#endif
    }

    void QQ_OnLoginSucceed(string result) {
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["QQ_onLoginSucceed"];
            fun.call(result);
        }
        else {
            Debug.LogError("QQ_OnLoginSucceed no find lua Channel");
        }
    }

    public static void QQBuy(string serialNumber, string tokenId, string pubAcc, string pubAccHint,
        string nonce, string timeStamp, string bargainorId, string sig, string sigType) {
#if UNITY_ANDROID
        currentActivity.Call("QQPay", serialNumber, tokenId, pubAcc, pubAccHint, nonce, long.Parse(timeStamp),
            bargainorId, sig, sigType);
#endif
    }

    public static void Pay(string data, string extData) {
#if UNITY_ANDROID
        currentActivity.Call("Pay", data, extData);
#endif
    }

    public static void ConsumeProduct(string data) {
#if !UNITY_EDITOR&&UNITY_ANDROID
        currentActivity.Call("ConsumeProduct", data);
#endif
    }

    public static void U3D_OnConsumeProductCompleted(string data) {
        Debug.Log("U3D_OnConsumeProductCompleted");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Ex_Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["OnConsumeProductCompleted"];
            fun.call(data);
        }
        else {
            Debug.LogError("U3D_OnConsumeProductCompleted no find lua Channel");
        }
    }

    public static void Logout() {
#if UNITY_ANDROID
        currentActivity.Call("Logout");
#endif
    }

    public static void SubmitRoleData(string data) {
#if !UNITY_EDITOR && UNITY_ANDROID
        currentActivity.Call("SubmitRoleData", data);
#endif
    }

    public static void AliBuy(string transactionId, string orderInfo) {
        Debug.Log("AliBuy:transactionId" + transactionId);
        Debug.Log("AliBuy:orderInfo" + orderInfo);
#if UNITY_ANDROID
        //currentActivity.Call("AliPay", transactionId, orderInfo);
        //新版sdk不需要订单号
        currentALiActivity.Call("AliPay", transactionId, orderInfo);
#endif
    }

    public static void InitAppStorePay() {
#if UNITY_IOS
        U3D_InitAppStorePay();
#endif
    }

    public static void AppStorePay(string transactionId, string productId, string extData) {
#if UNITY_IOS
        U3D_AppStorePay(transactionId, productId, extData);
#endif
    }

    public static void OnPaySuccess(string transactionId) {
#if UNITY_IOS
        U3D_FinishAppStorePay(transactionId);
#endif
    }

    public static void RestorePay() {
#if UNITY_IOS
        U3D_RestorePay();
#endif
    }

    public static void ReqWXOpenId() {
#if UNITY_ANDROID
        currentActivity.Call("ReqWXOpenId");
#endif
    }

    void U3D_OnGetWXCode(string result) {
        Debug.Log("U3D_OnGetWXCode");
        LuaTable Channel = LuaMgr.Inst.GetGlobalTable("Channel");
        if (Channel != null) {
            LuaFunction fun = (LuaFunction) Channel["onGetWXCode"];
            fun.call(result);
        }
        else {
            Debug.LogError("U3D_OnGetWXCode no find lua Channel");
        }
    }

    public static void ShareImage(GameObject go, string url, string tile, string description, int scene) {
        Texture2D backgroundTex = null;
        var camera = go.GetComponentInChildren<Camera>();
        if (camera != null && camera.targetTexture != null) {
            var renderTexture = camera.targetTexture;
            int width = renderTexture.width;
            int height = renderTexture.height;
            backgroundTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            backgroundTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            backgroundTex.Apply();
            RenderTexture.active = currentActiveRT;

            Texture2D texSave = new Texture2D(backgroundTex.width, backgroundTex.height);
            texSave.SetPixels(backgroundTex.GetPixels());

            texSave.Apply();
            var bytes = texSave.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, "share.png");
            System.IO.File.WriteAllBytes(path, bytes);
        }
        else {
            return;
        }

#if UNITY_IOS
        if (IsUseWXShare())
        {
            //U3D_ShareImage(url, tile, description, scene);
        }
        else
        {
            //qqShareImage(url, tile, description, scene);
        }
#else
        if (IsUseWXShare()) {
            currentActivity.Call("ShareImage", url, tile, description, scene);
        }
        else {
            currentActivity.Call("QQShareImage", url, tile, description, scene);
        }
#endif
    }

    #region 今日头条接入

    public static void TouTiaoRegister(int accountId) {
        Debug.Log("c#头条注册");
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("TouTiaoRegister",accountId);
#endif
    }

    public static void TouTiaoActive() {
        Debug.Log("c#头条激活");
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("TouTiaoActive");
#endif
    }

    public static void TouTiaoPay(string goodtype, string goodname, string goodId, int num, string payChannel, string moneyType, bool res, int payCost) {
        Debug.Log("c#头条发送支付：" + res.ToString());
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("TouTiaoPay",goodtype, goodname, goodId, num, payChannel, moneyType, res, payCost);
#endif
    }

    public static void TouTiaoImportantAction(string actionName, string actionValue) {
        Debug.Log("c#头条重要行为：" + actionName + actionValue);
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("TouTiaoImportantAction", actionName, actionValue);
#endif
    }

    #endregion

    #region 波克SDK接入

    public static void BokeRegister(int accountId) {
        Debug.Log("c#波克注册");
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("BokeRegister",accountId);
#endif
    }

    public static void BokeLoad(int accountId) {
        Debug.Log("c#波克登录");
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("BokeLoad",accountId);
#endif
    }

    public static void BokePay(int accountId, int payCost, int infullType, string orderNo) {
        Debug.Log("c#波克发送支付：" + accountId + payCost + infullType + orderNo);
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("BokePay",accountId, payCost, infullType, orderNo);
#endif
    }

    public static void BokeImportantAction(int accountId, string actionName, string actionValue) {
        Debug.Log("c#波克重要行为：" + accountId + actionName + actionValue);
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("BokeImportantAction",accountId, actionName, actionValue);
#endif
    }

    #endregion

    void FromAndroid(string content) {
        Debug.Log("来自安卓的log-->" + content);
    }


    public static void OpenURLInApplication(string url, string para = "") {
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("OpenURL", url, para);
#else
        Application.OpenURL(url);
#endif
    }

    public static void InstallApp(string filePath) {
#if UNITY_ANDROID && !UNITY_EDITOR
        currentActivity.Call("InstallApp", filePath);
#endif
    }

    void OnWebviewDestroy(string para) {
        Debug.Log("OnWebviewDestroy: " + para);
    }

#if (!UNITY_EDITOR&&UNITY_ANDROID)
    [DllImport("NativeLib")]
    public static extern int ReadAssetsBytes(string name, ref IntPtr ptr);

    [DllImport("NativeLib")]
    public static extern void ReleaseBytes(IntPtr ptr);
#endif


    #region reunionsdk

    public static void BokeReunionLogin() {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionLogin");
#endif
    }

    void BokeReunionLogin_onSuccess(string authCode) { }

    // extendJs.errorCode : int
    // extendJs.errorMsg : string
    void BokeReunionLogin_onFailed(string extendJs) { }

    public static void BokeReunionPay(string orderInfo) {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionPay", orderInfo);
#endif
    }

    void BokeReunionPay_onSuccess(string orderInfo) { }

    void BokeReunionPay_onDealing(string orderInfo) { }

    // extendJs.orderInfo : string
    // extendJs.errorCode : int
    // extendJs.msg : string
    void BokeReunionPay_onError(string extendJs) { }

    void BokeReunionPay_onCancel(string orderInfo) { }

    public static void BokeReunionLoginOut() {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionLoginOut");
#endif
    }

    void BokeReunionLoginOut_onSuccess() { }

    void BokeReunionLoginOut_onFailed() { }

    public static void BokeReunionExit() {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionExit");
#endif
    }

    void BokeReunionExit_onConfirm() { }

    void BokeReunionExit_onCancel() { }

    public static void BokeReunionReportGameRoleInfo(string data) {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionReportGameRoleInfo", data);
#endif
    }

    void BokeReunionReportGameRoleInfo_onSuccess() { }

    void BokeReunionReportGameRoleInfo_onFailed(string errorMsg) { }

    public static void BokeReunionVerifiedInfo() {
#if FakeAndroid || (UNITY_ANDROID && !UNITY_EDITOR)
        currentActivity.Call("BokeReunionVerifiedInfo");
#endif
    }

    void BokeReunionVerifiedInfo_onSuccess(string message) { }

    void BokeReunionVerifiedInfo_onFailed(string errorMsg) { }

    #endregion
}