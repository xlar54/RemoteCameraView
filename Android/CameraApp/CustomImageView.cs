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
        private Bitmap arrow;

        public int mouseX = -1;
        public int mouseY = -1;
        public List<DrawingAction> drawingActions = new List<DrawingAction>();

        public CustomImageView(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs)
        {
            arrow = BitmapFactory.DecodeResource(Resources, Resource.Drawable.arrow);

            SetWillNotDraw(false);

            this.BuildDrawingCache(true);
            bitmap = this.GetDrawingCache(true);
        }

        protected override void OnDraw(Canvas canvas)
        {
            this.BuildDrawingCache(true);
            bitmap = this.GetDrawingCache(true);

            int lastX = 0;
            int lastY = 0;

            Paint p = new Paint(PaintFlags.AntiAlias);

            foreach (DrawingAction da in drawingActions)
            {
                if (da.Command == DrawingActionCommands.Line)
                {
                    p.Color = da.color;
                    p.StrokeWidth = da.width;
                    canvas.DrawLine(da.startX, da.startY, da.endX, da.endY, p);

                    lastX = da.endX;
                    lastY = da.endY;
                }

                if (da.Command == DrawingActionCommands.Circle)
                {
                    p.Color = da.color;
                    p.StrokeWidth = da.width;
                    canvas.DrawCircle(da.startX, da.startY, da.radius, p);
                }

                if (da.Command == DrawingActionCommands.Text)
                {
                    Rect bounds = new Rect();
                    float scale = Resources.DisplayMetrics.Density;

                    p.Color = da.color;
                    p.SetStyle(Paint.Style.FillAndStroke);
                    p.StrokeWidth = da.width;
                    p.TextSize = da.size * scale;
                    canvas.DrawText(da.text, da.startX, da.startY, p);
                }

            }

            if(mouseX != -1 && mouseY != -1)
                canvas.DrawBitmap(arrow, mouseX, mouseY, p);

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