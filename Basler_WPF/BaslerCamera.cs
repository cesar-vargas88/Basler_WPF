using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Basler.Pylon;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Windows;

namespace Basler_WPF
{
    public class BaslerCamera
    {
        private Camera camera;
        private Thread thread_grab;
        private bool grabbing;

        public BaslerCamera()
        {
            camera = new Camera();
            grabbing = false;
        }

        public BaslerCamera(string ip)
        {
            foreach (ICameraInfo INFO in CameraFinder.Enumerate())
            {
                if (INFO.GetValueOrDefault("IpAddress", "0") == ip)
                {
                    camera = new Camera(INFO);
                    break;
                }
            }
            if (camera == null)
            {
                camera = new Camera();
            }

            grabbing = false;
        }

        public bool snapImage(Emgu.CV.UI.ImageBox imageBox, int height = 0, int width = 0, int imageRotation = 0)
        {
            try
            {
                IGrabResult grabResult = snap(height, width);

                using (grabResult)
                {
                    if (grabResult.GrabSucceeded)
                    {
                        // convert image from basler IImage to OpenCV Mat
                        Mat img = convertIImage2Mat(grabResult);
                        // convert image from BayerBG to RGB
                        CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.BayerBg2Rgb);
                        // rotate image x degrees
                        if (imageRotation != 0)
                            rotateImage(img, img, imageRotation);
                        // resize image  to fit the imageBox
                        CvInvoke.Resize(img, img, new System.Drawing.Size(imageBox.Height, imageBox.Width));
                        // draw the pointer
                        drawPointer(img, new MCvScalar(0, 100, 200), 1, Emgu.CV.CvEnum.LineType.EightConnected);
                        // copy processed image to imagebox.image
                        imageBox.Image = img;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);

                return false;
            }
        }

        public bool saveImage(string path, int height, int width, int imageRotation = 0)
        {
            try
            {
                IGrabResult grabResult = snap(height, width);

                using (grabResult)
                {
                    if (grabResult.GrabSucceeded)
                    {
                        // convert image from basler IImage to OpenCV Mat
                        Mat img = convertIImage2Mat(grabResult);
                        // convert image from BayerBG to RGB
                        CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.BayerBg2Rgb);
                        // rotate image x degrees
                        if (imageRotation != 0)
                            rotateImage(img, img, imageRotation);
                        // save image
                        CvInvoke.Imwrite(path, img);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);

                return false;
            }
        }

        public bool saveImage(Emgu.CV.UI.ImageBox imageBox, string path, int height = 0, int width = 0, int imageRotation = 0)
        {
            try
            {
                IGrabResult grabResult = snap(height, width);

                using (grabResult)
               {
                    if (grabResult.GrabSucceeded)
                    {
                        // convert image from basler IImage to OpenCV Mat
                        Mat img = convertIImage2Mat(grabResult);
                        // convert image from BayerBG to RGB
                        CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.BayerBg2Rgb);
                        // rotate image x degrees
                        if (imageRotation != 0)
                            rotateImage(img, img, imageRotation);
                        // save image
                        CvInvoke.Imwrite(path, img);
                        // resize image  to fit the imageBox
                        CvInvoke.Resize(img, img, new System.Drawing.Size(imageBox.Height, imageBox.Width));
                        // copy processed image to imagebox.image
                        imageBox.Image = img;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);

                return false;
            }
        }

        public bool grab(Emgu.CV.UI.ImageBox imageBox, int height = 0, int width = 0, int imageRotation = 0, int snap_wait = 500)
        {
            if (!grabbing)
            {
                grabbing = true;

                try
                {
                    Thread thread = new Thread(() => th_grab(imageBox, height, width, imageRotation, snap_wait));
                    thread.Start();             
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }

                return true;
            }
            else
                return false;
        }

        public void stop()
        {
            grabbing = false;
        }

        private void th_grab(Emgu.CV.UI.ImageBox imageBox, int height = 0, int width = 0, int imageRotation = 0, int snap_wait = 500)
        {
            try
            {
                // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                camera.CameraOpened += Configuration.AcquireContinuous;

                // Open the connection to the camera device.
                camera.Open();

                if (width == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                    camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
                else if (width < camera.Parameters[PLCamera.Height].GetMinimum())
                    camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
                else
                    camera.Parameters[PLCamera.Width].SetValue(width);

                if (height == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                    camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
                else if (height < camera.Parameters[PLCamera.Height].GetMinimum())
                    camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
                else
                    camera.Parameters[PLCamera.Height].SetValue(height);

                camera.Parameters[PLCamera.CenterX].SetValue(true);
                camera.Parameters[PLCamera.CenterY].SetValue(true);

                camera.StreamGrabber.Start();

                while (grabbing)
                {
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                    using (grabResult)
                    {
                        if (grabResult.GrabSucceeded)
                        {
                            // convert image from basler IImage to OpenCV Mat
                            Mat img = convertIImage2Mat(grabResult);
                            // convert image from BayerBG to RGB
                            CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.BayerBg2Rgb);
                            // rotate image x degrees
                            if (imageRotation != 0)
                                rotateImage(img, img, imageRotation);
                            // resize image  to fit the imageBox
                            CvInvoke.Resize(img, img, new System.Drawing.Size(imageBox.Height, imageBox.Width));
                            // draw the pointer
                            drawPointer(img, new MCvScalar(0, 100, 200), 1, Emgu.CV.CvEnum.LineType.EightConnected);
                            // copy processed image to imagebox.image
                            imageBox.Image = img;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }

                    Thread.Sleep(snap_wait);
                }

                camera.StreamGrabber.Stop();
                camera.Close();

            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);
            }
        }

        private Mat convertIImage2Mat(IGrabResult grabResult)
        {
            GCHandle pinnedArray = GCHandle.Alloc(grabResult.PixelData, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();
            pinnedArray.Free();
            return new Mat(grabResult.Height, grabResult.Width, Emgu.CV.CvEnum.DepthType.Cv8U, 1, ptr, grabResult.Width);
        }

        private IGrabResult snap(int height = 0, int width = 0)
        {
            // Set the acquisition mode to free running continuous acquisition when the camera is opened.
            camera.CameraOpened += Configuration.AcquireSingleFrame;

            // Open the connection to the camera device.
            camera.Open();

            if (width == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
            else if (width < camera.Parameters[PLCamera.Height].GetMinimum())
                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
            else
                camera.Parameters[PLCamera.Width].SetValue(width);

            if (height == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
            else if (height < camera.Parameters[PLCamera.Height].GetMinimum())
                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
            else
                camera.Parameters[PLCamera.Height].SetValue(height);

            camera.Parameters[PLCamera.CenterX].SetValue(true);
            camera.Parameters[PLCamera.CenterY].SetValue(true);

            camera.StreamGrabber.Start();
            IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            camera.StreamGrabber.Stop();
            camera.Close();

            return grabResult;
        }

        private void rotateImage(Mat src, Mat dst, int degrees)
        {
            Mat rotated = new Mat();
            CvInvoke.GetRotationMatrix2D(new System.Drawing.PointF(src.Height / 2, src.Width / 2), degrees, 1, rotated);
            CvInvoke.WarpAffine(src, dst, rotated, new System.Drawing.Size(src.Height, src.Width));
        }

        private void drawPointer(Mat src, MCvScalar color, int thickness, Emgu.CV.CvEnum.LineType lineType)
        {
            CvInvoke.Line(src, new System.Drawing.Point(0, src.Width / 2), new System.Drawing.Point(src.Height, src.Width / 2), color, thickness, lineType);
            CvInvoke.Line(src, new System.Drawing.Point(src.Height / 2, 0), new System.Drawing.Point(src.Height / 2, src.Width), color, thickness, lineType);
            CvInvoke.Circle(src, new System.Drawing.Point(src.Height / 2, src.Width / 2), src.Height / 10, color, thickness, lineType);
        }
    }
}
