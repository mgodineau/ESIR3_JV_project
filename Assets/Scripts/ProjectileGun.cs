using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : MonoBehaviour
{
    public GameObject projectile_prefab;
    public Transform gun_end;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            GameObject projectile = Instantiate(projectile_prefab, gun_end.position, transform.rotation);
            projectile.GetComponent<Rigidbody>().AddForce(gun_end.forward * 1000);
        }
    }
}
