using System.Drawing;

namespace GoveeMusicSync
{
    public class ImageHelper
    {

        private static readonly int blackFilter = 220;
        private static readonly int whiteFilter = 220;

        public static Color SmoothColor(Color oldCol, Color newCol, double time)
        {
            var vector = new Vector3(newCol.R - oldCol.R, newCol.G - oldCol.G, newCol.B - oldCol.B);
            var adjustedVector = new Vector3(vector.x * time, vector.y * time, vector.z * time);

            var SmoothedColorVector = new Vector3(oldCol.R + adjustedVector.x, oldCol.G + adjustedVector.y, oldCol.B + adjustedVector.z);

            var SmoothedColor = Color.FromArgb((int)SmoothedColorVector.x, (int)SmoothedColorVector.y, (int)SmoothedColorVector.z);
            return SmoothedColor;
        }


        public static Color GetDominantColor(Bitmap bmp)
        {
            //get initial cluster
            var random = new Random();
            _ = bmp.GetPixel(random.Next(0, bmp.Width), random.Next(0, bmp.Height));

            var n = bmp.Width * bmp.Height;

            double r = 0;
            double g = 0;
            double b = 0;

            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var color = bmp.GetPixel(x, y);
                    
                    if (GetEuclideanDist(color, Color.Black) >= blackFilter && GetEuclideanDist(color, Color.White) >= whiteFilter)
                    {
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    else
                    {
                        n--;
                    }
                     
                }
            }

            ////clamp values
            var red = (int)Math.Round(r / n);
            var green = (int)Math.Round(g / n);
            var blue = (int)Math.Round(b / n);

            red = Math.Min(255, Math.Max(0, red));
            green = Math.Min(255, Math.Max(0, green));
            blue = Math.Min(255, Math.Max(0, blue));


            var updatedCentre = Color.FromArgb(
                red,
                green,
                blue
                );
            return updatedCentre;
        }


        public static double GetEuclideanDist(Color c1, Color c2)
        {
            return Math.Sqrt(
                Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2)
                );
        }

        private struct Vector3
        {
            public double x, y, z;
            public Vector3(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
    }
}
