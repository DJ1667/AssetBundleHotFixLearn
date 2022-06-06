using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "Tools/打包配置/ABConfig", order = 1)]
public class ABConfig : ScriptableObject
{
    public List<string> m_PrefabPath = new List<string>();
    public List<FileDirABName> m_AllFileDirAB = new List<FileDirABName>();

    [System.Serializable]
    public class FileDirABName
    {
        public string abName;
        public string path;
    }
}