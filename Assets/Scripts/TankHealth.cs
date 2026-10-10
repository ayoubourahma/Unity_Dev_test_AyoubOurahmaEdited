using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public Image healthFillImage; // Reference to the fill image

    public GameObject GameOverPanel;
    public bool IsGodMode { get; private set; }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void takeDamage(int damage)
    {
        if (IsGodMode || damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            GameAudioManager.PlayLoseLevel();
            Time.timeScale = 0f;

            if (GameOverPanel != null)
            {
                GameOverPanel.SetActive(true);
            }

            Destroy(this.gameObject);
        }
    }

    public void SetGodMode(bool enabled)
    {
        IsGodMode = enabled;
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}
