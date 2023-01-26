using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_hp : MonoBehaviour
{
    //Stats
    public int health;
    int maxHealth;
    public bool isDead = false;

    void Awake()
    {
        maxHealth = health;
    }

    public void TakeDammage(int dmg)
    {
        health -= dmg;
        if (health <= 0) isDead = true;
    }
}
