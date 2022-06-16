using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DownLoadAssetBundle : DownLoadItem
{
    UnityWebRequest m_Request;

    public DownLoadAssetBundle(string url, string savePath, System.Action<DownLoadItem> mONComplete = null,
        System.Action<DownLoadItem> mONError = null) : base(url,
        savePath, mONComplete, mONError)
    {
    }

    public override IEnumerator DownLoad()
    {
        m_Request = UnityWebRequest.Get(m_url);
        m_isStartDownLoad = true;
        m_Request.timeout = 30;
        yield return m_Request.SendWebRequest();
        m_isStartDownLoad = false;

        if (m_Request.isNetworkError)
        {
            Debug.LogError("下载失败： " + m_Request.error);
            m_onError?.Invoke(this);
        }
        else
        {
            byte[] bytes = m_Request.downloadHandler.data;
            FileTool.CreateFile(m_saveFilePath, bytes);
            m_onComplete?.Invoke(this);
        }
    }

    public override float GetProcess()
    {
        if (m_Request == null)
        {
            return 0;
        }

        return m_Request.downloadProgress;
    }

    public override float GetCurLength()
    {
        if (m_Request == null)
        {
            return 0;
        }

        return (long) m_Request.downloadedBytes;
    }

    public override float GetLength()
    {
        return 0;
    }

    public override void Destory()
    {
        if (m_Request != null)
        {
            m_Request.Dispose();
            m_Request = null;
        }
    }
}