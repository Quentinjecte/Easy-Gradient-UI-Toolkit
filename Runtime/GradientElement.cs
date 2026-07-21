using UnityEngine;
using UnityEngine.UIElements;

namespace EasyGradient.UIToolkit
{
    /// <summary>
    /// VisualElement displaying a GradientStyle as its background, via a mesh generated
    /// procedurally by GradientPainter. Usable by code (<see cref="GradientStyle"/>)
    /// or via UXML (&lt;eg:GradientElement gradient-style="..."/&gt;).
    ///
    /// Repaints itself automatically when its size changes (GeometryChangedEvent) or when
    /// GradientStyle is reassigned.
    /// </summary>
    [UxmlElement]
    public partial class GradientElement : VisualElement
    {
        private string _gradientStylePath;
        private string _gradientStyleHoverPath;
        //private string _gradientStylePressPath;

        private GradientStyle _gradientStyle;
        private GradientStyle _hoverGradientStyle;
        //private GradientStyle _pressedGradientStyle;

        private bool _isHovered;
        //private bool _isPressed;

        // UXML attribute: gradient-style="UI/Gradients/haloGreen"
        [UxmlAttribute("gradient-style")]
        public string GradientStylePath
        {
            get => _gradientStylePath;
            set
            {
                _gradientStylePath = value;
                GradientStyle = LoadFromResources(value, "gradient-style");
            }
        }

        // Optional UXML attribute: gradient-style-hover="UI/Gradients/buttonHover".
        // Replaces the earlier attempt ".lc-button-wrapper:hover { --gradient-style: ...; }"
        // in USS, which could not work: a USS custom property cannot drive a C# object
        // reference (GradientStyle) — it is silently ignored, with no error. Hover is
        // therefore handled here in C#, via PointerEnter/LeaveEvent.
        [UxmlAttribute("gradient-style-hover")]
        public string GradientStyleHoverPath
        {
            get => _gradientStyleHoverPath;
            set
            {
                _gradientStyleHoverPath = value;
                HoverGradientStyle = LoadFromResources(value, "gradient-style-hover");
            }
        }

        // Optional UXML attribute: gradient-style-hover="UI/Gradients/buttonHover".
        // Replaces the earlier attempt ".lc-button-wrapper:hover { --gradient-style: ...; }"
        // in USS, which could not work: a USS custom property cannot drive a C# object
        // reference (GradientStyle) — it is silently ignored, with no error. Hover is
        // therefore handled here in C#, via PointerEnter/LeaveEvent.
        /*[UxmlAttribute("gradient-style-click")]
        public string GradientStylePressPath
        {
            get => _gradientStylePressPath;
            set
            {
                _gradientStylePressPath = value;
                PressGradientStyle = LoadFromResources(value, "gradient-style-click");
            }
        }*/

        /// <summary>Base gradient, shown outside of hover (or permanently if no HoverGradientStyle is set).</summary>
        public GradientStyle GradientStyle
        {
            get => _gradientStyle;
            set
            {
                _gradientStyle = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>Gradient shown while hovered (optional). Null = no hover effect.</summary>
        public GradientStyle HoverGradientStyle
        {
            get => _hoverGradientStyle;
            set
            {
                _hoverGradientStyle = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>Gradient shown while hovered (optional). Null = no hover effect.</summary>
        /*public GradientStyle PressGradientStyle
        {
            get => _pressedGradientStyle;
            set
            {
                _pressedGradientStyle = value;
                MarkDirtyRepaint();
            }
        }*/

        public GradientElement()
        {
            AddToClassList(UssClassName);
            style.overflow = Overflow.Hidden;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<PointerEnterEvent>(_ => { _isHovered = true; MarkDirtyRepaint(); });
            RegisterCallback<PointerLeaveEvent>(_ => { _isHovered = false; MarkDirtyRepaint(); });
            //RegisterCallback<PointerDownEvent>(_ => { _isPressed = false; MarkDirtyRepaint(); });
        }

        private GradientStyle LoadFromResources(string resourcePath, string attributeName)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            var loaded = Resources.Load<GradientStyle>(resourcePath);
            if (loaded == null)
                Debug.LogWarning($"[GradientElement] No GradientStyle found under Resources/{resourcePath} (UXML attribute {attributeName}).");
            return loaded;
        }

        public const string UssClassName = "gradient-element";

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            GradientStyle active = _gradientStyle;

            active = true switch
            {
                //_ when _isPressed && _pressedGradientStyle != null => _pressedGradientStyle,
                _ when _isHovered && _hoverGradientStyle != null => _hoverGradientStyle,
                _ => _gradientStyle
            };

            if (active == null)
                return;

            GradientPainter.Paint(mgc, paddingRect, active);
        }
    }
}
