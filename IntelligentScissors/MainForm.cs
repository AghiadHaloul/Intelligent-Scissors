using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace IntelligentScissors
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;
        public static List<List<List<Tuple<double, Tuple<int, int>>>>> Graph;
        List<Tuple<int, int>> shortestpath;
        bool anchor;
        bool imageOpen;
        Point anc;
        Point tmpAnc;
        bool select;
        Pen pen;
        Pen pen2;
        Graphics g;
        int ancperpix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            anchor = false;
            imageOpen = true;
            select = false;
            ancperpix = 100 / (int)numericUpDown1.Value;
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    pen = new Pen(Color.Pink, 1);
                    break;
                case 1:
                    pen = new Pen(Color.Red, 1);
                    break;
                case 2:
                    pen = new Pen(Color.Black, 1);
                    break;
                case 3:
                    pen = new Pen(Color.White, 1);
                    break;
                case 4:
                    pen = new Pen(Color.Green, 1);
                    break;
                default:
                    pen = new Pen(Color.Black, 1);
                    break;
            }
            pen2 = new Pen(Color.Red, 2);
            g = pictureBox1.CreateGraphics();
            pictureBox2.Image = null;
        }

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            if (imageOpen)
            {
                double sigma = double.Parse(txtGaussSigma.Text);
                int maskSize = (int)nudMaskSize.Value;
                ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            }
            else
                MessageBox.Show("Please Open an Image First :)");
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Select Button
            if (imageOpen)
            {
                if ((pictureBox1.Bounds.X == 3) && (pictureBox1.Bounds.Y == 3))
                    pictureBox1.SetBounds(0, 0, 570, 520);
                else if (pictureBox1.Bounds.X == 3)
                    pictureBox1.SetBounds(0, pictureBox1.Bounds.Y, 570, 520);
                else if(pictureBox1.Bounds.Y==3)
                    pictureBox1.SetBounds(pictureBox1.Bounds.X,0, 570, 520);
                Graph = ImageOperations.ConstructGraph(ImageMatrix, pictureBox1.Bounds.X, pictureBox1.Bounds.Y);
                MessageBox.Show("Please Place Anchor point on the left image");
                select = true;
            }
            else
                MessageBox.Show("Please Open an Image First");
        }
        /// <summary>
        /// draws livewire between the Last anchor point and current position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (select && (ImageOperations.GetHeight(ImageMatrix) > 520 || ImageOperations.GetWidth(ImageMatrix) > 570))
            {
                if ((anchor && checkBox1.Checked) && ((e.X + pictureBox1.Bounds.X > tmpAnc.X + ancperpix) ||
                  (e.X + pictureBox1.Bounds.X < tmpAnc.X - ancperpix) ||
                  (e.Y + pictureBox1.Bounds.Y < tmpAnc.Y - ancperpix) ||
                  (e.Y + pictureBox1.Bounds.Y > tmpAnc.Y + ancperpix)))
                {
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, e.Y + pictureBox1.Bounds.Y, e.X + pictureBox1.Bounds.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2 - pictureBox1.Bounds.X;
                        p1.Y = shortestpath[i].Item1 - pictureBox1.Bounds.Y;
                        pointsArray[i] = p1;
                    }
                    g.DrawCurve(pen, pointsArray);
                    tmpAnc.X = e.X + pictureBox1.Bounds.X;
                    tmpAnc.Y = e.Y + pictureBox1.Bounds.Y;
                }
            }
            else
            {
                if ((anchor && checkBox1.Checked) && ((e.X > tmpAnc.X + ancperpix) ||
                  (e.X < tmpAnc.X - ancperpix) || (e.Y < tmpAnc.Y - ancperpix) ||
                  (e.Y > tmpAnc.Y + ancperpix)))
                {
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, e.Y, e.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2;
                        p1.Y = shortestpath[i].Item1;
                        pointsArray[i] = p1;
                    }
                    g.DrawCurve(pen, pointsArray);
                    tmpAnc.X = e.X;
                    tmpAnc.Y = e.Y;
                }
            }
        }
        /// <summary>
        /// draws livewire between the Last anchor point and mouse click 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (select && (ImageOperations.GetHeight(ImageMatrix) > 520 || ImageOperations.GetWidth(ImageMatrix) > 570))
            {
                if ((e.X < ImageOperations.GetWidth(ImageMatrix)) && select && (e.Y < ImageOperations.GetHeight(ImageMatrix)) && !anchor)
                {
                    anchor = true;
                    anc.X = e.X + pictureBox1.Bounds.X;
                    anc.Y = e.Y + pictureBox1.Bounds.Y;
                    tmpAnc = anc;
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);

                }
                else if ((e.X < ImageOperations.GetWidth(ImageMatrix)) && select && (e.Y < ImageOperations.GetHeight(ImageMatrix)) && anchor )
                {
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, e.Y + pictureBox1.Bounds.Y, e.X + pictureBox1.Bounds.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2 - pictureBox1.Bounds.X;
                        p1.Y = shortestpath[i].Item1 - pictureBox1.Bounds.Y;
                        pointsArray[i] = p1;
                    }
                    if(pointsArray.Length>1)
                    g.DrawCurve(pen, pointsArray);
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);
                    tmpAnc.X = e.X + pictureBox1.Bounds.X;
                    tmpAnc.Y = e.Y + pictureBox1.Bounds.Y;
                }
            }
            else
            {
                if ((e.X < ImageOperations.GetWidth(ImageMatrix)) && select && (e.Y < ImageOperations.GetHeight(ImageMatrix)) && !anchor)
                {
                    anchor = true;
                    anc.X = e.X;
                    anc.Y = e.Y;
                    tmpAnc = anc;
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);

                }
                else if ((e.X < ImageOperations.GetWidth(ImageMatrix)) && select && (e.Y < ImageOperations.GetHeight(ImageMatrix)) && anchor &&(e.X!=tmpAnc.X)&&(e.Y!=tmpAnc.Y))
                {
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, e.Y, e.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2;
                        p1.Y = shortestpath[i].Item1;
                        pointsArray[i] = p1;
                    }
                    g.DrawCurve(pen, pointsArray);
                    Rectangle ee = new Rectangle(e.X, e.Y, 1, 1);
                    g.DrawRectangle(pen2, ee);
                    tmpAnc.X = e.X;
                    tmpAnc.Y = e.Y;
                }
            }
        }
        /// <summary>
        /// connects the last point with the first anchor point through the shortest path 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (select)
            {
                if (ImageOperations.GetHeight(ImageMatrix) > 520 || ImageOperations.GetWidth(ImageMatrix) > 570)
                {
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, anc.Y, anc.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2 - pictureBox1.Bounds.X;
                        p1.Y = shortestpath[i].Item1 - pictureBox1.Bounds.Y;
                        pointsArray[i] = p1;
                    }
                    g.DrawCurve(pen, pointsArray);
                }
                else
                {
                    shortestpath = ImageOperations.Dijkstra(tmpAnc.Y, tmpAnc.X, anc.Y, anc.X);
                    Point[] pointsArray = new Point[shortestpath.Count];
                    for (int i = 0; i < shortestpath.Count; i++)
                    {
                        Point p1 = new Point();
                        p1.X = shortestpath[i].Item2;
                        p1.Y = shortestpath[i].Item1;
                        pointsArray[i] = p1;
                    }
                    g.DrawCurve(pen, pointsArray);

                }
                select = false;
                anchor = false;
            }
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ancperpix = 100 / (int)numericUpDown1.Value;
        }
        /// <summary>
        /// changes color of Livewire
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    pen = new Pen(Color.Pink, 1);
                    break;
                case 1:
                    pen = new Pen(Color.Red, 1);
                    break;
                case 2:
                    pen = new Pen(Color.Black, 1);
                    break;
                case 3:
                    pen = new Pen(Color.White, 1);
                    break;
                case 4:
                    pen = new Pen(Color.Green, 1);
                    break;
                default:
                    pen = new Pen(Color.Black, 1);
                    break;
            }
        }

    }
}