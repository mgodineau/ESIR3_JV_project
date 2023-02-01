using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    //Define objects
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask isGround, isPlayer, isWall;

    private GameObject[] nearestEnemies;
    private int enemyAdd;
    public int group = 4;
    private Transform alertSender;

    //Stats
    public int health = 100;
    public int dmg = 10;
    public int communicationRange = 10;


    //States
    public bool isPatroling = true;
    public bool isChasing = false;
    public bool isAttacking = false;
    public bool Alerted = false;

    //Patroling
    public Vector3 walkPoint;           //Point to go while walking
    bool walkPointSet = false;          //If a walk point is set
    public float walkPointRange = 10;    //walkPoint def range

    //private GameObject s;     //physic point (DEBUG)

    //Attacking
    public float timeBetweenAttacks;
    bool alreadyAttacked = false;

    //Range
    public float sightRange = 10;
    public float attackRange = 1;
    bool playerInSightRange = false;
    bool playerInAttackRange = false;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        //Patroling state
        if (isPatroling)
        {
            //Check sight range
            playerInSightRange = inSigth();
            //Debug.Log("inSightRange" + playerInSightRange);

            if (playerInSightRange)
            {
                isChasing = true;
                isPatroling = false;
                //Send Alert
                //sendAlert();
            }
        }

        //Chasing state
        else if (isChasing)
        {
            //Check attack range
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, isPlayer);
            if (playerInAttackRange)
            {
                isAttacking = true; 
                isChasing = false;
            }

            //player escape
            if ((transform.position - player.position).magnitude > sightRange)
            {
                isPatroling = true;
                isChasing = false;
            }
        }

        //Attacking state
        else if (isAttacking)
        {
            //Check attack range
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, isPlayer);
            if(!playerInAttackRange)    //out of range
            {
                isChasing = true;
                isAttacking = false;
            }
        }


        if (isPatroling) Patroling();
        else if (isChasing) ChasePlayer();
        else if (isAttacking) AttackPlayer();

        //else if (Alerted) ChaseAlert();
    }

    private bool inSigth()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, isPlayer);
        if (playerInSightRange)
        {
            //check walls
            bool intersect = Physics.Raycast(transform.position, player.position - transform.position, sightRange, isWall);     //Raycast(position, direction, maxdistance, tag)

            if (!intersect)
            {
                return true;
            }
        }
        return false;
    }
    private void Patroling()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
            //Debug.Log("Patroling ZZZZ!");
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        //Calculate random point in range
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        //Show walkPoint DEBUG
        /*
        s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.transform.position = walkPoint;
        Destroy(s,3);
        */

        // Check walkPoint throught walls and check ground
        if (Physics.Raycast(walkPoint, -transform.up, 2f, isGround) && !Physics.Raycast(transform.position, walkPoint - transform.position, (walkPoint - transform.position).magnitude, isWall))
        {
            walkPointSet = true;
        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
        //Debug.Log("Found it !!!!!");
    }

    private void AttackPlayer()
    {
        //Make sure enemy doesn't move
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            ///Attack code here
            Debug.Log("PAF !");
            player.gameObject.TryGetComponent(out Player_hp playerComponent);
            playerComponent.TakeDammage(dmg);
            ///End of attack code

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void TakeDammage(int dmg)
    {
        health -= dmg;
        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }


    /// <summary>
    /// TEST ACTION GROUP
    /// </summary>
    private void FindClosestEnemy()
    {
        GameObject[] gos;

        gos = GameObject.FindGameObjectsWithTag("Enemy");
        //Debug.Log(gos.Length);
        Vector3 position = transform.position;
        enemyAdd = 0;

        foreach (GameObject go in gos)
        {
                if (enemyAdd > group-1) break;

                go.TryGetComponent(out EnemyBehaviour enemyComponent);
                bool alertState = enemyComponent.Alerted;

                if (!alertState)
                {
                    Vector3 diff = go.transform.position - position;
                    if (diff.magnitude < communicationRange)
                    {
                        nearestEnemies[enemyAdd] = go;
                        enemyAdd++;
                    }
                }
        }
    }

    //Send alert signal to nearest enemies to chase Player
    private void sendAlert()
    {
        nearestEnemies = new GameObject[group];
        FindClosestEnemy();
        for (int e = 0; e < enemyAdd; e++)
        {
            nearestEnemies[enemyAdd].TryGetComponent(out EnemyBehaviour enemyComponent);
            enemyComponent.setAlert(transform);
        }        
    }

    //set Alert state & alert sender (enemy who chase player)
    public void setAlert(Transform sender)
    {
        if (isPatroling)
        {
            Alerted = true;
            isPatroling = false;
            alertSender = sender;
        }
    }

    //Chase alert sender unitil find player
    private void ChaseAlert()
    {
        agent.SetDestination(alertSender.position);

        playerInSightRange = inSigth();
        if (playerInSightRange)
        {
            isChasing = true;
            Alerted = false;
        }

        //solution if alertSender stop chasing
        Vector3 diff = transform.position - alertSender.position;
        if (diff.magnitude < 1f)
        {
            isPatroling = true;
            Alerted = false;
        }
    }

    //global alert function
    private void inAlert()
    {
        FindClosestEnemy();
        sendAlert();

    }
}
