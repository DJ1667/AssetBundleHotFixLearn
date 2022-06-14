using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class ServerInfo
{
    [XmlElement("GameVersion")] public VersionInfo[] GameVersion;
}

/// <summary>
/// 当前大版本所有补丁包
/// </summary>
[System.Serializable]
public class VersionInfo
{
    [XmlAttribute] public string Version;
    [XmlElement] public Patches[] Patches;
}

/// <summary>
/// 总补丁包
/// </summary>
[System.Serializable]
public class Patches
{
    [XmlAttribute] public int Version;
    [XmlAttribute] public string Des;
    [XmlElement] public List<Patch> Files;
}

/// <summary>
/// 单个补丁包
/// </summary>
[System.Serializable]
public class Patch
{
    [XmlAttribute] public string Name;
    [XmlAttribute] public string Url;
    [XmlAttribute] public string Platform;
    [XmlAttribute] public string MD5;
    [XmlAttribute] public float Size;
}