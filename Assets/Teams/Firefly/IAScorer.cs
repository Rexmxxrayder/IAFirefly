using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    [System.Serializable]
    public class IAScorer {
        public BlackBoard.ScoreType scorer;
        public Vector2 valueBounds;
        public AnimationCurve animationCurve;
        public Vector2 scoreBounds;

        public float Compute() {
            float vall = BlackBoard.Gino.scores[scorer];
            float normalizedVal = Mathf.Clamp(Mathf.InverseLerp(valueBounds.x, valueBounds.y, vall), valueBounds.x, valueBounds.y);
            float normalizedScore = animationCurve.Evaluate(normalizedVal);
            return Mathf.Lerp(scoreBounds.x, scoreBounds.y, normalizedScore);
        }
    }
}
