using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    // Events
    public delegate void OnLeaveMatchDelegate();
    public static event OnLeaveMatchDelegate OnLeaveMatch;

    private const string _winText = "Win";
    private const string _loseText = "Lost";
    private const string _drawText = "Draw!";
    [SerializeField] private Text _endgameText = null;

    private void Awake()
    {
        PlayerManager.OnEndMatch += handleMatchEnded;
    }

    private void OnDestroy()
    {
        PlayerManager.OnEndMatch -= handleMatchEnded;
    }

    private void handleMatchEnded(bool draw, ulong winnerID)
    {
        var winner = !draw && winnerID == NetworkController.getOwnID();

        if(draw)
        {
            _endgameText.text = _drawText;
        }
        else
        {
            _endgameText.text = $"You {(winner? _winText : _loseText)}!";
        }

        this.toggleMenu();
    }

    // Button Events
    public void returnToMainMenu() => OnLeaveMatch?.Invoke();
}
