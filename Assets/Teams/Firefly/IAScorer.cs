using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    [System.Serializable]
    public class IAScorer {
        public AnimationCurve animationCurve;
        public BlackBoard.ScoreType scorer;
        public Vector2 valueBounds;
        public Vector2 scoreBounds;
        public float Compute() {
            float vall = BlackBoard.Gino.scores[scorer];
            float normalizedVal = Mathf.InverseLerp(valueBounds.x, valueBounds.y, vall);
            float normalizedScore = animationCurve.Evaluate(normalizedVal);
            return Mathf.Lerp(scoreBounds.x, scoreBounds.y, normalizedScore);
        }
    }
}
