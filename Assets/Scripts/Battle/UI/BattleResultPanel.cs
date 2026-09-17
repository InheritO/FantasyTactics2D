using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleResultPanel : MonoBehaviour
{
    public GameObject panelRoot;
    public TMP_Text resultLabel;
    public Button returnButton;

    public event Action OnReturnRequested;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(() => OnReturnRequested?.Invoke());
    }

    public void Show(bool playerWon)
    {
        if (resultLabel != null)
            resultLabel.text = playerWon ? "½Â¸®!" : "ÆÐ¹è...";

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}