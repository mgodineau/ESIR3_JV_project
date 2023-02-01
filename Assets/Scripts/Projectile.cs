using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called before the first frame update
    private int damage = 1;

    public int GetDammage()

    { return damage; }

    private float time_spawned;
    private float life_span = 5f;

    void Start()
    {
        time_spawned = Time.timeSinceLevelLoad;
    }

    // Update is called once per frame
    void FixedUpdate()  // Pour faire depop le projectile
    {
        if (Time.timeSinceLevelLoad > time_spawned + life_span)
            Destroy(gameObject);
    }
}
