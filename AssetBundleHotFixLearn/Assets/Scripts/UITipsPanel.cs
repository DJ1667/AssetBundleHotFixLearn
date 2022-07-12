using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITipsPanel : BasePanel
{
    public Button btnYes;
    public Button btnNo;

    public Text textInfo;

    private Action _mYesCallback;
    private Action _mNoCallback;

    void Start()
    {
        btnYes.onClick.AddListener(BtnOnClick_Yes);
        btnNo.onClick.AddListener(BtnOnClick_No);
    }

    public override void OnShow()
    {
        base.OnShow();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    public void Init(string info, Action yes = null, Action no = null)
    {
        textInfo.text = info;
        _mYesCallback = yes;
        _mNoCallback = no;
    }

    private void BtnOnClick_Yes()
    {
        UIManager.Instance.ClosePanel<UITipsPanel>();
        _mYesCallback?.Invoke();
    }

    private void BtnOnClick_No()
    {
        UIManager.Instance.ClosePanel<UITipsPanel>();
        _mNoCallback?.Invoke();
    }
}