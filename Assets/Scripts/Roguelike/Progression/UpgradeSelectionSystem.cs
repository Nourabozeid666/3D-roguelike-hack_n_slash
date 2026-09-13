using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the active upgrade selection lifecycle: rolls offers from UpgradeDatabase,
/// freezes gameplay, feeds cards to IUpgradeSelectView / UpgradeSelectPresenter, applies
/// the chosen upgrade to PlayerEntity, and handles consecutive pending level-up picks.
/// Implements IUpgradeSource so it plugs directly into the UI architecture.
/// </summary>
public class UpgradeSelectionSystem : IUpgradeSource
{
    public event Action<IReadOnlyList<UpgradeCardData>> Changed;
    public event Action<ScriptableObject> OnUpgradeApplied;

    readonly UpgradeDatabase database;
    readonly PlayerController playerController;
    readonly ProgressionSystem progressionSystem;
    readonly UpgradeSelectController selectController;
    readonly UpgradeSelectPresenter selectPresenter;

    List<ScriptableObject> currentOfferedAssets = new();
    List<UpgradeCardData> currentOfferedCards = new();
    bool isSelecting = false;
    float previousTimeScale = 1f;
    float selectionCooldown = 0.5f;
    float canSelectTime = 0f;

    public bool IsSelecting => isSelecting;
    public float SelectionCooldown
    {
        get => selectionCooldown;
        set => selectionCooldown = Mathf.Max(0f, value);
    }
    public bool CanSelect => Time.unscaledTime >= canSelectTime;
    public IReadOnlyList<UpgradeCardData> GetUpgrades() => currentOfferedCards;

    public UpgradeSelectionSystem(
        UpgradeDatabase database,
        PlayerController playerController,
        ProgressionSystem progressionSystem,
        UpgradeSelectController selectController,
        UpgradeSelectPresenter selectPresenter)
    {
        this.database = database;
        this.playerController = playerController;
        this.progressionSystem = progressionSystem;
        this.selectController = selectController;
        this.selectPresenter = selectPresenter;

        if (this.selectPresenter != null)
        {
            this.selectPresenter.Bind(this);
            this.selectPresenter.CardSelected += OnCardSelected;
        }

        if (this.selectController != null)
        {
            this.selectController.CardClicked += HandleCardClicked;
        }

        if (this.progressionSystem != null)
        {
            this.progressionSystem.OnPendingUpgradesChanged += HandlePendingUpgradesChanged;
        }
    }

    public void Unbind()
    {
        if (selectPresenter != null)
        {
            selectPresenter.Unbind(this);
            selectPresenter.CardSelected -= OnCardSelected;
        }
        if (selectController != null)
        {
            selectController.CardClicked -= HandleCardClicked;
        }
        if (progressionSystem != null)
        {
            progressionSystem.OnPendingUpgradesChanged -= HandlePendingUpgradesChanged;
        }
    }

    void HandlePendingUpgradesChanged(int pendingCount)
    {
        // Upgrades now appear at the end of the floor instead of interrupting active gameplay.
    }

    public void PresentNextUpgrade()
    {
        if (progressionSystem == null || progressionSystem.PendingUpgrades <= 0) return;
        if (database == null || database.Upgrades.Count == 0)
        {
            Debug.LogWarning("[UpgradeSelectionSystem] Cannot present upgrades: database is empty or null.");
            return;
        }

        // Check if player is dead or invalid
        if (playerController != null && playerController.Entity != null && playerController.Entity.IsDead)
        {
            return;
        }

        currentOfferedAssets = database.RollUpgrades(3);
        currentOfferedCards = new List<UpgradeCardData>();
        for (int i = 0; i < currentOfferedAssets.Count; i++)
        {
            currentOfferedCards.Add(UpgradeDatabase.ConvertToCardData(currentOfferedAssets[i]));
        }

        isSelecting = true;
        canSelectTime = Time.unscaledTime + selectionCooldown;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (selectPresenter != null)
        {
            Changed?.Invoke(currentOfferedCards);
        }
        else if (selectController != null)
        {
            selectController.ShowSelection(currentOfferedCards);
        }
    }

    void HandleCardClicked(int index)
    {
        if (!isSelecting) return;
        if (!CanSelect) return;
        if (index < 0 || index >= currentOfferedCards.Count) return;

        if (selectPresenter != null && !selectPresenter.SelectionResolved)
        {
            selectPresenter.Select(index);
        }

        if (isSelecting)
        {
            ResolvePick(index);
        }
    }

    void ResolvePick(int index)
    {
        if (!isSelecting) return;
        if (!CanSelect) return;
        if (index < 0 || index >= currentOfferedCards.Count) return;
        OnCardSelected(currentOfferedCards[index]);
    }

    void OnCardSelected(UpgradeCardData card)
    {
        if (!isSelecting) return;

        ScriptableObject chosenAsset = null;
        for (int i = 0; i < currentOfferedCards.Count; i++)
        {
            if (currentOfferedCards[i].id == card.id)
            {
                chosenAsset = currentOfferedAssets[i];
                break;
            }
        }

        // Fallback match by presenter selected index if id comparison didn't match
        if (chosenAsset == null && selectPresenter != null && selectPresenter.SelectedIndex >= 0 && selectPresenter.SelectedIndex < currentOfferedAssets.Count)
        {
            chosenAsset = currentOfferedAssets[selectPresenter.SelectedIndex];
        }

        if (chosenAsset != null && chosenAsset is IStatModifier modifier)
        {
            if (playerController != null && playerController.Entity is PlayerEntity pe)
            {
                pe.AddModifier(modifier);
                Debug.Log($"[UpgradeSelectionSystem] Applied upgrade '{chosenAsset.name}' to player!");
            }
            OnUpgradeApplied?.Invoke(chosenAsset);
        }

        progressionSystem.ConsumePendingUpgrade();

        if (selectController != null)
        {
            selectController.Hide();
        }

        isSelecting = false;

        // If another level-up was earned while selecting, present the next choice
        if (progressionSystem.PendingUpgrades > 0)
        {
            PresentNextUpgrade();
        }
        else
        {
            // Resume gameplay
            Time.timeScale = previousTimeScale;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Dismiss()
    {
        if (!isSelecting) return;
        isSelecting = false;
        if (selectController != null) selectController.Hide();
        if (selectPresenter != null) selectPresenter.Dismiss();
        Time.timeScale = previousTimeScale;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
