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
            //Debug.Log("//");
            for (int i = 0; i < iAScorers.Count; i++) {
                Debug.Log(iAScorers[i].scorer + " : " + iAScorers[i].Compute());
                //if (iAScorers[i].scorer == BlackBoard.ScoreType.MINE_FRONT) { Debug.Log(iAScorers[i].Compute()); }
                scorersTotal += iAScorers[i].Compute();
            }
            //Debug.Log("// - " + scorersTotal);
            return scorersTotal;
        }
    }
}