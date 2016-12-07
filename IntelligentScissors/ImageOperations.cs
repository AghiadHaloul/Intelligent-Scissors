using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
///Algorithms Project
///Intelligent Scissors
///

namespace IntelligentScissors
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
    /// Holds the edge energy between 
    ///     1. a pixel and its right one (X)
    ///     2. a pixel and its bottom one (Y)
    /// </summary>
    public struct Vector2D
    {
        public double X { get; set; }
        public double Y { get; set; }
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
                            Buffer[y, x].red = p[0];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[2];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

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
        /// Calculate edge energy between
        ///     1. the given pixel and its right one (X)
        ///     2. the given pixel and its bottom one (Y)
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns>edge energy with the right pixel (X) and with the bottom pixel (Y)</returns>
        public static Vector2D CalculatePixelEnergies(int x, int y, RGBPixel[,] ImageMatrix)
        {
            if (ImageMatrix == null) throw new Exception("image is not set!");

            Vector2D gradient = CalculateGradientAtPixel(x, y, ImageMatrix);

            double gradientMagnitude = Math.Sqrt(gradient.X * gradient.X + gradient.Y * gradient.Y);
            double edgeAngle = Math.Atan2(gradient.Y, gradient.X);
            double rotatedEdgeAngle = edgeAngle + Math.PI / 2.0;

            Vector2D energy = new Vector2D();
            energy.X = Math.Abs(gradientMagnitude * Math.Cos(rotatedEdgeAngle));
            energy.Y = Math.Abs(gradientMagnitude * Math.Sin(rotatedEdgeAngle));

            return energy;
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
                        p[0] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[2] = ImageMatrix[i, j].blue;
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


        #region Private Functions
        /// <summary>
        /// Calculate Gradient vector between the given pixel and its right and bottom ones
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns></returns>
        private static Vector2D CalculateGradientAtPixel(int x, int y, RGBPixel[,] ImageMatrix)
        {
            Vector2D gradient = new Vector2D();

            RGBPixel mainPixel = ImageMatrix[y, x];
            double pixelGrayVal = 0.21 * mainPixel.red + 0.72 * mainPixel.green + 0.07 * mainPixel.blue;

            if (y == GetHeight(ImageMatrix) - 1)
            {
                //boundary pixel.
                for (int i = 0; i < 3; i++)
                {
                    gradient.Y = 0;
                }
            }
            else
            {
                RGBPixel downPixel = ImageMatrix[y + 1, x];
                double downPixelGrayVal = 0.21 * downPixel.red + 0.72 * downPixel.green + 0.07 * downPixel.blue;

                gradient.Y = pixelGrayVal - downPixelGrayVal;
            }

            if (x == GetWidth(ImageMatrix) - 1)
            {
                //boundary pixel.
                gradient.X = 0;

            }
            else
            {
                RGBPixel rightPixel = ImageMatrix[y, x + 1];
                double rightPixelGrayVal = 0.21 * rightPixel.red + 0.72 * rightPixel.green + 0.07 * rightPixel.blue;

                gradient.X = pixelGrayVal - rightPixelGrayVal;
            }


            return gradient;
        }
        /// <summary>
        ///  Turns image into graph 
        /// </summary>
        /// <param name="Matrix"> The 2D array that contains pixels of image</param>
        /// <returns>Graph constructed from image</returns>
        public static List<List<List<Tuple<double, Tuple<int, int>>>>> ConstructGraph(RGBPixel[,] Matrix,int x,int y)
        {
            int height = GetHeight(Matrix), 
                width = GetWidth(Matrix);
            y = -y;
            x = -x;
            List<List<List<Tuple<double, Tuple<int, int>>>>> output;
            if ((height <= 520) || (width <= 570))
            {
                output = new List<List<List<Tuple<double, Tuple<int, int>>>>>(height);
                for (int i = 0; i < height; i++)
                {
                    output.Add(new List<List<Tuple<double, Tuple<int, int>>>>(width)); // reserves place for columns 
                    for (int j = 0; j < width; j++)
                        output[i].Add(new List<Tuple<double, Tuple<int, int>>>()); //makes each cell has an array within it 
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        Vector2D w = CalculatePixelEnergies(j, i, Matrix);//weight between this pixel and right one and weight between it and the below one. 
                        if ((j == width - 1) && (i == height - 1)) // this is the last cell 
                            continue;
                        else if (j == width - 1)
                        {
                            output[i][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(i + 1, j))); //connects with the one below it
                            output[i + 1][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(i, j)));
                        }
                        else if (i == height - 1)
                        {
                            output[i][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(i, j + 1)));//connects with the one on the right
                            output[i][j + 1].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(i, j)));
                        }
                        else
                        {
                            output[i][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(i, j + 1)));
                            output[i][j + 1].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(i, j)));
                            output[i][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(i + 1, j)));
                            output[i + 1][j].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(i, j)));
                        }
                    }
                }
            }
            else
            {
                output = new List<List<List<Tuple<double, Tuple<int, int>>>>>(520);
                for (int i = 0; i < 520; i++)
                {
                    output.Add(new List<List<Tuple<double, Tuple<int, int>>>>(570)); // reserves place for columns 
                    for (int j = 0; j < 570; j++)
                        output[i].Add(new List<Tuple<double, Tuple<int, int>>>()); //makes each cell has an array within it 
                }
                int m = 0;int n = 0;
                for (int i = y; i < y + 520; i++)
                {
                    n = 0;
                    for (int j = x; j < x + 570; j++)
                    {
                        Vector2D w = CalculatePixelEnergies(j, i, Matrix);//weight between this pixel and right one and weight between it and the below one. 
                        if ((i == y + 520 - 1) && (j == x + 570 - 1)) // this is the last cell 
                            continue;
                        else if (j == x + 570 - 1)
                        {
                            output[m][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(m + 1, n))); //connects with the one below it
                            output[m + 1][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(m, n)));
                        }
                        else if (i == y + 520 - 1)
                        {
                            output[m][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(m, n + 1)));//connects with the one on the right
                            output[m][n + 1].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(m, n)));
                        }
                        else
                        {
                            output[m][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(m, n + 1)));
                            output[m][n + 1].Add(new Tuple<double, Tuple<int, int>>(1 / (w.X + 0.0000001), new Tuple<int, int>(m, n)));
                            output[m][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(m + 1, n)));
                            output[m + 1][n].Add(new Tuple<double, Tuple<int, int>>(1 / (w.Y + 0.0000001), new Tuple<int, int>(m, n)));
                        }
                        n++;
                    }
                    m++;
                }
            }
            return output;

        }
        /// <summary>
        /// Gets shortest path between two points
        /// </summary>
        /// <param name="input">Graph created by ConstructGraph</param>
        /// <param name="x">X-coordinate of source anchor</param>
        /// <param name="y">Y-coordinate of source anchor</param>
        /// <param name="d1">X-coordinate of destination anchor</param>
        /// <param name="d2">Y-coordinate of destination anchor</param>
        /// <returns>Shortest path between source and destination</returns>
        public static List<Tuple <int,int>> Dijkstra(int x, int y, int d1, int d2)
        {
          
            List<List<double>> output = new List<List<double>>(MainForm.Graph.Count); 
            List<List<bool>> visited = new List<List<bool>>(MainForm.Graph.Count); 
            List<List<Tuple<int, int>>> FROm = new List<List<Tuple<int, int>>>(MainForm.Graph.Count); 
            // From contains a node as in [i][j] = [k][m], k and m are the source of i and j 
            for (int i = 0; i < MainForm.Graph.Count; i++)
            {
                output.Add(new List<double>(MainForm.Graph[0].Count));
                visited.Add(new List<bool>(MainForm.Graph[0].Count));
                FROm.Add(new List<Tuple<int, int>>(MainForm.Graph[0].Count));
                for (int j = 0; j < MainForm.Graph[0].Count; j++)
                {
                    output[i].Add(1 / (float)0);
                    visited[i].Add(new bool());
                    FROm[i].Add(new Tuple<int, int>(-1,-1));
                }
            }
            output[x][y] = 0;
            PriorityQueue<Tuple<double, Tuple<int, int>>> tmp = new PriorityQueue<Tuple<double, Tuple<int, int>>>();
            tmp.Enqueue(new Tuple<double, Tuple<int, int>>(0, new Tuple<int, int>(x, y)));
            while (tmp.Count > 0)
            {
                Tuple<double, Tuple<int, int>> num = tmp.Peek();
                tmp.Dequeue();
                int cx = num.Item2.Item1,
                    cy = num.Item2.Item2;
                if (visited[cx][cy] == true)
                    continue;
                else
                {
                    for (int i = 0; i < MainForm.Graph[cx][cy].Count; i++)
                    {
                        if (num.Item1 + MainForm.Graph[cx][cy][i].Item1 <= output[MainForm.Graph[cx][cy][i].Item2.Item1][MainForm.Graph[cx][cy][i].Item2.Item2])
                        {
                            output[MainForm.Graph[cx][cy][i].Item2.Item1][MainForm.Graph[cx][cy][i].Item2.Item2] = num.Item1 + MainForm.Graph[cx][cy][i].Item1; //distance from source + weight from current node till next < distance from source till next 3la tol not passing through current
                            tmp.Enqueue(new Tuple<double, Tuple<int, int>>(num.Item1 + MainForm.Graph[cx][cy][i].Item1, MainForm.Graph[cx][cy][i].Item2));
                            FROm[MainForm.Graph[cx][cy][i].Item2.Item1][MainForm.Graph[cx][cy][i].Item2.Item2] = num.Item2; // FROm [i] [j] = k,M. i,j is the coordinates of next node and k,m is the coordinates of current one. so i j came from k,m. 
                        }
                    }
                    visited[cx][cy] = true;
                }
                if (cx == d1 && cy == d2) break; // if the destination is the min we stop
            }
            //Path reconstruction
            List<Tuple<int, int>> path = new List<Tuple<int, int>>();
            Tuple<int, int> cur = new Tuple<int, int>(d1,d2);
            while (cur.Item1 != -1)
            {
                path.Add(cur);
                cur = FROm[cur.Item1][cur.Item2];// we are constructing the path backwards, the destination is at the top of the list and source at the end
            }
            path.Reverse();
            return path;
            // Output doesnt contain all right paths from source to end of graph since we stop when our distination is minimum
            //tmp is a replacement for priority queue 
        }
       
        #endregion
    }
}
