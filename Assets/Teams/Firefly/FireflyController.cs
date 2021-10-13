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
			float thrust = 0.1f;
			float targetOrient;
			//AsteroidView nearestAsteroid = data.Asteroids[1];
			//AsteroidView nearestAsteroid = GetClosestAsteroid(spaceship, data.Asteroids);
			//if (IsInRadius(spaceship.Position, nearestAsteroid.Position, nearestAsteroid.Radius + 0.5f))
			//{
			//	targetOrient = Orientation(Angle(spaceship.Position, nearestAsteroid.Position) + Mathf.Atan2(spaceship.Velocity.y, spaceship.Velocity.x));
			//	thrust = 0.5f;
			//} else
			//{
			//	targetOrient = Orientation(Angle(spaceship.Position, nearestAsteroid.Position));
			//	thrust = 0.75f;
			//}
			WayPointView nearestWayPoint = GetClosestPoint(spaceship, data.WayPoints);

			//if (IsInRadius(spaceship.Position, nearestWayPoint.Position, nearestWayPoint.Radius))
			//{
			//	targetOrient = Orientation(Angle(spaceship.Position, nearestWayPoint.Position) + Mathf.Atan2(spaceship.Velocity.y, spaceship.Velocity.x));
			//	thrust = 0.5f;
			//} else
			//{
				targetOrient = Orientation(Angle(spaceship.Position, nearestWayPoint.Position));
				thrust = 1.0f;
			//}

			//if (IsInRadius(spaceship.Position, nearestAsteroid.Position, nearestAsteroid.Radius + 2.0f))
			//{
			//	targetOrient = Orientation(Angle(spaceship.Position, GetClosestAsteroid(spaceship, data.Asteroids).Position) + 90.0f);
			//}
			//else
			//{
			//	targetOrient = spaceship.Orientation;
			//}
			bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
			return new InputData(thrust, targetOrient, needShoot, false, false);
		}

		public float Speed(float speed)
		{
			return speed;
		}

		public float Orientation(float orientation)
		{
			return orientation;
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

		public WayPointView GetClosestPoint(SpaceShipView spaceShip, List<WayPointView> waypoints)
		{
			WayPointView nearestPoint = waypoints[0];
			float dist = Vector2.Distance(nearestPoint.Position, spaceShip.Position);
			for (int i = 0; i < waypoints.Count; i++)
			{
				if (Vector2.Distance(waypoints[i].Position, spaceShip.Position) < dist && waypoints[i].Owner != spaceShip.Owner)
				{
					nearestPoint = waypoints[i];
				}
			}
			return nearestPoint;
		}

		public float Angle(Vector2 p1, Vector2 p2)
		{
			return Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * 180 / Mathf.PI;
		}

		public bool IsInRadius(Vector2 p1, Vector2 p2, float radius)
		{
			if (Vector2.Distance(p1, p2) < radius)
			{
				return true;
			}
			return false;
		} 
	}

}
