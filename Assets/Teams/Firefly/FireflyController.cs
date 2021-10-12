using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {

	public class FireflyController : BaseSpaceShipController
	{
		public List<IAAction> iaactions = new List<IAAction>();
		public override void Initialize(SpaceShipView spaceship, GameData data)
		{
		}

		public override InputData UpdateInput(SpaceShipView spaceship, GameData data)
		{
			SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
			Debug.Log("Move");
			float thrust = 1.0f;
			float targetOrient = Orientation(spaceship, GetClosestAsteroid(spaceship, data.Asteroids).Position.x + 90.0f);
			bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
			return new InputData(thrust, targetOrient, needShoot, false, false);
		}

		public float Speed(float speed)
		{
			return speed;
		}

		public float Orientation(SpaceShipView spaceShip, float orientation)
		{
			return spaceShip.Orientation + orientation;
		}

		public AsteroidView GetClosestAsteroid(SpaceShipView spaceShip, List<AsteroidView> asteroids)
		{
			AsteroidView nearestAsteroid = asteroids[0];
			float dist;
			dist = Vector2.Distance(nearestAsteroid.Position, spaceShip.Position);
			for (int i = 0; i < asteroids.Count; i++)
			{
				if (Vector2.Distance(asteroids[i].Position, spaceShip.Position) < dist)
				{
					nearestAsteroid = asteroids[i];
				}
			}
			return nearestAsteroid;
		}
	}

}
