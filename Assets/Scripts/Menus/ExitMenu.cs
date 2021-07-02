using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;

public class ExitMenu : NetworkBehaviour
{
    // Events
    public delegate void OnLeaveMatchDelegate();
    public static event OnLeaveMatchDelegate OnLeaveMatch;

    public delegate void OnStayOnMatchDelegate();
    public static event OnStayOnMatchDelegate OnStayOnMatch;

    private const string hostPrompt = "Stop";
    private const string clientPrompt = "Leave";
    [SerializeField] private Text _exitPrompt = null;

    private void Awake()
    {
        _exitPrompt.text = $"{(IsHost? hostPrompt : clientPrompt)} Match?";
        InputManager.OnEscapeKeyPress += handleMenuState;
        PlayerManager.OnEndMatch += disableMenu;
    }

    private void OnDestroy()
    {
        InputManager.OnEscapeKeyPress -= handleMenuState;
        PlayerManager.OnEndMatch -= disableMenu;
    }

    private void handleMenuState()
    {
        // Toggles menu active state
        gameObject.SetActive(!gameObject.activeSelf);

        // If pressing esc caused the menu to now be Inactive,
        // Then trigger OnStayOnMatch
        if(!gameObject.activeSelf)
        {
            stayOnMatch();
        }
    }

    private void disableMenu(bool draw, ulong winner)
    {
        Destroy(gameObject);
    }

    // Button Events
    public void leaveMatch() => OnLeaveMatch?.Invoke();
    public void stayOnMatch()
    {
        // Make sure menu is closed when the player decides to stay in the match
        gameObject.SetActive(false);
        OnStayOnMatch?.Invoke();
    }

}
