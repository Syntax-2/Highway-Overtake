using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCarUpgrades", menuName = "Car Game/Car Upgrades")]
public class CarUpgrades : ScriptableObject
{
    [Header("Car Identifier")]
    [Tooltip("This MUST match the Car Name in the corresponding CarData asset.")]
    public string carName;

    // This is a simple data structure for a single upgrade level
    [System.Serializable]
    public class UpgradeLevel
    {
        [Tooltip("Cost of this specific upgrade level.")]
        public int cost;
        [Tooltip("How much to increase the car's maxMotorTorque by.")]
        public float motorTorque_Increase;
        [Tooltip("How much to increase the car's maxSpeed by.")]
        public float maxSpeed_Increase;
        [Tooltip("How much to increase the car's accelerationRate by.")]
        public float accelerationRate_Increase;
        [Tooltip("How much to increase the car's maxBrakeTorque by.")]
        public float brakeTorque_Increase;
    }

    [Header("Upgrade Paths (Define levels here)")]
    [Tooltip("Each element represents the next available upgrade level for the Engine.")]
    public List<UpgradeLevel> engineUpgrades;

    [Tooltip("Each element represents the next available upgrade level for the Turbo.")]
    public List<UpgradeLevel> turboUpgrades;

    [Tooltip("Each element represents the next available upgrade level for the Brakes.")]
    public List<UpgradeLevel> brakeUpgrades;
}
