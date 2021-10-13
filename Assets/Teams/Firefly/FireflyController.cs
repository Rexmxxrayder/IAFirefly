using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {

	public class FireflyController : BaseSpaceShipController
	{
		public List<IAAction> iaActions = new List<IAAction>();
		public override void Initialize(SpaceShipView spaceship, GameData data)
		{
		}

		public override InputData UpdateInput(SpaceShipView spaceship, GameData data)
		{
            int ActionToDo = 0;
            float highestPriority = 0;
            float finalPriority = 0;
            for (int i = 0; i < iaActions.Count; i++) {
                float actionPriority = iaActions[i].Priority();
                float actionFinalPriority = iaActions[i].finalPriority;
                if (highestPriority < actionPriority || highestPriority == actionPriority && finalPriority < actionFinalPriority) {
                    highestPriority = actionPriority;
                    finalPriority = actionFinalPriority;
                    ActionToDo = i;
                }
            }
			iaActions[ActionToDo].onAction.Invoke();
            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float thrust = 0.5f;
            float targetOrient = 0f;
            //float targetOrient = Orientation(spaceship, GetClosestAsteroid(spaceship, data.Asteroids).Position.x + 90.0f);
            bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
           // return new InputData(thrust, targetOrient, needShoot, false, false);
            return new InputData(1, 180, false, false, false);
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

		public void Shoot() {
			Debug.Log("Shoot");
		}
		public void MoveToNearCheckPoint() {
			Debug.Log("MoveToNearCheckPoint");
		}
	}

}
