using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateABAssetPath
{
    static string ClassStr2 = "public static class ABAssetPathStr{\n#Content}\n";
    public static void ConvertToString(Dictionary<string, string> pathDict)
    {
        string ContentStr = "";
        foreach (var key in pathDict.Keys)
        {
            string temp1 = "" + key.Substring(key.LastIndexOf("/") + 1);
            string temp2 = temp1.Replace('-', '_');
            string temp3 = temp2.Replace(' ', '_');
            string temp4 = temp3.Replace('(', '_');
            string temp5 = temp4.Replace(')', '_');
            string temp6 = temp5.Replace('.', '_');
            ContentStr += "public const string " + temp6 + "=" + "\"" + key + "\"" + ";\n";
        }

        var resultStr = ClassStr2.Replace("#Content", ContentStr);

        CreateOrWriteFile(Application.dataPath + "/Scripts/ABAssetPathStr.cs", resultStr);
    }

    private static void CreateOrWriteFile(string path, string info)
    {

        //判断这个路径是否存在
        var tempPath = GetFilePathWithOutFileName(path);
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        //判断这个文件是否存在
        //写入文件
        if (File.Exists(path))
        {
            Debug.Log(path + "    文件已存在，将被替换");
        }
        else
        {
            Debug.Log(path + "    创建文件");
        }
        //补充 using(){} ()中的对象必须继承IDispose接口,在{}结束后会自动释放资源,也就是相当于帮你调用了Dispos()去释放资源
        using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            using (TextWriter textWriter = new StreamWriter(fileStream))
            {
                textWriter.Write(info);
            }
        }
        AssetDatabase.Refresh();
    }

    private static string GetFilePathWithOutFileName(string path)
    {
        var tempIndex = path.LastIndexOf("/");
        var result = path.Substring(0, tempIndex);

        return result;
    }
}
