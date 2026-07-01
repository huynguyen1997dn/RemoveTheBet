using UnityEngine;

public class GameApp : Singleton<GameApp>
{
    public GameState CurrentState { get; private set; }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
    }
}