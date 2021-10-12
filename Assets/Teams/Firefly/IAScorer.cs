using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    [System.Serializable]
    public class IAScorer {
        public AnimationCurve animationCurve;
        public BlackBoard.ScoreType scorer;
        public float valueMax;
        public float valueMin;
        public float scoreMax;
        public float scoreMin;
        BlackBoard blackBoard;

        public float Compute() {
            float vall = blackBoard.scores[scorer];
            float normalizedVal = Mathf.InverseLerp(valueMin, valueMax, vall);
            return animationCurve.Evaluate(normalizedVal);
        }
    }
}
