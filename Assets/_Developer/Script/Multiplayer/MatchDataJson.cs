using UnityEngine;

/// <summary>
/// Helper class for creating JSON strings for match state data.
/// Similar to FishGame's MatchDataJson pattern.
/// </summary>
public static class MatchDataJson
{
    /// <summary>
    /// Creates JSON string for position and rotation data.
    /// </summary>
    public static string PositionAndRotation(Vector3 position, float rotationZ, float autoRotationAngle)
    {
        return $"{{\"position.x\":{position.x},\"position.y\":{position.y},\"position.z\":{position.z},\"rotationZ\":{rotationZ},\"autoRotationAngle\":{autoRotationAngle}}}";
    }

    /// <summary>
    /// Creates JSON string for input data.
    /// </summary>
    public static string Input(bool isCharging, float currentForce, int fillDirection)
    {
        return $"{{\"isCharging\":{isCharging.ToString().ToLower()},\"currentForce\":{currentForce},\"fillDirection\":{fillDirection}}}";
    }

    /// <summary>
    /// Creates JSON string for hit target data.
    /// </summary>
    public static string HitTarget(bool isPlayerArrow)
    {
        return $"{{\"isPlayerArrow\":{isPlayerArrow.ToString().ToLower()}}}";
    }

    /// <summary>
    /// Creates JSON string for wind data.
    /// </summary>
    public static string Wind(Vector2 windForce, Vector2 windDirection, float endTime)
    {
        return $"{{\"windForce.x\":{windForce.x},\"windForce.y\":{windForce.y},\"windDirection.x\":{windDirection.x},\"windDirection.y\":{windDirection.y},\"endTime\":{endTime}}}";
    }

    /// <summary>
    /// Creates JSON string for power-up data.
    /// </summary>
    public static string PowerUp(int spawnPointIndex, int dataIndex)
    {
        return $"{{\"spawnPointIndex\":{spawnPointIndex},\"dataIndex\":{dataIndex}}}";
    }
}
