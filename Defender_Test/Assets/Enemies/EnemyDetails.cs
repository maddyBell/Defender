using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyDetails", menuName = "EnemyDetails")]
public class EnemyDetails : ScriptableObject
{
    //setting up the enemy template so i can implement more enemy types easier 

    public string enemyName;
    public int health;
    public int towerDamage;
    public int defenderDamage;
    public float attackSpeed;
    public float movementSpeed;
    public GameObject enemyPrefab;

}
