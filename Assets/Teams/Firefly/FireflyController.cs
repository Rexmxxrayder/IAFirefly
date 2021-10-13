using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {
    public class FireflyController : BaseSpaceShipController {
        public List<IAAction> iaActions = new List<IAAction>();

        public override void Initialize(SpaceShipView spaceship, GameData data) {

        }

        public override InputData UpdateInput(SpaceShipView spaceship, GameData data) {
            BestActionToInvoke().onAction.Invoke();

            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            //float thrust = 1f;

            InputData result = RushPoints(spaceship, data);

            bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
            //DebugSpaceShip(spaceship, nearestWayPoint.Position, targetOrient);
            return result;
        }

        #region Movements

        private float GoTo(Vector2 target, SpaceShipView spaceship, GameData data) {
            Vector2 targetDirection = target - spaceship.Position;

            float directionAngle = Atan2(targetDirection);
            float velocityAngle = Atan2(spaceship.Velocity);
            float symmertyAngle = directionAngle + (directionAngle - velocityAngle);
            if (Mathf.Abs(ObtuseAngle(velocityAngle) - ObtuseAngle(directionAngle)) > 90f) {
                symmertyAngle = directionAngle;
            }
            return symmertyAngle;
        }

        private InputData RushPoints(SpaceShipView spaceship, GameData data) {
            float targetOrient;
            WayPointView nearestWayPoint = GetClosestPoint(spaceship.Position + spaceship.Velocity / 2f, data.WayPoints, spaceship.Owner);
            if (nearestWayPoint == null) {
                Debug.Log(data.timeLeft);
                Debug.Break();
                nearestWayPoint = data.WayPoints[0];
            }

            WayPointView nearestNextWayPoint = GetClosestPoint(nearestWayPoint.Position, data.WayPoints, spaceship.Owner, nearestWayPoint);
            if (nearestNextWayPoint == null) {
                nearestNextWayPoint = data.WayPoints[0];
            }

            float nextPointAngle = Atan2(nearestNextWayPoint.Position - nearestWayPoint.Position);
            float angleNearestPoint = Atan2(spaceship.Position - nearestWayPoint.Position);
            float midAngle = (ObtuseAngle(nextPointAngle) - ObtuseAngle(angleNearestPoint)) / 2f;
            Debug.Log(midAngle + " .. " + nextPointAngle + " .. " + angleNearestPoint);
            if (Mathf.Abs(midAngle) > 90f) { midAngle -= 180f; }
            float targetAngle = angleNearestPoint + midAngle;

            Vector2 target = nearestWayPoint.Position + PointOnCircle(targetAngle, nearestWayPoint.Radius + spaceship.Radius / 2f);
            Debug.DrawLine(nearestWayPoint.Position, target, Color.blue);
            Debug.DrawLine(nearestNextWayPoint.Position, nearestWayPoint.Position, Color.grey);
            Debug.DrawLine(spaceship.Position, nearestWayPoint.Position, Color.grey);
            //targetDirection = target - spaceship.Position;
            //directionAngle = Atan2(targetDirection);

            targetOrient = GoTo(target, spaceship, data);

            DebugSpaceShip(spaceship, target, targetOrient);
            return new InputData(1f, targetOrient, false, false, false);
        }

        #endregion

        private Vector2 PointOnCircle(float angle, float radius) {
            float x = Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = Mathf.Sin(angle * Mathf.Deg2Rad);
            return new Vector2(x, y) * radius;
        }

        private float Atan2(Vector2 vector) {
            float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
            return angle;
        }

        public float ObtuseAngle(float angle) {
            if (angle < 0f) {
                angle += 360f;
            }
            return angle;
        }

        IAAction BestActionToInvoke() {
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
            return iaActions[ActionToDo];
        }

        public AsteroidView GetClosestAsteroid(SpaceShipView spaceShip, List<AsteroidView> asteroids) {
            AsteroidView nearestAsteroid = asteroids[0];
            float dist;
            dist = Vector2.Distance(nearestAsteroid.Position, spaceShip.Position);
            for (int i = 0; i < asteroids.Count; i++) {
                if (Vector2.Distance(asteroids[i].Position, spaceShip.Position) < dist) {
                    nearestAsteroid = asteroids[i];
                }
            }
            return nearestAsteroid;
        }

        public WayPointView GetClosestPoint(Vector2 position, List<WayPointView> waypoints, int owner, params WayPointView[] waypointToIgnore) {
            WayPointView nearestPoint = null;
            float dist = Mathf.Infinity;
            for (int i = 0; i < waypoints.Count; i++) {
                if (waypointToIgnore != null) {
                    bool skip = false;
                    for (int j = 0; j < waypointToIgnore.Length; j++) {
                        if (waypointToIgnore[j] == waypoints[i]) { skip = true; break; }
                    }
                    if (skip) { continue; }
                }
                float dist2 = Vector2.Distance(waypoints[i].Position, position);
                if (dist2 < dist && waypoints[i].Owner != owner) {
                    nearestPoint = waypoints[i];
                    dist = dist2;
                }
            }
            return nearestPoint;
        }

        public bool IsInRadius(Vector2 p1, Vector2 p2, float radius) {
            if (Vector2.Distance(p1, p2) < radius) {
                return true;
            }
            return false;
        }

        public void Shoot() {
            Debug.Log("Shoot");
        }

        public void MoveToNearCheckPoint() {
            Debug.Log("MoveToNearCheckPoint");
        }

        private void DebugSpaceShip(SpaceShipView spaceship, Vector2 target, float targetOrient) {
            targetOrient *= Mathf.Deg2Rad;
            Debug.DrawLine(spaceship.Position, spaceship.Position + spaceship.Velocity, Color.red);
            Debug.DrawLine(spaceship.Position, target, Color.green);
            Debug.DrawLine(spaceship.Position, spaceship.Position + new Vector2(Mathf.Cos(targetOrient), Mathf.Sin(targetOrient)), Color.white);
        }
    }
}

