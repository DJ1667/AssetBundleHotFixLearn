using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

public class SerializeEx
{
    /// <summary>
    /// 将类序列化成XML
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool XmlSerialize(string path, System.Object obj)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    XmlSerializer xs = new XmlSerializer(obj.GetType());
                    xs.Serialize(sw, obj);
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("此类无法转换成xml " + obj.GetType() + "," + e);
        }

        return false;
    }

    /// <summary>
    /// 将XML反序列化成类 （文件流加载） （用于编辑器）
    /// </summary>
    /// <param name="path"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static System.Object XmlDeserialize(string path, Type type)
    {
        System.Object obj = null;

        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                XmlSerializer xs = new XmlSerializer(type);
                obj = xs.Deserialize(fs);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"此xml无法转成{type.Name}: " + path + "," + e);
        }

        return obj;
    }

    /// <summary>
    /// 将XML反序列化成类（文件流加载） （用于编辑器）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public static T XmlDeserialize<T>(string path) where T : class
    {
        T t = default(T);

        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                t = (T) xs.Deserialize(fs);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"此xml无法转成{typeof(T)}: " + path + "," + e);
        }

        return t;
    }

    /// <summary>
    /// 将XML反序列化成类（通过Resource加载） （用于运行时）
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T XmlDeserializeRun<T>(string path) where T : class
    {
        T t = default(T);
        TextAsset textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError("cant load TextAsset: " + path);
            return null;
        }

        try
        {
            using (MemoryStream stream = new MemoryStream(textAsset.bytes))
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                t = (T) xs.Deserialize(stream);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("load TextAsset exception: " + path + "," + e);
        }

        return t;
    }

    /// <summary>
    /// 将类序列化成二进制
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool BinarySerialize(string path, System.Object obj)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, obj);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("此类无法转换成二进制 " + obj.GetType() + "," + e);
        }

        return false;
    }

    /// <summary>
    /// 将二进制反序列化成类（通过文件流加载） （用于编辑器）
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T BinaryDeserialize<T>(string path) where T : class
    {
        T t = default(T);
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T) bf.Deserialize(fs);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("此二进制无法转成类 " + typeof(T) + "," + e);
        }

        return t;
    }

    /// <summary>
    /// 将二进制反序列化成类（通过Resource加载） （用于运行时）
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T BinaryDeserializeRun<T>(string path) where T : class
    {
        T t = default(T);

        TextAsset textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError("cant load TextAsset: " + path);
            return null;
        }

        try
        {
            using (MemoryStream stream = new MemoryStream(textAsset.bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T) bf.Deserialize(stream);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("load TextAsset exception: " + path + "," + e);
        }

        return t;
    }
}