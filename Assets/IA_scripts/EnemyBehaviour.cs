using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditorInternal;
// using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyBehaviour : MonoBehaviour
{
    //Define objects
    private NavMeshAgent _agent;
    private Animator _anim;
    private GunBehaviour _gun;
    
    private Transform _player;	
    public LayerMask isWall;
    
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
    private static readonly int AimingTriggerHash = Animator.StringToHash("aiming");
    private static readonly int ShootTriggerHash = Animator.StringToHash("shoot");


    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _gun = GetComponentInChildren<GunBehaviour>();
    }

    private void Update()
    {
	    playerInAttackRange = Vector3.Distance( _player.transform.position, transform.position ) < sightRange;
		
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
            if(!playerInAttackRange)    //out of range
            {
                isChasing = true;
                isAttacking = false;
            }
        }
        
        _anim.SetBool(AimingTriggerHash, isAttacking);
        

        if (isPatroling) Patroling();
        else if (isChasing) ChasePlayer();
        else if (isAttacking) AttackPlayer();
        
    }

    private bool InSight()
    {
        // playerInSightRange = Physics.CheckSphere(transform.position, sightRange, isPlayer);
        playerInSightRange = Vector3.Distance(_player.position, transform.position) < sightRange;
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
        Vector3 targetDir = _player.position - transform.position;
        targetDir.y = 0;
        float deltaAngle = Vector3.SignedAngle( transform.forward, targetDir, Vector3.up);
        transform.Rotate( Vector3.up, deltaAngle / 90 * Time.deltaTime * _agent.angularSpeed );
        

        if (!alreadyAttacked)
        {
            ///Attack code here
			_anim.SetTrigger(ShootTriggerHash);

            ///End of attack code
            alreadyAttacked = true;
            StartCoroutine( ResetAttack(timeBetweenAttacks) );
        }
    }
    
    
    private IEnumerator ResetAttack(float delay) {
	    yield return new WaitForSeconds(delay);
        alreadyAttacked = false;
    }


    private void Shoot() {
	    _player.gameObject.TryGetComponent(out Player_hp playerComponent);
	    _gun.ShootAt(_player.transform.position);
	    playerComponent.TakeDammage(dmg);
    }

    public void TakeDammage(int dmg)
    {
        Debug.Log("damage taken");
        health -= dmg;
        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
}
