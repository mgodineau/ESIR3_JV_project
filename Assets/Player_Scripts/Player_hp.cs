using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Player_hp : MonoBehaviour
{
    //Stats
    private int _health;
	
    private int Health {
		get => _health;
		set {
			_health = Math.Max(0, value);
			healthDisplay.SetText("" + _health);
		}
		
    }

    [SerializeField] private TextMeshProUGUI healthDisplay;
    [SerializeField] private int maxHealth = 100;
    public bool isDead = false;

    void Start() {
	    Health = maxHealth;
    }

    public void TakeDammage(int dmg) {

	    Health -= dmg;
        isDead = Health <= 0;
    }
}
