using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectileShotgun : MonoBehaviour
{
    public GameObject projectile_prefab;
    public Transform gun_end;
    public Transform gun_end2; //Si pompe = plusieurs sorties de balle ?
    public Transform gun_end3;


    public TMP_Text ammo_text;

    public float range = 2500f;
    public int bulletsPerMag = 15;
    public int bulletsleft = 35;
    public int currentbullets;

    public float fireRate = 0.1f;

    Animator m_animator;


    // Start is called before the first frame update
    void Start()
    {
        currentbullets = bulletsPerMag;
        UpdateAmmoText();
        m_animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0)&& currentbullets>0)
        {
            GameObject projectile = Instantiate(projectile_prefab, gun_end.position, transform.rotation);
            GameObject projectile2 = Instantiate(projectile_prefab, gun_end2.position, transform.rotation);
            GameObject projectile3 = Instantiate(projectile_prefab, gun_end3.position, transform.rotation);
            projectile.GetComponent<Rigidbody>().AddForce(gun_end.forward * range);
            projectile2.GetComponent<Rigidbody>().AddForce(gun_end2.forward * range);
            projectile3.GetComponent<Rigidbody>().AddForce(gun_end3.forward * range);
            currentbullets = currentbullets-3;
            if(currentbullets<1)
            {
                currentbullets = 0;
            }
            //bulletsleft--;
            m_animator.SetTrigger("Shoot");
            UpdateAmmoText();
            
        }

        if(currentbullets == 0)
        {
            Debug.Log("Appyer sur R pour recharger");
        }

        if (Input.GetKeyDown(KeyCode.R) && bulletsleft >= 0)
        {
            if (bulletsleft >= bulletsPerMag)
            {
                bulletsleft = bulletsleft -(bulletsPerMag- currentbullets);
                PickupAmmo(bulletsPerMag  -currentbullets);
                
                UpdateAmmoText();
            }
            else
            {
                PickupAmmo(bulletsleft - currentbullets);
                bulletsleft = bulletsleft - currentbullets;
                UpdateAmmoText();
            }
            

        }
        else
        {
            Debug.Log("vous n'avez plus de munitions");
        }
        

   

    }
    public void PickupAmmo(int amount)
    {
        currentbullets += amount;
        UpdateAmmoText();
    }


    private void UpdateAmmoText()
    {
        if (bulletsleft<1)
        {
            bulletsleft = 0;
        }

        ammo_text.text = $"{currentbullets}/{bulletsleft}";    }
}
