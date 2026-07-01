using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public static string LosePopup = "LosePopup";
}
public class LosePopup : PopupBase
{
    [SerializeField] private Button _buttonReset;
    [SerializeField] private Button _btnGetMoreLive;

    private void OnEnable()
    {
        _buttonReset.onClick.AddListener(OnResetLevell);
        _btnGetMoreLive.onClick.AddListener(OnGetMoreLive);
    }

    private void OnResetLevell()
    {
        EventDispatcher.Dispatch(EventID.GAME_START);
        Hide();
    }

    private void OnGetMoreLive()
    {
        // if (AdsManager.AdsManager.Instance.IsInitialized && AdsManager.AdsManager.Instance.RewardedAd.IsReady)
        // {
        //     AdsManager.AdsManager.Instance.ShowRewarded(
        //         onRewarded: OnContinueGame,
        //         onClosed: OnContinueGame);
        // }
        // else
        // {
        //     OnContinueGame();
        // }
    }

    private void OnContinueGame()
    {
        EventDispatcher.Dispatch(EventID.GAME_CONTINUE);
        Hide();
    }

    private void OnDisable()
    {
        _buttonReset.onClick.RemoveListener(OnResetLevell);
        _btnGetMoreLive.onClick.RemoveListener(OnGetMoreLive);
    }
}
