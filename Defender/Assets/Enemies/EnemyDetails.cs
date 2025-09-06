using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyDetails", menuName = "EnemyDetails")]
public class EnemyDetails : ScriptableObject
{

    public string enemyName;
    public int health;
    public int towerDamage;
    public float attackSpeed;
    public float movementSpeed;
    public GameObject enemyPrefab;

}
