using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    public class BlackBoard : MonoBehaviour {
        public static BlackBoard Gino;
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
            ENNEMY_NEAR,
            ON_CHEKPOINT,
            TIME_LEFT,
            MINE_FRONT,
            MINE_NEAR
        }

        public List<ScoreType> types = new List<ScoreType>();
        public List<float> variables = new List<float>();
        public bool mustRefresh = false;
        public Dictionary<ScoreType, float> scores = new Dictionary<ScoreType, float>();
        public float near;
        private void Awake() {
            Gino = this;
            FillScores();
        }

        void FillScores() {
            for (int i = 0; i < variables.Count; i++) {
                scores.Add(types[i], variables[i]);
            }
        } 
        void ChangeScores() {
            for (int i = 0; i < variables.Count; i++) {
                scores[types[i]] =  variables[i];
            }
        }

        private void Update() {
            if (mustRefresh) {
                mustRefresh = false;
                ChangeScores();
            }
        }
    }
}
