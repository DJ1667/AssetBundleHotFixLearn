using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class DownLoadItem
{
    /// <summary>
    /// 网络资源路径
    /// </summary>
    protected string m_url;

    /// <summary>
    /// 资源下载存放路径，不包含文件名
    /// </summary>
    protected string m_savePath;

    /// <summary>
    /// 文件名，包含后缀
    /// </summary>
    protected string m_fileName;

    /// <summary>
    /// 文件名，不包含后缀
    /// </summary>
    protected string m_fileNameWithoutExtension;

    /// <summary>
    /// 文件后缀
    /// </summary>
    protected string m_fileExtension;

    /// <summary>
    /// 下载文件全路径，路径+文件名+后缀
    /// </summary>
    protected string m_saveFilePath;

    /// <summary>
    /// 原文件大小
    /// </summary>
    protected long m_fileSize;

    /// <summary>
    /// 当前下载大小
    /// </summary>
    protected long m_downLoadSize;

    /// <summary>
    /// 是否开始下载
    /// </summary>
    protected bool m_isStartDownLoad;

    protected System.Action<DownLoadItem> m_onComplete;
    protected System.Action<DownLoadItem> m_onError;

    public string Url
    {
        get { return m_url; }
    }

    public string SavePath
    {
        get { return m_savePath; }
    }

    public string FileName
    {
        get { return m_fileName; }
    }

    public string FileNameWithoutExtension
    {
        get { return m_fileNameWithoutExtension; }
    }

    public string FileExtension
    {
        get { return m_fileExtension; }
    }

    public string SaveFilePath
    {
        get { return m_saveFilePath; }
    }

    public long FileSize
    {
        get { return m_fileSize; }
    }

    public long DownLoadSize
    {
        get { return m_downLoadSize; }
    }

    public bool IsStartDownLoad
    {
        get { return m_isStartDownLoad; }
    }

    public DownLoadItem(string url, string savePath, System.Action<DownLoadItem> mONComplete = null, System.Action<DownLoadItem> mONError = null)
    {
        m_url = url;
        m_savePath = savePath;
        m_fileNameWithoutExtension = Path.GetFileNameWithoutExtension(m_url);
        m_fileExtension = Path.GetExtension(m_url);
        m_fileName = $"{m_fileNameWithoutExtension}{m_fileExtension}";
        m_saveFilePath = $"{m_savePath}/{m_fileName}";
        m_fileSize = 0;
        m_downLoadSize = 0;
        m_isStartDownLoad = false;
        m_onComplete = mONComplete;
        m_onError = mONError;
    }

    /// <summary>
    /// 开始下载
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator DownLoad()
    {
        yield return null;
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    public abstract float GetProcess();

    /// <summary>
    /// 获取当前下载的文件大小
    /// </summary>
    /// <returns></returns>
    public abstract float GetCurLength();

    /// <summary>
    /// 获取下载的文件大小
    /// </summary>
    /// <returns></returns>
    public abstract float GetLength();

    public abstract void Destory();
}