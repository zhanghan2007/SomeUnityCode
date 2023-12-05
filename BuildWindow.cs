using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using UpdateInfo = UpdateDownload.UpdateInfo;

public class BuildWindow : BaseNgToolWindow, UnityEditor.Build.IPostprocessBuild {
    public class Channel {
        public int channelType;
        public string name;
        public int channel;
        public int id;
        public int infullType;
        public string appId = "0";
    }

    public enum ChannelType {
        [EnumLabel("官方标准版")]
        Offical = 0,

        [EnumLabel("TapTap")]
        TapTap = 1,

        [EnumLabel("IOS")]
        IOS = 2,

        [EnumLabel("头条投放")]
        TouTiao = 3,

        [EnumLabel("OPPO")]
        OPPO = 4,

        [EnumLabel("vivo")]
        vivo = 5,

        [EnumLabel("B站")]
        BiliBili = 6,

        [EnumLabel("小米")]
        XiaoMi = 7,

        [EnumLabel("华为")]
        HuaWei = 8,

        [EnumLabel("4399")]
        SiSanJiuJiu = 9,

        [EnumLabel("好游快爆")]
        HaoYouKuaiBao = 10,

        [EnumLabel("波克聚合渠道")]
        BokeReunion = 11
    }

    public enum TAType {
        [EnumLabel("关闭")]
        Off,

        [EnumLabel("测试版")]
        Beta,

        [EnumLabel("正式版")]
        Official
    }
    
    public enum BIAnalysisType {
        [EnumLabel("关闭")]
        Off,

        [EnumLabel("开启")]
        On
    }

    static BuildWindow instance;

    private GUIStyle s;

    private string oldAppVer;
    private string oldResVer;
    private Vector3 resV = Vector3.zero;
    private Vector3 appV = Vector3.zero;
    static string specificFolder = "";
    private List<string> mutiReleaseCmd = new List<string>();

    [EnumLabel("服务器")]
    public UpdateDownload.ServerType serverType;

    [EnumLabel("热更类型")]
    public UpdateDownload.DownloadType downloadType;

    [EnumLabel("渠道")]
    public ChannelType channelType;

    #region 参数体

    public bool channelType0;
    public bool channelType1;
    public bool channelType2;
    public bool channelType3;
    public bool channelType4;
    public bool channelType5;
    public bool channelType6;
    public bool channelType7;
    public bool channelType8;
    public bool channelType9;

    public bool channelType10;
    public bool channelType11;
    public bool channelType12;
    public bool channelType13;
    public bool channelType14;
    public bool channelType15;
    public bool channelType16;
    public bool channelType17;
    public bool channelType18;
    public bool channelType19;

    public bool channelType20;
    public bool channelType21;
    public bool channelType22;
    public bool channelType23;
    public bool channelType24;
    public bool channelType25;
    public bool channelType26;
    public bool channelType27;
    public bool channelType28;
    public bool channelType29;

    #endregion

    public SerializedObject obj;
    public BuildTarget buildTarget = (BuildTarget) (-2);
    public BuildTargetGroup buildTargetGroup;
    public string versionPath;
    public bool forceRefreshResource;

    public bool autoIncreasesAppVersion;
    public bool autoIncreasesResVersion;
    public bool autoIncreasesVersion;
    public bool refreshBundleTag;
    public bool refreshFBXNameTag;

    public bool addBigResVersion;
    public bool addBigAppVersion;
    public bool addBigVersion;

    public bool useDevTool;
    public bool testPay;
    public bool socketEncrypt;
    public bool autoSymbols;
    public bool devProfileBuild;
    public bool buildZipData;
    public bool rebuildBundleName;
    public bool sdkLogin;
    public bool protection;
    public bool useUwa;

    [EnumLabel("数数数据采集")]
    public TAType taType;
    
    [EnumLabel("BI数据采集")]
    public BIAnalysisType bIAnalysisType;

    public UpdateInfo updateInfo;

    [MenuItem("aguaTool/BuildWindow")]
    static void Init() {
        if (instance == null)
            instance = EditorWindow.GetWindow(typeof(BuildWindow)) as BuildWindow;
        instance.minSize = new Vector2(800f, 70f);
        instance.titleContent = EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSettings");
        instance.titleContent.text = " agua's Tool";
        instance.Show();
    }

    protected void OnGUI() {
        s = new GUIStyle();
        s.fixedWidth = 170f;
        obj = new SerializedObject(this);
        obj.Update();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("@agua 2018", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(17f);


        BuildTool();

//        if (GUI.changed)
//            obj.ApplyModifiedProperties();
    }

//打包
    bool CheckPlatform() {
#if UNITY_STANDALONE||UNITY_STANDALONE_WIN
        specificFolder = "Pc";
        buildTargetGroup = BuildTargetGroup.Standalone;
        buildTarget = BuildTarget.StandaloneWindows64;
#elif UNITY_ANDROID
        specificFolder = "Android";
        buildTargetGroup = BuildTargetGroup.Android;
        buildTarget = BuildTarget.Android;
#elif UNITY_IOS
        specificFolder = "IOS";
        buildTargetGroup = BuildTargetGroup.iOS;
        buildTarget = BuildTarget.iOS;
#else
		buildTarget = (BuildTarget)(-2);
#endif
        return buildTarget.GetHashCode() != -2;
    }

    void SetSpriteAtlasLink() {
        BuildBundles.SetSpriteAtlasLink();
    }

    void SVNTool() {
        GUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button("更新SVN", GetBtnFieldWidth)) {
            USESVNUpdate.SVNUpdate();
        }

        if (GUILayout.Button("提交SVN", GetBtnFieldWidth)) {
            USESVNUpdate.SVNCommit();
        }

        GUILayout.EndHorizontal();
    }

    void AndroidStudioTool() {
        GUILayout.BeginHorizontal();
        var path = EditorPrefs.GetString("选择AndroidStudioPath", "尚未指定AndroidStudioPath");
        EditorGUILayout.LabelField("AndroidStudioPath --> ", string.IsNullOrEmpty(path) ? "尚未指定AndroidStudioPath" : path);
        EditorGUILayout.Space();
        if (GUILayout.Button("选择AndroidStudio路径", GetBtnFieldWidth)) {
            var str = EditorUtility.OpenFilePanelWithFilters("选择AndroidStudio路径", Path.Combine(Application.dataPath), new string[] {"EXE", "exe"});
            EditorPrefs.SetString("选择AndroidStudioPath", str);
        }

        GUILayout.EndHorizontal();
    }

    protected void BuildTool() {
        EditorGUILayout.Space();
        GUIStyle style = new GUIStyle();
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 30;
        style.normal.textColor = Color.red;


        var check = CheckPlatform();
        GUILayout.BeginHorizontal();
        GUILayout.Space(20f);
        GUILayout.Label("当前平台 : " + (buildTarget.GetHashCode() == (-2) ? "未知平台: " : buildTarget.ToString()), style);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUIToolFun(SVNTool);
        if (buildTarget == BuildTarget.Android) {
            GUIToolFun(AndroidStudioTool);
        }

        if (!check)
            return;
        GUILayout.BeginHorizontal();
        int serverValueIndex = GetInt("serverValueIndex", 0);
        obj.FindProperty("serverType").enumValueIndex = serverValueIndex;
        EditorGUILayout.PropertyField(obj.FindProperty("serverType"), new GUIContent("服务器: "));
        SetInt("serverValueIndex", obj.FindProperty("serverType").enumValueIndex);
        EditorGUILayout.Space();

        int downloadValueIndex = GetInt("downloadValueIndex", 0);
        obj.FindProperty("downloadType").enumValueIndex = downloadValueIndex;
        EditorGUILayout.PropertyField(obj.FindProperty("downloadType"), new GUIContent("热更类型: "));
        SetInt("downloadValueIndex", obj.FindProperty("downloadType").enumValueIndex);
        EditorGUILayout.Space();

        if (buildTarget == BuildTarget.Android) {
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            DrawFolderContent("多渠道发布", () => {
                var channelProperty = obj.FindProperty("channelType");
                mutiReleaseCmd.Clear();

                int alreadyIndex = 0;
                bool close = false;
                for (int i = 0; i < channelProperty.enumNames.Length; i++) {
                    var name = channelProperty.enumNames[i];
                    if (name == ChannelType.IOS.ToString())
                        continue;
                    var toggle = obj.FindProperty("channelType" + i);
                    if (toggle == null)
                        break;
                    var att = typeof(ChannelType).GetField(name).GetCustomAttributes(typeof(EnumLabelAttribute), false)[0] as EnumLabelAttribute;
                    var label = att.label;
                    var v = GetBool("channelType" + i, false);
                    toggle.boolValue = v;
                    if (alreadyIndex % 4 == 0) {
                        GUILayout.BeginHorizontal();
                        close = true;
                    }

                    EditorGUILayout.PropertyField(toggle, new GUIContent(label), GUILayout.Width(250f));
                    if (alreadyIndex++ % 4 == 3 || channelProperty.enumNames.Length - 1 == i) {
                        GUILayout.EndHorizontal();
                        close = false;
                    }

                    SetBool("channelType" + i, toggle.boolValue);
                    if (toggle.boolValue) {
                        string cmd = "start gradlew.bat assemble{0}Release";
                        if (name == ChannelType.Offical.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Saiyun"));
                        else if (name == ChannelType.TapTap.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Taptap"));
                        else if (name == ChannelType.TouTiao.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Toutiao"));
                        else if (name == ChannelType.OPPO.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "OPPO"));
                        else if (name == ChannelType.vivo.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "vivo"));
                        else if (name == ChannelType.BiliBili.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Bilibili"));
                        else if (name == ChannelType.XiaoMi.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Xiaomi"));
                        else if (name == ChannelType.HuaWei.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Huawei"));
                        else if (name == ChannelType.SiSanJiuJiu.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Sisanjiujiu"));
                        else if (name == ChannelType.HaoYouKuaiBao.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "HaoYouKuaiBao"));
                        else if (name == ChannelType.BokeReunion.ToString())
                            mutiReleaseCmd.Add(string.Format(cmd, "Bokereunion"));
                    }
                }

                if (close) {
                    GUILayout.EndHorizontal();
                    close = false;
                }
            }, 7f);
        }
        else {
            int channelValueIndex = GetInt("channelValueIndex", 0);
            obj.FindProperty("channelType").enumValueIndex = channelValueIndex;
            EditorGUILayout.PropertyField(obj.FindProperty("channelType"), new GUIContent("发布渠道: "));
            SetInt("channelValueIndex", obj.FindProperty("channelType").enumValueIndex);
            GUILayout.EndHorizontal();
        }


        GUILayout.BeginHorizontal();
        ExecuteSettingBool("Profile调试版本", "devProfileBuild");

        if (GetBool("rebuildBundleName", true) != BuildBundles.RebuildBundleName)
            SetBool("rebuildBundleName", BuildBundles.RebuildBundleName);
        EditorGUI.BeginChangeCheck();
        ExecuteSettingBool("刷新BundleName", "rebuildBundleName");
        if (EditorGUI.EndChangeCheck())
            BuildBundles.RebuildBundleName = GetBool("rebuildBundleName", true);

        ExecuteSettingBool("sdk登录", "sdkLogin", false);
        ExecuteSettingBool("测试工具", "useDevTool", false);
        ExecuteSettingBool("模拟支付", "testPay", false);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        // ExecuteSettingBool("测试工具", "devTool");
        ExecuteSettingBool("自动宏", "autoSymbols", true);
        ExecuteSettingBool("更新资源包", "forceRefreshResource", true);
        ExecuteSettingBool("更新Zip", "buildZipData", true);
        ExecuteSettingBool("强制过期", "protection", false);
        ExecuteSettingBool("网络加密", "socketEncrypt", true);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        ExecuteSettingBool("版本号递增", "autoIncreasesAppVersion");
        ExecuteSettingBool("资源号递增", "autoIncreasesResVersion", true);

        ExecuteSettingBool("升大版本号", "addBigAppVersion");
        ExecuteSettingBool("升大资源号", "addBigResVersion");
        ExecuteSettingBool("使用uwa", "useUwa");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        int taValueIndex = GetInt("taValueIndex", 0);
        obj.FindProperty("taType").enumValueIndex = taValueIndex;
        EditorGUILayout.PropertyField(obj.FindProperty("taType"), new GUIContent("数数数据采集: "), GUILayout.Width(370f));
        SetInt("taValueIndex", obj.FindProperty("taType").enumValueIndex);
        GUILayout.Space(33);
        int bIAnalysisIndex = GetInt("bIAnalysisIndex", 0);
        obj.FindProperty("bIAnalysisType").enumValueIndex = bIAnalysisIndex;
        EditorGUILayout.PropertyField(obj.FindProperty("bIAnalysisType"), new GUIContent("BI数据采集: "), GUILayout.Width(370f));
        SetInt("bIAnalysisIndex", obj.FindProperty("bIAnalysisType").enumValueIndex);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();


        if (GUILayout.Button("打开bundle路径", GetBtnFieldWidth)) {
            OpenDirectory(Application.dataPath.Replace("Assets", "Patcher/"));
        }

        if (GUILayout.Button("更新bundle包", GetBtnFieldWidth)) {
            UpdateBundle();
        }

        if (GUILayout.Button("更新图集和bundle包", GetBtnFieldWidth)) {
            SetSpriteAtlasLink();
            UpdateBundle();
        }

        if (GUILayout.Button("SetAltasValue", GetBtnFieldWidth)) {
            BuildBundles.SetAltasValue();
        }

        if (GUILayout.Button("制作data.zip", GetBtnFieldWidth)) {
            ZipData();
        }

        if (GUILayout.Button("解压data", GetBtnFieldWidth)) {
            var targetFile = Selection.activeObject;
            var path = Path.GetFullPath(AssetDatabase.GetAssetPath(targetFile));
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader br = new BinaryReader(fs)) {
                    var _version = br.ReadString();
                    var folder = Path.Combine(Application.dataPath, "TempDataPath/" + _version);
                    UnityUtil.DeleteFolder(folder);
                    Directory.CreateDirectory(folder);
                    var size = int.Parse(br.ReadString());
                    fs.Seek(-size, SeekOrigin.End);
                    var buf = br.ReadBytes(size);

                    using (MemoryStream ms = new MemoryStream(buf)) {
                        var dataBundle = AssetBundle.LoadFromStream(ms);
                        var files = dataBundle.GetAllAssetNames();
                        foreach (var file in files) {
                            var filePath = Path.Combine(folder, file);
                            var value = dataBundle.LoadAsset<TextAsset>(file);
                            File.WriteAllText(filePath + ".txt", value.text);
                        }

                        // Debug.Log(dataBundle.LoadAsset<TextAsset>("allroomtree"));

                        dataBundle.Unload(true);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();

        if (GUILayout.Button("清理AssetbundleName", GetBtnFieldWidth)) {
            var path = Application.dataPath;
            // var path = Path.Combine(Application.dataPath, "delImages");
            BuildBundles.ClearAssetBundelName(path);
            AssetDatabase.Refresh();
        }

        if (GUILayout.Button("热更脚本", GetBtnFieldWidth)) {
            ResetData();
            SetData(false, true);
            BuildBundles.RebuildScriptBundle();
        }

        if (GUILayout.Button("GetMD5", GetBtnFieldWidth)) {
            GetMD5();
        }

        if (GUILayout.Button("测试", GetBtnFieldWidth)) {
            Test();
        }

        if (GUILayout.Button("检查Image资源", GetBtnFieldWidth)) {
            EditorUtility.DisplayProgressBar("检查Image资源", "检查Image资源", 0);
            var list = new List<string>();
            GetFilesDeeply(Path.Combine(Application.dataPath, "Images"), ref list);
            for (int i = list.Count - 1; i >= 0; i--) {
                var fileName = Path.GetFileName(list[i]);
                if (fileName.EndsWith("meta") || fileName.StartsWith(".")) {
                    list.RemoveAt(i);
                }
            }

            int count = 0;
            foreach (var asset in list) {
                EditorUtility.DisplayProgressBar("检查Image资源", Path.GetFileNameWithoutExtension(asset), (float) ++count / (float) list.Count);
                var importer = AssetImporter.GetAtPath(NgTool.ToUnityRelativePath(asset));
                if (importer == null)
                    continue;
                var textureImporter = importer as TextureImporter;
                if (textureImporter == null)
                    continue;
                if (textureImporter.textureType != TextureImporterType.Sprite) {
                    Debug.Log(Path.GetFileName(asset) + "-->Image type Error: " + textureImporter.textureType, AssetDatabase.LoadAssetAtPath<Object>(NgTool.ToUnityRelativePath(asset)));
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.SaveAndReimport();
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log("检查Image资源完成 共: " + list.Count);
        }

        if (GUILayout.Button("获取发布信息", GetBtnFieldWidth)) {
            GetApkInfo();
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();


        if (GUILayout.Button("检查热更包大小", GetBtnFieldWidth)) {
            string p1 = "";

            string p2 = "";
            foreach (var VARIABLE in Selection.objects) {
                var p = AssetDatabase.GetAssetPath(VARIABLE);
                var fp = Path.GetFullPath(p);
                if (fp.EndsWith(".bytes")) {
                    if (string.IsNullOrEmpty(p1)) {
                        p1 = fp;
                    }
                    else if (string.IsNullOrEmpty(p2)) {
                        p2 = fp;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2) || p1 == p2) {
                Debug.LogError("请选择两个不同的filelist.bytes文件");
                return;
            }

            if (!File.Exists(p1)) {
                Debug.LogError("找不到文件: " + p1);
                return;
            }

            if (!File.Exists(p2)) {
                Debug.LogError("找不到文件: " + p2);
                return;
            }

            Stream s1 = new MemoryStream(File.ReadAllBytes(p1), false);
            Stream s2 = new MemoryStream(File.ReadAllBytes(p2), false);
            CheckHotFixSize(s1, s2);
            s1.Dispose();
            s1.Close();
            s2.Dispose();
            s2.Close();
        }

        if (GUILayout.Button("制作热更整包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResTotal();
        }

        if (GUILayout.Button("制作本地差异包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResAuto(UpdateDownload.ServerType.Internal);
        }

        if (GUILayout.Button("制作测试服差异包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResAuto(UpdateDownload.ServerType.Test);
        }

        if (GUILayout.Button("制作先行服差异包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResAuto(UpdateDownload.ServerType.Offical_Test);
        }

        if (GUILayout.Button("制作正式服差异包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResAuto(UpdateDownload.ServerType.Offical);
        }

        if (GUILayout.Button("制作内部体验服差异包", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
            ZipResAuto(UpdateDownload.ServerType.Internal_Test);
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("更新filelist下载地址", GetBtnFieldWidth)) {
            UpdateLocalFilelistDownloadPath();
        }

        GUILayout.Space(310 / 2);

        if (GUILayout.Button("制作本地脚本热更包", GetBtnFieldWidth)) {
            ZipScriptAuto(UpdateDownload.ServerType.Internal);
        }

        if (GUILayout.Button("制作测试服脚本热更包", GetBtnFieldWidth)) {
            ZipScriptAuto(UpdateDownload.ServerType.Test);
        }

        if (GUILayout.Button("制作先行服脚本热更包", GetBtnFieldWidth)) {
            ZipScriptAuto(UpdateDownload.ServerType.Offical_Test);
        }

        if (GUILayout.Button("制作正式服脚本热更包", GetBtnFieldWidth)) {
            ZipScriptAuto(UpdateDownload.ServerType.Offical);
        }

        if (GUILayout.Button("制作内部体验服脚本热更包", GetBtnFieldWidth)) {
            ZipScriptAuto(UpdateDownload.ServerType.Internal_Test);
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("优化Bundle资源", GetBtnFieldWidth)) {
            OpBundle();
        }

        if (GUILayout.Button("停止Bundle资源", GetBtnFieldWidth)) {
            StopOpBundle();
        }

        if (GUILayout.Button("检查特效资源大小", GetBtnFieldWidth)) {
            CheckEffRes();
        }

        if (GUILayout.Button("停止特效资源", GetBtnFieldWidth)) {
            StopCheckEffRes();
        }

        if (GUILayout.Button("替换DefaultMat", GetBtnFieldWidth)) {
            OpDefaultMat();
        }

        if (GUILayout.Button("停止替换DefaultMat", GetBtnFieldWidth)) {
            EditorCoroutineRunner.Clear();
            EditorUtility.ClearProgressBar();
        }

        if (GUILayout.Button("查找Build下引用", GetBtnFieldWidth)) {
            FindRefrence();
        }

        if (GUILayout.Button("停止查找", GetBtnFieldWidth)) {
            EditorCoroutineRunner.Clear();
            EditorUtility.ClearProgressBar();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (GUILayout.Button("发布", GetBtnFieldWidth)) {
            if (!EditorUtility.DisplayDialog("发布", string.Format("确认发布"), "是", "否"))
                return;
            if (buildTarget == BuildTarget.Android && mutiReleaseCmd.Count == 0) {
                Debug.Log("先选择发布渠道!");
                return;
            }

            var protectionFilePath = Path.Combine(Application.dataPath, "Resources/protection.txt");
            UnityUtil.DeleteFile(protectionFilePath);
            if (GetBoolValue("protection")) {
                using (FileStream fileStream = new FileStream(protectionFilePath, FileMode.Create)) {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream)) {
                        var buildTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        streamWriter.Write(buildTime);
                    }
                }
            }

            var appConfigPath = Path.Combine(Application.streamingAssetsPath, "AppConfig.json");
            var appConfig = JSON.JsonDecodeFromFile(appConfigPath);
            appConfig["sdkLogin"] = new JsonData(GetBoolValue("sdkLogin"));
            appConfig["useDevTool"] = new JsonData(GetBoolValue("useDevTool"));
            appConfig["testPay"] = new JsonData(GetBoolValue("testPay"));
            appConfig["socketEncrypt"] = new JsonData(GetBoolValue("socketEncrypt"));
            appConfig["useUwa"] = new JsonData(GetBoolValue("useUwa"));

            appConfig["taType"] = new JsonData(GetInt("taValueIndex", 0));
            appConfig["bIAnalysisType"] = new JsonData(GetInt("bIAnalysisIndex", 0));
            //安卓直接走多渠道发布
            if (buildTarget == BuildTarget.Android) {
                //上增Android BuildVersion
                var appBuildVersion = appConfig["androidBuildVersion"].ToInt();
                appConfig["androidBuildVersion"] = new JsonData(++appBuildVersion);
            }

            JSON.WriteToFile(appConfig, appConfigPath);
            AssetDatabase.Refresh();

            if (GetBoolValue("autoSymbols"))
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, GetDefineSymbols());

            if (GetBoolValue("devProfileBuild")) {
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.connectProfiler = true;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = true;
                EditorUserBuildSettings.allowDebugging = true;
            }
            else {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
                EditorUserBuildSettings.allowDebugging = false;
            }

            MutiPlatformSetting();
            SetChannelInfo();

            if (GetBoolValue("forceRefreshResource")) {
                SetSpriteAtlasLink();
                UpdateBundle();
            }
            else {
                ResetData();
                EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;
            }

            SetData(true, false);

            List<string> scenes = new List<string>() {"Assets/Scenes/LaunchScene.unity", "Assets/Scenes/LoginScene.unity"};

            var dirpath = Application.dataPath.Replace("Assets", "MutiPlatfotmRes/Android/cat/");
            string cmd = "call gradlew.bat clean\n";
            for (int i = 0; i < mutiReleaseCmd.Count; i++) {
                cmd += mutiReleaseCmd[i];
                cmd += "\n";
            }

            var fileName = dirpath + "release.bat";
            File.WriteAllText(fileName, cmd);

            var exportPath = GetExportPath();
            if (Directory.Exists(exportPath))
                Directory.Delete(exportPath, true);

#if UNITY_IOS||UNITY_ANDROID
            BuildPipeline.BuildPlayer(scenes.ToArray(), exportPath, buildTarget, BuildOptions.None);
#else
            var channelContent = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "channel.json"));
            var channelInfo = JsonUtility.FromJson<Channel>(channelContent);

            var channelTypeName = ((EnumLabelAttribute[]) typeof(ChannelType)
                .GetField(((ChannelType) channelInfo.channelType).ToString())
                .GetCustomAttributes(typeof(EnumLabelAttribute), false))[0].label;
            var serverTypeName = ((EnumLabelAttribute[]) typeof(UpdateDownload.ServerType)
                .GetField((updateInfo.serverType).ToString())
                .GetCustomAttributes(typeof(EnumLabelAttribute), false))[0].label;
            var downloadTypeName = ((EnumLabelAttribute[]) typeof(UpdateDownload.DownloadType)
                .GetField((updateInfo.downloadType).ToString())
                .GetCustomAttributes(typeof(EnumLabelAttribute), false))[0].label;
            var trueProductName = PlayerSettings.productName + "_" + channelTypeName + "_" + serverTypeName + "_" +
                                  downloadTypeName + "_" + updateInfo.resVer;

            var tarpath = exportPath + "/" + trueProductName + ".exe";
            BuildPipeline.BuildPlayer(scenes.ToArray(), tarpath, buildTarget, UnityEditor.BuildOptions.None);
#endif
        }
    }

    void ResetData() {
        versionPath = Path.Combine(Application.streamingAssetsPath, UpdateDownload.UpdateFilename);
        if (File.Exists(versionPath)) {
            string txt = File.ReadAllText(versionPath);
            updateInfo = JsonUtility.FromJson<UpdateInfo>(txt);
        }
        else
            updateInfo = new UpdateInfo();

        oldResVer = updateInfo.resVer;
        var resS = updateInfo.resVer.Split('.');
        if (resS.Length != 3) {
            updateInfo.resVer = oldResVer;
            resS = updateInfo.resVer.Split('.');
        }

        oldAppVer = updateInfo.appVer;
        var appS = updateInfo.appVer.Split('.');
        if (appS.Length != 3) {
            updateInfo.appVer = oldAppVer;
            appS = updateInfo.appVer.Split('.');
        }

        resV = new Vector3(int.Parse(resS[0]), int.Parse(resS[1]), int.Parse(resS[2]));
        appV = new Vector3(int.Parse(appS[0]), int.Parse(appS[1]), int.Parse(appS[2]));
    }

    void SetData(bool ver, bool res) {
        if (res) {
            var newResV = updateInfo.resVer;
            if (GetBoolValue("addBigResVersion"))
                newResV = string.Format("{0}.{1}.{2}", resV.x + 1, 0, 0);
            else if (GetBoolValue("autoIncreasesResVersion"))
                newResV = string.Format("{0}.{1}.{2}", resV.x, resV.y, resV.z + 1);
            updateInfo.resVer = newResV;
        }

        if (ver) {
            var newAppV = updateInfo.appVer;
            if (GetBoolValue("addBigAppVersion"))
                newAppV = string.Format("{0}.{1}.{2}", appV.x + 1, 0, 0);
            else if (GetBoolValue("autoIncreasesAppVersion"))
                newAppV = string.Format("{0}.{1}.{2}", appV.x, appV.y, appV.z + 1);
            updateInfo.appVer = newAppV;
        }

        updateInfo.serverType = (UpdateDownload.ServerType) obj.FindProperty("serverType").enumValueIndex;
        updateInfo.downloadType = (UpdateDownload.DownloadType) obj.FindProperty("downloadType").enumValueIndex;
        File.WriteAllText(versionPath, JsonUtility.ToJson(updateInfo));
        AssetDatabase.Refresh();
    }

    void UpdateBundle() {
        if (GetBoolValue("buildZipData")) {
            ZipData();
        }

        ResetData();
        SetData(false, true);
        BuildBundles.BuildAll();
        Debug.Log("资源打包完成 资源号: " + updateInfo.resVer);
    }

    string GetDefineSymbols() {
        var defineSymbols = "";
        defineSymbols += "LUA_5_3;";
        defineSymbols += "UNITY_POST_PROCESSING_STACK_V1;";
        if (GetBoolValue("useUwa")) {
            defineSymbols += "USE_UWA;";
        }

        return defineSymbols;
    }

    void ExecuteSettingBool(string disName, string name, bool defaultValue = false) {
        bool cache = GetBool(name, defaultValue);
        obj.FindProperty(name).boolValue = cache;
        EditorGUILayout.PropertyField(obj.FindProperty(name), new GUIContent(disName), GUILayout.Width(200f));
        SetBool(name, obj.FindProperty(name).boolValue);
    }

    public class BundleFileInfo {
        public string assetName;
        public string assetType;
        public string ext;
        public string relativePath;
        public string parObjRelativePath;
        public int refCount;

        public override string ToString() {
            return string.Format("assetName:{0}, assetType:{1}, ext:{2}, relativePath:{3}, refCount:{4}, parObjRelativePath:{5}", assetName, assetType, ext, relativePath, refCount,
                parObjRelativePath);
        }
    }

    public static void GetFilesDeeply(string folderPath, ref List<string> files) {
        NgTool.GetFilesDeeply(folderPath, ref files);
    }

    private static string GetAssetBundleFilePath(string filename) {
        string outPath = GetAssetBundleOutputPath();
        return Path.Combine(outPath, filename);
    }

    private static string GetAssetBundleOutputPath() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
            return Application.dataPath.Replace("Assets", "AssetBundle_Android");
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
            return Application.dataPath.Replace("Assets", "AssetBundle_iOS");
        }

        return Application.dataPath.Replace("Assets", "AssetBundle_PC");
    }

    public static string GetMD5HashFromFile(byte[] buf) {
        return UnityUtil.GetMD5(buf);
    }

    void Test() { }

    void SetChannelInfo() {
        if (buildTarget != BuildTarget.Android) {
            Channel c = new Channel();
            c.channelType = obj.FindProperty("channelType").enumValueIndex;

            var channelTypeName = ((EnumLabelAttribute[]) typeof(ChannelType).GetField(((ChannelType) c.channelType).ToString()).GetCustomAttributes(typeof(EnumLabelAttribute), false))[0].label;
            c.name = channelTypeName;
            if (c.channelType == ChannelType.Offical.GetHashCode()) {
                c.id = 80001292;
            }
            else if (c.channelType == ChannelType.TapTap.GetHashCode()) {
                c.id = 80001328;
            }
            else if (c.channelType == ChannelType.IOS.GetHashCode()) {
                c.id = 80001329;
                c.infullType = 14;
                c.appId = "com.saiyun.cat";
            }

            c.channel = c.id;
            var jsonText = JsonUtility.ToJson(c);
            System.IO.File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "channel.json"), jsonText);
            AssetDatabase.Refresh();
            Debug.Log("channel 刷新完成");
        }
    }

    //返回当次打包后的版本号
    public static string InitDataInfo() {
        var streamingInfoPath = Path.Combine(Application.streamingAssetsPath, UpdateDownload.dataFileInfo);
        UpdateDownload.DataZipFileInfo streamingInfo = new UpdateDownload.DataZipFileInfo();
        if (File.Exists(streamingInfoPath)) {
            streamingInfo = JsonUtility.FromJson<UpdateDownload.DataZipFileInfo>(File.ReadAllText(streamingInfoPath));
        }

        var version = streamingInfo.version;
        var resS = version.Split('.');
        return resS[0] + "." + resS[1] + "." + (int.Parse(resS[2]) + 1);
    }

    void ZipData() {
        // ZipHelper.ZipCatData();

        var platformName = "";
#if UNITY_ANDROID
        platformName = "android";
#elif UNITY_IOS
        platformName = "ios";
#else
        platformName = "other";
#endif

        var nextVersion = InitDataInfo();
        var datafolder = Path.Combine(Application.dataPath, "Build/Data");
        var sourceFile = new Dictionary<string, string>();
        var check = new Dictionary<string, string>();

        var files = new List<string>();
        GetFilesDeeply(datafolder, ref files);

        foreach (var VARIABLE in files) {
            if (Path.GetExtension(VARIABLE) == ".DS_Store" || Path.GetExtension(VARIABLE) == ".meta")
                continue;
            var zipFilePath = VARIABLE;
            var zipFileName = Path.GetFileNameWithoutExtension(VARIABLE);

            if (check.ContainsKey(zipFileName))
                Debug.LogError("存在同名文件: " + zipFileName);
            else
                check[zipFileName] = "";
            sourceFile[zipFileName] = NgTool.ToUnityRelativePath(zipFilePath);
        }

        var filelistHDir = Path.Combine(Application.dataPath, "FilelistHistory/DataZipCache");
        var tagetDic = Path.Combine(filelistHDir, platformName + "/" + nextVersion);
        if (Directory.Exists(tagetDic))
            Directory.Delete(tagetDic, true);
        Directory.CreateDirectory(tagetDic);

        //bundle名为dataBundle
        AssetBundleBuild scriptBundleBuild = new AssetBundleBuild();
        scriptBundleBuild.assetBundleName = UpdateDownload.dataBundleName;
        scriptBundleBuild.assetNames = new string[sourceFile.Count];
        scriptBundleBuild.addressableNames = new string[sourceFile.Count];

        int i = 0;
        foreach (var pair in sourceFile) {
            scriptBundleBuild.assetNames[i] = pair.Value;
            scriptBundleBuild.addressableNames[i] = pair.Key;
            i++;
        }

        BuildAssetBundleOptions bo = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle;
        BuildPipeline.BuildAssetBundles(tagetDic, new AssetBundleBuild[] {scriptBundleBuild}, bo, EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();

        //索引文件名dataFileInfo
        var dataPath = Path.Combine(tagetDic, UpdateDownload.dataBundleName);
        UpdateDownload.DataZipFileInfo streamingInfo = new UpdateDownload.DataZipFileInfo();
        streamingInfo.version = nextVersion;
        var buf = File.ReadAllBytes(dataPath);
        streamingInfo.md5 = GetMD5HashFromFile(buf);
        streamingInfo.size = buf.Length.ToString();
        var result = JsonUtility.ToJson(streamingInfo);
        File.WriteAllText(Path.Combine(tagetDic, UpdateDownload.dataFileInfo), result);

        streamingInfo.buf = buf;
        using (FileStream fs = new FileStream(Path.Combine(tagetDic, UpdateDownload.dataFilename), FileMode.Create)) {
            using (BinaryWriter br = new BinaryWriter(fs)) {
                br.Write(streamingInfo.version);
                br.Write(streamingInfo.size);
                br.Write(streamingInfo.md5);
                br.Write(streamingInfo.buf);
            }
        }

        File.Copy(Path.Combine(tagetDic, UpdateDownload.dataFilename),
            Path.Combine(Application.streamingAssetsPath, UpdateDownload.dataFilename), true);

        File.Copy(Path.Combine(tagetDic, UpdateDownload.dataFileInfo),
            Path.Combine(Application.streamingAssetsPath, UpdateDownload.dataFileInfo), true);

        AssetDatabase.Refresh();

        //拷贝到统一目录
        var patcherFolder = NgTool.FormatPath(Application.dataPath.Replace("Assets", "Patcher"));
        var patcherDataFolder = Path.Combine(patcherFolder, "dataInfo/" + nextVersion);
        patcherDataFolder = Path.Combine(patcherDataFolder, platformName);
        if (Directory.Exists(patcherDataFolder))
            Directory.Delete(patcherDataFolder, true);
        Directory.CreateDirectory(patcherDataFolder);


        File.Copy(Path.Combine(tagetDic, UpdateDownload.dataFilename),
            Path.Combine(patcherDataFolder, UpdateDownload.dataFilename), true);

        File.Copy(Path.Combine(tagetDic, UpdateDownload.dataFileInfo),
            Path.Combine(patcherDataFolder, UpdateDownload.dataFileInfo), true);

        var dataInfoZipPath = Path.Combine(patcherFolder, "dataInfo.zip");
        var tarZipData = new Dictionary<string, string>();
        tarZipData[Path.Combine(patcherDataFolder, UpdateDownload.dataFilename)] = "dataInfo/" + nextVersion + "/" + platformName;
        tarZipData[Path.Combine(patcherDataFolder, UpdateDownload.dataFileInfo)] = "dataInfo/" + nextVersion + "/" + platformName;
        ZipHelper.Zip(null, tarZipData, dataInfoZipPath);

        Debug.Log("更新配置文件: " + nextVersion);
    }

//多平台设置
    void MutiPlatformSetting() {
        PlayerSettings.companyName = "赛韵网络科技（上海）有限公司";
        PlayerSettings.productName = "猫咪公寓2";
#if UNITY_ANDROID
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
#else
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
#endif
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SetApplicationIdentifier(buildTargetGroup, "com.saiyun.cat");

        // if (GetBoolValue("useUwa"))
        // PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);
        // else
        PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        int[] iconSize = PlayerSettings.GetIconSizesForTargetGroup(buildTargetGroup);
        Texture2D[] textureArray = new Texture2D[iconSize.Length];
        int[] icons = new int[0];
#if UNITY_ANDROID
        icons = new int[] {192, 144, 96, 72, 48, 36};
#elif UNITY_STANDALONE||UNITY_STANDALONE_WIN
        icons = new int[] {1024, 512, 256, 128, 48, 32, 16};
#elif UNITY_IOS
        icons = new int[] {180, 167, 152, 144, 120, 114, 76, 72, 57, 120, 80, 40, 87, 58, 29, 60, 40, 20, 1024};
#endif
        for (int i = 0; i < icons.Length; i++) {
            var icon = string.Format("Assets/Icon/{0}.png", icons[i] + "");
            textureArray[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(icon);
        }

        PlayerSettings.SetIconsForTargetGroup(buildTargetGroup, textureArray);

        PlayerSettings.stripEngineCode = false;

#if UNITY_IOS
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.SetArchitecture(buildTargetGroup, 2);
        PlayerSettings.iOS.appleDeveloperTeamID = "4Z82BPS65A";
        PlayerSettings.iOS.applicationDisplayName = PlayerSettings.productName;
        PlayerSettings.iOS.hideHomeButton = true;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.targetOSVersionString = "9.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
#elif UNITY_ANDROID
//        PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.Center;
//        PlayerSettings.Android.;
//            = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Images/login/bg_loading.png");
#if UNITY_2020_1_OR_NEWER
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.SetApiCompatibilityLevel(buildTargetGroup, ApiCompatibilityLevel.NET_2_0_Subset);
#endif
        var keypath = Application.dataPath.Replace("Assets", "MutiPlatfotmRes/Android/keystore/");
        PlayerSettings.Android.keystoreName = keypath + "cat.keystore";
        PlayerSettings.Android.keystorePass = File.ReadAllText(keypath + "密码.txt");
        PlayerSettings.Android.keyaliasName = "cat";
        PlayerSettings.Android.keyaliasPass = File.ReadAllText(keypath + "密码.txt");
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

//导出路径
    string GetExportPath() {
#if UNITY_EDITOR
        return Application.dataPath.Replace("Assets", "Export/") + (specificFolder == "" ? "" : specificFolder + "/") + updateInfo.appVer + "_" + updateInfo.resVer;
#endif
    }

    bool GetBoolValue(string propertyName) {
        return obj.FindProperty(propertyName).boolValue;
    }

    public int callbackOrder {
        get { return 0; }
    }

    //回调的不是同一个实例
    //不要使用缓存数据 会报空
    public void OnPostprocessBuild(BuildTarget target, string path) {
        ResetData();
        Debug.Log(DateTime.Now + "打包完成 appver: " + updateInfo.appVer + " resver: " + updateInfo.resVer);

        if (target == BuildTarget.Android) {
            var newPath = path + "/../crProject";
            if (Directory.Exists(newPath))
                Directory.Delete(newPath, true);
            Directory.CreateDirectory(newPath);

            var crPojectAssetsPath = Path.Combine(newPath, "src/main/assets");
            var unityLibraryPath = Path.Combine(path, "unityLibrary");
            var unityOutPutAssetsPath = Path.Combine(unityLibraryPath, "src/main/assets");
            copyDirectory(unityOutPutAssetsPath, crPojectAssetsPath, new string[] {".DS_Store"});
            BuildBundles.CopyOutputBundleFolder(Path.Combine(crPojectAssetsPath, "AssetBundle"));

            var configPath = Path.Combine(crPojectAssetsPath, "channel.json");
            if (File.Exists(configPath))
                File.Delete(configPath);

            var launchAndroidStudio = string.Format("\"{0}\" \"{1}\"", NgTool.FormatPath(EditorPrefs.GetString("选择AndroidStudioPath", "")), NgTool.FormatPath(unityLibraryPath));
            File.WriteAllText(Path.Combine(unityLibraryPath, "launchAndroidStudio.bat"), launchAndroidStudio);

            var buildLib = string.Format(@"TIMEOUT /T 90 /NOBREAK
taskkill -f -t -im {0}
call gradlew.bat clean
start gradlew.bat BuildIl2CppTask", "studio64.exe");
            File.WriteAllText(Path.Combine(unityLibraryPath, "buildLib.bat"), buildLib);

            var work = "TIMEOUT /T 1 /NOBREAK\nstart launchAndroidStudio.bat\nstart buildLib.bat";
            File.WriteAllText(Path.Combine(unityLibraryPath, "work.bat"), work);

            var gradlePath = Path.Combine(unityLibraryPath, "build.gradle");
            var gradleFile = File.ReadAllLines(gradlePath).ToList();

            for (int i = gradleFile.Count - 1; i >= 0; i--) {
                var command = gradleFile[i];
                if (command.Contains("ant.move")) {
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + targetDirectory + abi + \"/libil2cpp.so\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniLibs/\" + abi + \"/libil2cpp.so\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + targetDirectory + abi + \"/libmain.so\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniLibs/\" + abi + \"/libmain.so\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + targetDirectory + abi + \"/libunity.so\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniLibs/\" + abi + \"/libunity.so\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + targetDirectory + abi + \"/gamesdk_classes_dex.o\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniLibs/\" + abi + \"/gamesdk_classes_dex.o\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + \"/src/main/jniStaticLibs/\" + abi + \"/baselib.a\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniStaticLibs/\" + abi + \"/baselib.a\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + \"/libs/unity-classes.jar\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/libs/unity-classes.jar\", overwrite: true)");
                    gradleFile.Insert(i + 1,
                        "    ant.copy(file: workingDir + \"/src/main/java/com/unity3d/player/UnityPlayerActivity.java\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/java/com/unity3d/player/UnityPlayerActivity.java\", overwrite: true)");

                    // gradleFile.Insert(i + 1,
                    // "    ant.copy(files: workingDir + targetDirectory + abi/\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniLibs/\" + abi/\", overwrite: true)");
                    // gradleFile.Insert(i + 1,
                    // "    ant.copy(files: workingDir + \"/src/main/jniStaticLibs/\" + abi/\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/jniStaticLibs/\" + abi/\", overwrite: true)");
                    // gradleFile.Insert(i + 1, "    ant.copy(file: workingDir + \"/libs/\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/libs/\", overwrite: true)");
                    // gradleFile.Insert(i + 1,
                    // "    ant.copy(file: workingDir + \"/src/main/java/com/unity3d/player/\", tofile: workingDir + \"/../../../../MutiPlatfotmRes/Android/cat/app/src/main/java/com/unity3d/player/\", overwrite: true)");
                }
                else if (command.Contains("BuildIl2Cpp") && command.Contains("arm64-v8a")) {
                    gradleFile.Insert(i + 1, "              ReleaseApk();");
                }
            }

            gradleFile.Insert(gradleFile.Count, @"
def ReleaseApk(){
    exec {
        workingDir './../../../../MutiPlatfotmRes/Android/cat/'
        commandLine 'F:/Cat/Client/trunk/cat/MutiPlatfotmRes/Android/cat/release.bat'
    }
}");
            //        commandLine 'cmd','release.bat'

            File.WriteAllLines(gradlePath, gradleFile);

            var startinfo = new ProcessStartInfo(Path.Combine(unityLibraryPath, "work.bat"));
            startinfo.UseShellExecute = true;
            startinfo.ErrorDialog = true;
            startinfo.CreateNoWindow = false;
            startinfo.WorkingDirectory = unityLibraryPath;

            if (startinfo.UseShellExecute) {
                startinfo.RedirectStandardOutput = false;
                startinfo.RedirectStandardError = false;
                startinfo.RedirectStandardInput = false;
            }
            else {
                startinfo.RedirectStandardOutput = true;
                startinfo.RedirectStandardError = true;
                startinfo.RedirectStandardInput = true;
            }

            Process.Start(startinfo);
        }

        OpenDirectory(path.Substring(0, path.LastIndexOf('/')));
    }

    static void copyDirectory(string sourceDirectory, string destDirectory, string[] outExtension) {
//判断源目录和目标目录是否存在，如果不存在，则创建一个目录
        if (!Directory.Exists(sourceDirectory)) {
            Directory.CreateDirectory(sourceDirectory);
        }

        if (!Directory.Exists(destDirectory)) {
            Directory.CreateDirectory(destDirectory);
        }

//拷贝文件
        copyFile(sourceDirectory, destDirectory, outExtension);

//拷贝子目录       
//获取所有子目录名称
        string[] directionName = Directory.GetDirectories(sourceDirectory);
        foreach (string directionPath in directionName) {
            if (directionPath.Contains(".svn"))
                continue;
            //根据每个子目录名称生成对应的目标子目录名称
            string directionPathTemp = destDirectory + "/" + directionPath.Substring(sourceDirectory.Length);

            //递归下去
            copyDirectory(directionPath, directionPathTemp, outExtension);
        }
    }

    static void copyFile(string sourceDirectory, string destDirectory, string[] outExtension) {
//获取所有文件名称
        string[] fileName = Directory.GetFiles(sourceDirectory);
        foreach (string filePath in fileName) {
            string ext = Path.GetExtension(filePath);
            List<string> fitlerList = new List<string>(outExtension);
            int idx = fitlerList.IndexOf(ext);
            if (idx != -1)
                continue;

            //根据每个文件名称生成对应的目标文件名称
            string name = Path.GetFileName(filePath);
            string filePathTemp = destDirectory + "/" + name;

            File.Copy(filePath, filePathTemp, true);
        }
    }

    static void GetMD5() {
        foreach (var VARIABLE in Selection.objects) {
            if (VARIABLE != null) {
                var path = Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(VARIABLE);
                var bytes = File.ReadAllBytes(path);
                var hash = BuildBundles.GetMD5HashFromFile(bytes);
                Debug.Log(string.Format("fileName:{0},size:{1},md5:{2}", Path.GetFileName(path), bytes.Length, hash));
            }
        }
    }

    static void GetApkInfo() {
        var dirPath = Application.dataPath + "/../Export/Android/";
        if (!Directory.Exists(dirPath)) return;

        var tarPath = EditorUtility.OpenFilePanel("选择apk", dirPath, "apk");
        if (string.IsNullOrEmpty(tarPath)) return;
        var preName = Path.GetFileName(tarPath);
        var downloadApkName = preName.Substring(preName.IndexOf("_") + 1);
        downloadApkName = downloadApkName.Substring(0, downloadApkName.LastIndexOf("_"));
        downloadApkName = downloadApkName.Substring(0, downloadApkName.LastIndexOf("_")) + ".apk";
        var newTarPath = Path.Combine(dirPath, downloadApkName);
        File.Copy(tarPath, newTarPath, true);

        var files = new string[] {tarPath};
        for (int i = 0; i < files.Length; i++) {
            var bytes = File.ReadAllBytes(files[i]);
            var hash = BuildBundles.GetMD5HashFromFile(bytes);
            var size = bytes.Length + "";
            string temp = @"var downdata = {
    downurl: 'https://smdwytest.saiyunyx.com/update/{0}',
    isdownok:true,
    downloadapkurl:'https://smdwytest.saiyunyx.com/update',
    filename: '{0}',
    hash: '{1}',
    size: '{2}'
};
exports.downdata = downdata;";
            var downdata = temp.Replace("{0}", downloadApkName).Replace("{1}", hash).Replace("{2}", size);

            var downdataPath = Application.dataPath + "/../Patcher/downdata.js";
            File.WriteAllText(downdataPath, downdata);
            Debug.Log("输出downdata完成: " + downloadApkName);
        }
    }

    static Dictionary<string, string> CheckHotFixSize(Stream oldStream, Stream newFileStream) {
        Dictionary<string, FileVerInfo> f1 = FileVerInfo.Read(oldStream);
        Dictionary<string, FileVerInfo> f2 = FileVerInfo.Read(newFileStream);
        int ds = 0;
        int dc = 0;

        var diff = new Dictionary<string, string>();
        foreach (var version in f2) {
            string fileName = version.Key;
            FileVerInfo f = version.Value;
            string serverMd5 = f.hash;
            //新增的资源
            if (!f1.ContainsKey(fileName)) {
                ds += f.size;
                dc++;
                Debug.Log("新增资源: " + fileName);
                diff.Add(fileName, f.downloadDir);
            }
            else {
                //需要替换的资源
                FileVerInfo lf = f1[fileName];
                string localMd5 = lf.hash;
                if (!serverMd5.Equals(localMd5)) {
                    ds += f.size;
                    dc++;
                    Debug.Log("替换资源: " + fileName);
                    diff.Add(fileName, f.downloadDir);
                }
            }
        }

        string dstr = "";
        if (ds >= 1024 * 1024) {
            float maxsize = (float) ds / (1024f * 1024f);
            dstr = string.Format("{0:F2}MB", maxsize);
        }

        else {
            float maxsize = (float) ds / (1024f);
            dstr = string.Format("{0:F2}KB", maxsize);
        }

        Debug.Log("总更新文件数: " + dc + " 总跟新大小: " + dstr);
        return diff;
    }

    static void UpdateLocalFilelistDownloadPath() {
        BuildBundles.CreateFileHash();
        BuildBundles.CacheFilelistHistory();
        AssetDatabase.Refresh();
    }

    static void ZipResTotal() {
        Stream s2 = new FileStream(Path.Combine(Application.streamingAssetsPath, "filelist.bytes"), FileMode.Open, FileAccess.Read);
        Dictionary<string, FileVerInfo> localf = FileVerInfo.Read(s2);
        s2.Dispose();
        s2.Close();
        var result = new List<string>();
        foreach (var file in localf.Keys)
            result.Add(GetAssetBundleFilePath(file));
        ZipHelper.ZipResFile(localf, result);
    }

    static void ZipResAuto(UpdateDownload.ServerType serverType) {
        var localUpdateInfo = JsonUtility.FromJson<UpdateInfo>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, UpdateDownload.UpdateFilename)));

        //先行服
        string updateUrl1 = "https://umdwy.saiyunyx.com/update/";
        //正式服
        string updateUrl2 = "https://update.mmgy3d.com/update/zs/";
        //测试服
        string testUpdateUrl = "https://smdwytest.saiyunyx.com/update/";
        //本地
        string localUpdateUrl = "http://192.168.8.243:7777/update/";
        //内部体验服
        string localTestUpdateUrl = "http://101.132.140.212/update/";

        string updateUrl = "";
        if (serverType == UpdateDownload.ServerType.Test)
            updateUrl = testUpdateUrl;
        else if (serverType == UpdateDownload.ServerType.Offical_Test)
            updateUrl = updateUrl1;
        else if (serverType == UpdateDownload.ServerType.Offical)
            updateUrl = updateUrl2;
        else if (serverType == UpdateDownload.ServerType.Internal)
            updateUrl = localUpdateUrl;
        else if (serverType == UpdateDownload.ServerType.Internal_Test)
            updateUrl = localTestUpdateUrl;

        var updateFile = updateUrl + UpdateDownload.UpdateFilename;

        HttpDownload.Init();
        HttpDownload.DownloadDirect(updateFile, delegate(byte[] buf) {
            if (buf != null) {
                UpdateInfo serverUpdateInfo = JsonUtility.FromJson<UpdateInfo>(Encoding.UTF8.GetString(buf));
                if (!UpdateDownload.VerifyVersion(serverUpdateInfo.resVer, localUpdateInfo.resVer)) {
                    Debug.LogError("本地资源版本低! serverUpdateInfo: " + serverUpdateInfo.resVer + " localUpdateInfo: " + localUpdateInfo.resVer);
                    return;
                }

                Debug.Log("制作热更包 " + serverUpdateInfo.resVer + " -----> " + localUpdateInfo.resVer);

                var bgVStr = serverUpdateInfo.resVer.Split('.');

                var platformFoler = "pc";
#if UNITY_ANDROID
                platformFoler = "android";
#elif UNITY_IOS
                    platformFoler = "ios";
#endif

                var folderPath = string.Format("{0}/{1}.{2}/{3}/", platformFoler, bgVStr[0], bgVStr[1], serverUpdateInfo.resVer);
                string filelistUrlPath = updateUrl + folderPath + "filelist.bytes";
                HttpDownload.DownloadDirect(filelistUrlPath, delegate(byte[] buf2) {
                    if (buf2 != null) {
                        Stream s1 = new MemoryStream(buf2);
                        Stream s2 = new FileStream(Path.Combine(Application.streamingAssetsPath, "filelist.bytes"), FileMode.Open, FileAccess.Read);
                        var diff = CheckHotFixSize(s1, s2);
                        s1.Dispose();
                        s1.Close();
                        s2.Dispose();
                        s2.Close();

                        if (diff.Count < 0) {
                            Debug.Log("没有新文件!");
                        }
                        else {
                            //更新下载路径
                            s1 = new MemoryStream(buf2);
                            s2 = new FileStream(Path.Combine(Application.streamingAssetsPath, "filelist.bytes"), FileMode.Open, FileAccess.Read);
                            Dictionary<string, FileVerInfo> serverf = FileVerInfo.Read(s1);
                            Dictionary<string, FileVerInfo> localf = FileVerInfo.Read(s2);
                            s1.Dispose();
                            s1.Close();
                            s2.Dispose();
                            s2.Close();
                            bool error = false;
                            var result = new List<string>();
                            EditorUtility.DisplayProgressBar("正在比对文件", "", 0);
                            int crrCount = 0;
                            //fileName是AB包名字  不需要考虑后缀
                            foreach (var fileName in localf.Keys) {
                                EditorUtility.DisplayProgressBar("正在比对文件", fileName, (float) ++crrCount / (float) localf.Count);
                                var path = GetAssetBundleFilePath(fileName);
                                if (File.Exists(path)) {
                                    if (diff.ContainsKey(fileName)) {
                                        result.Add(path);
                                        continue;
                                    }
                                    else if (serverf.ContainsKey(fileName))
                                        localf[fileName].downloadDir = serverf[fileName].downloadDir;
                                    else {
                                        Debug.LogError("未知文件: " + fileName);
                                        error = true;
                                    }
                                }
                                else {
                                    Debug.LogError("找不到文件: " + path);
                                    error = true;
                                }
                            }

                            EditorUtility.ClearProgressBar();
                            if (!error) {
                                ZipHelper.ZipResFile(localf, result);
                            }
                        }
                    }
                    else
                        Debug.LogError("找不到文件: " + filelistUrlPath);
                });
            }
            else
                Debug.LogError("找不到文件: " + updateFile);
        });
    }

    static void ZipScriptAuto(UpdateDownload.ServerType serverType) {
        var localUpdateInfo = JsonUtility.FromJson<UpdateInfo>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, UpdateDownload.UpdateFilename)));

        //先行服
        string updateUrl1 = "https://umdwy.saiyunyx.com/update/";
        //正式服
        string updateUrl2 = "https://update.mmgy3d.com/update/zs/";
        //测试服
        string testUpdateUrl = "https://smdwytest.saiyunyx.com/update/";
        //本地
        string localUpdateUrl = "http://192.168.8.243:7777/update/";
        //内部体验服
        string localTestUpdateUrl = "http://101.132.140.212/update/";

        string updateUrl = "";
        if (serverType == UpdateDownload.ServerType.Test)
            updateUrl = testUpdateUrl;
        else if (serverType == UpdateDownload.ServerType.Offical_Test)
            updateUrl = updateUrl1;
        else if (serverType == UpdateDownload.ServerType.Offical)
            updateUrl = updateUrl2;
        else if (serverType == UpdateDownload.ServerType.Internal)
            updateUrl = localUpdateUrl;
        else if (serverType == UpdateDownload.ServerType.Internal_Test)
            updateUrl = localTestUpdateUrl;

        var updateFile = updateUrl + UpdateDownload.UpdateFilename;

        HttpDownload.Init();
        HttpDownload.DownloadDirect(updateFile, delegate(byte[] buf) {
            if (buf != null) {
                UpdateInfo serverUpdateInfo = JsonUtility.FromJson<UpdateInfo>(Encoding.UTF8.GetString(buf));
                if (!UpdateDownload.VerifyVersion(serverUpdateInfo.resVer, localUpdateInfo.resVer)) {
                    Debug.LogError("本地资源版本低! serverUpdateInfo: " + serverUpdateInfo.resVer + " localUpdateInfo: " + localUpdateInfo.resVer);
                    return;
                }

                Debug.Log("制作脚本热更包 " + serverUpdateInfo.resVer + " -----> " + localUpdateInfo.resVer);

                var bgVStr = serverUpdateInfo.resVer.Split('.');

                var platformFoler = "pc";
#if UNITY_ANDROID
                platformFoler = "android";
#elif UNITY_IOS
                    platformFoler = "ios";
#endif

                var folderPath = string.Format("{0}/{1}.{2}/{3}/", platformFoler, bgVStr[0], bgVStr[1], serverUpdateInfo.resVer);
                string filelistUrlPath = updateUrl + folderPath + "filelist.bytes";
                HttpDownload.DownloadDirect(filelistUrlPath, delegate(byte[] buf2) {
                    if (buf2 != null) {
                        Stream s1 = new MemoryStream(buf2);
                        Dictionary<string, FileVerInfo> serverf = FileVerInfo.Read(s1);
                        s1.Dispose();
                        s1.Close();

                        byte[] result;
                        string filePath = Path.Combine(Application.streamingAssetsPath, "filelist.bytes");
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                            result = new byte[fs.Length];
                            fs.Read(result, 0, result.Length);
                        }

                        Dictionary<string, FileVerInfo> fileVerInfoList = new Dictionary<string, FileVerInfo>();
                        using (MemoryStream ms = new MemoryStream(result))
                            fileVerInfoList = FileVerInfo.Read(ms);

                        serverf["script"] = fileVerInfoList["script"];

                        ZipHelper.ZipResFile(serverf, new List<string> {GetAssetBundleFilePath("script")});
                    }
                });
            }
            else
                Debug.LogError("找不到文件: " + updateFile);
        });
    }

    public static void OpDefaultMat() {
        EditorCoroutineRunner.StartEditorCoroutine(_OpDefaultMat());
    }

    static IEnumerator _OpDefaultMat() {
        List<string> files = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Models"), ref files);
        for (int i = files.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集原素材", "当前正在处理: " + Path.GetFileName(files[i]), (float) (files.Count - i) / (float) files.Count);
            var ext = Path.GetExtension(files[i]);
            if (ext != ".prefab" && ext != ".unity")
                files.RemoveAt(i);
            else
                files[i] = GetRelativePath(files[i]);
            yield return null;
        }

        files = files.Concat(BuildBundles.SceneLevels).ToList();
        var unityDefaultMat = "fileID: 10303, guid: 0000000000000000f000000000000000, type: 0";
        var selfDefaultMat = "fileID: 2100000, guid: 61691ff97442044449264b4fa7835f3d, type: 2";
        var cnt = 0;
        for (int i = 0; i < files.Count; i++) {
            var path = files[i];
            EditorUtility.DisplayProgressBar("替换", "当前正在替换: " + Path.GetFileName(path), (float) (i / files.Count));
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite)) {
                using (StreamReader sr = new StreamReader(fs)) {
                    var str = sr.ReadToEnd();
                    if (str.Contains(unityDefaultMat)) {
                        Debug.Log(path);
                        str = str.Replace(unityDefaultMat, selfDefaultMat);
                        using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8)) {
                            bw.BaseStream.Position = 0;
                            bw.Write(str);
                        }

                        cnt++;
                    }
                }

                fs.Close();
                yield return null;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("DefaultMat替换完毕，共替换" + cnt + "个");
        EditorUtility.ClearProgressBar();
    }

    public static void FindRefrence() {
        // EditorCoroutineRunner.StartEditorCoroutine(_FindRefrence());
        // EditorCoroutineRunner.StartEditorCoroutine(_FindRefrenceUI());
        EditorCoroutineRunner.StartEditorCoroutine(_FindRefrenceImage());
        // EditorCoroutineRunner.StartEditorCoroutine(_FindRefrenceFashions());
    }

    /*
     * 替换 Build/Models/Fashions 下所有材质为 _defaultMat
     */
    static IEnumerator _FindRefrenceFashions() {
        var allfiles = new List<string>();
        var defalutMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Build/Materials/_defaultMat.mat");
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Models/Fashions"), ref allfiles);
        for (int i = allfiles.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集所有文件", "当前正在处理: " + allfiles[i], (float) (allfiles.Count - i) / (float) allfiles.Count);
            var ext = Path.GetExtension(allfiles[i]);
            if (ext != ".prefab")
                allfiles.RemoveAt(i);
            else {
                allfiles[i] = GetRelativePath(allfiles[i]);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(allfiles[i]);
                var mrArr = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var mr in mrArr) {
                    if (mr.sharedMaterials.Length > 0) {
                        var needChange = false;
                        foreach (var mat in mr.sharedMaterials) {
                            if (mat == null || mat.name != "_defaultMat") {
                                needChange = true;
                                break;
                            }
                        }

                        if (needChange) {
                            var len = mr.sharedMaterials.Length;
                            var newSharedMat = new Material[len];
                            for (int j = 0; j < len; j++) {
                                newSharedMat[j] = defalutMat;
                            }

                            mr.sharedMaterials = newSharedMat;
                            EditorUtility.SetDirty(obj);
                            yield return null;
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    /*
     * 查找所有 没有被用到的，，名字带element的UI
     */
    static IEnumerator _FindRefrenceUI() {
        var elementDict = new Dictionary<string, bool>();
        var allfiles = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/UI"), ref allfiles);
        for (int i = allfiles.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集所有文件", "当前正在处理: " + allfiles[i], (float) (allfiles.Count - i) / (float) allfiles.Count);
            var ext = Path.GetExtension(allfiles[i]);
            if (ext != ".prefab")
                allfiles.RemoveAt(i);
            else {
                allfiles[i] = GetRelativePath(allfiles[i]);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(allfiles[i]);
                var loopArr = obj.GetComponentsInChildren<LoopHorizontalScrollRect>(true);
                if (loopArr.Length > 0) {
                    foreach (var loop in loopArr) {
                        if (!elementDict.ContainsKey(loop.prefabName))
                            elementDict.Add(loop.prefabName, true);
                    }
                }

                var loopArr2 = obj.GetComponentsInChildren<LoopVerticalScrollRect>(true);
                if (loopArr2.Length > 0) {
                    foreach (var loop in loopArr2) {
                        if (!elementDict.ContainsKey(loop.prefabName))
                            elementDict.Add(loop.prefabName, true);
                    }
                }
            }

            yield return null;
        }

        var keepDict = new Dictionary<string, bool>();
        var keepFile = Path.Combine(Application.dataPath, "UIelement_keep.txt");
        using (FileStream fs = new FileStream(keepFile, FileMode.Open, FileAccess.Read)) {
            using (StreamReader sr = new StreamReader(fs)) {
                while (sr.Peek() > -1) {
                    keepDict.Add(sr.ReadLine(), true);
                }
            }
        }

        var filePath = Path.Combine(Application.dataPath, "UIelement_waitDelete.txt");
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
            using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8)) {
                foreach (var file in allfiles) {
                    if (file.Contains("Element") || file.Contains("element")) {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (!elementDict.ContainsKey(fileName) && !keepDict.ContainsKey(fileName)) {
                            bw.WriteLine(fileName);
                            AssetDatabase.DeleteAsset(file);
                        }
                    }
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    static IEnumerator _FindRefrenceImage() {
        //引用到的image
        var usedImages = new Dictionary<string, bool>();
        var allUIPrefab = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/UI"), ref allUIPrefab);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Models"), ref allUIPrefab);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Effects"), ref allUIPrefab);
        for (int i = allUIPrefab.Count - 1; i >= 0; i--) {
            var path = allUIPrefab[i];
            EditorUtility.DisplayProgressBar("收集images引用", "当前正在处理: " + path, (float) (allUIPrefab.Count - i) / (float) allUIPrefab.Count);
            var ext = Path.GetExtension(path);
            if (ext == ".prefab") {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(GetRelativePath(path));
                var imageArr = obj.GetComponentsInChildren<Image>(true);
                if (imageArr.Length > 0) {
                    foreach (var img in imageArr) {
                        var usedImgPath = AssetDatabase.GetAssetPath(img.sprite.GetInstanceID());
                        if (!usedImages.ContainsKey(usedImgPath))
                            usedImages.Add(usedImgPath, true);
                    }
                }
            }

            yield return null;
        }

        var allfiles = new List<string>();
        //所有images
        GetFilesDeeply(Path.Combine(Application.dataPath, "Images"), ref allfiles);
        for (int i = allfiles.Count - 1; i >= 0; i--) {
            var path = allfiles[i];
            EditorUtility.DisplayProgressBar("收集所有Image", "当前正在处理: " + path, (float) (allfiles.Count - i) / (float) allfiles.Count);
            var ext = Path.GetExtension(path);
            if (ext == ".png" || ext == ".jpg")
                allfiles[i] = GetRelativePath(path);
            else {
                allfiles.RemoveAt(i);
            }

            yield return null;
        }

        //白名单
        var keepPathDict = new Dictionary<string, bool>();
        var keepFile = Path.Combine(Application.dataPath, "Images_keep_dir.txt");
        var keepDirArr = new List<string>();
        using (FileStream fs = new FileStream(keepFile, FileMode.Open, FileAccess.Read)) {
            using (StreamReader sr = new StreamReader(fs)) {
                while (sr.Peek() > -1) {
                    keepDirArr.Add(sr.ReadLine());
                }
            }
        }

        var excludefiles = new List<string>();
        foreach (var keepDir in keepDirArr) {
            GetFilesDeeply(Path.Combine(Application.dataPath, keepDir), ref excludefiles);
        }

        foreach (var excludePath in excludefiles) {
            var ext = Path.GetExtension(excludePath);
            if (ext != ".meta")
                keepPathDict.Add(GetRelativePath(excludePath), true);
        }

        //待删除
        var filePath = Path.Combine(Application.dataPath, "Images_waitDelete.txt");
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
            using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8)) {
                foreach (var file in allfiles) {
                    if (!keepPathDict.ContainsKey(file) && !usedImages.ContainsKey(file)) {
                        bw.WriteLine(file);
                        // AssetDatabase.DeleteAsset(file);
                    }
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    /*
     * 查找Dependence下不被依赖的资源
     */
    static IEnumerator _FindRefrence() {
        Dictionary<string, int> dependTimesDict = new Dictionary<string, int>();
        //总和
        var allfiles = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Dependence"), ref allfiles);
        //需要查找依赖项的
        var targetfiles = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Models"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Effects"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/UI"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Materials"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Animators"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/ScriptableObjects"), ref targetfiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Resources"), ref targetfiles);
        var allScenesGUID = AssetDatabase.FindAssets("t:Scene");
        var allScenesPath = new List<string>();
        Array.ForEach(allScenesGUID, guid => allScenesPath.Add(AssetDatabase.GUIDToAssetPath(guid)));
        targetfiles = targetfiles.Concat(allScenesPath).ToList();
        //需要排除的路径
        var excludeDict = new Dictionary<string, bool>();
        var excludefiles = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Dependence/Spine"), ref excludefiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Dependence/LobbyTheatre"), ref excludefiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Dependence/Room"), ref excludefiles);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Dependence/NPC"), ref excludefiles);
        foreach (var path in excludefiles) {
            var ext = Path.GetExtension(path);
            if (ext != ".meta") {
                excludeDict.Add(GetRelativePath(path), true);
            }
        }

        //开始
        for (int i = allfiles.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集所有文件", "当前正在处理: " + allfiles[i], (float) (allfiles.Count - i) / (float) allfiles.Count);
            var ext = Path.GetExtension(allfiles[i]);
            if (ext == ".meta")
                allfiles.RemoveAt(i);
            else {
                string relativePath = GetRelativePath(allfiles[i]);
                allfiles[i] = relativePath;
            }

            yield return null;
        }

        for (int i = targetfiles.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集依赖项", "当前正在处理: " + targetfiles[i], (float) (targetfiles.Count - i) / (float) targetfiles.Count);
            var ext = Path.GetExtension(targetfiles[i]);
            if (ext == ".meta")
                continue;
            else {
                string relativePath = GetRelativePath(targetfiles[i]);
                string[] depPathArr = AssetDatabase.GetDependencies(relativePath, true);
                foreach (var path in depPathArr) {
                    if (path != relativePath) {
                        if (dependTimesDict.TryGetValue(path, out int time)) {
                            dependTimesDict[path] = time + 1;
                        }
                        else {
                            dependTimesDict.Add(path, 1);
                        }
                    }
                }
            }

            yield return null;
        }

        var filePath = Path.Combine(Application.dataPath, "deletFile_2021_12_7.txt");
        int crProgress = 0;
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
            using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8)) {
                foreach (var path in allfiles) {
                    if (excludeDict.ContainsKey(path))
                        continue;
                    var ext = Path.GetExtension(path);
                    //需要输出的类型放这里
                    if (ext != ".meta" && ext != ".txt") {
                        EditorUtility.DisplayProgressBar("写入结果", "当前资源: " + path, (float) (++crProgress) / (float) allfiles.Count);
                        var dependTimes = 0;
                        dependTimesDict.TryGetValue(path, out dependTimes);
                        if (dependTimes == 0) {
                            bw.WriteLine(path);
                            AssetDatabase.DeleteAsset(path);
                        }

                        yield return null;
                    }
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    public static void OpBundle() {
        EditorCoroutineRunner.StartEditorCoroutine(_OpBundle());
        // BuildBundles.SetDependenceBundleName();
    }

    static IEnumerator _OpBundle() {
        List<string> files = new List<string>();
        // GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Models/PVE/Test"), ref files);
        // GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Effects/EffTest"), ref files);
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build"), ref files);
        for (int i = files.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集原素材", "当前正在处理: " + Path.GetFileName(files[i]), (float) (files.Count - i) / (float) files.Count);
            var ext = Path.GetExtension(files[i]);
            if (ext == ".meta" || ext == ".unity")
                files.RemoveAt(i);
            else if (files[i].IndexOf("AutoMoveAssets") != -1) {
                files.RemoveAt(i);
            }
            else
                files[i] = GetRelativePath(files[i]);
            yield return null;
        }

        Debug.Log("待处理文件数: " + files.Count);
        
        Dictionary<string, BundleFileInfo> results = new Dictionary<string, BundleFileInfo>();
        for (int i = 0; i < files.Count; i++) {
            var relativePath = files[i];
            if (System.Text.RegularExpressions.Regex.IsMatch(relativePath, @"^[\u4e00-\u9fa5]+$")) {
                Debug.Log("有中文" + relativePath);
                continue;
            }
            var asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
            Object[] dependObjs = EditorUtility.CollectDependencies(new Object[] {asset});
          
            //一个bundle内反复引用的资源也只会打一次资源
            Dictionary<string, BundleFileInfo> oneTimeRelation = new Dictionary<string, BundleFileInfo>();
            for (int j = 0; j < dependObjs.Length; j++) {
                var dependObj = dependObjs[j];
                if (dependObj == null) {
                    continue;
                }
                string dependObjPath = AssetDatabase.GetAssetPath(dependObj.GetInstanceID());
                if (dependObjPath.Equals(files[i])) {
                    continue;
                }
                EditorUtility.DisplayProgressBar("收集依赖项: " + (i + 1) + "/" + files.Count, "当前正在处理: " + dependObjPath, (float) (j + 1) / (float) dependObjs.Length);
                
                //这个文件夹刨除
                var filter = dependObjPath.StartsWith("Assets/Art/Fashions/");
                filter |= !dependObjPath.StartsWith("Assets/Art/") && !dependObjPath.StartsWith("Assets/Build/Dependence/AutoMoveAssets");
        
                //散包刨除
                // filter |= dependObjPath.StartsWith("Assets/Build/");
                //脚本刨除
                filter |= dependObjPath.EndsWith(".cs");
                //dll刨除
                filter |= dependObjPath.EndsWith(".dll");
                //内嵌资源刨除
                // filter |= !dependObjPath.StartsWith("Assets/");
                //UI图片刨除
                // filter |= dependObjPath.StartsWith("Assets/Images/");
                //Resouece刨除
                // filter |= dependObjPath.StartsWith("Assets/Resources/");
                //插件除外
                // filter |= dependObjPath.StartsWith("Assets/Plugins/");
                // filter |= dependObjPath.StartsWith("Assets/AmplifyShaderEditor/");
                // filter |= dependObjPath.StartsWith("Assets/Demigiant/");
        
                if (!filter) {
                    if (!oneTimeRelation.ContainsKey(dependObjPath)) {
                        var fileInfo = new BundleFileInfo() {
                            assetType = dependObj.GetType().ToString(),
                            assetName = Path.GetFileNameWithoutExtension(dependObjPath),
                            ext = Path.GetExtension(dependObjPath),
                            relativePath = dependObjPath,
                            parObjRelativePath = files[i],
                            refCount = 1
                        };
                        oneTimeRelation[dependObjPath] = fileInfo;
                    }
                }
        
                yield return null;
            }
        
            foreach (var pairs in oneTimeRelation) {
                if (!results.ContainsKey(pairs.Key)) {
                    results[pairs.Key] = pairs.Value;
                }
                else {
                    results[pairs.Key].refCount++;
                    results[pairs.Key].parObjRelativePath += "|" + pairs.Value.parObjRelativePath;
                }
            }
        }
        
        var result = results.OrderByDescending(o => o.Value.refCount).ToDictionary(p => p.Key, o => o.Value);
        
        var realMoveResult = new Dictionary<string, BundleFileInfo>();
        int executeIndex = 1;
        foreach (var pairs in result) {
            var path = pairs.Key;
            if (pairs.Value.refCount > 1) {
                EditorUtility.DisplayProgressBar("移动资源", path, (float) executeIndex++ / (float) result.Count);
                // MoveAsset(path);
                MoveAutoMoveFolderAssets(pairs.Value, ref realMoveResult);
            }
            else if (path.StartsWith("Assets/Build/Dependence/AutoMoveAssets")) {
                if (pairs.Value.refCount == 1) {
                }
                MoveAutoMoveFolderAssets(pairs.Value, ref realMoveResult);
            }
            else {
                EditorUtility.DisplayProgressBar("跳过资源", path, (float) executeIndex++ / (float) result.Count);
            }
        }

        EditorUtility.ClearProgressBar();
        realMoveResult = realMoveResult.OrderByDescending(o => o.Value.refCount).ToDictionary(p => p.Key, o => o.Value);
        var filePath = Path.Combine(Application.dataPath, "bundleFileInfo.txt");
        int crProgress = 0;
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
            using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8)) {
                foreach (var VARIABLE in realMoveResult) {
                    EditorUtility.DisplayProgressBar("写入优化结果", "当前资源: " + VARIABLE.Value.assetName, (float) (++crProgress) / (float) result.Count);
                    bw.Write(VARIABLE.Value.ToString() + "\n");
                }
            }
        
            yield return null;
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        Debug.Log("写入结果完成");
    }

    // public static void MoveAsset(string assetRelativePath) {
    //     //先判断类型
    //     var ext = Path.GetExtension(assetRelativePath);
    //     var fileType = ext.ToLower().Replace(".", "");
    //
    //     var folderName = Path.Combine(Application.dataPath, "Build/Dependence/AutoMoveAssets/") + fileType + "/";
    //     if (!Directory.Exists(folderName)) {
    //         Directory.CreateDirectory(folderName);
    //         AssetDatabase.Refresh();
    //     }
    //     var fileNameWithoutExt = Path.GetFileNameWithoutExtension(assetRelativePath);
    //     int index = 0;
    //     var oldPath = NgTool.ToUnityRelativePath(assetRelativePath);
    //
    //     var tarPath = "";
    //     while (true) {
    //         var houZhui = index == 0 ? "" : ("_" + index);
    //         var newPath = folderName + fileNameWithoutExt + houZhui + ext;
    //         if (File.Exists(newPath))
    //             index++;
    //         else {
    //             tarPath = NgTool.ToUnityRelativePath(newPath);
    //             AssetDatabase.MoveAsset(oldPath, tarPath);
    //             break;
    //         }
    //     }
    //
    //     var fullPath = Path.GetFullPath(assetRelativePath);
    //     if (File.Exists(fullPath)) {
    //         Debug.LogError("移动资源失败: " + NgTool.ToUnityRelativePath(assetRelativePath), AssetDatabase.LoadAssetAtPath<Object>(assetRelativePath));
    //     }
    // }

    public static void MoveAutoMoveFolderAssets(BundleFileInfo bundleFileInfo, ref Dictionary<string, BundleFileInfo> realMoveResult) {
        string assetRelativePath = bundleFileInfo.relativePath;
        var ext = Path.GetExtension(assetRelativePath);
        var fileType = ext.ToLower().Replace(".", "");
        if (fileType.Equals("shader")) {
            MoveDependShader(bundleFileInfo, ref realMoveResult);
            return;
        }
        string[] parObjRelativePaths = bundleFileInfo.parObjRelativePath.Split('|');
        string folderName = Path.GetFileNameWithoutExtension(parObjRelativePaths[0]);
        string multipleReference = (bundleFileInfo.refCount > 1) ? "AAMultipleReference/" : "";
        string folderFullName = Path.Combine(Application.dataPath, "Build/Dependence/AutoMoveAssets/") + multipleReference + folderName + "/" + fileType + "/";
        if (!Directory.Exists(folderFullName)) {
            Directory.CreateDirectory(folderFullName);
            AssetDatabase.Refresh();
        }
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(assetRelativePath);
        int index = 0;
        var oldPath = NgTool.ToUnityRelativePath(assetRelativePath);
        var tarPath = "";
        while (true) {
            var houZhui = index == 0 ? "" : ("_" + index);
            var newPath = folderFullName + fileNameWithoutExt + houZhui + ext;
            var newPathRelativePath = NgTool.ToUnityRelativePath(newPath);
            if (newPathRelativePath.Equals(oldPath)) {
                break;
            }

            if (File.Exists(newPath)) {
                index++;
            }
            else {
                tarPath = newPathRelativePath;
                AssetDatabase.MoveAsset(oldPath, tarPath);
                var fullPath = Path.GetFullPath(assetRelativePath);
                if (File.Exists(fullPath)) {
                    Debug.LogError("移动资源失败: " + NgTool.ToUnityRelativePath(assetRelativePath), AssetDatabase.LoadAssetAtPath<Object>(assetRelativePath));
                }
                else {
                    realMoveResult.Add(bundleFileInfo.relativePath, bundleFileInfo);
                }
                break;
            }
        }
    }

    public static void MoveDependShader(BundleFileInfo bundleFileInfo, ref Dictionary<string, BundleFileInfo> realMoveResult) {
        string folderFullName = Path.Combine(Application.dataPath, "Build/Dependence/Shader/");
        if (!Directory.Exists(folderFullName)) {
            Directory.CreateDirectory(folderFullName);
            AssetDatabase.Refresh();
        }
        string assetRelativePath = bundleFileInfo.relativePath;
        var ext = Path.GetExtension(assetRelativePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(assetRelativePath);
        int index = 0;
        var oldPath = NgTool.ToUnityRelativePath(assetRelativePath);
        var tarPath = "";
        while (true) {
            var houZhui = index == 0 ? "" : ("_" + index);
            var newPath = folderFullName + fileNameWithoutExt + houZhui + ext;
            var newPathRelativePath = NgTool.ToUnityRelativePath(newPath);
            if (newPathRelativePath.Equals(oldPath)) {
                break;
            }

            if (File.Exists(newPath)) {
                index++; 
            }
            else {
                tarPath = newPathRelativePath;
                // Debug.Log(oldPath + "-----" + tarPath);
                AssetDatabase.MoveAsset(oldPath, tarPath);
                var fullPath = Path.GetFullPath(assetRelativePath);
                if (File.Exists(fullPath)) {
                    Debug.LogError("移动资源失败: " + NgTool.ToUnityRelativePath(assetRelativePath), AssetDatabase.LoadAssetAtPath<Object>(assetRelativePath));
                }
                else {
                    realMoveResult.Add(bundleFileInfo.relativePath, bundleFileInfo);
                }
                break;
            }
        }
    }

    public static void StopOpBundle() {
        EditorCoroutineRunner.Clear();
        EditorUtility.ClearProgressBar();
        AssetBundle.UnloadAllAssetBundles(true);
    }

    public static void StopCheckEffRes() {
        EditorCoroutineRunner.Clear();
        EditorUtility.ClearProgressBar();
    }

    public static void CheckEffRes() {
        EditorCoroutineRunner.StartEditorCoroutine(_CheckEffRes());
    }

    static IEnumerator _CheckEffRes() {
        var files = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Art/Effects"), ref files);
        var buildEffRes = new List<string>();
        GetFilesDeeply(Path.Combine(Application.dataPath, "Build/Effects"), ref buildEffRes);
        files.AddRange(buildEffRes);
        for (int i = files.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("收集原素材", "当前正在处理: " + Path.GetFileName(files[i]), (float) (files.Count - i) / (float) files.Count);
            var ext = Path.GetExtension(files[i]);
            if (ext == ".meta")
                files.RemoveAt(i);
            else
                files[i] = GetRelativePath(files[i]);
            yield return null;
        }

        Debug.Log("待处理文件数: " + files.Count);
        for (int i = files.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("分析素材", "当前正在处理: " + Path.GetFileName(files[i]), (float) (files.Count - i) / (float) files.Count);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);
            if (texture != null && (texture.width >= 512 || texture.height >= 512)) {
                Debug.Log(Path.GetFileName(files[i]) + ": width-->" + texture.width + " height-->" + texture.height, texture);
            }

            yield return null;
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("分析完成!");
    }

    private static string GetRelativePath(string path) {
        return NgTool.ToUnityRelativePath(path);
    }
}