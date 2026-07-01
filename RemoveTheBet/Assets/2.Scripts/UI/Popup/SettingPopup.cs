using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public static string SettingPopup = "SettingPopup";
}

public class SettingPopup : PopupBase
{
    private const string HAPTIC_KEY = "HAPTIC_ON";
    [SerializeField] private  string PRIVACY_URL = "https://example.com/privacy";
    [SerializeField] private  string TERMS_URL = "https://example.com/terms";

    [Header("Buttons")]
    [SerializeField] private Button _bgmToggleButton;
    [SerializeField] private Button _hapticToggleButton;
    [SerializeField] private Button _privacyButton;
    [SerializeField] private Button _termsButton;

    [Header("BGM State")]
    [SerializeField] private GameObject _bgmOn;
    [SerializeField] private GameObject _bgmOff;

    [Header("Haptic State")]
    [SerializeField] private GameObject _hapticOn;
    [SerializeField] private GameObject _hapticOff;

    private string _previousViewId;

    private bool IsHapticOn
    {
        get => PlayerPrefs.GetInt(HAPTIC_KEY, 1) == 1;
        set => PlayerPrefs.SetInt(HAPTIC_KEY, value ? 1 : 0);
    }

    protected override void Awake()
    {
        base.Awake();
        _bgmToggleButton.onClick.AddListener(ToggleBgm);
        _hapticToggleButton.onClick.AddListener(ToggleHaptic);
        _privacyButton.onClick.AddListener(() => Application.OpenURL(PRIVACY_URL));
        _termsButton.onClick.AddListener(() => Application.OpenURL(TERMS_URL));
    }

    public override void Show(params object[] args)
    {
        base.Show(args);

        _previousViewId = args.Length > 0 && args[0] is string viewId
            ? viewId
            : ViewID.MainView;

        RefreshUI();
    }

    private void ToggleBgm()
    {
        SoundManager.Instance.ToggleSound(SoundType.BGM);
        RefreshBgmUI();
    }

    private void ToggleHaptic()
    {
        IsHapticOn = !IsHapticOn;
        RefreshHapticUI();
    }

    private void RefreshUI()
    {
        RefreshBgmUI();
        RefreshHapticUI();
    }

    private void RefreshBgmUI()
    {
        bool isOn = SoundManager.Instance.IsBgmOn;

        _bgmOn.SetActive(isOn);
        _bgmOff.SetActive(!isOn);
    }

    private void RefreshHapticUI()
    {
        bool isOn = IsHapticOn;

        _hapticOn.SetActive(isOn);
        _hapticOff.SetActive(!isOn);
    }

    // private void Close()
    // {
    //     Hide();
    //     // UIManager.Instance.OnShowView(_previousViewId);
    // }
}