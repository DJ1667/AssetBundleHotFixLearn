using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class AssetBundleConfig
{
    [XmlElement("ABList")] public List<ABBase> ABList { get; set; }
}

[System.Serializable]
public class ABBase
{
    [XmlAttribute("Path")] public string Path { get; set; }
    [XmlAttribute("Crc")] public uint Crc { get; set; }
    [XmlAttribute("ABName")] public string ABName { get; set; }
    [XmlAttribute("AssetName")] public string AssetName { get; set; }
    [XmlAttribute("ABDepends")] public List<string> ABDepends { get; set; }
}