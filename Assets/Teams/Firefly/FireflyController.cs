using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {
    public class FireflyController : BaseSpaceShipController {
        public enum ActionInvokable {
            RUSH_POINTS,
            FOLLOW_ENEMY,
            SHOOT,
            LAND_MINE,
            SHOCKWAVE
        }

        public delegate void MyDelegate(SpaceShipView spaceship, GameData data);

        public Dictionary<ActionInvokable, ActionDelegate> invokableDelegates;

        public List<IAAction> iaActions = new List<IAAction>();
        private InputData inputData;
        private MyDelegate ValueUpdater;

        // Variables updaters
        //private SpaceShipView spaceship;
        //private GameData data;
        [SerializeField] private bool isStun = false;
        [SerializeField] private bool isStunEnnemy = false;
        private float timerValue = 0;
        private float timerValueEnnemy = 0;
        public int hitCount;
        public int hitCountEnnemy;
        public LayerMask asteroidMask;
        public LayerMask CheckPointMask;
        public LayerMask asteroidAndCheckPointMask;
        public LayerMask MineMask;

        public override void Initialize(SpaceShipView spaceship, GameData data) {
            InitializeActionInvokable();
            InitializeValueUpdater();
            ValueUpdater(spaceship, data);
        }

        public override InputData UpdateInput(SpaceShipView spaceship, GameData data) {

            //SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);

            //result = RushPoints(spaceship, data, result);

            //bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
            //DebugSpaceShip(spaceship, nearestWayPoint.Position, targetOrient);
            ValueUpdater(spaceship, data);
            if (BlackBoard.Gino.ManualMode == true) {
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
            } else {
                inputData = new InputData(0f, spaceship.Orientation, false, false, Input.GetKeyDown(KeyCode.Space));
                //BestActionToInvoke().onAction.Invoke();
                //RushPoints();
                IAAction bestAction = BestActionToInvoke();
                for (int i = 0; i < bestAction.onAction.Count; i++) {
                    invokableDelegates[bestAction.onAction[i]](spaceship, data);
                    //BestActionToInvoke().onAction[i](spaceship, data);
                }
                //RushPoints(spaceship, data);
                //if (BlackBoard.Gino.scores[BlackBoard.ScoreType.AIMING_ENEMY_TRAJECTORY] == 1) {
                //    Shoot(spaceship, data);
                //}
                InputData result = inputData;
                return result;
            }
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
            //Debug.DrawLine(asteroid.Position, soluce, Color.red);
            return soluce;
        }

        #endregion

        #region Utilities

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
            int actionToDo = 0;
            float highestPriority = 0;
            float finalPriority = 0;
            for (int i = 0; i < iaActions.Count; i++) {
                float actionPriority = iaActions[i].Priority();
                float actionFinalPriority = iaActions[i].finalPriority;
                Debug.LogWarning("// Action Numero " + (i + 1) + " Score : " + actionPriority);
                if (highestPriority < actionPriority || highestPriority == actionPriority && finalPriority < actionFinalPriority) {
                    highestPriority = actionPriority;
                    finalPriority = actionFinalPriority;
                    actionToDo = i;
                }
            }
            Debug.LogWarning(" - ///-> Action done : " + (actionToDo + 1));
            return iaActions[actionToDo];
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

        #endregion

        #region ActionFunction

        public void Shoot(SpaceShipView spaceship, GameData data) {
            inputData.shoot = true;
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_SHOOT] = 0f;
        }

        public void LandMine(SpaceShipView spaceship, GameData data) {
            inputData.dropMine = true;
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_MINE] = 0f;
        }

        public void Shockwave(SpaceShipView spaceship, GameData data) {
            inputData.fireShockwave = true;
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_SHOCKWAVE] = 0f;
        }

        public void RushPoints(SpaceShipView spaceship, GameData data) {
            float targetOrient;
            WayPointView nearestWayPoint = GetClosestPoint(spaceship.Position + spaceship.Velocity / 2f, data.WayPoints, spaceship.Owner);
            if (nearestWayPoint == null) {
                //Debug.Log(data.timeLeft);
                //Debug.Break();
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
            //Debug.DrawLine(nearestWayPoint.Position, target, Color.blue);
            //Debug.DrawLine(nearestNextWayPoint.Position, nearestWayPoint.Position, Color.grey);
            //Debug.DrawLine(spaceship.Position, nearestWayPoint.Position, Color.grey);

            targetOrient = GoTo(target, spaceship, data);

            //DebugSpaceShip(spaceship, target, targetOrient);
            inputData.thrust = 1f;
            inputData.targetOrientation = targetOrient;

            if (spaceship.Energy < 1f &&
                spaceship.Velocity.magnitude >= spaceship.SpeedMax &&
                Mathf.Abs(Atan2(spaceship.Velocity) - targetOrient) < 5f) {
                inputData.thrust = 0f;
            }
        }

        public void FollowEnemy(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            inputData.thrust = 1f;
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

        private void InitializeActionInvokable() {
            invokableDelegates = new Dictionary<ActionInvokable, ActionDelegate>();
            invokableDelegates.Add(ActionInvokable.RUSH_POINTS, RushPoints);
            invokableDelegates.Add(ActionInvokable.FOLLOW_ENEMY, FollowEnemy);
            invokableDelegates.Add(ActionInvokable.LAND_MINE, LandMine);
            invokableDelegates.Add(ActionInvokable.SHOCKWAVE, Shockwave);
            invokableDelegates.Add(ActionInvokable.SHOOT, Shoot);
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
            ValueUpdater += COUNTDOWN_MINE_UPDATER; 
            ValueUpdater += COUNTDOWN_SHOOT_UPDATER;
            ValueUpdater += COUNTDOWN_SHOCKWAVE_UPDATER;
            ValueUpdater += AIMING_ENEMY_TRAJECTORY_UPDATER;
            ValueUpdater += IS_BULLET_BEHIND_US_UPDATER;
        }

        void DISTANCE_TO_SHIP_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_SHIP] = Vector2.Distance(spaceship.Position, data.GetSpaceShipForOwner(1 - spaceship.Owner).Position);
        }

        void DISTANCE_TO_NEAR_OPEN_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {
            WayPointView point = GetClosestPoint(spaceship.Position + spaceship.Velocity / 2f, data.WayPoints, spaceship.Owner);
            if (point == null) { BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_NEAR_OPEN_CHECKPOINT] = Mathf.Infinity; return; }
            float distance = Vector2.Distance(point.Position, spaceship.Position);
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_NEAR_OPEN_CHECKPOINT] = distance;
        }

        void DISTANCE_TO_NEAR_ASTEROID_UPDATER(SpaceShipView spaceship, GameData data) {
            AsteroidView asteroid = GetClosestAsteroid(spaceship, data.Asteroids);
            float distance = Vector2.Distance(asteroid.Position, spaceship.Position);
            distance -= Mathf.Abs(asteroid.Radius);
            BlackBoard.Gino.scores[BlackBoard.ScoreType.DISTANCE_TO_NEAR_ASTEROID] = distance;
        }

        void ENERGY_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.ENERGY] = spaceship.Energy;
        }

        void ENNEMY_STUN_UPDATER(SpaceShipView spaceship, GameData data) {
            if (hitCountEnnemy != data.GetSpaceShipForOwner(1 - spaceship.Owner).HitCount) {
                isStun = true;
                timerValue = data.GetSpaceShipForOwner(1 - spaceship.Owner).HitCountdown;
                hitCountEnnemy = data.GetSpaceShipForOwner(1 - spaceship.Owner).HitCount;
            }

            if (isStun) {
                timerValue -= Time.deltaTime;
                if (timerValue <= 0) {
                    isStun = false;
                }
            }

            if (isStun) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_STUN] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_STUN] = 0;
            }
        }

        void STUN_UPDATER(SpaceShipView spaceship, GameData data) {
            if (hitCount != spaceship.HitCount) {
                isStunEnnemy = true;
                timerValueEnnemy = spaceship.HitCountdown;
                hitCount = spaceship.HitCount;
            }

            if (isStunEnnemy) {
                timerValueEnnemy -= Time.deltaTime;
                if (timerValueEnnemy <= 0) {
                    isStunEnnemy = false;
                }
            }

            if (isStunEnnemy) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.STUN] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.STUN] = 0;
            }
        }

        void ENNEMY_BEHIND_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = (spaceship.Orientation + 180f) % 360;
            float youEnemyAngle = Atan2(otherSpaceShip.Position - spaceship.Position) % 360;
            float deltaAngle = Mathf.Abs((orientAngle - youEnemyAngle) % 360);
            if (deltaAngle > 180f) { deltaAngle -= 360; deltaAngle = Mathf.Abs(deltaAngle); }
            if (deltaAngle < 45f) { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_BEHIND_US] = 1; }
            else { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_BEHIND_US] = 0; }
        }

        void ENNEMY_IN_FRONT_OF_US_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            float orientAngle = spaceship.Orientation % 360f;
            float youEnemyAngle = Atan2(otherSpaceShip.Position - spaceship.Position) % 360f;
            float deltaAngle = Mathf.Abs((orientAngle - youEnemyAngle) % 360);
            if (deltaAngle > 180f) { deltaAngle -= 360; deltaAngle = Mathf.Abs(deltaAngle); }
            if (deltaAngle < 45f) { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_IN_FRONT_OF_US] = 1; } else { BlackBoard.Gino.scores[BlackBoard.ScoreType.ENNEMY_IN_FRONT_OF_US] = 0; }
        }

        void CHECKPOINT_BEHIND_ENNEMY_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            SpaceShipView otherSpaceShip = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            Vector2 distanceOtherSpaceShip = otherSpaceShip.Position - spaceship.Position;
            hit = Physics2D.Raycast(otherSpaceShip.Position, distanceOtherSpaceShip, Mathf.Infinity, asteroidAndCheckPointMask);
            if (hit) {
                if (hit.transform.CompareTag("WayPoint")) {
                    if (hit.transform.GetComponent<WayPoint>().Owner != otherSpaceShip.Owner) {
                        BlackBoard.Gino.scores[BlackBoard.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 1;
                        return;
                    }
                }
                BlackBoard.Gino.scores[BlackBoard.ScoreType.CHECKPOINT_BEHIND_ENNEMY] = 0;
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
            hit = Physics2D.OverlapCircleAll(spaceship.Position, BlackBoard.Gino.radiusShockwave / 2f, MineMask);
            if (hit.Length > 0) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_NEAR] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_NEAR] = 0;
            }
        }


        void ON_CHECKPOINT_UPDATER(SpaceShipView spaceship, GameData data) {
            Collider2D hit;
            hit = Physics2D.OverlapPoint(spaceship.Position, CheckPointMask);
            if (hit) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ON_CHECKPOINT] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.ON_CHECKPOINT] = 0;
            }
        }

        void MINE_FRONT_UPDATER(SpaceShipView spaceship, GameData data) {
            RaycastHit2D hit;
            float spaceshipAngle = (spaceship.Orientation + 360) % 360;
            float x = Mathf.Cos(spaceshipAngle * Mathf.Deg2Rad);
            float y = Mathf.Sin(spaceshipAngle * Mathf.Deg2Rad);
            Vector2 front = new Vector2(x, y);

            hit = Physics2D.Raycast(spaceship.Position, front, Mathf.Infinity, MineMask);
            if (hit) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_FRONT] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.MINE_FRONT] = 0;
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
            BlackBoard.Gino.scores[BlackBoard.ScoreType.NEAR_CHECKPOINT_ENEMY] = dist;
        }

        void NEAR_CHECKPOINT_NEUTRAL_UPDATER(SpaceShipView spaceship, GameData data) {
            float dist = Mathf.Infinity;
            for (int i = 0; i < data.WayPoints.Count; i++) {
                float dist2 = Vector2.Distance(data.WayPoints[i].Position, spaceship.Position);
                if (dist2 < dist && data.WayPoints[i].Owner == -1) {
                    dist = dist2;
                }
            }
            BlackBoard.Gino.scores[BlackBoard.ScoreType.NEAR_CHECKPOINT_NEUTRAL] = dist;
        }

        void AIMING_ENEMY_TRAJECTORY_UPDATER(SpaceShipView spaceship, GameData data) {
            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
            if (AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f)) {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.AIMING_ENEMY_TRAJECTORY] = 1;
            } else {
                BlackBoard.Gino.scores[BlackBoard.ScoreType.AIMING_ENEMY_TRAJECTORY] = 0;
            }
        }

        void COUNTDOWN_MINE_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_MINE] += Time.deltaTime;
        }

        void COUNTDOWN_SHOOT_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_SHOOT] += Time.deltaTime;
        }

        void COUNTDOWN_SHOCKWAVE_UPDATER(SpaceShipView spaceship, GameData data) {
            BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_SHOCKWAVE] += Time.deltaTime;
        }

        void IS_BULLET_BEHIND_US_UPDATER(SpaceShipView spaceship, GameData data) {
            //BlackBoard.Gino.scores[BlackBoard.ScoreType.COUNTDOWN_SHOCKWAVE] += Time.deltaTime;
            List<BulletView> bullets = data.Bullets;
            for (int i = 0; i < bullets.Count; i++) {
                float distance = Vector2.Distance(bullets[i].Position, spaceship.Position);
                if (distance < 1f) {
                    float angleToShip = Atan2(spaceship.Position - bullets[i].Position) % 360;
                    float directionAngle = Atan2(bullets[i].Velocity) % 360;
                    float deltaAngle = Mathf.Abs((angleToShip - directionAngle) % 360);
                    if (deltaAngle < 10f) {
                        BlackBoard.Gino.scores[BlackBoard.ScoreType.IS_BULLET_BEHIND_US] = 1f;
                        return;
                    }
                }
            }
            BlackBoard.Gino.scores[BlackBoard.ScoreType.IS_BULLET_BEHIND_US] = 0f;
        }

        #endregion
    }
}
