using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayGun : MonoBehaviour
{


    public Transform gun_end;
    // [SerializeField] private int damage = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
   /* void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            ShootRay();
        }
    }

    public void ShootRay()
    {
        RaycastHit hit;
        if (Physics.Raycast(gun_end.position, gun_end.forward, out hit, 500))
        {

            if(hit.transform.GetComponent<ShootableTarget>() != null)
            {
                hit.transform.GetComponent<ShootableTarget>().TakeDamage(damage);
            }
        }

    }*/
}
