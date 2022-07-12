using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildApp
{
    private static string m_AppName = PlayerSettings.productName;
    public static string m_AndroidPath = Application.dataPath + "/../BuildTarget/Android/";
    public static string m_IOSPath = Application.dataPath + "/../BuildTarget/IOS/";
    public static string m_WindowsPath = Application.dataPath + "/../BuildTarget/Windows/";

    [MenuItem("Tools/打包相关/Version写入")]
    public static void TVersion()
    {
        SaveVersion(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/打包相关/生成ServerInfo")]
    public static void TServerInfo()
    {
        string savePath = Application.dataPath + "/Resources/ServerInfo.xml";
        ServerInfo testInfo = new ServerInfo();

        var versionInfo = new VersionInfo();
        versionInfo.Version = "1";
        versionInfo.Patches = new Patches[1];
        testInfo.GameVersion = new []{versionInfo};
        SerializeEx.XmlSerialize(savePath, testInfo);
        AssetDatabase.Refresh();
    }

    static void SaveVersion(string version, string package)
    {
        string content = "Version|" + version + ";PackageName|" + package + ";";
        string savePath = Application.dataPath + "/Resources/Version.txt";
        string oneLine = "";
        string all = "";
        using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8))
            {
                all = sr.ReadToEnd();
                oneLine = all.Split('\r')[0];
            }
        }

        using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate))
        {
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                if (string.IsNullOrEmpty(all))
                {
                    all = content;
                }
                else
                {
                    all = all.Replace(oneLine, content);
                }

                sw.Write(all);
            }
        }
    }
}