using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    [CreateAssetMenu(fileName = "newAction", menuName = "IAUtility/Action")]
    public class IAAction : ScriptableObject {
        public List<IAScorer> iAScorers = new List<IAScorer>();

        void Start() {

        }

        void Update() {

        }
    }
}