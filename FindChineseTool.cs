using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
 
public class FindChineseTool
{
    [MenuItem("Tools/查找代码中文")]
    public static void Pack()
    {
        Rect wr = new Rect(300, 400, 400, 100);
        FindChineseWindow window = (FindChineseWindow)EditorWindow.GetWindowWithRect(typeof(FindChineseWindow), wr, true, "查找项目中的中文字符");
        window.Show();
    }
}
 
public class FindChineseWindow : EditorWindow
{
    private ArrayList csList = new ArrayList();
    private int eachFrameFind = 4;
    private int currentIndex = 0;
    private bool isBeginUpdate = false;
    private string outputText;
    public string filePath = "/ClientLogic";
    private string strForShader = "";
 
 
    private void GetAllFile(DirectoryInfo dir)
    {
        FileInfo[] allFile = dir.GetFiles();
        foreach (FileInfo fi in allFile)
        {
            if (fi.DirectoryName.Contains("FindChineseTool")
                || fi.Name.Contains("functions")
                || fi.Name.Contains("GamePropUtil"))//排除指定名称的代码  
            {
                continue;
            }
            if (fi.FullName.IndexOf(".meta") == -1 && fi.FullName.IndexOf(".lua") != -1)
            {
                csList.Add(fi.DirectoryName + "/" + fi.Name);
            }
        }
        DirectoryInfo[] allDir = dir.GetDirectories();
        foreach (DirectoryInfo d in allDir)
        {
            GetAllFile(d);
        }
    }
    public void OnGUI()
    {
        filePath = EditorGUILayout.TextField("路径：", filePath);
 
        EditorGUILayout.Space();
        EditorGUILayout.Space();
 
        if (GUILayout.Button("开始遍历目录"))
        {
            csList.Clear();
            DirectoryInfo d = new DirectoryInfo(Application.dataPath + filePath);
            GetAllFile(d);
            GetAllFile(d);
            outputText = "游戏内代码文件的数量：" + csList.Count;
            isBeginUpdate = true;
            outputText = "开始遍历项目";
            // string s = "GameMgrInst.Alert(\"找不到SetActive对象\")";
            // Debug.Log((s.IndexOf("Alert") == 0));
        }
        GUILayout.Label(outputText, EditorStyles.boldLabel);
    }
    void Update()
    {
        if (isBeginUpdate &&currentIndex< csList.Count)
        {
            int count = (csList.Count - currentIndex) > eachFrameFind ? eachFrameFind : (csList.Count - currentIndex);
            for (int i = 0; i< count; i++)
            {
                string url = csList[currentIndex].ToString();
                currentIndex = currentIndex + 1;
                url = url.Replace("\\", "/");
                printChinese(url);
            }
            if (currentIndex >= csList.Count)
            {
                isBeginUpdate = false;
                currentIndex = 0;
                outputText = "遍历结束，总共" + csList.Count;
            }
        }
    }
    private bool HasChinese(string str)
    {
        return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
    }
    private Regex regex = new Regex("\"[^\"]*\"");
    private void printChinese(string path)
    {
        if (path.IndexOf("LuaPanda") != -1) {
            return;
        }
        if (File.Exists(path))
        {
            string[] fileContents = File.ReadAllLines(path, Encoding.UTF8);
            int count = fileContents.Length;
            for (int i = 0; i< count; i++)
            {
                string printStr = fileContents[i].Trim();
                if (printStr.IndexOf("//") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("Debug.Log") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("log") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("--") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("print") != -1)  //说明是注释
                    continue;
                if (printStr.IndexOf("logError") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("table.print") == 0)  //说明是注释
                    continue;
                if (printStr.IndexOf("Log") != -1)  //说明是注释
                    continue;
                if (printStr.IndexOf("Alert") != -1)
                    continue;
                if (printStr.IndexOf("assert") != -1)
                    continue;
                if (printStr.IndexOf("xpcall") != -1)
                    continue;
                if (printStr.IndexOf("error") == 0)  //说明是注释
                    continue;
                
                MatchCollection matches = regex.Matches(printStr);
                foreach (Match match in matches)
                {
                    if (HasChinese(match.Value))
                    {
                        string[] fullPath = path.Split('/');
                        path = fullPath[fullPath.Length - 1];
                        Debug.Log("路径:" + path + " 行数:" + (i+1) + " 内容:" + printStr);
                        break;
                    }
                }
            }
            fileContents = null;
        }
    }
}