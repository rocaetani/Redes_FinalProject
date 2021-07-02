using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    // Events
    public delegate void OnEndMatchDelegate(bool draw, ulong winner);
    public static event OnEndMatchDelegate OnEndMatch;

    private static PlayerManager _instance = null;

    public bool enablePlayerBehaviour = true;

    private static List<ulong> _playersDead;
    private Coroutine _checkEndGameCoroutine = null;

    public static ColorPick ColorPick;
    public ColorPick SettableColorPick;

    public static Dictionary<BombermanColor, BombermanMaterial> PossibleBombermanColors;
    public List<BombermanMaterial> BombermanColors;

    private void Awake()
    {
        _instance = _instance ?? this;

        _playersDead = new List<ulong>();

        //ColorPick = SettableColorPick;
        PossibleBombermanColors = new Dictionary<BombermanColor, BombermanMaterial>();
        foreach (var bombermanColor in BombermanColors)
        {
            PossibleBombermanColors.Add(bombermanColor.BombermanColor, bombermanColor);
        }
    }

    private void OnDestroy()
    {
        if(_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        PlayerController.playerBehaviourEnabled = enablePlayerBehaviour;
    }

    public static void RegisterDeadPlayer(ulong playerID)
    {
        _playersDead.Add(playerID);

        checkWinner();
    }

    private static void checkWinner()
    {
        if(!NetworkController.IsServer)
        {
            return;
        }

        if(_instance._checkEndGameCoroutine != null)
        {
            return;
        }

        print("Checking Winner");
        IEnumerator checkPlayers()
        {
            const float waitTime = 1;
            yield return new WaitForSeconds(waitTime);

            var players = NetworkController.getConnectedPlayers();

            if(_playersDead.Count >= players.Count - 1)
            {

                bool winner = false;
                ulong winnerID = 0;

                foreach(var player in players)
                {
                    if(!_playersDead.Contains(player))
                    {
                        winner = true;
                        winnerID = player;
                    }
                }

                _instance.broadcastWinner_ClientRpc(!winner, winnerID);
            }

            _instance._checkEndGameCoroutine = null;
        }

        _instance._checkEndGameCoroutine = _instance.StartCoroutine(checkPlayers());
    }


    [ClientRpc]
    private void broadcastWinner_ClientRpc(bool draw, ulong winner)
    {
        print($"Clientside {draw}: {winner}");
        OnEndMatch?.Invoke(draw, winner);
    }
}
