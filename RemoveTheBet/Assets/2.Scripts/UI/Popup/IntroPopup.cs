using System;
using System.Collections;
using UnityEngine;


public class IntroPopup : PopupBase
{
    [SerializeField] private float _delay = 2f;


    protected override void OnShow()
    {
        base.OnShow();
        // StartCoroutine(DelayAndProceed());
    }

    private IEnumerator DelayAndProceed()
    {
        yield return new WaitForSeconds(_delay);
        Hide();
        UIManager.Instance.OnShowView(ViewID.MainView);
    }

    public void Start()
    {
        StartCoroutine(DelayAndProceed());
    }
}
