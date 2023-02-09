using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Weapons
{

	[RequireComponent(typeof(Animator))]
	public class RifleArm : MonoBehaviour
	{


		private PlayerInventory inventory;

		private Animator _anim;
		private static readonly int Shoot = Animator.StringToHash("shoot");
		private static readonly int Change = Animator.StringToHash("change");

		[SerializeField] private GameObject shotgun;

		[SerializeField] private Transform canonEnd;
		[SerializeField] private ParticleSystem bulletParticles;
		[SerializeField] private int bulletParticlesCount = 1;
		[SerializeField] private int raycastCount = 20;


		[SerializeField] private float totalDamage = 1.0f;
		[SerializeField] private float spreadAngle = 10;
		[SerializeField] private float maxRange = 150;


		[SerializeField] private float bagAnimSpeed = 1.0f;

		private bool _readyToShoot = true;
		private float _ammoInMagRatio = 0.0f;
		private float _currentAmmoInMagRatio = 0.0f;

		public AudioSource AudioSource;
		public AudioClip shoot;
		public AudioClip reload;

		bool change = false;
		private void Awake()
		{
			_anim = GetComponent<Animator>();

		}

		private void OnEnable()
		{
			change = false;
		}


		private void Update() {
			if (change && _anim.GetCurrentAnimatorStateInfo(0).IsName("change"))
			{
				shotgun.SetActive(true);
				gameObject.SetActive(false);
				
			}
		}


		private void ReadyToShoot()
		{
			_readyToShoot = true;
		}

		private void OnChange()
		{
			print("changement arme");
			_anim.SetTrigger(Change);
			change = true;
		}
		private void OnFire()
		{

			if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("shoot"))
			{

				bulletParticles.Emit(bulletParticlesCount);
				_anim.SetTrigger(Shoot);

				for (int i = 0; i < raycastCount; i++)
				{
					Vector3 rayDir = Vector3.forward;

					rayDir = Quaternion.Euler(Random.Range(0, spreadAngle), 0, 0) * rayDir;
					rayDir = Quaternion.Euler(0, 0, Random.Range(0, 360)) * rayDir;

					rayDir = Quaternion.FromToRotation(Vector3.forward, transform.forward) * rayDir;


					RaycastHit hit;
					if (Physics.Raycast(canonEnd.position, rayDir, out hit, maxRange))
					{

						ShootableTarget target = hit.transform.GetComponent<ShootableTarget>();
						if (target != null)
						{
							target.TakeDamage(totalDamage / raycastCount, rayDir, ShootableTarget.DamageType.Explosion);
						}

					}

				}

				AudioSource.PlayOneShot(shoot);
				//_readyToShoot = false;
			}




		}
	}
}