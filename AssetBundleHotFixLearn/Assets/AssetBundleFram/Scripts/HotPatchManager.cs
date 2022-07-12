using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class HotPatchManager : Singleton<HotPatchManager>
{
    private MonoBehaviour m_Mono;

    private string m_UnPackPath = Application.persistentDataPath + "/Origin";
    private string m_DownLoadPath = Application.persistentDataPath + "/DownLoad";

    private string m_CurVersion;

    public string CurVersion
    {
        get { return m_CurVersion; }
    }

    private string m_CurPackName;

    private string m_ServerXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
    private string m_LocalXmlPath = Application.persistentDataPath + "/LocalInfo.xml";

    private ServerInfo m_ServerInfo;
    private ServerInfo m_LocalInfo;
    private VersionInfo m_GameVersion;
    private Patches m_CurPatches;

    //所有需要热更的资源
    private Dictionary<string, Patch> m_HotFixDict = new Dictionary<string, Patch>();

    //所有需要下载的东西
    private List<Patch> m_DownLoadList = new List<Patch>();

    //所有需要下载的东西
    private Dictionary<string, Patch> m_DownLoadDict = new Dictionary<string, Patch>();

    //服务器上的资源名对应的MD5，用于下载后MD5校验
    private Dictionary<string, string> m_DownLoadMD5Dict = new Dictionary<string, string>();

    //计算需要解压的文件
    private List<string> m_UnPackedList = new List<string>();

    //原包记录的MD5码
    private Dictionary<string, ABMD5Base> m_PackedMd5 = new Dictionary<string, ABMD5Base>();

    //服务器列表获取错误回调
    public Action ServerInfoErrorCallBack;

    //文件下载出错回调
    public Action<string> ItemErrorCallBack;

    //下载完成回调
    public Action LoadOverCallBack;

    //存储已经下载的资源
    public List<Patch> m_AlreadyDownLoadList = new List<Patch>();

    //是否开始下载
    public bool StartDownLoad = false;

    //尝试重新下载次数
    private int m_TryDownLoadCount = 0;
    private const int DOWNLOADCOUNT = 4;

    //当前正在下载的资源
    private DownLoadAssetBundle m_CurDownLoad = null;

    // 需要下载资源个数
    public int LoadFileCount { get; set; } = 0;

    // 需要下载资源大小 KB
    public float LoadSumSize { get; set; } = 0;

    //是否开始解压
    public bool StartUnPack = false;

    //解压文件总大小
    public float UnPackSumSize { get; set; } = 0;

    //已解压大小
    public float AlreadyUnPackSize { get; set; } = 0;

    public void Init(MonoBehaviour mono)
    {
        m_Mono = mono;
        ReadMD5();
    }

    /// <summary>
    /// 读取本地资源MD5码
    /// </summary>
    private void ReadMD5()
    {
        m_PackedMd5.Clear();

        var md5 = Resources.Load<TextAsset>("ABMD5");
        if (md5 == null)
        {
            Debug.LogError($"未读取到ABMD5文件   Resources/ABMD5不存在");
            return;
        }

        using (MemoryStream stream = new MemoryStream(md5.bytes))
        {
            BinaryFormatter bf = new BinaryFormatter();
            ABMD5 abmd5 = bf.Deserialize(stream) as ABMD5;
            foreach (var abmd5Base in abmd5.ABMD5List)
            {
                m_PackedMd5.Add(abmd5Base.Name, abmd5Base);
            }
        }
    }

    /// <summary>
    /// 版本检查  判断是否需要热更
    /// </summary>
    /// <param name="hotCallBack"></param>
    public void CheckVersion(Action<bool> hotCallBack = null)
    {
        m_TryDownLoadCount = 0;
        m_HotFixDict.Clear();
        ReadVersion();

        m_Mono.StartCoroutine(ReadXml(() =>
        {
            if (m_ServerInfo == null)
            {
                ServerInfoErrorCallBack?.Invoke();
                return;
            }

            foreach (var versionInfo in m_ServerInfo.GameVersion)
            {
                //找到当前大版本对应的热更内容
                if (versionInfo.Version == m_CurVersion)
                {
                    m_GameVersion = versionInfo;
                    break;
                }
            }

            GetHotAB();

            ComputeDownLoad();

            LoadFileCount = m_DownLoadList.Count;
            LoadSumSize = m_DownLoadList.Sum(x => x.Size);

            hotCallBack?.Invoke(m_DownLoadList.Count > 0);
        }));
    }

    /// <summary>
    /// 读取打包时的版本
    /// </summary>
    private void ReadVersion()
    {
        var versionText = Resources.Load<TextAsset>("Version");
        if (versionText == null)
        {
            Debug.LogError($"未读取到Version文件   Resources/Version不存在");
            return;
        }

        string[] all = versionText.text.Split('\n');
        if (all.Length > 0)
        {
            string[] infoList = all[0].Split(';');
            if (infoList.Length >= 2)
            {
                m_CurVersion = infoList[0].Split('|')[1];
                m_CurPackName = infoList[1].Split('|')[1];
            }
        }
    }

    /// <summary>
    /// 读取服务器资源信息
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator ReadXml(Action callback)
    {
        //服务器热更信息配置文件
        string xmlUrl = "http://127.0.0.1/AssetBundleHotFixLearn/ServerInfo.xml";

        var webRequest = UnityWebRequest.Get(xmlUrl);
        webRequest.timeout = 30;
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError)
        {
            Debug.LogError("下载服务器热更信息配置文件失败 " + webRequest.error);
        }
        else
        {
            FileTool.CreateFile(m_ServerXmlPath, webRequest.downloadHandler.data);
            if (File.Exists(m_ServerXmlPath))
            {
                m_ServerInfo = SerializeEx.XmlDeserialize(m_ServerXmlPath, typeof(ServerInfo)) as ServerInfo;
            }
            else
            {
                Debug.LogError("读取服务器热更信息配置文件失败");
            }
        }

        callback?.Invoke();
    }

    /// <summary>
    /// 获取所有热更包信息
    /// </summary>
    private void GetHotAB()
    {
        if (m_GameVersion != null && m_GameVersion.Patches != null && m_GameVersion.Patches.Length > 0)
        {
            //拿到最近的一次热更包  这是针对当前版本的更新（最近一次热更包包含此版本之前所有东西）
            Patches lastPatch = m_GameVersion.Patches[m_GameVersion.Patches.Length - 1];
            if (lastPatch != null && lastPatch.Files != null)
            {
                foreach (var patch in lastPatch.Files)
                {
                    if (!m_HotFixDict.ContainsKey(patch.Name))
                        m_HotFixDict.Add(patch.Name, patch);
                }
            }
        }
    }

    /// <summary>
    /// 计算下载的资源
    /// </summary>
    private void ComputeDownLoad()
    {
        m_DownLoadList.Clear();
        m_DownLoadDict.Clear();
        m_DownLoadMD5Dict.Clear();

        if (m_GameVersion != null && m_GameVersion.Patches != null && m_GameVersion.Patches.Length > 0)
        {
            m_CurPatches = m_GameVersion.Patches[m_GameVersion.Patches.Length - 1];
            if (m_CurPatches.Files != null && m_CurPatches.Files.Count > 0)
            {
                foreach (var patch in m_CurPatches.Files)
                {
                    if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                        && patch.Platform.Contains("StandaloneWindows64"))
                    {
                        AddToDownLoadList(patch);
                    }
                    else if ((Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor)
                             && patch.Platform.Contains("Android"))
                    {
                        AddToDownLoadList(patch);
                    }
                    else if ((Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.WindowsEditor)
                             && patch.Platform.Contains("IOS"))
                    {
                        AddToDownLoadList(patch);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将资源加入到下载列表
    /// </summary>
    private void AddToDownLoadList(Patch patch)
    {
        string filePath = m_DownLoadPath + "/" + patch.Name;
        if (File.Exists(filePath))
        {
            string md5 = MD5Manager.Instance.BuildFileMd5(filePath);
            if (patch.MD5 != md5)
            {
                // Debug.Log($"需要下载：{patch.Url}");
                
                m_DownLoadList.Add(patch);
                m_DownLoadDict.Add(patch.Name, patch);
                m_DownLoadMD5Dict.Add(patch.Name, patch.MD5);
            }
        }
        else
        {
            // Debug.Log($"需要下载：{patch.Url}");
            
            m_DownLoadList.Add(patch);
            m_DownLoadDict.Add(patch.Name, patch);
            m_DownLoadMD5Dict.Add(patch.Name, patch.MD5);
        }
    }

    /// <summary>
    /// 开始下载AB包
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="allPatch"></param>
    /// <returns></returns>
    public IEnumerator StartDownLoadAB(Action callback, List<Patch> allPatch = null)
    {
        m_AlreadyDownLoadList.Clear();
        StartDownLoad = true;

        if (allPatch == null)
        {
            allPatch = m_DownLoadList;
        }

        if (!Directory.Exists(m_DownLoadPath))
        {
            Directory.CreateDirectory(m_DownLoadPath);
        }

        List<DownLoadAssetBundle> downLoadAssetBundleList = new List<DownLoadAssetBundle>();
        foreach (var patch in allPatch)
        {
            downLoadAssetBundleList.Add(new DownLoadAssetBundle(patch.Url, m_DownLoadPath));
        }

        foreach (var downLoadAB in downLoadAssetBundleList)
        {
            m_CurDownLoad = downLoadAB;
            yield return m_Mono.StartCoroutine(downLoadAB.DownLoad());
            Patch patch = FindPatchByGamePath(downLoadAB.FileName);
            if (patch != null)
            {
                m_AlreadyDownLoadList.Add(patch);
            }

            downLoadAB.Destory();
        }

        //MD5码校验
        VerifyMD5(downLoadAssetBundleList, callback);
    }


    /// <summary>
    /// 对下载完的资源进行MD5校验
    /// </summary>
    /// <param name="downLoadAssetBundleList"></param>
    /// <param name="callback"></param>
    private void VerifyMD5(List<DownLoadAssetBundle> downLoadAssetBundleList, Action callback)
    {
        List<Patch> needDownLoadAgainList = new List<Patch>();
        foreach (var downLoadAB in downLoadAssetBundleList)
        {
            string md5 = "";
            if (m_DownLoadMD5Dict.TryGetValue(downLoadAB.FileName, out md5))
            {
                if (MD5Manager.Instance.BuildFileMd5(downLoadAB.SaveFilePath) != md5)
                {
                    Debug.Log($"此文件{downLoadAB.FileName}MD5码校验失败，即将重新下载");

                    Patch patch = FindPatchByGamePath(downLoadAB.FileName);
                    if (patch != null)
                    {
                        needDownLoadAgainList.Add(patch);
                    }
                }
            }
        }

        if (needDownLoadAgainList.Count <= 0)
        {
            m_DownLoadMD5Dict.Clear();
            if (callback != null)
            {
                StartDownLoad = false;
                callback();
            }

            LoadOverCallBack?.Invoke();
        }
        else
        {
            if (m_TryDownLoadCount >= DOWNLOADCOUNT)
            {
                StartDownLoad = false;
                string allName = needDownLoadAgainList.Aggregate("", (current, patch) => current + (patch.Name + "/"));

                Debug.LogError($"资源重复下载{DOWNLOADCOUNT}次失败，请检查资源 " + allName);

                ItemErrorCallBack?.Invoke(allName);
            }
            else
            {
                m_TryDownLoadCount++;
                m_DownLoadMD5Dict.Clear();
                foreach (var patch in needDownLoadAgainList)
                {
                    m_DownLoadMD5Dict.Add(patch.Name, patch.MD5);
                }

                m_Mono.StartCoroutine(StartDownLoadAB(callback, needDownLoadAgainList));
            }
        }
    }

    /// <summary>
    /// 根据资源名获取Patch补丁信息
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private Patch FindPatchByGamePath(string name)
    {
        Patch patch = null;
        m_DownLoadDict.TryGetValue(name, out patch);
        return patch;
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    public float GetProgress()
    {
        return GetAlreadyDownLoadSize() / LoadSumSize;
    }

    /// <summary>
    /// 获取已下载的内容大小
    /// </summary>
    /// <returns></returns>
    public float GetAlreadyDownLoadSize()
    {
        float alreadySize = m_AlreadyDownLoadList.Sum(x => x.Size);
        float curAlreadySize = 0;

        if (m_CurDownLoad != null)
        {
            Patch patch = FindPatchByGamePath(m_CurDownLoad.FileName);

            if (patch != null && !m_AlreadyDownLoadList.Contains(patch))
            {
                curAlreadySize = m_CurDownLoad.GetProcess() + patch.Size;
            }
        }

        return (alreadySize + curAlreadySize);
    }

    /// <summary>
    /// 计算需要解压的文件 (只有安卓需要解压，因为安卓不能直接读取streamingAssetsPath目录下的文件，要用WWW读取，所以先读取出来存到另一个地方)
    /// 热更之前，先把本地的AB包资源解压到一个可以直接读取的文件夹
    /// </summary>
    /// <returns></returns>
    public bool ComputeUnPackFile()
    {
#if UNITY_ANDROID
        if (!Directory.Exists(m_UnPackPath))
        {
            Directory.CreateDirectory(m_UnPackPath);
        }

        m_UnPackedList.Clear();
        foreach (var fileName in m_PackedMd5.Keys)
        {
            var filePath = m_UnPackPath + "/" + fileName;
            if (File.Exists(filePath))
            {
                string md5 = MD5Manager.Instance.BuildFileMd5(filePath);
                if (m_PackedMd5[fileName].MD5 != md5)
                {
                    m_UnPackedList.Add(fileName);
                }
            }
            else
            {
                m_UnPackedList.Add(fileName);
            }
        }

        foreach (var fileName in m_UnPackedList)
        {
            if (m_PackedMd5.ContainsKey(fileName))
            {
                UnPackSumSize += m_PackedMd5[fileName].Size;
            }
        }

        return m_UnPackedList.Count > 0;
#else
        return false;
#endif
    }

    /// <summary>
    /// 开始解压
    /// </summary>
    /// <param name="callback"></param>
    public void StartUnPackFile(Action callback)
    {
        StartUnPack = true;
        m_Mono.StartCoroutine(UnPackToPersistentDataPath(callback));
    }

    /// <summary>
    /// 将包里的原始资源解压到本地
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    IEnumerator UnPackToPersistentDataPath(Action callback)
    {
        foreach (var fileName in m_UnPackedList)
        {
            string targetPath = $"{Application.streamingAssetsPath}/{fileName}";

#if !UNITY_EDITOR && UNITY_ANDROID
            targetPath = $"{Application.streamingAssetsPath}/{fileName}";
#endif
            targetPath = $"file://{Application.streamingAssetsPath}/{fileName}";
#if UNITY_IOS || UNITY_IPHONE
#endif

            UnityWebRequest unityWebRequest = UnityWebRequest.Get(targetPath);
            unityWebRequest.timeout = 30;
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.isNetworkError)
            {
                Debug.Log("解压错误: " + unityWebRequest.error);
            }
            else
            {
                byte[] bytes = unityWebRequest.downloadHandler.data;
                FileTool.CreateFile(m_UnPackPath + "/" + fileName, bytes);
            }

            if (m_PackedMd5.ContainsKey(fileName))
            {
                AlreadyUnPackSize += m_PackedMd5[fileName].Size;
            }

            unityWebRequest.Dispose();
        }

        callback?.Invoke();

        StartUnPack = false;
    }

    /// <summary>
    /// 获取解压进度
    /// </summary>
    /// <returns></returns>
    public float GetUnpackProgress()
    {
        return AlreadyUnPackSize / UnPackSumSize;
    }

    /// <summary>
    /// 获取AB包路径
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string ComputeABPath(string name)
    {
        Patch patch = null;
        m_HotFixDict.TryGetValue(name, out patch);
        if (patch != null)
            return m_DownLoadPath + "/" + name;

        return "";
    }
}