using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileTool : MonoBehaviour
{
     public static void CreateFile(string filePath, byte[] bytes)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        FileInfo file = new FileInfo(filePath);
        Stream stream = file.Create();
        stream.Write(bytes, 0, bytes.Length);
        stream.Close();
        stream.Dispose();
    }

    /// <summary>
    /// 删除文件夹下所有文件
    /// </summary>
    /// <param name="fullPath"></param>
    public static void DeleteAllFile(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo directory = new DirectoryInfo(fullPath);
            FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                //if (files[i].Name.EndsWith(".mate"))
                //{
                //    continue;
                //}
                File.Delete(files[i].FullName);
            }
        }
    }

    public static string RootPath
    {
        get
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            {
                string tempPath = Application.persistentDataPath, dataPath;
                if (!string.IsNullOrEmpty(tempPath))
                {

                    dataPath = PlayerPrefs.GetString("DataPath", "");
                    if (string.IsNullOrEmpty(dataPath))
                    {
                        PlayerPrefs.SetString("DataPath", tempPath);
                    }

                    return tempPath + "/";
                }
                else
                {
                    Debug.Log("Application.persistentDataPath Is Null.");

                    dataPath = PlayerPrefs.GetString("DataPath", "");

                    return dataPath + "/";
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
            {
                ///*如果是电脑的编辑模式，先放在项目外面*/
                return Application.dataPath.Replace("Assets", "");
            }
            else
            {
                return Application.dataPath + "/";
            }
        }
    }

    //从服务器下载的数据的保存目录 
    public static string ServerExtensionDataPath
    {
        get
        {
            return RootPath + "ServerExtensionData/";
        }
    }

    /// <summary>
    /// 写文件操作
    /// 指定路径文件不存在会被创建
    /// </summary>
    /// <param name="path">文件路径（包含Application.persistentDataPath）.</param>
    /// <param name="name">文件名.</param>
    /// <param name="info">写入内容.</param>
    public static string createORwriteFile(string fileName, string info)
    {
        string p = RootPath + fileName;

        //创建文件夹
        var dir = Path.GetDirectoryName(p);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        FileStream fs = new FileStream(p, FileMode.Create, FileAccess.Write);
        fs.Close();
        StreamWriter sw = new StreamWriter(p);
        sw.WriteLine(info);
        sw.Close();
        sw.Dispose();

        return p;
    }

    public static string createORwriteBytesFile(string fileName, byte[] info)
    {
        string p = RootPath + fileName;
        //创建文件夹
        var dir = Path.GetDirectoryName(p);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(p, info);

        return p;
    }

    public static byte[] ReadFileBytes(string fileName)
    {
        string p = RootPath + fileName;

        return File.ReadAllBytes(p);
    }

    // /// <summary>
    // /// 写加密文件操作
    // /// 指定路径文件不存在会被创建
    // /// </summary>
    // /// <param name="path">文件路径（包含Application.persistentDataPath）.</param>
    // /// <param name="name">文件名.</param>
    // /// <param name="info">写入内容.</param>
    // 	public static void createORwriteFileEncrypt (string fileName, string info)
    // 	{
    // 		string encrypt_info = DesCode.EncryptDES (info);
    // 		FileStream fs = new FileStream (RootPath + fileName, FileMode.Create, FileAccess.Write);
    // 		StreamWriter sw = new StreamWriter (fs);   
    // 		fs.SetLength (0);	///*清空文件*/
    // 		sw.WriteLine (encrypt_info);
    // 		sw.Close ();
    // 		sw.Dispose ();
    // 	}

    /// <summary>
    /// 读取文件内容  仅读取第一行
    /// </summary>
    /// <returns>The file.</returns>
    /// <param name="path">Path.</param>
    /// <param name="name">Name.</param>
    public static string ReadFileLine(string fileName)
    {
        string fileContent;
        StreamReader sr = null;
        try
        {
            sr = File.OpenText(RootPath + fileName);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return null;
        }
        while ((fileContent = sr.ReadLine()) != null)
        {
            break;

        }
        sr.Close();
        sr.Dispose();
        return fileContent;
    }

    /// <summary>
    /// 读取文件所有内容
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string ReadAllFile(string fileName)
    {
        string fileContent;
        StreamReader sr = null;
        try
        {
            sr = File.OpenText(RootPath + fileName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return null;
        }
        fileContent = sr.ReadToEnd();
        sr.Close();
        sr.Dispose();
        return fileContent;

    }

    /// <summary>
    /// 读取文件内容并解密  仅读取第一行
    /// </summary>
    /// <returns>The file.</returns>
    /// <param name="path">Path.</param>
    /// <param name="name">Name.</param>
    //	public static string ReadFileDecrypt (string fileName)
    //	{
    //		string fileContent; 
    //		StreamReader sr = null;
    //		try{
    //			sr = File.OpenText(RootPath + fileName);
    //		}
    //		catch(Exception e){
    //			return null;
    //		}
    //		
    //		while ((fileContent = sr.ReadLine()) != null) {
    //			break; 
    //		}
    //		sr.Close ();
    //		sr.Dispose ();
    //		return DesCode.DecryptDES(fileContent);
    //	}

    /// <summary>
    /// 是否存在
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static bool IsFileExists(string fileName)
    {
        return File.Exists(RootPath + fileName);
    }

    public static void DelectFile(string fileName)
    {
        File.Delete(RootPath + fileName);
    }

    public static bool IsFolderExists(string folderName)
    {
        return Directory.Exists(RootPath + folderName);
    }

    public static void CreateFolder(string folderName)
    {
        Directory.CreateDirectory(RootPath + folderName);
    }

    /// <summary>
    /// 复制文件夹
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    public static void CopyFolder(string from, string to)
    {
        if (!Directory.Exists(to))
            Directory.CreateDirectory(to);

        ///* 子文件夹*/
        foreach (string sub in Directory.GetDirectories(from))
            CopyFolder(sub, to + Path.GetFileName(sub) + "/");

        ///* 文件*/
        foreach (string file in Directory.GetFiles(from))
            File.Copy(file, to + Path.GetFileName(file), true);
    }

    public static void CopyFile(string from, string to, bool overWrite)
    {
        File.Copy(from, to, overWrite);
    }
}
