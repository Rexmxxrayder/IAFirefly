using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DoNotModify
{
	public class SpaceShipView
	{
		public SpaceShipView(SpaceShip spaceship) { _spaceship = spaceship; }

		public int Owner { get { return _spaceship.Owner; } }
		public float Thrust { get { return _spaceship.Thrust; } }
		public Vector2 Velocity { get { return _spaceship.Velocity; } }
		public float SpeedMax { get { return _spaceship.SpeedMax; } }
		public Vector2 Position { get { return (Vector2)(_spaceship.Position); } }
		public float Orientation { get { return _spaceship.Orientation; } }
		public float Radius { get { return _spaceship.Radius; } }
		public float HitCountdown { get { return _spaceship.HitCountdown; } }
		public float Energy { get { return _spaceship.Energy; } }
		public float MineEnergyCost { get { return _spaceship.MineEnergyCost; } }
		public float ShootEnergyCost { get { return _spaceship.ShootEnergyCost; } }
		public float ShockwaveEnergyCost { get { return _spaceship.ShockwaveEnergyCost; } }
		public int HitCount { get { return _spaceship.HitCount; } }
		public int Score { get { return _spaceship.Score; } }

		private SpaceShip _spaceship;
	}

	public class SpaceShip : MonoBehaviour
	{
		public SpaceShipView view;

		public int Owner { get { return _owner; } }
		public float Thrust { get { return _thrust; } }
		public Vector2 Velocity { get { return _rigidbody.velocity; } }
		public float SpeedMax { get { return _speedMax; } }
		public Vector2 Position { get { return (Vector2)(transform.position); } }
		public float Orientation { get { return transform.eulerAngles.z; } }
		public float Radius { get { return _collider.radius * _collider.transform.lossyScale.x; } }
		public float HitCountdown { get { return _hitCountdown; } }
		public float Energy { get { return _energy; } }
		public float MineEnergyCost { get { return _mineEnergyCost; } }
		public float ShootEnergyCost { get { return _shootEnergyCost; } }
		public float ShockwaveEnergyCost { get { return _shockwaveEnergyCost; } }
		public int HitCount { get { return _hitCount; } }
		public int Score { get { return GameManager.Instance.GetScoreForPlayer(_owner); } }

		// Gameplay
		[SerializeField]
		private Color color;
		[SerializeField]
		private Mine minePrefab;
		[SerializeField]
		private Bullet bulletPrefab;
		[SerializeField]
		private Shockwave shockwavePrefab;

		private int _owner;

		private const float _hitDuration = 3.0f;
		private float _hitSpeedFactor = 0.3f;
		private float _hitCountdown = 0.0f;
		private int _hitCount = 0;

		private const float _stunDuration = 1.5f;
		private float _stunCountdown = 0.0f;

		private float _energy = 1.0f;
		private float _mineEnergyCost = 0.2f;
		private float _shootEnergyCost = 0.2f;
		private float _shockwaveEnergyCost = 0.4f;
		private float _energyPerSecond = 0.12f;
		private float _trustConsumption = 0.5f;

		// Visual
		[SerializeField]
		private Material hitMaterial = null;		
		[SerializeField]
		private GameObject stunFXPrefab;
		private GameObject _stunFX = null;
		private MeshRenderer _meshRenderer = null;

		// Spaceship speed
		private float _thrust = 0.0f;
		private const float _speedForThrust = 5.0f;
		private const float _speedMax = 2.5f;

		// Spaceship orientation
		private float _orientationTarget = 0.0f;
		private const float _rotationSpeed = 180.0f;

		private BaseSpaceShipController _controller = null;

		// Spaceship physics
		private Rigidbody2D _rigidbody = null;
		private CircleCollider2D _collider = null;
		private ParticleSystem _thrustFX = null;

		private Vector2 previousPosition = Vector2.zero;
		private float previousRotation = 0.0f;
		private bool willCheckRigidbody = false;

		private void Awake()
		{

			_rigidbody = GetComponent<Rigidbody2D>();
			_collider = GetComponentInChildren<CircleCollider2D>();
			previousPosition = _rigidbody.position;
			previousRotation = _rigidbody.rotation;
			_thrustFX = GetComponentInChildren<ParticleSystem>();
			ParticleSystem.EmissionModule emission = _thrustFX.emission;
			emission.enabled = false;
			_orientationTarget = transform.eulerAngles.z;
			_meshRenderer = GetComponentInChildren<MeshRenderer>();

			view = new SpaceShipView(this);
			GameManager.Instance.GetGameData().SpaceShips.Add(view);
		}

		private void OnDestroy()
		{
			GameManager.Instance.GetGameData().SpaceShips.Remove(view);
		}


		private void Update()
		{
			if (GameManager.Instance.IsGameFinished())
				return;

			if (IsHit())
			{
				_hitCountdown = Mathf.Max(_hitCountdown - Time.deltaTime, 0);
				if (_hitCountdown <= 0)
				{
					_meshRenderer.materials = new Material[1] { _meshRenderer.materials[0] };
				}
			}

			if (IsStun())
			{
				_stunCountdown = Mathf.Max(_stunCountdown - Time.deltaTime, 0);
				if (_stunCountdown <= 0)
				{
					Destroy(_stunFX);
				}
			}


			if (_controller == null)
				return;

			InputData inputData = _controller.UpdateInput(this.view, GameManager.Instance.GetGameData());
			_thrust = 0.0f;
			if (!IsStun())
			{
				_thrust = Mathf.Clamp01(inputData.thrust);
				_orientationTarget = Mathf.Repeat(inputData.targetOrientation, 360.0f);
				_orientationTarget = Mathf.Repeat(inputData.targetOrientation, 360.0f);

				if (inputData.shoot)
				{
					Shoot();
				}
				if (inputData.dropMine)
				{
					DropMine();
				}
				if (inputData.fireShockwave)
				{
					FireShockwave();
				}
			}
			_energy = Mathf.Clamp01(_energy + _energyPerSecond * Time.deltaTime * Mathf.Lerp(1, 1-_trustConsumption, _thrust));

			ParticleSystem.EmissionModule emission = _thrustFX.emission;
			emission.enabled = _thrust > 0;
		}

		private void LateUpdate()
		{
			willCheckRigidbody = true;
			previousPosition = _rigidbody.position;
			previousRotation = _rigidbody.rotation;
		}


		private void FixedUpdate()
		{
			if (IsStun())
				return;
			/*if(!CheckRigidbody())
				return;*/
			float speedMax = _speedMax * (IsHit() ? _hitSpeedFactor : 1.0f);
			Vector2 direction = new Vector2(Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad), Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad));
			_rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity + direction * _thrust * _speedForThrust * Time.fixedDeltaTime, speedMax);
			if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, _orientationTarget)) > Mathf.Epsilon)
			{
				float deltaRotation = _rotationSpeed * Time.fixedDeltaTime;
				if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, _orientationTarget)) < deltaRotation)
				{
					transform.eulerAngles = new Vector3(0.0f, 0.0f, _orientationTarget);
				}
				else
				{
					transform.eulerAngles += new Vector3(0.0f, 0.0f, deltaRotation * Mathf.Sign(Mathf.DeltaAngle(transform.eulerAngles.z, _orientationTarget)));
				}
			}
		}

		private bool CheckRigidbody()
		{
			if (!willCheckRigidbody)
				return true;

			if (_rigidbody.position != previousPosition)
			{
				Debug.LogError("GameObject position was changed elsewhere. Be sure not to change position directly in your controller.");
				return false;
			}
			if (_rigidbody.rotation != previousRotation)
			{
				Debug.LogError("GameObject rotation was changed elsewhere. Be sure not to change rotation directly in your controller.");
				return false;
			}
			return true;
		}

		public void Initialize(BaseSpaceShipController controller, int owner)
		{
			_controller = controller;
			_owner = owner;
		}

		public void DropMine()
		{
			if (_energy < _mineEnergyCost)
				return;
			GameObject.Instantiate(minePrefab, transform.position, Quaternion.identity);
			_energy -= _mineEnergyCost;
		}

		public void Shoot()
		{
			if (_energy < _shootEnergyCost)
				return;
			Bullet spawned = GameObject.Instantiate<Bullet>(bulletPrefab, transform.position, transform.rotation);
			spawned.SetOwner(_owner);
			_energy -= _shootEnergyCost;
		}

		public void FireShockwave()
		{
			if (_energy < _shockwaveEnergyCost)
				return;
			Shockwave spawned = GameObject.Instantiate<Shockwave>(shockwavePrefab, transform.position, transform.rotation);
			spawned.SetOwner(_owner);
			_energy -= _shockwaveEnergyCost;
		}

		public void OnHitMine(Mine mine)
		{
			if (!mine.IsHitting())
				return;
			Hit();
		}

		public void OnHitBullet(Bullet bullet)
		{
			if (!bullet.IsHitting())
				return;
			Hit();
		}

		public void OnApplyShockwave(Shockwave shockwave)
		{
			Stun();
		}

		private void Stun()
		{
			if (!IsStun())
			{
				_stunFX = GameObject.Instantiate<GameObject>(stunFXPrefab, transform, false);
			}
			_stunCountdown = _stunDuration;
		}

		private void Hit()
		{
			if (!IsHit())
			{
				_hitCount++;
			}

			_hitCountdown = _hitDuration;
			_meshRenderer.materials = new Material[2] { _meshRenderer.materials[0], hitMaterial };
		}

		public bool IsHit()
		{
			return _hitCountdown > 0.0f;
		}

		public bool IsStun()
		{
			return _stunCountdown > 0.0f;
		}

		public Color GetColor()
		{
			return color;
		}
	}

}