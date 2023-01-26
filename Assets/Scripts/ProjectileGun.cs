using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : MonoBehaviour
{
    public GameObject projectile_prefab;
    public Transform gun_end;


    public float range = 100f;
    public int bulletsPerMag = 10;
    public int bulletsleft = 35;
    public int currentbullets;

    public float fireRate = 0.1f;

    


    // Start is called before the first frame update
    void Start()
    {
        currentbullets = bulletsPerMag;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0)&& currentbullets>0)
        {
            GameObject projectile = Instantiate(projectile_prefab, gun_end.position, transform.rotation);
            projectile.GetComponent<Rigidbody>().AddForce(gun_end.forward * 600);
            currentbullets--;
            bulletsleft--;
            
        }

        if(currentbullets == 0)
        {
            Debug.Log("Appyer sur R pour recharger");
        }

        if (Input.GetKeyDown(KeyCode.R) && bulletsleft >= 0)
        {
            PickupAmmo(bulletsPerMag-currentbullets);
            

        }
        else
        {
            Debug.Log("vous n'avez plus de munitions");
        }
        

   

    }
    public void PickupAmmo(int amount)
    {
        currentbullets += amount;
    }

}
