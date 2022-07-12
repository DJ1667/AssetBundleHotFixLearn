using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance = null;

    public List<BasePanel> allPanelList = new List<BasePanel>();

    private Dictionary<string, BasePanel> allPanelDict = new Dictionary<string, BasePanel>();

    private void Awake()
    {
        Instance = this;

        foreach (var panel in allPanelList)
        {
            var panelName = panel.GetType().Name;
            if (!allPanelDict.ContainsKey(panelName))
            {
                allPanelDict.Add(panelName, panel);

                Debug.Log("注册面板：" + panelName);
            }
        }
    }

    public void OpenPanel<T>(Action<T> callBack = null) where T : BasePanel
    {
        var panelName = typeof(T).Name;

        if (allPanelDict.ContainsKey(panelName))
        {
            var panel = allPanelDict[panelName] as T;

            callBack?.Invoke(panel);

            if (panel is null) return;
            panel.gameObject.SetActive(true);
            panel.OnShow();
        }
    }

    public void ClosePanel<T>() where T : BasePanel
    {
        var panelName = typeof(T).Name;

        if (allPanelDict.ContainsKey(panelName))
        {
            var panel = allPanelDict[panelName];

            panel.gameObject.SetActive(false);
            panel.OnHide();
        }
    }
}