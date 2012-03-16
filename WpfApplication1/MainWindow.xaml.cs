using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        KinectSensor _sensor;


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];

                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.ColorStream.Enable();
                    _sensor.DepthStream.Enable();
                    _sensor.SkeletonStream.Enable();
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
                    _sensor.Start();
                }
            }
            //BitmapImage img = new BitmapImage(image2.Source);
        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;
            const int alpha = 3;

            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;


                if (depth <= 900)
                {
                    pixels[colorIndex + BlueIndex] = 255; //closest objects blue
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                    pixels[colorIndex + alpha] = 255;

                }
                else if (depth < 2000)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255; //medium depth is green
                    pixels[colorIndex + RedIndex] = 0;
                    pixels[colorIndex + alpha] = 255;

                }
                else
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 255; //farthest is red
                    pixels[colorIndex + alpha] = 255;

                }

                if (player > 0)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255;
                    pixels[colorIndex + RedIndex] = 255;
                    pixels[colorIndex + alpha] = 255;
                    
                }

            }

            return pixels;

        }

        private byte[] MessWithColor(byte[] pixels)
        {
            byte[] frame = new byte[pixels.Length];
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;
            const int alpha = 3;

            //make it shades of red (remove b and g)
            for (int idx = 0;
                idx < frame.Length;
                idx += 4)//stride, bgr32
            {
                frame[idx + BlueIndex] = pixels[idx + BlueIndex]; //blue pixel
                frame[idx + GreenIndex] = pixels[idx + GreenIndex]; //green pixel
                frame[idx + RedIndex] = pixels[idx + RedIndex];//red pixel
                frame[idx + alpha] = 50;
            }

            return frame;
        }


        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.AudioSource.Stop();
            }
        }

        
        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {


            //throw new NotImplementedException();
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) //new method that uses color camera and depth camera
                {
                    if (depthFrame == null)
                    {
                        return;
                    }

                    //                    byte[] pixels = GenerateColoredBytes(depthFrame);
                    //                    int stride = depthFrame.Width * 4;


                    short[] rawDepthData = new short[depthFrame.PixelDataLength];
                    depthFrame.CopyPixelDataTo(rawDepthData);

                    Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];
                    colorFrame.CopyPixelDataTo(pixels);

                    const int BlueIndex = 0;
                    const int GreenIndex = 1;
                    const int RedIndex = 2;
                    const int alpha = 3;


                    byte[] frame = new byte[pixels.Length];
                    bool playerFound = false;
                    bool playerDone = false;
                    //make it shades of red (remove b and g)
                    for (int depthIndex = 0, idx = 0;
                        depthIndex < rawDepthData.Length && idx < pixels.Length;
                        depthIndex++, idx += 4)
                    {
                        int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                        int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                        if (player > 0)
                        {
                            playerFound = true;
                            frame[idx + BlueIndex] = pixels[idx + BlueIndex]; //blue pixel
                            frame[idx + GreenIndex] = pixels[idx + GreenIndex]; //green pixel
                            frame[idx + RedIndex] = pixels[idx + RedIndex];//red pixel
                            frame[idx + alpha] = 255;

                            if (playerFound && !playerDone)
                            {
                                ellipse1.Height = depth / 30;
                                ellipse1.Width = depth / 30;
                                playerDone=true;
                            }
                        }
                        else
                        {
                            frame[idx + BlueIndex] = 0; //blue pixel
                            frame[idx + GreenIndex] = 0; //green pixel
                            frame[idx + RedIndex] = 0;//red pixel
                            frame[idx + alpha] = 0;


                        }
                    }


                    int stride = colorFrame.Width * 4;

                    image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                        96, 96, PixelFormats.Bgra32, null, frame, stride);
                }

            }

/*
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) //new method that uses color camera and depth camera
            {
                if (depthFrame == null)
                {
                    return;
                }

                byte[] pixels = GenerateColoredBytes(depthFrame);
                int stride = depthFrame.Width * 4;

                image2.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                    96, 96, PixelFormats.Bgra32, null, pixels, stride);


            }
*/
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(_sensor);
        }

        //buttons?




    }
}
