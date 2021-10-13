using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriedFly {
    [CreateAssetMenu(fileName = "newAction", menuName = "IAUtility/Action")]
    public class IAAction : ScriptableObject {
        public List<IAScorer> iAScorers = new List<IAScorer>();
        public int finalPriority;
        public enum FunctionToTrigger {
            SHOOT,
            MOVE_TO_NEAR_CHECKPOINT
        }
        public List<FunctionToTrigger> enumFunctions = new List<FunctionToTrigger>();
        public delegate void FunctionEater();
        
        Dictionary<FunctionToTrigger, FunctionEater> dictionnaryFunctions = new Dictionary<FunctionToTrigger, FunctionEater>() {
        };
        public FunctionEater theFunctionEater;
        public float Priority() {
            float scorersTotal = 0;
            for (int i = 0; i < iAScorers.Count; i++) {
                scorersTotal = iAScorers[i].Compute();
            }
            return scorersTotal;
        }
        public void LoadTheFunctionEater() {
            dictionnaryFunctions.Add(FunctionToTrigger.SHOOT, Shoot);
            dictionnaryFunctions.Add(FunctionToTrigger.MOVE_TO_NEAR_CHECKPOINT, MoveToNearCheckPoint);
            for (int i = 0; i < enumFunctions.Count; i++) {
                theFunctionEater += dictionnaryFunctions[enumFunctions[i]];
            }
        }

        void Shoot() {
            Debug.Log("Shoot");
        } 
        void MoveToNearCheckPoint() {
            Debug.Log("MoveToNearCheckPoint");
        }
    }
}