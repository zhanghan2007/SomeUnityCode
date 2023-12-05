using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Object = UnityEngine.Object;
using UnityEngine.U2D;
using UnityEditor.U2D;

public class BuildBundles : EditorWindow {
    public static bool RebuildBundleName {
        get { return EditorPrefs.GetBool("rebuildBundleName", true); }
        set { EditorPrefs.SetBool("rebuildBundleName", value); }
    }

    private static UpdateDownload.UpdateInfo updateInfo {
        get { return JsonUtility.FromJson<UpdateDownload.UpdateInfo>(File.ReadAllText(versionPath)); }
    }

    public static void BuildAll() {
        string outBuildlePath = GetAssetBundleOutputPath();
        if (Directory.Exists(outBuildlePath)) {
            Directory.Delete(outBuildlePath, true);
        }

        string outPath = Application.streamingAssetsPath;
        if (!Directory.Exists(outPath)) {
            Directory.CreateDirectory(outPath);
        }

        Build(EditorUserBuildSettings.activeBuildTarget);
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

    /// <summary>
    /// 遍历目录及其子目录
    /// </summary>
    public static void SetAssetBundelName(string path, string assetBundleName, List<string> fitlerList,
        bool clear = true) {
        if (!Directory.Exists(path))
            return;

        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            string apath = filename.Substring(filename.IndexOf("Assets"));
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (ai != null) {
                string ext = Path.GetExtension(filename).ToLower();
                if (ext == ".mat") {
                    if (filename.Contains("- Default") || filename.Contains("No Name")) {
                        ai.assetBundleName = null;
                        continue;
                    }
                }

                if (clear) {
                    int idx = fitlerList.IndexOf(ext);
                    ai.assetBundleName = idx != -1 ? assetBundleName : null;
                }
                else {
                    if (ai.assetBundleName != assetBundleName) {
                        int idx = fitlerList.IndexOf(ext);
                        if (idx != -1) {
                            ai.assetBundleName = assetBundleName;
                        }
                    }
                }
            }
        }

        foreach (string dir in dirs) {
            if (dir.Contains(".svn") || dir.Contains(".DS_Store"))
                continue;
            SetAssetBundelName(dir, assetBundleName, fitlerList, clear);
        }
    }

    public static void SetAssetBundelName(string path, string[] exclude, string assetBundleName = null) {
        if (!Directory.Exists(path))
            return;

        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            string ext = Path.GetExtension(filename).ToLower();
            List<string> fitlerList = new List<string>(exclude);
            int idx = fitlerList.IndexOf(ext);
            string apath = filename.Substring(filename.IndexOf("Assets"));
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (ai != null) {
                if (ext == ".mat") {
                    if (filename.Contains("- Default") || filename.Contains("No Name")) {
                        ai.assetBundleName = null;
                        continue;
                    }
                }

                if (assetBundleName != null) {
                    ai.assetBundleName = idx == -1 ? assetBundleName : null;
                }
                else {
                    string name = Path.GetFileNameWithoutExtension(filename);
                    ai.assetBundleName = idx == -1 ? name : null;
                }
            }
        }

        foreach (string dir in dirs) {
            if (dir.Contains(".svn"))
                continue;
            SetAssetBundelName(dir, exclude, assetBundleName);
        }
    }

    public static void SetAssetBundelName(string path, string assetBundleName = null) {
        if (!Directory.Exists(path))
            return;

        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            string apath = filename.Substring(filename.IndexOf("Assets"));
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (filename.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase)) {
                Debug.LogError("资源路径里有脚本: " + apath, ai);
                continue;
            }

            if (ai != null) {
                string ext = Path.GetExtension(filename);

                if (ext == ".prefab") {
                    if (assetBundleName != null) {
                        ai.assetBundleName = assetBundleName;
                    }
                    else {
                        string name = Path.GetFileNameWithoutExtension(filename);
                        ai.assetBundleName = name;
                    }
                }
                else {
                    ai.assetBundleName = null;
                }
            }
        }

        foreach (string dir in dirs) {
            if (dir.Contains(".svn"))
                continue;
            SetAssetBundelName(dir, assetBundleName);
        }
    }

    public static void ClearAssetBundelName(string path) {
        if (!Directory.Exists(path))
            return;

        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            var relativePath = NgTool.ToUnityRelativePath(filename);
            if (relativePath.Contains("Assets/Build/"))
                continue;
            if (relativePath.Contains("Assets/Images/"))
                continue;
            string apath = NgTool.ToUnityRelativePath(filename);
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (filename.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase)
                || filename.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase)
            ) {
                // Debug.Log("异常文件: " + apath, ai);
                continue;
            }

            EditorUtility.DisplayProgressBar("ClearAssetBundelName", apath, 0.0f);
            if (ai != null && !string.IsNullOrEmpty(ai.assetBundleName)) {
                if (apath.EndsWith(".unity") && BuildBundles.SceneLevels.Contains(apath))
                    continue;
                ai.assetBundleName = null;
                Debug.Log("清理BundleName: " + apath, ai);
            }

            EditorUtility.DisplayProgressBar("ClearAssetBundelName", apath, 1.0f);
        }

        EditorUtility.ClearProgressBar();

        foreach (string dir in dirs) {
            if (dir.Contains(".svn"))
                continue;
            ClearAssetBundelName(dir);
        }
    }

    static void BuildScript(string srcDir, string tgtDir) {
        DirectoryInfo source = new DirectoryInfo(srcDir);
        DirectoryInfo target = new DirectoryInfo(tgtDir);

        if (target.FullName.StartsWith(source.FullName, System.StringComparison.CurrentCultureIgnoreCase)) {
            throw new System.Exception("父目录不能拷贝到子目录！");
        }

        if (!source.Exists) {
            return;
        }

        if (!target.Exists) {
            target.Create();
        }

        FileInfo[] files = source.GetFiles();

        for (int i = 0; i < files.Length; i++) {
            string filename = files[i].Name;
            string ext = Path.GetExtension(filename);
            if (ext != ".lua")
                continue;

            byte[] tar = null;
            using (FileStream fileStream = new FileStream(files[i].FullName, FileMode.Open, FileAccess.Read)) {
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                tar = SLua.LuaState.CleanUTF8Bom(buffer);
            }

            filename = filename.Replace(".lua", ".bytes");
            using (FileStream fileStream = new FileStream(target.FullName + "/" + filename, FileMode.Create, FileAccess.Write))
                fileStream.Write(tar, 0, tar.Length);
        }

        DirectoryInfo[] dirs = source.GetDirectories();

        for (int j = 0; j < dirs.Length; j++) {
            if (dirs[j].FullName.Contains(".svn"))
                continue;
            BuildScript(dirs[j].FullName, tgtDir);
        }
    }

    static void EncryptScripts(string sSourcePath) {
//        if (!File.Exists(@"E:\cool\client\trunk\lua\luac.exe"))
//            return;

        //在指定目录及子目录下查找文件,在list中列出子目录及文件
        DirectoryInfo Dir = new DirectoryInfo(sSourcePath);
        DirectoryInfo[] DirSub = Dir.GetDirectories();

        foreach (DirectoryInfo d in DirSub) //查找子目录 
        {
            if (d.Name.Contains(".svn"))
                continue;
            EncryptScript(d.FullName);
        }

        foreach (FileInfo f in Dir.GetFiles("*.bytes", SearchOption.TopDirectoryOnly)) //查找文件
        {
            EncryptScript(f.FullName);
        }
    }

    static void EncryptScript(string filename) {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
            return;
        }

        try {
            System.Diagnostics.Process proc = null;
            proc = new System.Diagnostics.Process();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
                proc.StartInfo.FileName = Application.dataPath + @"/../../lua/luac";
            }
            else {
                proc.StartInfo.FileName = Application.dataPath + @"/../../lua/luac.exe";
            }

            proc.StartInfo.Arguments = string.Format("-o {0} {1}", filename, filename); //this is argument
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
        }
        catch (System.Exception ex) {
            UnityEngine.Debug.LogException(ex);
        }
    }

    static void BuildScript() {
        string tempdir = Application.dataPath + "/luatemp";
        if (Directory.Exists(tempdir)) {
            Directory.Delete(tempdir, true);
        }

        BuildScript(Application.dataPath + "/lua/", tempdir);
        //EncryptScripts(tempdir);
        copyDirectory(tempdir, Application.dataPath + "/luabuild", new string[] { });
        if (Directory.Exists(tempdir)) {
            Directory.Delete(tempdir, true);
        }

        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("lua编译完成");
    }
    
    static void SetFolderOneAssetBundelName(string folderPath, string assetBundleName) {
        List<string> fileNames = new List<string>();
        NgTool.GetFilesDeeply(folderPath, ref fileNames);
        for (int i = 0; i < fileNames.Count; ++i) {
            string filename = fileNames[i];
            string ext = Path.GetExtension(filename);
            if (ext.Contains(".meta") || ext.Contains(".DS_Store"))
                continue;
            string apath = filename.Substring(filename.IndexOf("Assets"));
            apath = apath.Replace("\\", "/");
            EditorUtility.DisplayProgressBar("SetBundleName", apath, 0.0f);
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (ai != null) {
                ai.assetBundleName = assetBundleName;
            }

            EditorUtility.DisplayProgressBar("SetBundleName", apath, 1.0f);
        }

        EditorUtility.ClearProgressBar();
    }

    static void SetYiCengFloderOneAssetBundleName(string floderPath, string assetBundleName, string[] exclude = null) {
        SetSingleAssetBundelName(floderPath, assetBundleName + "base");
        List<string> excudeFolder = new List<string>();
        if (exclude != null) {
            excudeFolder = new List<string>(exclude);
        }
        string[] childrenDirs = Directory.GetDirectories(floderPath);
        for (int i = 0; i < childrenDirs.Length; ++i) {
            if (childrenDirs[i].Contains(".svn"))
                continue;
            childrenDirs[i] = childrenDirs[i].Replace("\\", "/");
            bool ignore = false;

            foreach (var VARIABLE in exclude) {
                if (childrenDirs[i].IndexOf(VARIABLE) != -1) {
                    ignore = true;
                    break;
                }
            }

            if (ignore) {
                continue;
            }
            
            var childFolderName = childrenDirs[i].Substring(childrenDirs[i].LastIndexOf("/") + 1);
            if (excudeFolder.IndexOf(childFolderName) != -1) {
                continue;
            }
            SetFolderOneAssetBundelName(childrenDirs[i], assetBundleName + "_" + childFolderName);
        }
    }

    static void SetSingleAssetBundelName(string path, string assetBundleName) {
        string[] fileNames = Directory.GetFiles(path);
        for (int i = 0; i < fileNames.Length; ++i) {
            string filename = fileNames[i];
            string ext = Path.GetExtension(filename);
            if (ext.Contains(".meta") || ext.Contains(".DS_Store"))
                continue;
            string apath = filename.Substring(filename.IndexOf("Assets"));
            apath = apath.Replace("\\", "/");
            EditorUtility.DisplayProgressBar("SetBundleName", apath, 0.0f);
            AssetImporter ai = AssetImporter.GetAtPath(apath);
            if (ai != null) {
                ai.assetBundleName = assetBundleName;
            }

            EditorUtility.DisplayProgressBar("SetBundleName", apath, 1.0f);
        }

        EditorUtility.ClearProgressBar();
    }

    static void SetChildDirAssetBundelName(string rootDir, string prefix) {
        SetSingleAssetBundelName(rootDir, prefix);
        string[] childrenDirs = Directory.GetDirectories(rootDir);
        for (int i = 0; i < childrenDirs.Length; ++i) {
            if (childrenDirs[i].Contains(".svn"))
                continue;
            childrenDirs[i] = childrenDirs[i].Replace("\\", "/");
            var childFolderName = childrenDirs[i].Substring(childrenDirs[i].LastIndexOf("/") + 1);
            SetChildDirAssetBundelName(childrenDirs[i], prefix + "_" + childFolderName);
        }
    }

    static void SetMultiAssetBundelName(string rootDir, string prefix) {
        SetSingleAssetBundelName(rootDir, prefix + "base");
        string[] childrenDirs = Directory.GetDirectories(rootDir);
        for (int i = 0; i < childrenDirs.Length; ++i) {
            if (childrenDirs[i].Contains(".svn"))
                continue;
            childrenDirs[i] = childrenDirs[i].Replace("\\", "/");
            var childFolderName = childrenDirs[i].Substring(childrenDirs[i].LastIndexOf("/") + 1);
            SetChildDirAssetBundelName(childrenDirs[i], prefix + childFolderName);
        }
    }

    [MenuItem("资源/Update AssetBundelName")]
    static void BuildAssetBundleName() {
        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Shaders", 0.0f);
        SetAssetBundelName(Application.dataPath + "/Build/Shaders/", new string[] {".meta"}, "shaders");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Materials", 0.05f);
        SetAssetBundelName(Application.dataPath + "/Build/Materials/", new string[] {".meta"}, "materials");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Fonts", 0.1f);
        SetAssetBundelName(Application.dataPath + "/Build/Fonts/", new string[] {".meta"}, "font");
        // EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Data", 0.1f);
        // SetAssetBundelName(Application.dataPath + "/Build/Data/", new string[] {".meta"}, "data");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "ScriptableObjects", 0.2f);
        SetAssetBundelName(Application.dataPath + "/Build/ScriptableObjects/", new string[] {".meta"},
            "scriptableobjects");

        EditorUtility.DisplayProgressBar("BuildPostProcessProfilesName", "PostProcessProfiles", 0.25f);
        SetAssetBundelName(Application.dataPath + "/Build/PostProcessProfiles/", new string[] {".meta"},
            "postprocessprofiles");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Characters", 0.3f);
        SetAssetBundelName(Application.dataPath + "/Build/Characters/", new string[] {".meta"}, "characters");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Effects", 0.4f);
        SetAssetBundelName(Application.dataPath + "/Build/Effects/", new string[] {".meta"}, "effect");

        // EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Effects Res", 0.5f);
        // SetAssetBundelName(Application.dataPath + "/Build/Effects/", "effectres",
        //     new List<string>(new string[] {".png", ".tga", ".fbx", ".mat", ".controller", ".anim"}), false);

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Textures", 0.55f);
        SetAssetBundelName(Application.dataPath + "/Build/Textures/", new string[] {".meta"});

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Materials", 0.58f);
        SetAssetBundelName(Application.dataPath + "/Build/Materials/", new string[] {".meta"}, "material");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Animators", 0.7f);
        SetAssetBundelName(Application.dataPath + "/Build/Animators/", new string[] {".meta"});

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Models", 0.75f);
        SetAssetBundelName(Application.dataPath + "/Build/Models/", new string[] {".meta"});

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Images", 0.85f);
        SetMultiAssetBundelName(Application.dataPath + "/Images/", "image_");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Dependence", 0.8f);
        // SetAssetBundelName(Application.dataPath + "/Build/Dependence/", new string[] {".meta"});
        SetDependenceBundleName();

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Sounds", 0.95f);
        SetMultiAssetBundelName(Application.dataPath + "/Build/Sounds/", "sound_");

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "UI", 0.99f);
        SetMultiAssetBundelName(Application.dataPath + "/Build/UI/", "ui_");


        foreach (var scene in SceneLevels) {
            EditorUtility.DisplayProgressBar("SetBundleName", scene, 0.0f);
            AssetImporter ai = AssetImporter.GetAtPath(scene);
            if (ai != null) {
                ai.assetBundleName = Path.GetFileNameWithoutExtension(scene).ToLower();
            }

            EditorUtility.DisplayProgressBar("SetBundleName", scene, 1.0f);
        }

        AssetDatabase.Refresh();
        Debug.Log("SetAseetBundleTag完成");

        EditorUtility.ClearProgressBar();
    }

    public static void SetDependenceBundleName() {
        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Dependence", 0.8f);
        string rootDir = Application.dataPath + "/Build/Dependence/";
        string assetBundleName = "depend_";
        string baseStr = "base";
        string multipleReferenceStr = "AutoMoveAssets/AAMultipleReference";
        string decStr = "Decoration/Tex";
        SetSingleAssetBundelName(rootDir, assetBundleName + baseStr);
        
        List<string> zhengBaos = new List<string> { "CatboxBalloon", "ComicBook", "DouMaoBang", "Emoji", "MainHall"};
        List<string> cengCengFenBaos = new List<string>{"NPC","Potential","Spine","Toys"};
        List<string> yiCengFenBaos = new List<string>{"CollectBook", "AutoMoveAssets", "Decoration","XiaoDongWu"};
        // List<string> yiCengFenBaos = new List<string>{"Decoration"};
        
        string[] childrenDirs = Directory.GetDirectories(rootDir);
        for (int i = 0; i < childrenDirs.Length; ++i) {
            if (childrenDirs[i].Contains(".svn"))
                continue;
            childrenDirs[i] = childrenDirs[i].Replace("\\", "/");
            var childFolderName = childrenDirs[i].Substring(childrenDirs[i].LastIndexOf("/") + 1);
            if (zhengBaos.IndexOf(childFolderName) != -1) {
                SetFolderOneAssetBundelName(childrenDirs[i], assetBundleName + childFolderName);
            }
            else if (cengCengFenBaos.IndexOf(childFolderName) != -1) {
                SetMultiAssetBundelName(childrenDirs[i], assetBundleName + childFolderName + "_");
            }
            else if (yiCengFenBaos.IndexOf(childFolderName) != -1) {
                SetYiCengFloderOneAssetBundleName(childrenDirs[i], assetBundleName + childFolderName, 
                    new string[] {multipleReferenceStr, decStr});
            }
            //默认散包
            else {
                SetAssetBundelName(childrenDirs[i], new string[] {".meta"});
            }
            // if (yiCengFenBaos.IndexOf(childFolderName) != -1) {
            //     SetYiCengFloderOneAssetBundleName(childrenDirs[i], assetBundleName + childFolderName, 
            //         new string[] {multipleReferenceStr, decStr});
            // }
        }

        SetAssetBundelName(rootDir + multipleReferenceStr + "/", new string[] {".meta"});
        SetAssetBundelName(rootDir + decStr + "/", new string[] {".meta"});
        // AssetDatabase.Refresh();
        // Debug.Log("SetAseetBundleTag完成");
        // EditorUtility.ClearProgressBar();
    }

    public static bool CheckFileExsit(string dir, string file) {
        string[] fileNames = Directory.GetFiles(dir);
        for (int k = 0; k < fileNames.Length; ++k) {
            string filename = fileNames[k];
            string ext = Path.GetExtension(filename);
            if (ext.Contains(".meta") || ext.Contains(".DS_Store"))
                continue;
            var crName = Path.GetFileNameWithoutExtension(filename);
            if (crName.Substring(crName.IndexOf("/") + 1) == file)
                return true;
        }

        return false;
    }

    public static void CollectAllSpriteAtlas(string dir) {
        var childrenDirs = Directory.GetDirectories(dir).ToList();
        for (int i = childrenDirs.Count - 1; i >= 0; i--) {
            if (!childrenDirs[i].Contains(".svn") && !childrenDirs[i].Contains("spriteatlas"))
                CollectAllSpriteAtlas(childrenDirs[i]);
        }

        var childrenFiles = Directory.GetFiles(dir).ToList();
        for (int i = childrenFiles.Count - 1; i >= 0; i--) {
            var str = childrenFiles[i];
            if (str.Contains(".meta") || str.Contains(".DS_Store"))
                continue;
            if (str.Contains(".spriteatlas") && oldSpriteAtlaCache != null) {
                oldSpriteAtlaCache.Add(str);
            }
        }
    }

    public static void SaveOldFilelist() {
        var rootname = Application.dataPath + "/Images";

        var childrenDirs = Directory.GetDirectories(rootname).ToList();
        for (int i = childrenDirs.Count - 1; i >= 0; i--) {
            if (childrenDirs[i].Contains(".svn") || childrenDirs[i].Contains("spriteatlas"))
                childrenDirs.RemoveAt(i);
        }
    }

    private static List<string> oldSpriteAtlaCache = new List<string>();

    static void GetExtFile(string path, string ext, List<string> list) {
        var files = Directory.GetFiles(path);
        foreach (var file in files) {
            if (file.Contains(".meta"))
                continue;
            if (file.Contains(ext))
                list.Add(file);
        }

        var dirs = Directory.GetDirectories(path);
        foreach (var dir in dirs) {
            if (dir.Contains("."))
                continue;
            GetExtFile(dir, ext, list);
        }
    }

    public static void SetSpriteAtlasLink() {
        EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;
        var dir = Application.dataPath + "/Images";

        oldSpriteAtlaCache.Clear();
        CollectAllSpriteAtlas(dir);
        SetChildSpriteAtlasLink(dir);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        foreach (var str in oldSpriteAtlaCache) {
            File.Delete(str);
            File.Delete(str + ".meta");
        }

        AssetDatabase.Refresh();
        oldSpriteAtlaCache.Clear();

        SetAltasValue();

        Debug.Log("图集更新完成!");
    }

    //参数置零 不同平台运行时容易改变到 保持svn一致
    public static void SetAltasValue() {
        var dir = Application.dataPath + "/Images";
        List<string> tujiLiebiao = new List<string>();
        GetExtFile(dir, ".spriteatlas", tujiLiebiao);
        foreach (var newFileName in tujiLiebiao) {
            var fullPath = Path.GetFullPath(newFileName);
            using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite)) {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                var buff = Encoding.UTF8.GetString(bytes);
                var contents = buff.Split('\n');
                for (int i = 0; i < contents.Length; i++) {
                    if (contents[i].IndexOf("totalSpriteSurfaceArea") != -1) {
                        contents[i] = "    totalSpriteSurfaceArea: 0";
                        // Debug.Log("totalSpriteSurfaceArea =0 : " + newFileName);
                        break;
                    }
                }

                fs.SetLength(0);
                var newBuffer = Encoding.UTF8.GetBytes(String.Join("\n", contents));
                fs.Write(newBuffer, 0, newBuffer.Length);
            }
        }

        AssetDatabase.Refresh();
    }

    static void SetSpriteAltasReadable(SpriteAtlas spriteAtlas, bool state) {
        SpriteAtlasTextureSettings spriteTexSetting = spriteAtlas.GetTextureSettings();
        spriteTexSetting.readable = state;
        spriteAtlas.SetTextureSettings(spriteTexSetting);
    }

    static void FormatSpriteAltas(SpriteAtlas spriteAtlas) {
        SpriteAtlasPackingSettings spriteAtlasPackingSettings = spriteAtlas.GetPackingSettings();
        spriteAtlasPackingSettings.enableRotation = true;
        spriteAtlasPackingSettings.enableTightPacking = false;
        spriteAtlasPackingSettings.padding = 2;
        SpriteAtlasTextureSettings spriteTexSetting = spriteAtlas.GetTextureSettings();
        spriteTexSetting.sRGB = true;
        spriteTexSetting.filterMode = FilterMode.Bilinear;
        spriteTexSetting.generateMipMaps = false;

        //IOS
        TextureImporterPlatformSettings iosPlatformSettings = spriteAtlas.GetPlatformSettings("iPhone");
        iosPlatformSettings.format = TextureImporterFormat.ASTC_4x4;
        iosPlatformSettings.overridden = true;
        iosPlatformSettings.maxTextureSize = 1024;
        iosPlatformSettings.allowsAlphaSplitting = false;
        iosPlatformSettings.crunchedCompression = false;
        iosPlatformSettings.textureCompression = TextureImporterCompression.Compressed;

        //Android
        TextureImporterPlatformSettings androidPlatformSettings = spriteAtlas.GetPlatformSettings("Android");
        androidPlatformSettings.format = TextureImporterFormat.ETC2_RGBA8;
        androidPlatformSettings.overridden = true;
        androidPlatformSettings.maxTextureSize = 1024;
        androidPlatformSettings.allowsAlphaSplitting = false;
        androidPlatformSettings.crunchedCompression = false;
        androidPlatformSettings.textureCompression = TextureImporterCompression.Compressed;

        spriteAtlas.SetPackingSettings(spriteAtlasPackingSettings);
        spriteAtlas.SetTextureSettings(spriteTexSetting);
        spriteAtlas.SetPlatformSettings(iosPlatformSettings);
        spriteAtlas.SetPlatformSettings(androidPlatformSettings);
        spriteAtlas.SetIncludeInBuild(false);
    }

    public static void SetChildSpriteAtlasLink(string dir) {
        var childrenDirs = Directory.GetDirectories(dir).ToList();
        for (int i = childrenDirs.Count - 1; i >= 0; i--) {
            if (childrenDirs[i].Contains(".svn") || childrenDirs[i].Contains("spriteatlas")
                                                 || childrenDirs[i].Contains("login") || childrenDirs[i].Contains("_dontUseAltas"))
                childrenDirs.RemoveAt(i);
            else
                SetChildSpriteAtlasLink(childrenDirs[i]);
        }

        var newFile = NgTool.FormatPath(dir.Substring(dir.IndexOf("Image")));

        var newFileName = "Assets/" + newFile + "/" + newFile.Replace("/", "_") + ".spriteatlas";
        // Debug.Log(newFileName);

        var files = Directory.GetFiles(dir);
        EditorUtility.DisplayProgressBar("制作图集", newFileName, 0.0f);

        var readable = false;
        List<Object> assets = new List<Object>();

        foreach (var f in files) {
            if (f.Contains(".meta") || f.Contains(".spriteatlas") || f.Contains(".DS_Store") || f.Contains(".xml"))
                continue;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NgTool.ToUnityRelativePath(f));
            TextureImporter ti = (TextureImporter) TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            if (tex.width <= 1024 && tex.height <= 1024) {
                if (!readable)
                    readable = ti.isReadable;
                assets.Add(tex);
            }
        }

        if (assets.Count > 0) {
            if (assets.Count > 2) {
                assets.Sort((a1, a2) => {
                    return a1.name.CompareTo(a2.name);
                });
            }

            SpriteAtlas spriteAtlas = null;

            //找到错误项 删除 重新打包
            for (int i = oldSpriteAtlaCache.Count - 1; i >= 0; i--) {
                var str = NgTool.FormatPath(oldSpriteAtlaCache[i]);
                //从Image/ 后去比对 防止重名
                var name = str.Substring(str.IndexOf("Images"));
                if (name == newFileName.Substring(newFileName.IndexOf("Images"))) {
                    // Debug.Log("找到: " + newFileName);
                    oldSpriteAtlaCache.RemoveAt(i);
                    spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(newFileName);
                    break;
                }
            }

            if (!spriteAtlas) {
                spriteAtlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(spriteAtlas, newFileName);
            }

            FormatSpriteAltas(spriteAtlas);
            SetSpriteAltasReadable(spriteAtlas, readable);
            var oldAssets = spriteAtlas.GetPackables();
            spriteAtlas.Remove(oldAssets);
            spriteAtlas.Add(assets.ToArray());
            // var path = Path.GetFullPath(newFileName);
            // var bytes = File.ReadAllBytes(path);
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayProgressBar("制作图集", newFileName, 1.0f);
    }

    public static void BuildAndroid() {
        Build(BuildTarget.Android);
    }

    public static void BuildIOS() {
        Build(BuildTarget.iOS);
    }

    public static void BuildPC() {
        Build(BuildTarget.StandaloneWindows);
    }

    [MenuItem("资源/Clear AssetBundelName")]
    public static void ClearAssetBundelName() {
        ClearAssetBundelName(Application.dataPath + "/Art/");
        ClearAssetBundelName(Application.dataPath + "/Build/");
        ClearAssetBundelName(Application.dataPath + "/Images/");
        AssetDatabase.Refresh();
    }

    public static void CacheFilelistHistory() {
        var filelistHDir = Application.dataPath + "/FilelistHistory";
#if UNITY_ANDROID
        filelistHDir = Path.Combine(filelistHDir, "android");
#elif UNITY_IOS
        filelistHDir = Path.Combine(filelistHDir, "ios");
#else
        filelistHDir = Path.Combine(filelistHDir, "other");
#endif
        if (!Directory.Exists(filelistHDir))
            Directory.CreateDirectory(filelistHDir);
        var resV = updateInfo.resVer;
        var fN = filelistHDir + "/" + resV + ".bytes";
        File.Copy(Application.streamingAssetsPath + "/filelist.bytes", fN, true);
        AssetDatabase.Refresh();
    }

    public static List<string> SceneLevels = new List<string>() {
        "Assets/Scenes/PVEScene.unity",
        "Assets/Scenes/DrawScene.unity",
        "Assets/Scenes/CatBagScene.unity",
        "Assets/Scenes/PotentialScene.unity",
        "Assets/Scenes/PVE_summer.unity",
        "Assets/Scenes/LobbyScene_aglint.unity",
        "Assets/Scenes/LobbyScene_aglint_autumn.unity",
        "Assets/Scenes/LobbyScene_aglint_christmas.unity",
        "Assets/Scenes/LobbyScene_aglint_newyear.unity",
        "Assets/Scenes/LobbyScene_aglint_princekin.unity",
        "Assets/Scenes/LobbyScene_aglint_summer.unity",
        "Assets/Scenes/LobbyScene_aglint_winter.unity",
        "Assets/Scenes/PVE_autumn.unity",
        "Assets/Scenes/LoveHouse.unity",
        "Assets/Scenes/NurseryScene.unity",
        "Assets/Scenes/HouseScene_ApartmentBranch.unity",
        "Assets/Art/CatIsland/Scene/ActiveObj/MusicScene.unity"
    };

    static string GetTimeSpanStr(TimeSpan ts) {
        return "相差:"
               + ts.Days.ToString() + "天"
               + ts.Hours.ToString() + "小时"
               + ts.Minutes.ToString() + "分钟"
               + ts.Seconds.ToString() + "秒";
    }

    public static void BuildScene(BuildTarget target) {
        // 打包场景
        EditorUtility.DisplayProgressBar("打包场景", "scene", 0);
        string outPath = GetAssetBundleOutputPath();
        string outScenePath = Path.Combine(outPath, "Scene");

        if (Directory.Exists(outScenePath)) {
            Directory.Delete(outScenePath, true);
        }

        Directory.CreateDirectory(outScenePath);

        for (int i = 0; i < SceneLevels.Count; i++) {
            var index = SceneLevels[i].LastIndexOf("/");
            var scenePath = SceneLevels[i].Substring(index + 1);
            scenePath = "/" + Path.GetFileNameWithoutExtension(scenePath).ToLower();
            EditorUtility.DisplayProgressBar("打包场景", scenePath + ": " + (i + 1) + "/" + SceneLevels.Count,
                (i + 1) / SceneLevels.Count);
            BuildPipeline.BuildPlayer(new string[] {SceneLevels[i]},
                outScenePath + scenePath, target, BuildOptions.BuildAdditionalStreamedScenes);
            if (!File.Exists(outScenePath + scenePath)) {
                Debug.LogError("打包场景 失败了: " + outScenePath + scenePath);
                break;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    static void Build(BuildTarget target) {
        if (target != EditorUserBuildSettings.activeBuildTarget) {
            Debug.LogError("编译平台和当前平台不匹配");
            return;
        }

        var t1 = DateTime.Now;
        Debug.Log("开始打包Bundle: " + t1.ToString("yyyyMMdd_HHmmss"));

        string outPath = GetAssetBundleOutputPath();

        if (Directory.Exists(outPath))
            Directory.Delete(outPath, true);
        Directory.CreateDirectory(outPath);

        //ClearAssetBundelName(Application.dataPath + "/Build/");

        // Debug.Log("RebuildBundleName: " + RebuildBundleName);
        if (RebuildBundleName)
            BuildAssetBundleName();

        Debug.Log("BuildAssetBundleName完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Script", 0.0f);
        BuildScript();

        Debug.Log("BuildScript完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        EditorUtility.DisplayProgressBar("BuildAssetBundleName", "Script", 0.9f);
        SetAssetBundelName(Application.dataPath + "/luabuild", new string[] {".meta"}, "script");

        Debug.Log("SetAssetBundelName完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        AssetDatabase.Refresh();

        Debug.Log("Refresh完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        //OptimizeTool.OptimizeAll();

        EditorUtility.DisplayProgressBar("图集优化", "Script", 0.8f);
        SetAltasValue();

        Debug.Log("SetAltasValue完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        EditorUtility.DisplayProgressBar("BuildAssetBundles", "BuildAssetBundles", 0.0f);
        EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;
        BuildPipeline.BuildAssetBundles(outPath, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle, target);
        EditorUtility.ClearProgressBar();

        Debug.Log("BuildAssetBundles完成: " + GetTimeSpanStr(DateTime.Now - t1));
        t1 = DateTime.Now;

        File.Move(GetOriAssetBundleManifestPath(), GetAssetBundleManifestPath());
        CopyAssetToPatcher();
        Debug.Log("CopyAssetToPatcher完成: " + GetTimeSpanStr(DateTime.Now - t1));
    }

    public static void CopyOutputBundleFolder(string folderName) {
        if (Directory.Exists(folderName))
            Directory.Delete(folderName, true);
        Directory.CreateDirectory(folderName);

        EditorUtility.DisplayProgressBar("拷贝", "AssetBundle", 0.0f);

        var destBundleManifestPath = Path.Combine(folderName, "AssetBundle");
        File.Copy(GetAssetBundleManifestPath(), destBundleManifestPath, true);

        AssetBundle ab = AssetBundle.LoadFromFile(destBundleManifestPath);
        AssetBundleManifest assetBundleManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        string[] abNames = assetBundleManifest.GetAllAssetBundles();
        for (int i = 0; i < abNames.Length; ++i) {
            EditorUtility.DisplayProgressBar("拷贝", abNames[i], (float) i / (float) abNames.Length);
            File.Copy(GetAssetBundleFilePath(abNames[i]), Path.Combine(folderName, abNames[i]), true);
        }

        EditorUtility.ClearProgressBar();

        ab.Unload(true);
    }

    private static void CopyAssetToPatcher() {
        string floder = "/pc";
#if UNITY_ANDROID
        floder = "/android";
#elif UNITY_IOS
        floder = "/ios";
#endif
        string dir = Application.dataPath.Replace("Assets", "Patcher");
        var resVer = updateInfo.resVer;
        var path = Path.Combine(dir, resVer + floder);
        //copyDirectory(Application.streamingAssetsPath, dir, new string[] {".meta", ".mp4", ".DS_Store"});

        CopyOutputBundleFolder(Path.Combine(path, "AssetBundle"));

        CreateFileHash();
        CacheFilelistHistory();
        AssetDatabase.Refresh();
        Debug.Log("资源拷贝完成 " + System.DateTime.Now);
    }

    //打包后文件实际路径
    private static string GetAssetBundleFilePath(string filename) {
        string outPath = GetAssetBundleOutputPath();
        return Path.Combine(outPath, filename);
    }

    //打包后Manifest实际路径(改名后)
    private static string GetAssetBundleManifestPath() {
        string outPath = GetAssetBundleOutputPath();
        return Path.Combine(outPath, "AssetBundle");
    }

    //打包后Manifest实际路径(改名前)
    private static string GetOriAssetBundleManifestPath() {
        string outPath = GetAssetBundleOutputPath();
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            return Path.Combine(outPath, "AssetBundle_Android");
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            return Path.Combine(outPath, "AssetBundle_iOS");
        return Path.Combine(outPath, "AssetBundle_PC");
    }

    //打包目标文件夹
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

    public static void CreateFileHash() {
        EditorUtility.DisplayProgressBar("CreateFileHash", "CreateFileHash", 0.0f);

        var resVer = updateInfo.resVer;

        AssetBundle ab = AssetBundle.LoadFromFile(GetAssetBundleManifestPath());
        AssetBundleManifest assetBundleManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] abNames = assetBundleManifest.GetAllAssetBundles();
        Dictionary<string, FileVerInfo> fileVerInfoList = new Dictionary<string, FileVerInfo>();
        FileVerInfo mfvi = new FileVerInfo();
        byte[] mbuf = File.ReadAllBytes(GetAssetBundleManifestPath());
        mfvi.filename = "AssetBundle";
        mfvi.hash = GetMD5HashFromFile(mbuf);
        mfvi.size = mbuf.Length;
        mfvi.downloadDir = resVer;
        fileVerInfoList.Add(mfvi.filename, mfvi);
        int crc = 0;
        foreach (var name in abNames) {
            byte[] buf = File.ReadAllBytes(GetAssetBundleFilePath(name));
            //脚本可以单独热更 走md5
            var hash = name == "script" ? GetMD5HashFromFile(buf) : assetBundleManifest.GetAssetBundleHash(name).ToString();
            FileVerInfo fvi = new FileVerInfo();
            fvi.filename = name;
            fvi.hash = hash;
            fvi.size = buf.Length;
            fvi.downloadDir = resVer;
            EditorUtility.DisplayProgressBar("CreateFileHash", name, (float) ++crc / (float) abNames.Length);
            fileVerInfoList.Add(name, fvi);
        }

        EditorUtility.ClearProgressBar();
        ab.Unload(true);
        FileVerInfo.Write(Application.streamingAssetsPath + "/filelist.bytes", fileVerInfoList);
    }

    public static void RecursiveLoadObject(string path, List<Object> assetObjs, string[] filter) {
        if (!Directory.Exists(path))
            return;
        string[] names = Directory.GetFiles(path);

        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            string ext = Path.GetExtension(filename);
            List<string> fitlerList = new List<string>(filter);
            int idx = fitlerList.IndexOf(ext.ToLower());
            if (idx != -1) {
                string apath = filename.Substring(filename.IndexOf("Assets"));
                Object o = AssetDatabase.LoadMainAssetAtPath(apath);
                assetObjs.Add(o);
            }
        }

        foreach (string dir in dirs) {
            if (dir.Contains(".svn"))
                continue;
            RecursiveLoadObject(dir, assetObjs, filter);
        }
    }

    [MenuItem("资源/优化特效Shader")]
    public static void ChangeEffectShader() {
        List<Object> assetObjs = new List<Object>();
        RecursiveLoadObject(Application.dataPath + "/Build/Effects/", assetObjs, new string[] {".prefab"});
        foreach (var o in assetObjs) {
            GameObject go = o as GameObject;
            Renderer[] smr = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in smr) {
                foreach (var m in r.sharedMaterials) {
                    try {
                        if (m.shader != null && m.shader.name == "Particles/Additive")
                            m.shader = Shader.Find("Mobile/Particles/Additive");
                        if (m.shader != null && m.shader.name == "Particles/Alpha Blended")
                            m.shader = Shader.Find("Mobile/Particles/Alpha Blended");
                    }
                    catch (System.Exception e) {
                        Debug.Log(e);
                    }
                }
            }
        }

        Debug.Log("优化特效Shader完成");
    }

    [MenuItem("资源/还原特效Shader")]
    public static void RevertEffectShader() {
        List<Object> assetObjs = new List<Object>();
        RecursiveLoadObject(Application.dataPath + "/Build/Effects/", assetObjs, new string[] {".prefab"});
        foreach (var o in assetObjs) {
            GameObject go = o as GameObject;
            Renderer[] smr = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in smr) {
                foreach (var m in r.sharedMaterials) {
                    try {
                        if (m.shader != null && m.shader.name == "Mobile/Particles/Additive")
                            m.shader = Shader.Find("Particles/Additive");
                        if (m.shader != null && m.shader.name == "Mobile/Particles/Alpha Blended")
                            m.shader = Shader.Find("Particles/Alpha Blended");
                    }
                    catch (System.Exception e) {
                        Debug.Log(e);
                    }
                }
            }
        }

        Debug.Log("还原特效Shader完成");
    }

    static string versionPath {
        get {
            var s = Path.Combine(Application.streamingAssetsPath, UpdateDownload.UpdateFilename);
            return s;
        }
    }

    public static void RebuildScriptBundle() {
        string tarPath = Path.Combine(GetAssetBundleOutputPath(), "RebuildScriptTemp");
        if (Directory.Exists(tarPath))
            Directory.Delete(tarPath, true);
        Directory.CreateDirectory(tarPath);

        BuildScript();
        string[] scriptsName = Directory.GetFiles(Path.Combine(Application.dataPath, "luabuild"));
        var collection = scriptsName.Where(s => s.EndsWith("bytes")).ToArray();
        AssetBundleBuild scriptBundleBuild = new AssetBundleBuild();
        scriptBundleBuild.assetBundleName = "script";
        scriptBundleBuild.assetNames = new string[collection.Length];
        scriptBundleBuild.addressableNames = new string[collection.Length];
        for (int i = 0; i < collection.Length; i++) {
            scriptBundleBuild.assetNames[i] = NgTool.ToUnityRelativePath(collection[i]);
            scriptBundleBuild.addressableNames[i] = Path.GetFileNameWithoutExtension(collection[i]);
        }

        BuildAssetBundleOptions bo = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle;
        BuildPipeline.BuildAssetBundles(tarPath, new AssetBundleBuild[] {scriptBundleBuild}, bo, EditorUserBuildSettings.activeBuildTarget);
        File.Copy(Path.Combine(tarPath, "script"), GetAssetBundleFilePath("script"), true);

        byte[] result;
        string filePath = Path.Combine(Application.streamingAssetsPath, "filelist.bytes");
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
            result = new byte[fs.Length];
            fs.Read(result, 0, result.Length);
        }

        Dictionary<string, FileVerInfo> fileVerInfoList = new Dictionary<string, FileVerInfo>();
        using (MemoryStream ms = new MemoryStream(result))
            fileVerInfoList = FileVerInfo.Read(ms);

        //更新脚本下载路径
        //更新索引
        var newFileInfo = new FileVerInfo();
        newFileInfo.filename = "script";
        newFileInfo.downloadDir = updateInfo.resVer;
        byte[] buf = File.ReadAllBytes(GetAssetBundleFilePath("script"));
        newFileInfo.hash = GetMD5HashFromFile(buf);
        newFileInfo.size = buf.Length;

        fileVerInfoList["script"] = newFileInfo;
        FileVerInfo.Write(Application.streamingAssetsPath + "/filelist.bytes", fileVerInfoList);

        CacheFilelistHistory();
        AssetDatabase.Refresh();
        Debug.Log("热更脚本成功: " + updateInfo.resVer);
    }
}