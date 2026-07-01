
using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ViewID
{
   public static string GamePlayView = "GamePlayView";

}

public partial class EventID
{
   public static string GamePlayView_LevelStart = "GamePlayView_LevelStart";
   public const string GamePlayView_RefreshView = "GamePlayView_Update_View";
   public const string GAME_CONTINUE = "GAME_CONTINUE";
   public const string GamePlayView_OnWrongFlash = "GamePlayView_OnWrongFlash";
   public const string GamePlayView_OnShowHint = "GamePlayView_OnShowHint";
   public const string GamePlayView_OnHideHint = "GamePlayView_OnHideHint";
   public const string TUTORIAL_SHOW = "TUTORIAL_SHOW";
   public const string TUTORIAL_DISMISS = "TUTORIAL_DISMISS";

   public const string TapeSetupComplete = "TapeSetupComplete";
   public const string TapeRemoved = "TapeRemoved";
   public const string WrongTap = "WrongTap";
   public const string TapeRefreshCount = "TapeRefreshCount";
}
public class GamePlayView : ViewBase
{
   
   [SerializeField] private PlayerDataSO playerDataSO;

    [SerializeField] private Button _btnBack;
    [SerializeField] private Button _btnReset;
  

    [SerializeField] private TMP_Text _tmpLevel;
   


   public void Start()
   {

      _btnBack.onClick.AddListener(() =>
      {
         // GameController.Instance.OnBackToMenu();
         Hide();
         UIManager.Instance.OnShowView(ViewID.MainView);
      });
      _btnReset.onClick.AddListener(() =>
      {

         EventDispatcher.Dispatch(EventID.GAME_START);
      });
   
   }

   private void OnEnable()
   {
      EventDispatcher.Subscribe(EventID.GamePlayView_LevelStart, GAME_SETUP_COMPLETE);
   }

   private void GAME_SETUP_COMPLETE()
   {
      int level = playerDataSO.GetLevel();
     

   }



   private void OnDisable()
   {
      EventDispatcher.Unsubscribe(EventID.GamePlayView_LevelStart, GAME_SETUP_COMPLETE);


   }

}
