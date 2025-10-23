using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    int current;

    void Start()
    {
        current = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        current -= dmg;
        Debug.Log($"{gameObject.name} took {dmg}. HP left: {current}");
        if (current <= 0) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}