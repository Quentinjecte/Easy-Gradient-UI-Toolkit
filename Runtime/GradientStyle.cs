using System;
using UnityEngine;

namespace EasyGradient.UIToolkit
{
    /// <summary>Gradient projection mode (see GradientPainter).</summary>
    public enum GradientMode
    {
        Linear,
        Radial,
    }

    /// <summary>
    /// Data for a custom gradient (multi-stop color steps, angle, mode).
    /// Consumed by GradientElement / GradientPainter for procedural rendering in UI
    /// Toolkit (panels, headers, menus, HUD...).
    ///
    /// Reusable anywhere in the game: create an asset via
    /// Assets > Create > Jurassic Containment > UI Toolkit > Gradient Style, then
    /// assign it to a GradientElement (by code or via the UXML attribute "gradient-style").
    /// </summary>
    [CreateAssetMenu(fileName = "GradientStyle", menuName = "UI Toolkit/Gradient Style")]
    public class GradientStyle : ScriptableObject
    {
        /// <summary>
        /// A single color stop of the gradient. Grouped into one struct (rather than two
        /// separate color/position lists) to avoid any index misalignment — same principle
        /// as UnityEngine.Gradient's GradientColorKey.
        /// </summary>
        [Serializable]
        public struct ColorStop
        {
            public Color color;
            [Range(0f, 1f)] public float position;
        }

        [Tooltip("Color stops of the gradient. Sorted by position when evaluated, input order does not matter.")]
        public ColorStop[] stops =
        {
            new ColorStop { color = Color.black, position = 0f },
            new ColorStop { color = Color.white, position = 1f },
        };

        [Tooltip("Angle of the Linear gradient, in degrees (0 = left→right, clockwise). Ignored in Radial mode.")]
        [Range(0f, 360f)]
        public float angle;

        [Tooltip("Linear: bands along an axis. Radial: concentric rings from the element's center.")]
        public GradientMode mode = GradientMode.Linear;

        /// <summary>Copy of the stops sorted by ascending position (does not modify <see cref="stops"/>).</summary>
        public ColorStop[] GetSortedStops()
        {
            var sorted = (ColorStop[])stops.Clone();
            Array.Sort(sorted, (a, b) => a.position.CompareTo(b.position));
            return sorted;
        }

        /// <summary>Evaluates the gradient color at position t (0-1), linearly interpolating between adjacent stops.</summary>
        public Color Evaluate(float t)
        {
            var sorted = GetSortedStops();
            if (sorted.Length == 0) return Color.magenta; // visual error signal (no stop configured)
            if (sorted.Length == 1) return sorted[0].color;

            t = Mathf.Clamp01(t);
            if (t <= sorted[0].position) return sorted[0].color;
            if (t >= sorted[sorted.Length - 1].position) return sorted[sorted.Length - 1].color;

            for (int i = 0; i < sorted.Length - 1; i++)
            {
                var a = sorted[i];
                var b = sorted[i + 1];
                if (t < a.position || t > b.position) continue;

                float span = b.position - a.position;
                float localT = span > 0f ? (t - a.position) / span : 0f;
                return Color.LerpUnclamped(a.color, b.color, localT);
            }

            return sorted[sorted.Length - 1].color;
        }
    }
}
