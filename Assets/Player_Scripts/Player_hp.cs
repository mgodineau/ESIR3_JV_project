using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;

public class Player_hp : MonoBehaviour
{
    //Stats
    private int _health;
    
    public int Health {
		get => _health;
		set {
			_health = Math.Clamp(value, 0, maxHealth);
			UIManager.UpdateHealth(_health);
		}
		
    }

    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth;
    
    public bool isDead = false;

    void Start() {
	    Health = maxHealth;
    }

    public void TakeDammage(int dmg) {

	    Health -= dmg;
        isDead = Health <= 0;
    }
}
