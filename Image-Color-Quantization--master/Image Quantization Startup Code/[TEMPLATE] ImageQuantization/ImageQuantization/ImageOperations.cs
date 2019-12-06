using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }
            MST(Buffer);
            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }

        public static Dictionary<RGBPixel, int> G;

        public static Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> constructGrapgh(RGBPixel[,] imageMatrix)
        {
            int height = GetHeight(imageMatrix), width = GetWidth(imageMatrix);
            Dictionary<RGBPixel, int> distinctColors = new Dictionary<RGBPixel, int>();
            Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> Graph = new Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double>();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {   
                    distinctColors[imageMatrix[i, j]] = 1;
                }
            }
            foreach (KeyValuePair<RGBPixel, int> i in distinctColors)
            {
                foreach (KeyValuePair<RGBPixel, int> j in distinctColors)
                {
                    int x = j.Key.red;
                    if (i.Key.red == j.Key.red && i.Key.blue == j.Key.blue && i.Key.green == j.Key.green)
                    {
                        continue;
                    }
                    KeyValuePair<RGBPixel, RGBPixel> currentNode = new KeyValuePair<RGBPixel, RGBPixel>(i.Key, j.Key);
                    Graph[currentNode] = (GetEgdeWeight(i.Key, j.Key));
                    
                }

            }
            return Graph;
        }

        public static double GetEgdeWeight(RGBPixel Color1, RGBPixel Color2)
        {
            return Math.Sqrt((Math.Pow(Color1.blue - Color2.blue, 2)) + (Math.Pow(Color1.red - Color2.red, 2)) + (Math.Pow(Color1.green - Color2.green, 2)));
        }

        public static Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> Sorttt(RGBPixel[,] imageMatrix)
        {
            Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> Graph = constructGrapgh(imageMatrix);

            Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> Graph2 = new Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double>();

            foreach (KeyValuePair<KeyValuePair<RGBPixel, RGBPixel>, double> item in Graph.OrderBy(key => key.Value))
            {
                Graph2[item.Key] = item.Value;
            }

            return Graph2;
        }
       
        public struct DisjointSets
        {
            
            Dictionary<RGBPixel, RGBPixel> parent;
            Dictionary<RGBPixel, int> rank;
            // Constructor. 
           public DisjointSets(Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> aa)
            {
                parent = new Dictionary<RGBPixel, RGBPixel>();
                rank = new Dictionary<RGBPixel, int>();
                foreach (var item in aa)
                {

                    KeyValuePair<RGBPixel, RGBPixel> p = item.Key;
                    parent[p.Key] = p.Key;
                    parent[p.Value] = p.Value;
                    rank[p.Value] = 0;
                    rank[p.Key] = 0;
                }
            }
            public RGBPixel find(RGBPixel r)
            {
                if (r.blue != parent[r].blue && r.red != parent[r].red && r.green != parent[r].green)
                    parent[r] = find(parent[r]);
                return parent[r];
            }

            public void merge(RGBPixel x, RGBPixel y)
            {
                x = find(x);
                y = find(y);

                if (rank[x] > rank[y])
                    parent[y] = x;
                else
                    parent[x] = y;

                if (rank[x] == rank[y])
                    rank[y]++;
            }
        };

        public static double MST(RGBPixel[,] imageMatrix)
        {

            Dictionary<KeyValuePair<RGBPixel, RGBPixel>, double> Graphh = Sorttt(imageMatrix);

            double W = 0;
            DisjointSets ds = new DisjointSets(Graphh);
            foreach (var item in Graphh)
            {
                KeyValuePair<RGBPixel, RGBPixel> p = item.Key;
                RGBPixel u = p.Value;
                RGBPixel v = p.Key;

                RGBPixel set_u = ds.find(u);
                RGBPixel set_v = ds.find(v);
                if (set_u.blue != set_v.blue && set_u.red != set_v.red && set_u.green != set_v.green)
                {
                    W += item.Value;
                    ds.merge(set_u, set_v);
                }
            }

            return W;

        }

    }

}