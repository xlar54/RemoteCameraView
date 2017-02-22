using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace CameraApp
{
    public enum DrawingActionCommands
    {
        Line,
        Circle,
        Text
    }

    class DrawingAction
    {
        public DrawingActionCommands Command;
        public int startX;
        public int startY;
        public int endX;
        public int endY;
        public int width;
        public int radius;
        public string text;
        public int size;
        public Android.Graphics.Color color;
    }
}