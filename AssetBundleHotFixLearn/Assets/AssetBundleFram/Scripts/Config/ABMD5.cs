using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class ABMD5
{
    [XmlElement("ABMD5List")] public List<ABMD5Base> ABMD5List { get; set; }
}

[System.Serializable]
public class ABMD5Base
{
    [XmlAttribute("Name")] public string Name { get; set; } //资源名称
    [XmlAttribute("MD5")] public string MD5 { get; set; } //资源MD5值
    [XmlAttribute("Size")] public float Size { get; set; } //资源大小
}