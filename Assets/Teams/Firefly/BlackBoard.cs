using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    public class BlackBoard : MonoBehaviour {
        public enum ScoreType {
            DISTANCE_TO_SHIP,
            DISTANCE_TO_NEAR_OPEN_CHECKPOINT,
            DISTANCE_TO_NEAR_ASTEROID,
            ENERGY,
            STUN,
            ENNEMY_STUN,
            ENNEMY_BEHIND_US,
            ENNEMY_IN_FRONT_OF_US,
            CHECKPOINT_BEHIND_ENNEMY,
            SCORE_HIGHER,
            ENNEMY_HIDE,
            ON_CHECKPOINT,
            TIME_LEFT,
            MINE_FRONT,
            MINE_NEAR,
            NEAR_CHECKPOINT_ENEMY,
            NEAR_CHECKPOINT_NEUTRAL,
            AIMING_ENEMY_TRAJECTORY,
            COUNTDOWN_MINE,
            COUNTDOWN_SHOOT,
            COUNTDOWN_SHOCKWAVE,
            IS_BULLET_BEHIND_US
        }

        public static BlackBoard Gino;

        public Dictionary<ScoreType, float> scores = new Dictionary<ScoreType, float>();

        public List<ScoreType> types = new List<ScoreType>();
        public List<float> variables = new List<float>();
        public float[] seeVariables;
        public bool mustRefresh = false;
        public float radiusShockwave;
        public float ennemyNear;
        public float radiusCheckPoint;
        public bool ManualMode;

        private void Awake() {
            Gino = this;
            seeVariables = new float[22];
            FillScores();
            SeeScores();
        }

        void FillScores() {
            for (int i = 0; i < seeVariables.Length; i++) {
                scores.Add(types[i], 0f);
            }
        }

        void ChangeScores() {
            for (int i = 0; i < variables.Count; i++) {
                scores[types[i]] = variables[i];
            }
        }

        void SeeScores() {
            for (int i = 0; i < scores.Count; i++) {
                seeVariables[i] = scores[types[i]];
            }
        }

        private void Update() {
            if (mustRefresh) {
                mustRefresh = false;
                ChangeScores();
            }
            SeeScores();
        }
    }
}
