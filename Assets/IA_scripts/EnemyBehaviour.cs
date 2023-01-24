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

    //States
    public bool isPatroling = true;
    public bool isChasing = false;
    public bool isAttacking = false;

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
            }
        }

        //Chasing state
        else if (isChasing)
        {
            //Chack attack range
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, isPlayer);
            if (playerInAttackRange)
            {
                isAttacking = true; 
                isChasing = false;
            }
        }

        //Attacking state

        if (isPatroling) Patroling();
        else if (isChasing) ChasePlayer();
        else if (isAttacking) AttackPlayer();
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
        Debug.Log("Found it !!!!!");
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
            ///End of attack code

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    private void ResetAttack()
    {
        alreadyAttacked = false;
    }
}
