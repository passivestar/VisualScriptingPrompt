using UnityEngine;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class Util
    {
        public const float overlapThreshold = 100f;
        public const float distance = 10f;
        public const float horizontalGap = 40f;

        public static void PositionNewWidget(IGraphElementWidget widget, IGraphElementWidget selectedWidget, bool left)
        {
            var newPosX = left
                ? selectedWidget.position.xMin - widget.position.width - horizontalGap
                : selectedWidget.position.xMax + horizontalGap;

            widget.position = new Rect(
                new Vector2(newPosX, selectedWidget.position.yMin),
                widget.position.size
            );

            widget.BringToFront();
            widget.CachePositionFirstPass();
            widget.CachePosition();
            widget.Reposition();
        }

        public static void SpaceWidgetOut(IGraphElementWidget widget, ICanvas canvas)
        {
            if (widget.canDrag)
            {
                var timeout = 100;
                var timeoutIndex = 0;

                while (GraphGUI.PositionOverlaps(canvas, widget, overlapThreshold))
                {
                    widget.position = new Rect(
                        widget.position.position
                        + new Vector2(0, distance), widget.position.size
                    ).PixelPerfect();

                    widget.CachePositionFirstPass();
                    widget.CachePosition();
                    widget.Reposition();

                    if (++timeoutIndex > timeout)
                    {
                        break;
                    }
                }
            }
        }
    }
}