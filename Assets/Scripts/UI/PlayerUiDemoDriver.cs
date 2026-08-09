using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEST/DEMO-ONLY keyboard driver for the Player UI in TestingScene: pushes fake HUD/upgrade/game-over
/// data through the real (mock-backed) UI chain so the whole flow is playable before real systems
/// exist. Added automatically by PlayerUiBootstrap when enableDemoDriver is on. NOT production code.
/// Keys: H damage, J heal, K gain XP, L next floor, U offer upgrades, G game over, R retry.
/// </summary>
public class PlayerUiDemoDriver : MonoBehaviour
{
    PlayerUiBootstrap ui;
    PlayerHudData hud = PlayerHudData.Default();
    int offerRound;

    void Awake()
    {
        ui = GetComponent<PlayerUiBootstrap>();
    }

    void Update()
    {
        if (ui == null) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.hKey.wasPressedThisFrame) Damage(25);
        else if (kb.jKey.wasPressedThisFrame) Heal(15);
        else if (kb.kKey.wasPressedThisFrame) GainXp(25);
        else if (kb.lKey.wasPressedThisFrame) AdvanceFloor();
        else if (kb.uKey.wasPressedThisFrame) OfferUpgrades();
        else if (kb.gKey.wasPressedThisFrame) ShowGameOver();
        else if (kb.rKey.wasPressedThisFrame) ui.RetryRun();
    }

    void Damage(int amount)
    {
        hud.currentHealth = Mathf.Max(0, hud.currentHealth - amount);
        ui.HudSource.SetPlayerHud(hud);
    }

    void Heal(int amount)
    {
        hud.currentHealth = Mathf.Min(hud.maxHealth, hud.currentHealth + amount);
        ui.HudSource.SetPlayerHud(hud);
    }

    void GainXp(int amount)
    {
        hud.xp += amount;
        while (hud.xp >= hud.xpRequired)
        {
            hud.xp -= hud.xpRequired;
            hud.xpRequired += 50;
            hud.level++;
        }
        ui.HudSource.SetPlayerHud(hud);
    }

    void AdvanceFloor()
    {
        hud.floor++;
        ui.HudSource.SetPlayerHud(hud);
    }

    void OfferUpgrades()
    {
        offerRound++;
        if (offerRound % 2 == 0)
        {
            ui.UpgradeSource.SetUpgrades(MockUpgradeSource.CreateDefaultCards());
        }
        else
        {
            ui.UpgradeSource.SetUpgrades(new[]
            {
                new UpgradeCardData("upg_bag", "Bigger Pouch", "Carry more loot on every run.", "+2 slots", "bag"),
            });
        }
    }

    void ShowGameOver()
    {
        ui.GameOverSource.SetGameOver(new GameOverData(hud.floor, 42, 17 * 60 + 42f));
    }
}
