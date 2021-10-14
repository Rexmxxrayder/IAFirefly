using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;

namespace FriedFly {
    public delegate void ActionDelegate(SpaceShipView spaceship, GameData data);

    [CreateAssetMenu(fileName = "newAction", menuName = "IAUtility/Action")]
    public class IAAction : ScriptableObject {
        //public UnityEvent onAction;
        public List<FireflyController.ActionInvokable> onAction;
        public List<IAScorer> iAScorers = new List<IAScorer>();
        public int finalPriority;

        public float Priority() {
            float scorersTotal = 0;
            for (int i = 0; i < iAScorers.Count; i++) {
                scorersTotal = iAScorers[i].Compute();
            }
            return scorersTotal;
        }
    }
}