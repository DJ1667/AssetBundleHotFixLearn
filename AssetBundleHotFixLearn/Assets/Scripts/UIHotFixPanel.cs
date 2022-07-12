using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHotFixPanel : BasePanel
{
    public Slider slider;
    public Text tmp_Speed, tmp_Tips, tmp_progress;

    float m_SumTime = 0;

    private void Awake()
    {
        HotPatchManager.Instance.Init(this);
    }

    void Start()
    {
        OnShow();
    }

    private void Update()
    {
        if (HotPatchManager.Instance.StartUnPack)
        {
            m_SumTime += Time.deltaTime;
            slider.value = HotPatchManager.Instance.GetUnpackProgress();
            float speed = (HotPatchManager.Instance.AlreadyUnPackSize / 1024f) / m_SumTime;
            tmp_Speed.text = $"{speed:F} M/S";
            tmp_progress.text = $"{(slider.value * 100):00}%";
        }

        if (HotPatchManager.Instance.StartDownLoad)
        {
            m_SumTime += Time.deltaTime;
            slider.value = HotPatchManager.Instance.GetProgress();
            float speed = (HotPatchManager.Instance.GetAlreadyDownLoadSize() / 1024f) / m_SumTime;
            tmp_Speed.text = $"{speed:F} M/S";
            tmp_progress.text = $"{(slider.value * 100):0000}%";
        }
    }

    public override void OnShow()
    {
        HotPatchManager.Instance.ServerInfoErrorCallBack += ServerInfoError;
        HotPatchManager.Instance.ItemErrorCallBack += ItemError;

        CheckAsset();
    }

    public override void OnHide()
    {
        HotPatchManager.Instance.ServerInfoErrorCallBack -= ServerInfoError;
        HotPatchManager.Instance.ItemErrorCallBack -= ItemError;
    }


    void CheckAsset()
    {
        if (HotPatchManager.Instance.ComputeUnPackFile())
        {
            HotPatchManager.Instance.StartUnPackFile(HotFix);
        }
        else
        {
            HotFix();
        }
    }

    void HotFix()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            //网络错误
            UIManager.Instance.OpenPanel<UITipsPanel>((panel) =>
            {
                panel.Init("网络连接失败，请检查网络！", () => { Application.Quit(); }, () => { Application.Quit(); });
            });
        }
        else
        {
            CheckVersion();
        }
    }

    void CheckVersion()
    {
        HotPatchManager.Instance.CheckVersion((isNeedHot) =>
        {
            if (isNeedHot)
            {
                UIManager.Instance.OpenPanel<UITipsPanel>((panel) =>
                {
                    panel.Init($"当前版本{HotPatchManager.Instance.CurVersion},有{(HotPatchManager.Instance.LoadSumSize / 1024):F}M资源需要更新，是否下载？",
                        OnClickStartDownLoad, () => { Application.Quit(); });
                });
            }
            else
            {
                m_SumTime = 0;

                StartOnFinish();
            }
        });
    }

    private void OnClickStartDownLoad()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
        {
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                UIManager.Instance.OpenPanel<UITipsPanel>((panel) =>
                {
                    panel.Init($"当前使用的是手机流量，是否继续下载",
                        StartDownLoad, () => { Application.Quit(); });
                });
            }
        }
        else
        {
            StartDownLoad();
        }
    }

    private void ANewDownLoad()
    {
        HotPatchManager.Instance.CheckVersion((isNeedHot) =>
        {
            if (isNeedHot)
            {
                StartDownLoad();
            }
            else
            {
                StartOnFinish();
            }
        });
    }

    private void StartDownLoad()
    {
        StartCoroutine(HotPatchManager.Instance.StartDownLoadAB(StartOnFinish));
    }

    private void StartOnFinish()
    {
        OnFinish();
    }

    private void ServerInfoError()
    {
        UIManager.Instance.OpenPanel<UITipsPanel>((panel) =>
        {
            panel.Init($"服务器列表获取失败，请检查网络连接，尝试重新下载",
                CheckVersion, () => { Application.Quit(); });
        });
    }

    private void ItemError(string str)
    {
        UIManager.Instance.OpenPanel<UITipsPanel>((panel) =>
        {
            panel.Init($"资源更新失败，是否重试？\n以下资源下载失败\n{str}",
                ANewDownLoad, () => { Application.Quit(); });
        });
    }

    private void OnFinish()
    {
        //开始游戏
        UIManager.Instance.OpenPanel<UITipsPanel>((panel) => { panel.Init($"可以开始游戏了"); });
    }
}