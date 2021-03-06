﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarPlateRecon.Core.Roi
{
    public class Region_of_Interest : abstract_Region_of_Interest
    {
        public Mat SnakePlate = new Mat();
        public Mat OriginalImage = new Mat();
        public Mat SnakeRGB = new Mat();

        public List<Mat> pieceMats = new List<Mat>();

        private List<Rect> SnakeRect = new List<Rect>();
        private List<Rect> SortPoint = new List<Rect>();
        private List<Rect> FindRect = new List<Rect>();


        Point[][] contours;

        public Region_of_Interest(
            Mat OriginalImage,
            Mat SnakePlate,
            Mat SnakeRGB,
            Point[][] contours
            )
        {
            this.OriginalImage = OriginalImage;
            this.SnakePlate = SnakePlate;
            this.SnakeRGB = SnakeRGB;
            this.contours = contours;
        }

        public override Mat GetRegion()
        {
            setContours();
            snakeGame();
            return setInterestRegion();
        }


        private void setContours()
        {
            for (int i = 0; i < contours.Length; i++)
            {
                var contour = contours[i];
                var boundingRect = Cv2.BoundingRect(contour);
                if (
                boundingRect.Width > 8 &&
                boundingRect.Height > 5 &&
                boundingRect.Width < 40 &&
                boundingRect.Height < 40 &&
                boundingRect.Width * boundingRect.Height > 40
                )
                {
                    SortPoint.Add(boundingRect);
                    Cv2.Rectangle(SnakeRGB,
                        new Point(boundingRect.X, boundingRect.Y),
                        new Point(boundingRect.X + boundingRect.Width, boundingRect.Y + boundingRect.Height),
                        new Scalar(0, 0, 255),
                        2);
                }
            }
        }

        private void snakeGame()
        {
            int count = 0;
            int friend_count = 0;
            pieceMats.Clear();
            SnakeRect.Clear();

            for (int i = 0; i < SortPoint.Count; i++)
            {
                for (int j = 0; j < (SortPoint.Count - 1) - (i + 1); j++)
                {
                    var temp_rect = SortPoint[j];
                    SortPoint[j] = SortPoint[j + 1];
                    SortPoint[j + 1] = temp_rect;
                }
            }

            for (int i = 0; i < SortPoint.Count; i++)
            {
                for (int j = i + 1; j < SortPoint.Count; j++)
                {
                    int Delta_x = Math.Abs(SortPoint[j].TopLeft.X - SortPoint[i].TopLeft.X);

                    if (Delta_x > 150)
                        break;

                    int Delta_y = Math.Abs(SortPoint[j].TopLeft.Y - SortPoint[i].TopLeft.Y);

                    if (Delta_x == 0)
                    {
                        Delta_x = 1;
                    }
                    if (Delta_y == 0)
                    {
                        Delta_y = 1;
                    }

                    double gradient = (double)Delta_y / Delta_x;

                    if (gradient < 0.15)
                    {
                        count += 1;
                    }
                    if (count > friend_count)
                    {
                        int selected = i;
                        friend_count = count;
                        Cv2.Rectangle(SnakeRGB, SortPoint[selected], new Scalar(0, 0, 255), 2);
                        int plate_width = Delta_x;
                        Cv2.Line(SnakeRGB,
                            new Point(SortPoint[selected].TopLeft.X, SortPoint[selected].TopLeft.Y),
                            new Point(SortPoint[selected].TopLeft.X + plate_width, SortPoint[selected].TopLeft.Y),
                            new Scalar(255, 0, 0),
                            2
                            );
                        var Rect = new Rect(SortPoint[selected].TopLeft.X - 35, SortPoint[selected].TopLeft.Y, plate_width + 105, SortPoint[selected].Height);
                        FindRect.Add(Rect);
                        SnakeRect.Add(Rect);
                    }
                }
                if (SnakeRect.Count > 0)
                {
                    pieceMats.Add(setSnakeRegion(SnakeRect));
                    SnakeRect.Clear();
                }
            }
        }

      

        private Mat setSnakeRegion(List<Rect> rect)
        {
            var width = 0;
            var height = 0;
            for (int i = 0; i < rect.Count; i++)
            {
                if (width < rect[i].Width)
                {
                    width = rect[i].Width;
                }
            }
            for (int i = 0; i < rect.Count; i++)
            {
                if (height < rect[i].Height)
                {
                    height = rect[i].Height;
                }
            }

            int frch = 0;
            Mat interestRegion = new Mat(new Size(width, height), OriginalImage.Type()).EmptyClone();
            foreach (Rect tmp in rect)
            {
                if (
                       0 <= tmp.X
                       && 0 <= tmp.Width
                       && tmp.X + tmp.Width <= OriginalImage.Cols
                       && 0 <= tmp.Y
                       && 0 <= tmp.Height
                       && tmp.Y + tmp.Height <= OriginalImage.Rows
                       )
                {
                    var ss = new Mat(OriginalImage, tmp);
                    var roi = new Mat(interestRegion, new Rect(0, 0, ss.Width, ss.Height));
                    ss.CopyTo(roi);
                    frch++;
                }
            }

            return interestRegion;
        }



        private Mat setInterestRegion()
        {
            int frch = 0;
            Mat interestRegion = new Mat(OriginalImage.Rows, OriginalImage.Cols, OriginalImage.Type()).EmptyClone();
            foreach (Rect tmp in FindRect)
            {
                if (
                       0 <= tmp.X
                       && 0 <= tmp.Width
                       && tmp.X + tmp.Width <= OriginalImage.Cols
                       && 0 <= tmp.Y
                       && 0 <= tmp.Height
                       && tmp.Y + tmp.Height <= OriginalImage.Rows
                       )
                {
                    var ss = new Mat(OriginalImage, tmp);
                    var roi = new Mat(interestRegion, new Rect(tmp.X, tmp.Y, ss.Width, ss.Height));
                    ss.CopyTo(roi);
                    frch++;
                }
            }
            return interestRegion;
        }
    }
}
