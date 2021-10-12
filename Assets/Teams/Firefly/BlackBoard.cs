using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackBoard : MonoBehaviour
{
    public enum ScoreType {
        DISTANCE_TO_SHIP,
        DISTANCE_TO_POINT,
        DISTANCE_TO_ASTEROID,
        ENERGY
    }

    public Dictionary<ScoreType, float> scores = new Dictionary<ScoreType, float>();

    void Start()
    {
        
    }
    void Update()
    {
        
    }
}
