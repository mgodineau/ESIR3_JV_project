using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviour : MonoBehaviour
{
    //Define objects
    private NavMeshAgent _agent;

    private Transform _player;
    public LayerMask isPlayer, isWall;

    //Stats
    private int health = 100;
    private int dmg = 10;

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
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
		
	    //Patroling state
        if (isPatroling)
        {
            //Check sight range
            playerInSightRange = InSight();
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
            //Check attack range
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, isPlayer);
            if (playerInAttackRange)
            {
                isAttacking = true; 
                isChasing = false;
            }
            
            //player escape
            if ((transform.position - _player.position).magnitude > sightRange)
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
        
    }

    private bool InSight()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, isPlayer);
        if (playerInSightRange)
        {
            //check walls
            bool intersect = Physics.Raycast(transform.position, _player.position - transform.position, sightRange, isWall);     //Raycast(position, direction, maxdistance, tag)

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
            _agent.SetDestination(walkPoint);
        }
		

        //Walkpoint reached
        if (_agent.remainingDistance <= _agent.stoppingDistance)
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
        walkPointSet = true;
    }

    private void ChasePlayer()
    {
        _agent.SetDestination(_player.position);
        // Debug.Log("Found it !!!!!");
    }

    private void AttackPlayer()
    {
        //Make sure enemy doesn't move
        _agent.SetDestination(transform.position);

        transform.LookAt(_player);

        if (!alreadyAttacked)
        {
            ///Attack code here
            // Debug.Log("PAF !");
            _player.gameObject.TryGetComponent(out Player_hp playerComponent);
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
}
