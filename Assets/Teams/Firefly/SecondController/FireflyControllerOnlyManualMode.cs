using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {
    public class FireflyControllerOnlyManualMode : BaseSpaceShipController {
        private InputData inputData;
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
        private LayerMask CheckPointMask = 11;
        private LayerMask asteroidAndCheckPointMask = 1 << 12 & 1 << 11;
        private LayerMask MineMask = 13;
        public override void Initialize(SpaceShipView spaceship, GameData data) {
            InitializeValueUpdater();
            timerTime = spaceship.HitCountdown;
            timerTimeEnnemy = spaceship.HitCountdown;
        }
        public override InputData UpdateInput(SpaceShipView spaceship, GameData data) {
            ValueUpdater(spaceship, data);
            float thrust = (Input.GetAxis("KbVertical") > 0.0f) ? 1.0f : 0.0f;
            float targetOrient = spaceship.Orientation;
            float direction = Input.GetAxis("KbHorizontal");
            if (direction != 0.0f) {
                targetOrient -= Mathf.Sign(direction) * 90;
            }
            bool shoot = Input.GetButtonDown("KbFire1");
            bool dropMine = Input.GetButtonDown("KbFire2");
            bool fireShockwave = Input.GetButtonDown("KbFire3");

            return new InputData(thrust, targetOrient, shoot, dropMine, fireShockwave);
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
            inputData.shoot = true;
        }

        public void LandMine() {
            inputData.dropMine = true;
        }

        public void Shockwave() {
            inputData.fireShockwave = true;
        }

        public void RushPoints(SpaceShipView spaceship, GameData data) {
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
        }

        public void FollowEnemy(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            inputData.thrust = 1.0f;
            inputData.targetOrientation = GoTo(otherSpaceship.Position, spaceship, data, true);
            if (spaceship.Energy < 1f &&
                spaceship.Velocity.magnitude >= spaceship.SpeedMax &&
                Mathf.Abs(Atan2(spaceship.Velocity) - inputData.targetOrientation) < 5f) {
                inputData.thrust = 0f;
            }
        }

        public void TurretMode(SpaceShipView spaceship, GameData data) {

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
            ValueUpdater += ENNEMY_BEHIND_US_UPDATER;
            ValueUpdater += ON_CHECKPOINT_UPDATER;
            ValueUpdater += MINE_FRONT_UPDATER;
            ValueUpdater += MINE_NEAR_UPDATER;
            ValueUpdater += NEAR_CHECKPOINT_ENEMY_UPDATER;
            ValueUpdater += NEAR_CHECKPOINT_NEUTRAL_UPDATER;
        }

        void DISTANCE_TO_SHIP_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.DISTANCE_TO_SHIP] = Vector2.Distance(spaceship.Position, data.GetSpaceShipForOwner(1 - spaceship.Owner).Position);
        }
        void DISTANCE_TO_NEAR_OPEN_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {
            WayPointView point = GetClosestPoint(spaceship.Position + spaceship.Velocity / 2f, data.WayPoints, spaceship.Owner);
            float distance = Vector2.Distance(point.Position, spaceship.Position);
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.DISTANCE_TO_NEAR_OPEN_CHECKPOINT] = distance;
        }
        void DISTANCE_TO_NEAR_ASTEROID_UPDATER(SpaceShipView spaceship, GameData data) {
            AsteroidView asteroid = GetClosestAsteroid(spaceship, data.Asteroids);
            float distance = Vector2.Distance(asteroid.Position, spaceship.Position);
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.DISTANCE_TO_NEAR_ASTEROID] = distance;
        }
        void ENERGY_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENERGY] = spaceship.Energy;
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
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.STUN] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.STUN] = 0;
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
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_STUN] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_STUN] = 0;
            }
        }
        void ENNEMY_BEHIND_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = -spaceship.Orientation;
            float youEnemyAngle = Atan2(spaceship.Position - otherSpaceShip.Position);
            if (Mathf.Abs(orientAngle - youEnemyAngle) < 45f) { BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_BEHIND_US] = 1; } else { BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_BEHIND_US] = 0; }
        }
        void ENNEMY_IN_FRONT_OF_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = spaceship.Orientation;
            float youEnemyAngle = Atan2(spaceship.Position - otherSpaceShip.Position);
            if (Mathf.Abs(orientAngle - youEnemyAngle) < 45f) { BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_IN_FRONT_OF_US] = 1; } else { BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_IN_FRONT_OF_US] = 0; }
        }
        void CHECKPOINT_BEHIND_ENNEMY_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            Vector2 distanceOtherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner).Position - spaceship.Position;
            hit = Physics2D.Raycast(data.GetSpaceShipForOwner(1 - spaceship.Owner).Position, distanceOtherSpaceShip, asteroidAndCheckPointMask);
            if (hit) {
                if (hit.transform.CompareTag("WayPoint")) {
                    BlackBoard2.Gino.scores[BlackBoard2.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 0;
                }
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 1;
            }
        }
        void SCORE_HIGHER_UPDATER(SpaceShipView spaceship, GameData data) {
            if (spaceship.Score < data.GetSpaceShipForOwner(1 - spaceship.Owner).Score) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.SCORE_HIGHER] = -1;
            } else if (spaceship.Score == data.GetSpaceShipForOwner(1 - spaceship.Owner).Score) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.SCORE_HIGHER] = 0;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.SCORE_HIGHER] = 1;
            }
        }
        void ENNEMY_HIDE_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            Vector2 toOtherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner).Position - spaceship.Position;
            hit = Physics2D.Raycast(spaceship.Position, toOtherSpaceShip, toOtherSpaceShip.magnitude, asteroidMask);
            if (hit) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_HIDE] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ENNEMY_HIDE] = 0;
            }
        }

        void TIME_LEFT_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.TIME_LEFT] = data.timeLeft;
        }

        void MINE_NEAR_UPDATER(SpaceShipView spaceship, GameData data) {
            Collider2D[] hit;
            hit = Physics2D.OverlapCircleAll(spaceship.Position, BlackBoard2.Gino.radiusShockwave, MineMask);
            if (hit.Length > 0) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.MINE_NEAR] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.MINE_NEAR] = 0;
            }
        }


        void ON_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {
            Collider2D hit;
            hit = Physics2D.OverlapPoint(spaceship.Position, CheckPointMask);
            if (hit) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ON_CHECKPOINT] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.ON_CHECKPOINT] = 0;
            }
        }

        void MINE_FRONT_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            float spaceshipAngle = (spaceship.Orientation + 360) % 360;
            float x = Mathf.Cos(spaceshipAngle * Mathf.Deg2Rad);
            float y = Mathf.Sin(spaceshipAngle * Mathf.Deg2Rad);
            Vector2 front = new Vector2(x, y);

            hit = Physics2D.Raycast(spaceship.Position, front, MineMask);
            if (hit) {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.MINE_FRONT] = 1;
            } else {
                BlackBoard2.Gino.scores[BlackBoard2.ScoreType.MINE_FRONT] = 0;
            }
        }

        void NEAR_CHECKPOINT_ENEMY_UPDATER(SpaceShipView spaceship, GameData data) {
            float dist = Mathf.Infinity;
            for (int i = 0; i < data.WayPoints.Count; i++) {
                float dist2 = Vector2.Distance(data.WayPoints[i].Position, spaceship.Position);
                if (dist2 < dist && data.WayPoints[i].Owner == data.GetSpaceShipForOwner(1 - spaceship.Owner).Owner) {
                    dist = dist2;
                }
            }
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.NEAR_CHECKPOINT_ENEMY] = dist;
        }
        void NEAR_CHECKPOINT_NEUTRAL_UPDATER(SpaceShipView spaceship, GameData data) {
            float dist = Mathf.Infinity;
            for (int i = 0; i < data.WayPoints.Count; i++) {
                float dist2 = Vector2.Distance(data.WayPoints[i].Position, spaceship.Position);
                if (dist2 < dist && data.WayPoints[i].Owner == data.GetSpaceShipForOwner(1 - spaceship.Owner).Owner) {
                    dist = dist2;
                }
            }
            BlackBoard2.Gino.scores[BlackBoard2.ScoreType.NEAR_CHECKPOINT_NEUTRAL] = dist;
        }


        #endregion
    }
}
