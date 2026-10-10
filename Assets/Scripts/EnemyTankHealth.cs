using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyTankHealth : MonoBehaviour
{
    public static int ActiveEnemyCount { get; private set; }

    public int maxHealth;
    [SerializeField] private Image healthFillImage;
    private int currentHealth;
    private EnemyTankAI enemyTankAI;
    private bool isDead;
    public GameObject turrent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveEnemyCount()
    {
        ActiveEnemyCount = 0;
    }

    private void OnEnable()
    {
        ActiveEnemyCount++;
    }

    private void OnDisable()
    {
        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
        if (isDead && ActiveEnemyCount == 0)
        {
            LevelEndController.ShowWin();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        enemyTankAI = GetComponent<EnemyTankAI>();
        UpdateHealthBar();
    }
    public void takeDamage(int damage)
    {
        if (isDead || damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;
        UpdateHealthBar();
        if (currentHealth <= 0)
        {
            isDead = true;
            if (enemyTankAI != null)
            {
                enemyTankAI.enabled = false;
            }

            Destroy(this.gameObject);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = maxHealth > 0
                ? Mathf.Clamp01((float)currentHealth / maxHealth)
                : 0f;
        }
    }
}
