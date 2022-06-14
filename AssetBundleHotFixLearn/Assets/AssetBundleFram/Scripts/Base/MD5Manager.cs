using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class MD5Manager : Singleton<MD5Manager>
{
    /// <summary>
    /// 储存Md5码，filePath为文件路径
    /// </summary>
    /// <param name="filePath"></param>
    public void SaveMd5(string filePath)
    {
        string md5 = BuildFileMd5(filePath);
        string name = filePath + "_md5.dat";
        if (File.Exists(name))
        {
            File.Delete(name);
        }

        StreamWriter sw = new StreamWriter(name, false, Encoding.UTF8);
        if (sw != null)
        {
            sw.Write(md5);
            sw.Flush();
            sw.Close();
        }
    }

    /// <summary>
    /// 获取之前储存的Md5码
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string GetMd5(string path)
    {
        string name = path + "_md5.dat";
        try
        {
            StreamReader sr = new StreamReader(name, Encoding.UTF8);
            string content = sr.ReadToEnd();
            sr.Close();
            return content;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 获取文件的MD5码
    /// </summary>
    /// <param name="fliePath"></param>
    /// <returns></returns>
    public string BuildFileMd5(string fliePath)
    {
        string filemd5 = null;
        try
        {
            using (var fileStream = File.OpenRead(fliePath))
            {
                var md5 = MD5.Create();
                var fileMD5Bytes = md5.ComputeHash(fileStream); //计算指定Stream 对象的哈希值                                     
                filemd5 = FormatMD5(fileMD5Bytes);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }

        return filemd5;
    }

    /// <summary>
    /// 将MD5字节数组转换成字符串
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public string FormatMD5(Byte[] data)
    {
        return System.BitConverter.ToString(data).Replace("-", "").ToLower(); //将byte[]装换成字符串
    }
}