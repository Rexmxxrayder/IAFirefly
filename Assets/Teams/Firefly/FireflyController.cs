using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {
    public class FireflyController : BaseSpaceShipController {
        public List<IAAction> iaActions = new List<IAAction>();
        float speed = 0;
        float orientation = 0;
        bool mustShockwave = false;
        bool mustLandMine = false;
        bool mustShoot = false;
        public delegate void MyDelegate(SpaceShipView spaceship, GameData data);
        private MyDelegate ValueUpdater;

        public override void Initialize(SpaceShipView spaceship, GameData data) {
            InitializeValueUpdater();
        }
        public override InputData UpdateInput(SpaceShipView spaceship, GameData data) {
            speed = 1f;
            orientation = 0;
            mustShockwave = Input.GetKeyDown(KeyCode.Space);
            mustLandMine = false;
            mustShoot = false;
            BestActionToInvoke().onAction.Invoke();
            InputData result = new InputData(speed, orientation, mustShoot, mustLandMine, mustShockwave);

            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);

            result = RushPoints(spaceship, data, result);

            bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
            //DebugSpaceShip(spaceship, nearestWayPoint.Position, targetOrient);
            return result;
        }

        #region Movements

        private float GoTo(Vector2 target, SpaceShipView spaceship, GameData data, bool avoidAsteroid = true) {
            Vector2 targetDirection = target - spaceship.Position;

            float directionAngle = Atan2(targetDirection);
            float velocityAngle = Atan2(spaceship.Velocity);
            float symmetryAngle = directionAngle + (directionAngle - velocityAngle);
            if (Mathf.Abs(ObtuseAngle(velocityAngle) - ObtuseAngle(directionAngle)) > 90f) {
                symmetryAngle = directionAngle;
            }

            if (!avoidAsteroid) { return symmetryAngle; }

            RaycastHit2D[] hits = Physics2D.CircleCastAll(spaceship.Position, spaceship.Radius, targetDirection, targetDirection.magnitude);
            if (hits.Length > 0) {
                for (int i = 0; i < hits.Length; i++) {
                    if (hits[i].collider.CompareTag("Asteroid")) {
                        return GoTo(AvoidAsteroid(target, spaceship, data, hits[i].collider.gameObject.GetComponentInParent<Asteroid>()), spaceship, data, false);
                    }
                }
            }

            return symmetryAngle;
        }

        private Vector2 AvoidAsteroid(Vector2 target, SpaceShipView spaceship, GameData data, Asteroid asteroid) {
            Vector2 perp = -Vector2.Perpendicular(target - spaceship.Position);
            Vector2 toTarget = target - spaceship.Position;
            Vector2 toAsteroid = asteroid.Position - spaceship.Position;
            if (Vector2.SignedAngle(toTarget, toAsteroid) < 0f) {
                perp *= -1;
            }
            perp.Normalize();
            perp *= asteroid.Radius + spaceship.Radius + spaceship.Radius * 0.2f;
            Vector2 soluce = asteroid.Position + perp;
            Debug.DrawLine(asteroid.Position, soluce, Color.red);
            return soluce;
        }

        private InputData RushPoints(SpaceShipView spaceship, GameData data, InputData inputData) {
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
            if (Mathf.Abs(midAngle) > 90f) { midAngle -= 180f; }
            float targetAngle = angleNearestPoint + midAngle;

            Vector2 target = nearestWayPoint.Position + PointOnCircle(targetAngle, Mathf.Abs(nearestWayPoint.Radius) + spaceship.Radius / 2f);
            Debug.DrawLine(nearestWayPoint.Position, target, Color.blue);
            Debug.DrawLine(nearestNextWayPoint.Position, nearestWayPoint.Position, Color.grey);
            Debug.DrawLine(spaceship.Position, nearestWayPoint.Position, Color.grey);

            targetOrient = GoTo(target, spaceship, data);

            DebugSpaceShip(spaceship, target, targetOrient);
            inputData.targetOrientation = targetOrient;

            if (spaceship.Energy < 1f &&
                spaceship.Velocity.magnitude >= spaceship.SpeedMax &&
                Mathf.Abs(Atan2(spaceship.Velocity) - targetOrient) < 5f) {
                inputData.thrust = 0f;
            }
            return inputData;
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

        #region VariableUpdater
        void InitializeValueUpdater() {

        }

        void DISTANCE_TO_SHIP_UPDATER(SpaceShipView spaceship, GameData data) {
           // BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_SHIP] = 
        }
        void DISTANCE_TO_NEAR_OPEN_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {
            WayPointView point = GetClosestPoint(spaceship.Position + spaceship.Velocity / 2f, data.WayPoints, spaceship.Owner);
            float distance = Vector2.Distance(point.Position, spaceship.Position);
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_NEAR_OPEN_CHECKPOINT] = distance;
        }
        void DISTANCE_TO_NEAR_ASTEROID_UPDATER(SpaceShipView spaceship, GameData data) {
            AsteroidView asteroid = GetClosestAsteroid(spaceship, data.Asteroids);
            float distance = Vector2.Distance(asteroid.Position, spaceship.Position);
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_NEAR_ASTEROID] = distance;
        }
        void ENERGY_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void STUN_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENNEMY_STUN_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENNEMY_BEHIND_US_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENNEMY_IN_FRONT_OF_US_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void CHECKPOINT_BEHIND_ENNEMY_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void SCORE_HIGHER_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENNEMY_HIDE_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENNEMY_NEAR_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        #endregion

    }
}

