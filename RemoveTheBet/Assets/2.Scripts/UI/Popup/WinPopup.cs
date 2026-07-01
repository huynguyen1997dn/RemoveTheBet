using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public static string WinPopup = "WinPopup";
}
public class WinPopup : PopupBase
{
    [SerializeField] private Button _buttonNextLevel;
    [SerializeField] private TMP_Text _tmpNextGame;

    [SerializeField] private PlayerDataSO _playerData;

    
    private void OnEnable()
    {
        _tmpNextGame.text = $"Next Game\n<size=100%>Level {_playerData.GetLevel()}</size>";
        _buttonNextLevel.onClick.AddListener(OnNextLevel);
        
    }

    private void OnNextLevel()
    {
        EventDispatcher.Dispatch(EventID.GAME_START);
        Hide();
    }

    private void OnDisable()
    {
        _buttonNextLevel.onClick.RemoveListener(OnNextLevel);
    }
}
