using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics.Drawables;

namespace CameraApp
{
    class CustomImageView : ImageView
    {
        private Bitmap bitmap;
        private Color color = Color.Black;

        public List<DrawingAction> drawingActions = new List<DrawingAction>();

        public CustomImageView(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs)
        {
            SetWillNotDraw(false);

            this.BuildDrawingCache(true);
            bitmap = this.GetDrawingCache(true);
        }

        protected override void OnDraw(Canvas canvas)
        {
            this.BuildDrawingCache(true);
            bitmap = this.GetDrawingCache(true);

            foreach (DrawingAction da in drawingActions)
            {
                if (da.Command == DrawingActionCommands.Line)
                {
                    Paint p = new Paint(PaintFlags.AntiAlias);
                    p.Color = da.color;
                    p.StrokeWidth = da.width;
                    canvas.DrawLine(da.startX, da.startY, da.endX, da.endY, p);
                }

                if (da.Command == DrawingActionCommands.Circle)
                {
                    Paint p = new Paint(PaintFlags.AntiAlias);
                    p.Color = da.color;
                    p.StrokeWidth = da.width;
                    canvas.DrawCircle(da.startX, da.startY, da.radius, p);
                }

                if (da.Command == DrawingActionCommands.Text)
                {
                    Rect bounds = new Rect();
                    Paint p = new Paint(PaintFlags.AntiAlias);
                    float scale = Resources.DisplayMetrics.Density;

                    p.Color = da.color;
                    p.SetStyle(Paint.Style.FillAndStroke);
                    p.StrokeWidth = da.width;
                    p.TextSize = da.size * scale;
                    canvas.DrawText(da.text, da.startX, da.startY, p);
                }

            }

            canvas.DrawBitmap(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height), new Rect(0, 0, bitmap.Width, bitmap.Height), null);
            base.OnDraw(canvas);

        }

        public void Clear()
        {
            drawingActions.Clear();
            this.Invalidate();
        }

        public void DrawLine(int startX, int startY, int endX, int endY, Color color, int width)
        {
            DrawingAction da = new DrawingAction();
            da.Command = DrawingActionCommands.Line;
            da.color = color;
            da.startX = startX;
            da.startY = startY;
            da.endX = endX;
            da.endY = endY;
            da.width = width;
            
            drawingActions.Add(da);

            this.Invalidate();
        }

        public void DrawCircle(int cx, int cy, int radius, Color color, int width)
        {
            DrawingAction da = new DrawingAction();
            da.Command = DrawingActionCommands.Circle;
            da.color = color;
            da.startX = cx;
            da.startY = cy;
            da.radius = radius;
            da.width = width;

            drawingActions.Add(da);

            this.Invalidate();
        }

        public void DrawText(string text, int x, int y, int size, Color color)
        {
            DrawingAction da = new DrawingAction();
            da.Command = DrawingActionCommands.Text;
            da.color = color;
            da.startX = x;
            da.startY = y;
            da.text = text;
            da.size = size;

            drawingActions.Add(da);

            this.Invalidate();
        }


    }
}