using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class ShootableTarget : MonoBehaviour
{

	public enum DamageType {
		Standard,
		Explosion,
		Petrifaction
	}

	
	
	
    [SerializeField] private float health = 5;//a  modif en fonction de la vie de la cible
    
	
    public void TakeDamage(float dmg, Vector3 direction, DamageType damageType = DamageType.Standard)
    {
        health -= dmg;
        if (health <= 0)
        {
	        KillTarget(direction, damageType);
        }
    }


    protected virtual void KillTarget( Vector3 direction, DamageType damageType ) {
	    Destroy(this.gameObject);
    }
    
    
    private void OnCollisionEnter(Collision collision) {

	    Projectile projectile = collision.gameObject.GetComponent<Projectile>();
	    if (projectile == null) {
		    return;
	    }
        TakeDamage(collision.gameObject.GetComponent<Projectile>().GetDammage(), collision.contacts[0].normal );
        Destroy(collision.gameObject);
        
        
    }
}
