using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootableTarget : MonoBehaviour
{


    [SerializeField] private float health = 5;//a  modif en fonction de la vie de la cible
    
	
    public void TakeDamage(float dmg)
    {
        health -= dmg;
        // Debug.Log($"Took {dmg} damage, now at {health} health");
        if (health <= 0)
        {
            //this.gameObject.SetActive(false); // Fais juste disparaitre l'objet mais toujours dans la hierarchie
            Destroy(this.gameObject); // A voir si destruction en plusieurs morceaux possible ??
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.GetComponent<Projectile>() !=null)
        {
            TakeDamage(collision.gameObject.GetComponent<Projectile>().GetDammage());
            Destroy(collision.gameObject);
        }
        
    }
}
