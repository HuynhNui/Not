using UnityEngine;

namespace _Project.Scripts.Utils
{
    /// <summary>
    /// Provides shared math helpers for lightweight gameplay calculations.
    /// </summary>
    public static class MathHelper
    {
        public static float GetSignedDirection(Vector2 value)
        {
            return Mathf.Sign(value.x);
        }
    }
}
