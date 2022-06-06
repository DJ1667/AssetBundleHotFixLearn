using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using FileMode = System.IO.FileMode;

public class BundleEditor
{
    private static string m_BunleTargetPath = Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString();

    private static string ABCONFIGPATH = "Assets/AssetBundleFram/Config/ABConfig.asset";

    //所有AB包的路径
    private static List<string> m_AllFileAbPathList = new List<string>();

    //存储所有有效路径
    private static List<string> m_ConfigFilePath = new List<string>();

    [MenuItem("Tools/打包相关/打AB包", false, 2000)]
    public static void NormalBuild()
    {
        Build();
    }

    /// <summary>
    /// 打包入口
    /// </summary>
    public static void Build()
    {
        m_AllFileAbPathList.Clear();
        m_ConfigFilePath.Clear();
        
        //key是包名，value是包的路径  所有文件夹ab包Dict
        Dictionary<string, string> allFileDict = new Dictionary<string, string>();
        //单个Prefab的AB包和他依赖的资源
        Dictionary<string, List<string>> allPrefabDict = new Dictionary<string, List<string>>();

        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        foreach (var fileDir in abConfig.m_AllFileDirAB)
        {
            if (allFileDict.ContainsKey(fileDir.abName))
            {
                Debug.LogError("AB包名重复：" + fileDir.abName);
            }
            else
            {
                allFileDict.Add(fileDir.abName, fileDir.path);
                m_AllFileAbPathList.Add(fileDir.path);
                m_ConfigFilePath.Add(fileDir.path);
            }
        }

        //处理所有与预制体相关的资源
        string[] prefabGuidArray = AssetDatabase.FindAssets("t:Prefab", abConfig.m_PrefabPath.ToArray());
        for (int i = 0; i < prefabGuidArray.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(prefabGuidArray[i]);

            //进度条
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab：" + path, (float) i / prefabGuidArray.Length);
            m_ConfigFilePath.Add(path);
            if (!IsContainInAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDependPath = AssetDatabase.GetDependencies(path);
                List<string> allDependPathList = new List<string>();
                for (int j = 0; j < allDependPath.Length; j++)
                {
                    var dependPath = allDependPath[j];
                    if (!IsContainInAllFileAB(dependPath))
                    {
                        m_AllFileAbPathList.Add(dependPath);
                        allDependPathList.Add(dependPath);
                    }
                }

                if (allPrefabDict.ContainsKey(obj.name))
                {
                    Debug.LogError("Prefab名重复：" + obj.name);
                }
                else
                {
                    allPrefabDict.Add(obj.name, allDependPathList);
                }
            }
        }

        foreach (var name in allFileDict.Keys)
        {
            SetABName(name, allFileDict[name]);
        }

        foreach (var name in allPrefabDict.Keys)
        {
            SetABName(name, allPrefabDict[name]);
        }

        BuildAssetBundle();

        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字: " + oldABNames[i], (float) i / oldABNames.Length);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 打AB包
    /// </summary>
    private static void BuildAssetBundle()
    {
        //获取所有的AB包名
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();

        Dictionary<string, string> resPathDict = new Dictionary<string, string>();
        for (int i = 0; i < allBundles.Length; i++)
        {
            var bundleName = allBundles[i];
            //获取该包名下的所有资源路径
            var allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                var path = allBundlePath[j];
                if (path.EndsWith(".cs"))
                    continue;

                Debug.Log("此AB包：" + bundleName + "  下面包含的资源文件路径：" + path);
                resPathDict.Add(path, bundleName);
            }
        }

        if (!Directory.Exists(m_BunleTargetPath))
        {
            Directory.CreateDirectory(m_BunleTargetPath);
        }

        DeleteAB();
        //生成配置
        WriteData(resPathDict);
        //生成路径Str类
        // CreateABAssetPath.ConvertToString(resPathDict);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(m_BunleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
            Debug.LogError("打包失败");
        }
        else
        {
            Debug.Log("打包完毕");

            DeleteManifest();

            EncryptAB();
        }
    }

    /// <summary>
    /// 删除所有的.manifest文件
    /// </summary>
    private static void DeleteManifest()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Name.EndsWith(".manifest"))
            {
                File.Delete(files[i].FullName);
            }
        }
    }

    /// <summary>
    /// 写入一些ab的数据
    /// </summary>
    /// <param name="resPathDict"></param>
    private static void WriteData(Dictionary<string, string> resPathDict)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();

        foreach (var path in resPathDict.Keys)
        {
            if (!IsValidPath(path))
                continue;

            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = Crc32.GetCrc32(path);
            abBase.ABName = resPathDict[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDepends = new List<string>();
            string[] resDepends = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDepends.Length; i++)
            {
                string tempPath = resDepends[i];
                if (tempPath == path || path.EndsWith(".cs"))
                    continue;

                string abName = "";
                if (resPathDict.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDict[path])
                        continue;
                    if (!abBase.ABDepends.Contains(abName))
                        abBase.ABDepends.Add(abName);
                }
            }

            config.ABList.Add(abBase);
        }

        //写入Xml
        string xmlPath = Application.dataPath + "/AssetBundleConfig.xml";
        if (File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        //写入二进制
        foreach (var abBase in config.ABList)
        {
            abBase.Path = "";
        }

        string bytePath = Application.dataPath + "/AssetBundleConfig.bytes"; //修改成可可配置
        FileStream fs = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        fs.Seek(0, SeekOrigin.Begin);
        fs.SetLength(0);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
        AssetDatabase.Refresh();
        SetABName("assetbundleconfig", bytePath);
    }

    /// <summary>
    /// 检查ab资源路径是否合法
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool IsValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFilePath.Count; i++)
        {
            if (path.Contains(m_ConfigFilePath[i]))
                return true;
        }

        Debug.LogError("路径不在配置文件中：" + path);
        return false;
    }

    /// <summary>
    /// 删除无用的AB包
    /// </summary>
    private static void DeleteAB()
    {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            if (ContainABName(file.Name, allBundles) || file.Name.EndsWith(".meta") || file.Name.EndsWith(".manifest") ||
                file.Name.EndsWith(".assetbundleconfig"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经被删除或改名：" + file.Name);
                if (File.Exists(file.FullName))
                    File.Delete(file.FullName);
                if (File.Exists(file.FullName + ".manifest"))
                    File.Delete(file.FullName + ".manifest");
            }
        }
    }

    /// <summary>
    /// 遍历存放ab的文件夹中的文件名与设置的所有AB包名进行检查判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="allABName"></param>
    /// <returns></returns>
    private static bool ContainABName(string name, string[] allABName)
    {
        for (int i = 0; i < allABName.Length; i++)
        {
            if (string.Equals(name, allABName[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 设置AB包名
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pathList"></param>
    private static void SetABName(string name, List<string> pathList)
    {
        for (int i = 0; i < pathList.Count; i++)
        {
            SetABName(name, pathList[i]);
        }
    }

    /// <summary>
    /// 设置AB包名
    /// </summary>
    /// <param name="name"></param>
    /// <param name="path"></param>
    private static void SetABName(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
        {
            Debug.LogError("路径不存在：" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
            // Debug.Log("设置包名：" + name + "  " + path);
        }
    }

    /// <summary>
    /// 检查是否包含在所有AB包中
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool IsContainInAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAbPathList.Count; i++)
        {
            var abPath = m_AllFileAbPathList[i];
            if (string.Equals(path, abPath) || (path.Contains(abPath) && (path.Replace(abPath, "")[0] == '/')))
                return true;
        }

        return false;
    }

    #region 加密相关

    /// <summary>
    /// 加密AB包
    /// </summary>
    public static void EncryptAB()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (!files[i].Name.EndsWith(".manifest") && !files[i].Name.EndsWith(".meta"))
            {
                AES.AESFileEncrypt(files[i].FullName, "MrD");
            }
        }

        Debug.Log("加密完成");
    }

    /// <summary>
    /// 解密AB包
    /// </summary>
    public static void DecryptAB()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (!files[i].Name.EndsWith(".manifest") && !files[i].Name.EndsWith(".meta"))
            {
                AES.AESFileDecrypt(files[i].FullName, "MrD");
            }
        }

        Debug.Log("解密完成");
    }

    #endregion
}