using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// 版本检查
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
        string xmlUrl = "http://127.0.0.1/ServerInfo.xml";

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
                m_DownLoadList.Add(patch);
                m_DownLoadDict.Add(patch.Name, patch);
                m_DownLoadMD5Dict.Add(patch.Name, patch.MD5);
            }
        }
        else
        {
            m_DownLoadList.Add(patch);
            m_DownLoadDict.Add(patch.Name, patch);
            m_DownLoadMD5Dict.Add(patch.Name, patch.MD5);
        }
    }
}