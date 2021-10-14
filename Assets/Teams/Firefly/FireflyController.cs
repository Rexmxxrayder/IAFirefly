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
        private bool isStun = false;
        private bool isStunEnnemy = false;
        private float timerTime;
        private float timerTimeEnnemy;
        private float timerValue = 0;
        private float timerValueEnnemy = 0;
        private int hitCount;
        private int hitCountEnnemy;
        private LayerMask asteroidMask = 12;
        public LayerMask asteroidAndCheckPointMask = 1 << 12 & 1 << 11;
        public LayerMask MineMask = 13;
        public override void Initialize(SpaceShipView spaceship, GameData data) {
            InitializeValueUpdater();
            timerTime = spaceship.HitCountdown;
            timerTimeEnnemy = spaceship.HitCountdown;
        }
        public override InputData UpdateInput(SpaceShipView spaceship, GameData data) {
            speed = 0;
            orientation = 0;
            mustShockwave = false;
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
            Debug.Log(Vector2.SignedAngle(toTarget, toAsteroid));
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

        #region ActionFunction
        public void Shoot() {
            mustShoot = true;
        }

        public void MoveToNearCheckPoint() {
            Debug.Log("MoveToNearCheckPoint");
        }

        public void LandMine() {
            mustLandMine = true;
        }

        public void Shockwave() {
            mustShockwave = true;
        }
        #endregion 

        private void DebugSpaceShip(SpaceShipView spaceship, Vector2 target, float targetOrient) {
            targetOrient *= Mathf.Deg2Rad;
            Debug.DrawLine(spaceship.Position, spaceship.Position + spaceship.Velocity, Color.red);
            Debug.DrawLine(spaceship.Position, target, Color.green);
            Debug.DrawLine(spaceship.Position, spaceship.Position + new Vector2(Mathf.Cos(targetOrient), Mathf.Sin(targetOrient)), Color.white);
        }

        #region VariableUpdater
        void InitializeValueUpdater() {
            ValueUpdater += DISTANCE_TO_SHIP_UPDATER;
            ValueUpdater += DISTANCE_TO_NEAR_OPEN_CHECKPOINT_UPDATER;
            ValueUpdater += DISTANCE_TO_NEAR_ASTEROID_UPDATER;
            ValueUpdater += ENERGY_UPDATER;
            ValueUpdater += STUN_UPDATER;
            ValueUpdater += ENNEMY_STUN_UPDATER;
            ValueUpdater += ENNEMY_IN_FRONT_OF_US_UPDATER;
            ValueUpdater += CHECKPOINT_BEHIND_ENNEMY_UPDATER;
            ValueUpdater += SCORE_HIGHER_UPDATER;
            ValueUpdater += ENNEMY_HIDE_UPDATER;
            ValueUpdater += TIME_LEFT_UPDATER;
        }

        void DISTANCE_TO_SHIP_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_SHIP] = Vector2.Distance(spaceship.Position, data.GetSpaceShipForOwner(1 - spaceship.Owner).Position);
        }
        void DISTANCE_TO_NEAR_OPEN_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void DISTANCE_TO_NEAR_ASTEROID_UPDATER(SpaceShipView spaceship, GameData data) {

        }
        void ENERGY_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.ENERGY] = spaceship.Energy;
        }
        void STUN_UPDATER(SpaceShipView spaceship, GameData data) {
            if (hitCountEnnemy != data.GetSpaceShipForOwner(1 - spaceship.Owner).HitCount) {
                isStun = true;
                timerValue = timerTime;
            }
            if (isStun) {
                timerValue -= Time.deltaTime;
                if (timerValue <= 0) {
                    isStun = false;
                }
            }
            if (isStun) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.STUN] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.STUN] = 0;
            }
        }

        void ENNEMY_STUN_UPDATER(SpaceShipView spaceship, GameData data) {
            if (hitCount != spaceship.HitCount) {
                isStunEnnemy = true;
                timerValueEnnemy = timerTimeEnnemy;
            }
            if (isStunEnnemy) {
                timerValueEnnemy -= Time.deltaTime;
                if (timerValueEnnemy <= 0) {
                    isStunEnnemy = false;
                }
            }
            if (isStunEnnemy) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_STUN] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_STUN] = 0;
            }
        }
        void ENNEMY_BEHIND_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = -spaceship.Orientation;
            float youEnemyAngle = Atan2(spaceship.Position - otherSpaceShip.Position);
            if (Mathf.Abs(orientAngle - youEnemyAngle) < 45f) { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_BEHIND_US] = 1; } else { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_BEHIND_US] = 0; }

        }
        void ENNEMY_IN_FRONT_OF_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = spaceship.Orientation;
            float youEnemyAngle = Atan2(spaceship.Position - otherSpaceShip.Position);
            if (Mathf.Abs(orientAngle - youEnemyAngle) < 45f) { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_IN_FRONT_OF_US] = 1; } else { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_IN_FRONT_OF_US] = 0; }
        }
        void CHECKPOINT_BEHIND_ENNEMY_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            Vector2 distanceOtherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner).Position - spaceship.Position;
            hit = Physics2D.Raycast(data.GetSpaceShipForOwner(1 - spaceship.Owner).Position, distanceOtherSpaceShip, asteroidAndCheckPointMask);
            if (hit) {
                if (hit.transform.CompareTag("WayPoint")) {
                    BlackBoard.Gino.scores[BlackBoard.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 0;
                }
                BlackBoard.Gino.scores[BlackBoard.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 1;
            }
        }
        void SCORE_HIGHER_UPDATER(SpaceShipView spaceship, GameData data) {
            if (spaceship.Score < data.GetSpaceShipForOwner(1 - spaceship.Owner).Score) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.SCORE_HIGHER] = -1;
            } else if (spaceship.Score == data.GetSpaceShipForOwner(1 - spaceship.Owner).Score) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.SCORE_HIGHER] = 0;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.SCORE_HIGHER] = 1;
            }
        }
        void ENNEMY_HIDE_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            Vector2 toOtherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner).Position - spaceship.Position;
            hit = Physics2D.Raycast(spaceship.Position, toOtherSpaceShip, toOtherSpaceShip.magnitude, asteroidMask);
            if (hit) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_HIDE] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_HIDE] = 0;
            }
        }

        void TIME_LEFT_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.TIME_LEFT] = data.timeLeft;
        }

        void MINE_NEAR_UPDATER(SpaceShipView spaceship, GameData data) {
            Collider2D[] hit;
            hit = Physics2D.OverlapCircleAll(spaceship.Position, BlackBoard.Gino.mineNear, MineMask);
            if (hit.Length > 0) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_NEAR] = 1;
            }else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_NEAR] = 0;
            }
        }


        void ON_CHEKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {

        }

        void MINE_FRONT_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            float spaceshipAngle = (spaceship.Orientation + 360) % 360;
            float x = Mathf.Cos(spaceshipAngle * Mathf.Deg2Rad);
            float y = Mathf.Sin(spaceshipAngle * Mathf.Deg2Rad);
            Vector2 front = new Vector2(x, y);

            hit = Physics2D.Raycast(spaceship.Position, front, MineMask);
            if (hit) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_FRONT] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_FRONT] = 0;
            }
        }
        #endregion
    }
}

