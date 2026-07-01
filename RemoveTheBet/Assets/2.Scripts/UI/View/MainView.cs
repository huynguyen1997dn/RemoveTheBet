using System;
using TMPro;
using Toggle = UnityEngine.UI.Toggle;
using UnityEngine;
using UnityEngine.UI;

public partial class EventID
{
  public static string GAME_START = "GAME_START";
  public static string GAME_NEXT = "GAME_NEXT";
  public static string GAME_SETUP_COMPLETE = "GAME_SETUP_COMPLETE";
  public static string GAME_REQUEST_HINT = "GAME_REQUEST_HINT";

}

public partial class ViewID
{
  public static string MainView = "MainView";

}
public class MainView : ViewBase
{
  [SerializeField] private PlayerDataSO playerData;

  [SerializeField] private TMP_Text _tmpLevel;
  [SerializeField] private Button _btnPlay;

  public void Start()
  {
    _btnPlay.onClick.AddListener(OnPlayClick);
  }

  private void OnTimeModeToggle(bool value)
  {
    playerData.SetUseTimeMode(value);
  }
  private void OnEnable()
  {
    playerData.Load();
    playerData.OnPlayerDataChanged += onPlayerDataChaged;
    onPlayerDataChaged(null);
  }

  private void OnDisable()
  {
    playerData.OnPlayerDataChanged -= onPlayerDataChaged;

  }

  private void onPlayerDataChaged(PlayerDataSO.PlayerData obj)
  {
    _tmpLevel.text = $"Level {playerData.GetLevel()}";
  }
  //


  private void OnPlayClick()
  {
    UIManager.Instance.OnShowView(ViewID.GamePlayView);
    EventDispatcher.Dispatch(EventID.GAME_START);
    Hide();
  }
}
