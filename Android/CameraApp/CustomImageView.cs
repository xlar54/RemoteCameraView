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
        private int startX, startY, endX, endY, radius, width;
        private string text = "";
        bool doDrawLine = false;
        bool doDrawCircle = false;
        bool doDrawText = false;

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

            if (doDrawLine)
            {
                Paint p = new Paint(PaintFlags.AntiAlias);
                p.Color = color;
                p.StrokeWidth = width;
                canvas.DrawLine(startX, startY, endX, endY, p);
                doDrawLine = false;
            }

            if (doDrawCircle)
            {
                Paint p = new Paint(PaintFlags.AntiAlias);
                p.Color = color;
                p.StrokeWidth = width;
                canvas.DrawCircle(startX, startY, radius, p);
                doDrawLine = false;
            }

            if (doDrawText)
            {
                Paint p = new Paint(PaintFlags.AntiAlias);
                p.Color = color;
                p.StrokeWidth = width;
                canvas.DrawText(text, startX, startY, p);
                doDrawText = false;
            }


            canvas.DrawBitmap(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height), new Rect(0, 0, bitmap.Width, bitmap.Height), null);

            base.OnDraw(canvas);
        }

        public void DrawLine(int startX, int startY, int endX, int endY, Color color, int width)
        {
            this.color = color;
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.width = width;
            this.doDrawLine = true;

            this.Invalidate();
        }

        public void DrawCircle(int cx, int cy, int radius, Color color, int width)
        {
            this.color = color;
            this.startX = cx;
            this.startY = cy;
            this.radius = radius;
            this.width = width;
            this.doDrawCircle = true;

            this.Invalidate();
        }

        public void DrawText(string text, int x, int y, Color color, int width)
        {
            this.color = color;
            this.startX = x;
            this.startY = y;
            this.text = text;
            this.width = width;
            this.doDrawText = true;

            this.Invalidate();
        }


    }
}