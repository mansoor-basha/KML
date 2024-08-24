using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;


namespace KML_Desk_Project
{
    public partial class HomePage : Form
    {



        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        private bool isCameraRunning = false;
        private int frameCount = 0;
        private DateTime lastTime = DateTime.Now;
        private float fps = 0.0f;
        private float zoomFactor = 1.0f;
        private float maxZoomLevel = 5.0f;
        private float minZoomLevel = 0.1f;
        private bool isDragging = false;
        private Point mouseDownPosition;
        private Bitmap currentFrame;
        private readonly object frameLock = new object();
        private float brightnessFactor = 1.0f;
        private float contrastFactor = 1.0f;
        private float exposureFactor = 1.0f;
        private float highlightFactor = 1.0f;
        private float sharpnessFactor = 1.0f;
        private float saturationFactor = 1.0f;
        private Size originalPictureBoxSize;
        private Point originalPictureBoxLocation;
        private int imageCounter = 1;
        private string lastSavedDirectory = null;
        private bool CLICK;
        private bool flipX = false;
        private bool flipY = false;
        private Timer fpsErrorTimer;
        private float fpsThreshold = 15.0f;
        private bool click = false;
        private Bitmap originalImage;
        private Timer deviceMonitorTimer;
        private int cropWidth, cropHeight;
        private bool applyCrop = false;


        public HomePage()
        {
            InitializeComponent();


            lastTime = DateTime.Now;
            panel_Home.Visible = true;

            panel_INFO.Visible = false;
            panel_SETUP.Visible = false;
            panel_password.Visible = false;
            textBox1.Text = "Camera is Off";
            Error_textBox.Text = "Camera is Off";
            label1.BackColor = Color.Azure;
            label1.ForeColor = Color.Azure;
            Home.BackColor = Color.Aquamarine;
            Info.BackColor = Color.Gray;
            setup.BackColor = Color.Gray;
            Home.FlatAppearance.BorderSize = 4;
            Home.FlatAppearance.BorderColor = Color.Green;
            pictureBox_CAMON.Visible = false;
            pictureBox2.BackColor = Color.Azure;
            panel_SETUP.Enabled = false;

            if (isCameraRunning)
            {
                DateTime now = DateTime.Now;
                Error_textBox.Text = $"Camera is On\nStart Time: {now.ToString("F")}";
            }

            Load += new EventHandler(HomePage_Load);
            pictureBox_cam.MouseWheel += new MouseEventHandler(pictureBox_cam_MouseWheel);
            pictureBox_cam.MouseDown += new MouseEventHandler(pictureBox_cam_MouseDown);
            pictureBox_cam.MouseMove += new MouseEventHandler(pictureBox_cam_MouseMove);
            pictureBox_cam.MouseUp += new MouseEventHandler(pictureBox_cam_Mouseup);

            brightness_trackBar.Scroll += new EventHandler(brightness_trackBar_Scroll);
            Contrast_trackBar.Scroll += new EventHandler(Contrast_trackBar_Scroll);
            Exposure_trackBar.Scroll += new EventHandler(Exposure_trackBar_Scroll);
            sharpness_trackBar.Scroll += new EventHandler(sharpness_trackBar_Scroll);
            saturation_trackBar.Scroll += new EventHandler(saturation_trackBar_Scroll);
            Highlights_trackBar.Scroll += new EventHandler(Highlights_trackBar_Scroll);

            // Attach MouseWheel event handlers for TrackBars
            brightness_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);
            Contrast_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);
            Exposure_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);
            sharpness_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);
            saturation_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);
            Highlights_trackBar.MouseWheel += new MouseEventHandler(trackBar_MouseWheel);

            // Add MouseEnter event for trackbars
            brightness_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
            Contrast_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
            Exposure_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
            sharpness_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
            saturation_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
            Highlights_trackBar.MouseEnter += new EventHandler(trackBar_MouseEnter);
        }
        private void HomePage_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No video devices found");
                textBox1.Text = "Camera is not connected";
                cam_onoff.Enabled = false;
                return;
            }

            InitializeComboBox();

            originalPictureBoxSize = pictureBox_cam.Size;
            originalPictureBoxLocation = pictureBox_cam.Location;

            // Set initial values for the trackbar textboxes
            Bright_textBox.Text = brightness_trackBar.Value.ToString();
            exposure_textBox.Text = Exposure_trackBar.Value.ToString();
            contrast_textBox.Text = Contrast_trackBar.Value.ToString();
            sharpness_textBox.Text = sharpness_trackBar.Value.ToString();
            saturation_textBox.Text = saturation_trackBar.Value.ToString();
            highlight_textBox.Text = Highlights_trackBar.Value.ToString();

            LoadState();

            // Initialize and start the device monitor timer
            deviceMonitorTimer = new Timer();
            deviceMonitorTimer.Interval = 2000; // Check every 2 seconds
            deviceMonitorTimer.Tick += MonitorDevices;
            deviceMonitorTimer.Start();
        }
        private void MonitorDevices(object sender, EventArgs e)
        {
            var currentDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (currentDevices.Count != videoDevices.Count)
            {
                videoDevices = currentDevices;
                InitializeComboBox();
            }
        }
        private void InitializeComboBox()
        {
            comboBox.Items.Clear();

            foreach (FilterInfo device in videoDevices)
            {
                comboBox.Items.Add(device.Name);
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
                cam_onoff.Enabled = true;
            }
            else
            {
                comboBox.Text = string.Empty;
                cam_onoff.Enabled = false;
            }
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedDeviceIndex = comboBox.SelectedIndex;

            if (selectedDeviceIndex >= 0 && selectedDeviceIndex < videoDevices.Count && isCameraRunning)
            {
                Stopfeed();
                videoSource = new VideoCaptureDevice(videoDevices[selectedDeviceIndex].MonikerString);
                Startfeed();
            }
           
        }
        // Home Page
        private void Home_Click(object sender, EventArgs e)
        {
            panel_Home.Visible = true;
            this.Text = "Home";
            Home.BackColor = Color.Aquamarine;
            Info.BackColor = Color.Gray;
            setup.BackColor = Color.Gray;
            Home.FlatAppearance.BorderSize = 4;
            Home.FlatAppearance.BorderColor = Color.Green;
            panel_INFO.Visible = false;
            panel_SETUP.Visible = false;
            panel_password.Visible = false;

        }
        private void cam_onoff_Click(object sender, EventArgs e)
        {
            if (!isCameraRunning)
            {
                Startfeed();
            }
            else
            {
                Stopfeed();
            }
        }
        private void Startfeed()
        {
            try
            {
                // Stop any current feed if running
                if (videoSource != null && videoSource.IsRunning)
                {
                    Stopfeed();
                }

                // Check if any video devices are available
                if (videoDevices == null || videoDevices.Count == 0)
                {
                    textBox1.Text = "Camera is not connected";
                    MessageBox.Show("No video devices found. Please connect a camera and try again.");
                    comboBox.Text = string.Empty;
                    return;
                }

                // Check if a valid camera is selected
                if (comboBox.SelectedIndex < 0 || comboBox.SelectedIndex >= videoDevices.Count)
                {
                    textBox1.Text = "No camera selected";
                    MessageBox.Show("Please select a valid camera from the list.");
                    return;
                }

                // Initialize the selected video source
                videoSource = new VideoCaptureDevice(videoDevices[comboBox.SelectedIndex].MonikerString);

                if (videoSource != null)
                {
                    videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                    videoSource.Start();

                    cam_onoff.Text = "Off Camera";
                    textBox1.Text = "Camera is On";
                    label1.BackColor = Color.Green;
                    pictureBox2.BackColor = Color.Green;
                    label1.Text = "READY";
                    pictureBox3.Visible = false;
                    pictureBox_OFFCAM.Visible = false;
                    pictureBox_CAMON.Visible = true;
                    panel_SETUP.Enabled = true;

                    DateTime now = DateTime.Now;
                    string startMessage = $"Camera is On\nStart Time: {now:F}";
                    LogToTextBox(startMessage);

                    isCameraRunning = true;
                    
                    Timer monitorTimer = new Timer();
                    monitorTimer.Interval = 1000;
                    monitorTimer.Tick += (s, e) =>
                    {
                        
                        if (videoSource != null && !videoSource.IsRunning)
                        {
                            monitorTimer.Stop();
                            HandleCameraDisconnection();
                        }
                    };
                    monitorTimer.Start();
                }
                else
                {
                    DisplayError("Failed to initialize the camera.");
                    Error_textBox.Text = "Error: Camera is not connected.";
                    MessageBox.Show("Camera is not connected.");
                    isCameraRunning = false;
                }
            }
            catch (Exception ex)
            {
                DisplayError("An error occurred while starting the camera: " + ex.Message);
                Error_textBox.Text = "Error: Camera is not connected.";
            }
        }
        private void HandleCameraDisconnection()
        {
            try
            { 
                if (isCameraRunning)
                {
                    Stopfeed();
                }
                textBox1.Text = "Camera is not connected";                
                comboBox.Items.Clear();
                comboBox.Text = string.Empty;
                cam_onoff.Enabled = false;
                label1.BackColor = Color.Red;
                pictureBox2.BackColor = Color.Red;
                label1.Text = "ERROR";
                MessageBox.Show("Camera was disconnected. Please reconnect the camera.");

                string disconnectionMessage = $"Camera was disconnected at {DateTime.Now:F}";
                LogToTextBox(disconnectionMessage);
            }
            catch (Exception ex)
            {
                DisplayError("An error occurred while handling the camera disconnection: " + ex.Message);
            }
        }
        private void Stopfeed()
        {
            try
            {
                if (pictureBox_cam.Image != null && isCameraRunning)
                {
                    videoSource.SignalToStop();                                  
                    pictureBox_cam.Image.Dispose();
                    videoSource = null;
                    pictureBox_cam.Image = null;
                    
                    Bitmap cameraOffImage = new Bitmap(pictureBox_cam.Width, pictureBox_cam.Height);
                    using (Graphics man = Graphics.FromImage(cameraOffImage))
                    {
                        man.Clear(Color.Black);
                        man.DrawString("Camera Off", new Font("Arial", 24), Brushes.White,
                            new PointF(pictureBox_cam.Width / 2 - 75, pictureBox_cam.Height / 2 - 20));
                    }
                    pictureBox_cam.Image = cameraOffImage;
                                        
                    cam_onoff.Text = "On Camera";
                    textBox1.Text = "Camera is Off";
                    label1.BackColor = Color.LightBlue;
                    pictureBox2.BackColor = Color.Green;
                    label1.Text = "STATUS";
                    label1.ForeColor = Color.Black;
                    pictureBox_OFFCAM.Visible = true;
                    pictureBox_CAMON.Visible = false;
                    panel_SETUP.Enabled = false;
                    
                    DateTime now = DateTime.Now;
                    string stopMessage = $"Camera is Off\nEnd Time: {now.ToString("F")}";
                    LogToTextBox(stopMessage);
                    
                    isCameraRunning = false;
                }
            }
            catch (Exception ex)
            {                
                DisplayError("An error occurred whi00le stopping the camera: " + ex.Message);
            }
        }
        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            lock (frameLock)
            {
                DateTime now = DateTime.Now;
                TimeSpan elapsed = now - lastTime;

                // Limit the frame rate processing to 20 FPS
                if (elapsed.TotalSeconds < 1.0 / 20.0)
                {
                    return;
                }

                Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone(); // Clone the frame

                // Apply flipping if required
                if (flipX || flipY)
                {
                    bitmap.RotateFlip(
                        (flipX && flipY) ? RotateFlipType.RotateNoneFlipXY :
                        (flipX) ? RotateFlipType.RotateNoneFlipX :
                        RotateFlipType.RotateNoneFlipY);
                }

                // Get frame dimensions
                int frameWidth = bitmap.Width;
                int frameHeight = bitmap.Height;

                // Invoke to update the Height and Width text boxes
                Height.Invoke(new Action(() =>
                {
                    Height.Text = frameHeight.ToString();
                }));

                Width.Invoke(new Action(() =>
                {
                    Width.Text = frameWidth.ToString();
                }));

                // Dispose of the previous frame to free resources
                currentFrame?.Dispose();
                currentFrame = bitmap;

                // Apply effects or other modifications to the frame
                Bitmap processedFrame = effects(currentFrame);

                // Check if cropping is enabled
                if (applyCrop)
                {
                    int newWidth = cropWidth;
                    int newHeight = cropHeight;

                    if (newWidth > 0 && newHeight > 0 && newWidth <= frameWidth && newHeight <= frameHeight)
                    {
                        int startX = (frameWidth - newWidth) / 2;
                        int startY = (frameHeight - newHeight) / 2;
                        Rectangle cropArea = new Rectangle(startX, startY, newWidth, newHeight);

                        Bitmap croppedFrame = new Bitmap(newWidth, newHeight);
                        using (Graphics g = Graphics.FromImage(croppedFrame))
                        {
                            g.DrawImage(processedFrame, new Rectangle(0, 0, newWidth, newHeight), cropArea, GraphicsUnit.Pixel);
                        }
                        processedFrame.Dispose(); // Dispose the uncropped frame
                        processedFrame = croppedFrame; // Replace with the cropped frame
                    }
                }

                // Update PictureBox with the processed frame
                pictureBox_cam.Invoke(new Action(() =>
                {
                    pictureBox_cam.Image = processedFrame;
                }));

                frameCount++;
                elapsed = now - lastTime;

                // FPS calculation and update
                if (elapsed.TotalSeconds >= 1.0)
                {
                    fps = frameCount / (float)elapsed.TotalSeconds;
                    fps_textBox.Invoke(new Action(() =>
                    {
                        fps_textBox.Text = $"FPS: {fps:F2}";
                    }));

                    frameCount = 0;
                    lastTime = now;

                    // FPS error handling
                    if (fps < fpsThreshold)
                    {
                        if (fpsErrorTimer == null)
                        {
                            fpsErrorTimer = new Timer();
                            fpsErrorTimer.Interval = 600000; // 10 minutes
                            fpsErrorTimer.Tick += (s, e) =>
                            {
                                string errorMessage = $"Error: FPS is low ({fps:F2})\nTime: {now.ToString("F")}";
                                LogToTextBox(errorMessage);
                                fpsErrorTimer.Stop();
                            };
                        }
                        fpsErrorTimer.Start();
                    }
                }
            }
        }

        private void pictureBox_cam_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomIn(e.X, e.Y);
            }
            else
            {
                ZoomOut(e.X, e.Y);
            }
        }
        private void ZoomIn(int centerX, int centerY)
        {
            if (pictureBox_cam.Image != null && isCameraRunning && zoomFactor < maxZoomLevel)
            {
                zoomFactor *= 1.2f;
                var newWidth = (int)(pictureBox_cam.Width * 1.2f);
                var newHeight = (int)(pictureBox_cam.Height * 1.2f);
                var offsetX = (int)((centerX * 1.2f) - centerX);
                var offsetY = (int)((centerY * 1.2f) - centerY);
                pictureBox_cam.Width = newWidth;
                pictureBox_cam.Height = newHeight;
                pictureBox_cam.Location = new Point(pictureBox_cam.Location.X - offsetX, pictureBox_cam.Location.Y - offsetY);
                pictureBox_cam.SizeMode = PictureBoxSizeMode.StretchImage;
            }
        }
        private void ZoomOut(int centerX, int centerY)
        {
            if (pictureBox_cam.Image != null && isCameraRunning && zoomFactor > minZoomLevel)
            {
                zoomFactor /= 1.2f;
                var newWidth = (int)(pictureBox_cam.Width / 1.2f);
                var newHeight = (int)(pictureBox_cam.Height / 1.2f);
                var offsetX = (int)(centerX - (centerX / 1.2f));
                var offsetY = (int)(centerY - (centerY / 1.2f));
                pictureBox_cam.Width = newWidth;
                pictureBox_cam.Height = newHeight;
                pictureBox_cam.Location = new Point(pictureBox_cam.Location.X + offsetX, pictureBox_cam.Location.Y + offsetY);
                pictureBox_cam.SizeMode = PictureBoxSizeMode.StretchImage;
            }
        }
        private void pictureBox_cam_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                mouseDownPosition = e.Location;
                pictureBox_cam.Cursor = Cursors.Hand;
            }
        }
        private void pictureBox_cam_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                pictureBox_cam.Left += e.X - mouseDownPosition.X;
                pictureBox_cam.Top += e.Y - mouseDownPosition.Y;
            }
        }
        private void pictureBox_cam_Mouseup(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                pictureBox_cam.Cursor = Cursors.Default;
            }
        }
        private Bitmap effects(Bitmap bitmap)
        {
            Bitmap effectsBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            using (Graphics man = Graphics.FromImage(effectsBitmap))
            {
                man.Clear(Color.Black);
                man.InterpolationMode = InterpolationMode.HighQualityBicubic;
                man.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                using (ImageAttributes imageAttributes = new ImageAttributes())
                {
                    float[][] colorMatrixElements = {
                    new float[] {contrastFactor, 0, 0, 0, 0},
                    new float[] {0, contrastFactor * saturationFactor, 0, 0, 0},
                    new float[] {0, 0, contrastFactor * saturationFactor, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {brightnessFactor - 1 + highlightFactor - 1 + exposureFactor - 1,
                             brightnessFactor - 1 + highlightFactor - 1 + exposureFactor - 1,
                             brightnessFactor - 1 + highlightFactor - 1 + exposureFactor - 1, 0, 1}
                    };
                    ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
                    imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    man.DrawImage(bitmap,
                                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                0, 0, bitmap.Width, bitmap.Height,
                                GraphicsUnit.Pixel, imageAttributes);
                }
                if (sharpnessFactor != 1.0f)
                {
                    float sharpnessLevel = 1.0f + (sharpnessFactor - 1.0f) * 0.5f;
                    float[][] sharpnessMatrix = {
                    new float[] {-sharpnessLevel, -sharpnessLevel, -sharpnessLevel},
                    new float[] {-sharpnessLevel, 1.0f + 8.0f * sharpnessLevel, -sharpnessLevel},
                    new float[] {-sharpnessLevel, -sharpnessLevel, -sharpnessLevel}
                    };
                    ConvolutionFilter(effectsBitmap, sharpnessMatrix);
                }
                effectsBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            return effectsBitmap;
        }
        private void ConvolutionFilter(Bitmap image, float[][] kernel)
        {
            Bitmap sourceImage = (Bitmap)image.Clone();
            int width = image.Width;
            int height = image.Height;
            BitmapData srcData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int bytes = srcData.Stride * srcData.Height;
            byte[] pixelBuffer = new byte[bytes];
            byte[] resultBuffer = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, pixelBuffer, 0, bytes);
            sourceImage.UnlockBits(srcData);
            int filterOffset = 1;
            int calcOffset = 0;
            int byteOffset = 0;

            for (int offsetY = filterOffset; offsetY < height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < width - filterOffset; offsetX++)
                {
                    float blue = 0;
                    float green = 0;
                    float red = 0;

                    byteOffset = offsetY * srcData.Stride + offsetX * 4;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            calcOffset = byteOffset + (filterX * 4) + (filterY * srcData.Stride);

                            blue += (float)(pixelBuffer[calcOffset]) * kernel[filterY + filterOffset][filterX + filterOffset];
                            green += (float)(pixelBuffer[calcOffset + 1]) * kernel[filterY + filterOffset][filterX + filterOffset];
                            red += (float)(pixelBuffer[calcOffset + 2]) * kernel[filterY + filterOffset][filterX + filterOffset];
                        }
                    }
                    resultBuffer[byteOffset] = (byte)Math.Min(Math.Max((int)(blue), 0), 255);
                    resultBuffer[byteOffset + 1] = (byte)Math.Min(Math.Max((int)(green), 0), 255);
                    resultBuffer[byteOffset + 2] = (byte)Math.Min(Math.Max((int)(red), 0), 255);
                    resultBuffer[byteOffset + 3] = pixelBuffer[byteOffset + 3];
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(resultBuffer, 0, dstData.Scan0, bytes);
            image.UnlockBits(dstData);
        }
        private void altHeight_TextChanged(object sender, EventArgs e)
        {
        }
        private void altWidth_TextChanged(object sender, EventArgs e)
        {
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(altWidth.Text, out cropWidth) || !int.TryParse(altHeight.Text, out cropHeight))
                {
                    MessageBox.Show("Please enter valid numeric dimensions.");
                    return;
                }

                if (cropWidth <= 0 || cropHeight <= 0)
                {
                    MessageBox.Show("Dimensions must be positive integers.");
                    return;
                }

                applyCrop = true; // Enable cropping for the live feed
            }
            catch (Exception ex)
            {
                string errorMessage = "Error applying new dimensions: " + ex.Message;
                MessageBox.Show(errorMessage);
                LogToTextBox(errorMessage);
            }
        }
        private void caputre_Click(object sender, EventArgs e)
        {
            if (pictureBox_cam.Image != null)
            {
                SaveCapturedImage((Bitmap)pictureBox_cam.Image);

            }
        }
        private void SaveCapturedImage(Bitmap bitmap)
        {
            try
            {
                if (string.IsNullOrEmpty(lastSavedDirectory))
                {
                    return;
                }
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = System.IO.Path.Combine(lastSavedDirectory, $"Capture_{timeStamp}_{imageCounter}.jpeg");

                bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                imageCounter++;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void selectfolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    lastSavedDirectory = folderDialog.SelectedPath;
                    MessageBox.Show($"Selected folder: {lastSavedDirectory}", "Folder Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void Reset_Click(object sender, EventArgs e)
        {
            try
            {
                zoomFactor = 1.0f; 
                pictureBox_cam.Size = originalPictureBoxSize;
                pictureBox_cam.Location = originalPictureBoxLocation;                
                altWidth.Text = string.Empty;
                altHeight.Text = string.Empty;                              
                applyCrop = false;                
                pictureBox_cam.Image = originalImage?.Clone() as Bitmap;                
                flipX = false;
                flipY = false;
                
                if (originalImage != null)
                {
                    pictureBox_cam.Image = effects(originalImage?.Clone() as Bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during reset: " + ex.Message);
            }
        }







        //Info
        private void Info_Click(object sender, EventArgs e)
        {
            panel_Home.Visible = false;
            this.Text = "INFO";
            Info.BackColor = Color.Aquamarine;
            Home.BackColor = Color.Gray;
            setup.BackColor = Color.Gray;
            Info.FlatAppearance.BorderSize = 4;
            Info.FlatAppearance.BorderColor = Color.Green;
            panel_INFO.Visible = true;
            panel_SETUP.Visible = false;
            panel_password.Visible = false;
        }
        private void LogToTextBox(string message)
        {
            if (Error_textBox.InvokeRequired)
            {

                Error_textBox.Invoke(new Action(() => LogToTextBox(message)));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Error_textBox.Text))
                {
                    Error_textBox.AppendText(Environment.NewLine);
                }
                Error_textBox.AppendText($"{message}\n");
            }
        }
        private void DisplayError(string message)
        {
            MessageBox.Show(message);
            label1.BackColor = Color.Red;
            pictureBox2.BackColor = Color.Red;
            label1.Text = "ERROR";
            pictureBox3.Visible = true;
            LogToTextBox($"Error: {message}"); // Log the error
        }
        private void SaveLogToFile(string logContent)
        {
            try
            {
                string folderPath = @"C:\Users\User\Desktop\Mansoor_Project\KML_Desk_Project\KML_Desk_Project\bin\Debug\error_logs";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string dateTimeFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                string filePath = Path.Combine(folderPath, dateTimeFileName);

                File.WriteAllText(filePath, logContent);
                MessageBox.Show("Log saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the log: {ex.Message}");
            }
        }
        private void save_Click(object sender, EventArgs e)
        {
            SaveLogToFile(Error_textBox.Text);
        }
        private void Error_textBox_TextChanged(object sender, EventArgs e)
        {
            SaveLogToFile(Error_textBox.Text);
        }
        private void clear_Click(object sender, EventArgs e)
        {
            Error_textBox.Text = string.Empty;
        }





        // SETUP STARTS
        private void OK_Click(object sender, EventArgs e)
        {
            if (Password_textBox.Text == "12345")
            {
                click = true;  

                panel_password.Visible = false;
                panel_Home.Visible = false;
                this.Text = "SETUP";
                setup.BackColor = Color.Aquamarine;
                Info.BackColor = Color.Gray;
                Home.BackColor = Color.Gray;
                setup.FlatAppearance.BorderSize = 4;
                setup.FlatAppearance.BorderColor = Color.Green;
                panel_INFO.Visible = false;
                panel_SETUP.Visible = true;

                AdjustBrightnessFactor(brightness_trackBar.Value / 50f);
                AdjustExposureFactor(Exposure_trackBar.Value / 50f);
                AdjustContrastFactor(Contrast_trackBar.Value / 50f);
                AdjustSharpnessFactor(sharpness_trackBar.Value / 50f);
                AdjustSaturationFactor(saturation_trackBar.Value / 50f);
                AdjustHighlightFactor(Highlights_trackBar.Value / 50f);
            }
            else
            {
                MessageBox.Show("Password Incorrect");
            }
        }
        private void setup_Click(object sender, EventArgs e)
        {
            Password_textBox.Text = string.Empty;

            panel_password.Visible = true;
            panel_Home.Visible = false;
            panel_INFO.Visible = false;
            panel_SETUP.Visible = false;
            this.Text = "Home";
            setup.BackColor = Color.Gray;
            Info.BackColor = Color.Gray;
            Home.BackColor = Color.Aquamarine;
            setup.FlatAppearance.BorderSize = 1;
            setup.FlatAppearance.BorderColor = Color.Gray;
        }
        private void AdjustImageFactor(ref float factor, float newValue)
        {
            factor = newValue;
            UpdateImage();
        }        
        private void AdjustExposureFactor(float factor) => AdjustImageFactor(ref exposureFactor, factor);
        private void AdjustBrightnessFactor(float factor) => AdjustImageFactor(ref brightnessFactor, factor);
        private void AdjustHighlightFactor(float factor) => AdjustImageFactor(ref highlightFactor, factor);
        private void AdjustContrastFactor(float factor) => AdjustImageFactor(ref contrastFactor, factor);
        private void AdjustSharpnessFactor(float factor) => AdjustImageFactor(ref sharpnessFactor, factor);
        private void AdjustSaturationFactor(float factor) => AdjustImageFactor(ref saturationFactor, factor);

        private void UpdateImage()
        {
            if (click && pictureBox_cam.Image != null)
            {
                Bitmap originalBitmap = (Bitmap)pictureBox_cam.Image;
                Bitmap adjustedBitmap = effects(originalBitmap);

                if (flipX || flipY)
                {
                    using (Graphics g = Graphics.FromImage(adjustedBitmap))
                    {
                        if (flipX)
                        {
                            g.ScaleTransform(-1, 1);
                            g.TranslateTransform(-adjustedBitmap.Width, 0);
                        }
                        if (flipY)
                        {
                            g.ScaleTransform(1, -1);
                            g.TranslateTransform(0, -adjustedBitmap.Height);
                        }

                        g.DrawImage(adjustedBitmap, new Point(0, 0));
                    }
                }
                pictureBox_cam.Image = adjustedBitmap;
                originalBitmap.Dispose();
            }
        }
        private void brightness_trackBar_Scroll(object sender, EventArgs e)
        {
            float newBrightnessFactor = brightness_trackBar.Value / 50f;
            AdjustBrightnessFactor(newBrightnessFactor);
            float brightnessPercentage = (brightness_trackBar.Value / (float)brightness_trackBar.Maximum) * 100;
            Bright_textBox.Text = $"{brightnessPercentage}%";
        }
        private void Exposure_trackBar_Scroll(object sender, EventArgs e)
        {
            float newExposureFactor = Exposure_trackBar.Value / 50f;
            AdjustExposureFactor(newExposureFactor);
            float exposurePercentage = (Exposure_trackBar.Value / (float)Exposure_trackBar.Maximum) * 100;
            exposure_textBox.Text = $"{exposurePercentage}%";
        }
        private void Contrast_trackBar_Scroll(object sender, EventArgs e)
        {
            float newContrastFactor = Contrast_trackBar.Value / 50f;
            AdjustContrastFactor(newContrastFactor);
            float contrastPercentage = (Contrast_trackBar.Value / (float)Contrast_trackBar.Maximum) * 100;
            contrast_textBox.Text = $"{contrastPercentage}%";
        }
        private void sharpness_trackBar_Scroll(object sender, EventArgs e)
        {
            float newSharpnessFactor = sharpness_trackBar.Value / 50f;
            AdjustSharpnessFactor(newSharpnessFactor);
            float sharpnessPercentage = (sharpness_trackBar.Value / (float)sharpness_trackBar.Maximum) * 100;
            sharpness_textBox.Text = $"{sharpnessPercentage}%";
        }
        private void saturation_trackBar_Scroll(object sender, EventArgs e)
        {
            float newSaturationFactor = saturation_trackBar.Value / 50f;
            AdjustSaturationFactor(newSaturationFactor);
            float saturationPercentage = (saturation_trackBar.Value / (float)saturation_trackBar.Maximum) * 100;
            saturation_textBox.Text = $"{saturationPercentage}%";
        }
        private void Highlights_trackBar_Scroll(object sender, EventArgs e)
        {
            float newHighlightFactor = Highlights_trackBar.Value / 50f;
            AdjustHighlightFactor(newHighlightFactor);
            float highlightPercentage = (Highlights_trackBar.Value / (float)Highlights_trackBar.Maximum) * 100;
            highlight_textBox.Text = $"{highlightPercentage}%";
        }
        private void saveeffects_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("Do you want to save the effect settings?", "Save Effects", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    string folderPath = @"C:\Users\User\Desktop\Mansoor_Project\KML_Desk_Project\KML_Desk_Project\bin\Debug\effects_details";

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = $"effects_{DateTime.Now:yyyyMMdd}.xml";

                    string filePath = Path.Combine(folderPath, fileName);

                    XDocument xmlDoc;
                    if (File.Exists(filePath))
                    {
                        xmlDoc = XDocument.Load(filePath);

                        xmlDoc.Root.Add(new XComment("---------------------------------------------------"));

                        xmlDoc.Root.Add(
                            new XElement("EffectsSettings",
                                new XAttribute("Time", DateTime.Now.ToString("HH:mm:ss")),
                                new XElement("Brightness", brightness_trackBar.Value),
                                new XElement("Exposure", Exposure_trackBar.Value),
                                new XElement("Contrast", Contrast_trackBar.Value),
                                new XElement("Sharpness", sharpness_trackBar.Value),
                                new XElement("Saturation", saturation_trackBar.Value),
                                new XElement("Highlights", Highlights_trackBar.Value)
                            )
                        );
                    }
                    else
                    {
                        xmlDoc = new XDocument(
                            new XElement("EffectsSettingsList",
                                new XElement("EffectsSettings",
                                    new XAttribute("Time", DateTime.Now.ToString("HH:mm:ss")),
                                    new XElement("Brightness", brightness_trackBar.Value),
                                    new XElement("Exposure", Exposure_trackBar.Value),
                                    new XElement("Contrast", Contrast_trackBar.Value),
                                    new XElement("Sharpness", sharpness_trackBar.Value),
                                    new XElement("Saturation", saturation_trackBar.Value),
                                    new XElement("Highlights", Highlights_trackBar.Value)
                                )
                            )
                        );
                    }
                    xmlDoc.Save(filePath);
                    MessageBox.Show("Effect settings saved successfully!", "Save Effects", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Save operation canceled.", "Save Effects", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void FlipX_Click(object sender, EventArgs e)
        {
            flipX = !flipX;
            UpdateImage();
        }
        private void FlipY_Click(object sender, EventArgs e)
        {
            flipY = !flipY;
            UpdateImage();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            SaveState();
            base.OnFormClosing(e);
        }
        private void SaveState()
        {
            string filePath = @"C:\Users\User\Desktop\Mansoor_Project\KML_Desk_Project\KML_Desk_Project\bin\Debug\trackbar_reset\state.xml";

            XDocument xmlDoc = new XDocument(
                new XElement("State",
                    new XElement("Brightness", brightness_trackBar.Value),
                    new XElement("Exposure", Exposure_trackBar.Value),
                    new XElement("Contrast", Contrast_trackBar.Value),
                    new XElement("Sharpness", sharpness_trackBar.Value),
                    new XElement("Saturation", saturation_trackBar.Value),
                    new XElement("Highlights", Highlights_trackBar.Value),
                    new XElement("FlipX", flipX),
                    new XElement("FlipY", flipY),
                    new XElement("ZoomFactor", zoomFactor),
                    new XElement("PictureBoxSize", new XElement("Width", pictureBox_cam.Size.Width), new XElement("Height", pictureBox_cam.Size.Height)),
                    new XElement("PictureBoxLocation", new XElement("X", pictureBox_cam.Location.X), new XElement("Y", pictureBox_cam.Location.Y))
                )
            );
            xmlDoc.Save(filePath);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadState();
        }
        private void LoadState()
        {
            string filePath = @"C:\Users\User\Desktop\Mansoor_Project\KML_Desk_Project\KML_Desk_Project\bin\Debug\trackbar_reset\state.xml";

            if (File.Exists(filePath))
            {
                XDocument xmlDoc = XDocument.Load(filePath);

                // Load the values
                brightness_trackBar.Value = int.Parse(xmlDoc.Root.Element("Brightness").Value);
                Exposure_trackBar.Value = int.Parse(xmlDoc.Root.Element("Exposure").Value);
                Contrast_trackBar.Value = int.Parse(xmlDoc.Root.Element("Contrast").Value);
                sharpness_trackBar.Value = int.Parse(xmlDoc.Root.Element("Sharpness").Value);
                saturation_trackBar.Value = int.Parse(xmlDoc.Root.Element("Saturation").Value);
                Highlights_trackBar.Value = int.Parse(xmlDoc.Root.Element("Highlights").Value);

                flipX = bool.Parse(xmlDoc.Root.Element("FlipX").Value);
                flipY = bool.Parse(xmlDoc.Root.Element("FlipY").Value);

                zoomFactor = float.Parse(xmlDoc.Root.Element("ZoomFactor").Value);

                // Load PictureBox size and location
                int pictureBoxWidth = int.Parse(xmlDoc.Root.Element("PictureBoxSize").Element("Width").Value);
                int pictureBoxHeight = int.Parse(xmlDoc.Root.Element("PictureBoxSize").Element("Height").Value);
                pictureBox_cam.Size = new Size(pictureBoxWidth, pictureBoxHeight);

                int pictureBoxX = int.Parse(xmlDoc.Root.Element("PictureBoxLocation").Element("X").Value);
                int pictureBoxY = int.Parse(xmlDoc.Root.Element("PictureBoxLocation").Element("Y").Value);
                pictureBox_cam.Location = new Point(pictureBoxX, pictureBoxY);

                // Update text boxes
                Bright_textBox.Text = $"{(brightness_trackBar.Value / (float)brightness_trackBar.Maximum) * 100}%";
                contrast_textBox.Text = $"{(Contrast_trackBar.Value / (float)Contrast_trackBar.Maximum) * 100}%";
                exposure_textBox.Text = $"{(Exposure_trackBar.Value / (float)Exposure_trackBar.Maximum) * 100}%";
                sharpness_textBox.Text = $"{(sharpness_trackBar.Value / (float)sharpness_trackBar.Maximum) * 100}%";
                highlight_textBox.Text = $"{(Highlights_trackBar.Value / (float)Highlights_trackBar.Maximum) * 100}%";
                saturation_textBox.Text = $"{(saturation_trackBar.Value / (float)saturation_trackBar.Maximum) * 100}%";

                UpdateImage(); // Apply effects now
            }
        }
        private void resetall()
        {
            zoomFactor = 0.1f;

            pictureBox_cam.Size = originalPictureBoxSize;
            pictureBox_cam.Location = originalPictureBoxLocation;

            brightness_trackBar.Value = 50;
            Contrast_trackBar.Value = 50;
            Exposure_trackBar.Value = 50;
            sharpness_trackBar.Value = 0;
            saturation_trackBar.Value = 50;
            Highlights_trackBar.Value = 50;

            Bright_textBox.Text = "50%";
            contrast_textBox.Text = "50%";
            exposure_textBox.Text = "50%";
            sharpness_textBox.Text = "0%";
            highlight_textBox.Text = "50%";
            saturation_textBox.Text = "50%";

            brightnessFactor = 1.0f;
            contrastFactor = 1.0f;
            exposureFactor = 1.0f;
            highlightFactor = 1.0f;
            sharpnessFactor = 1.0f;
            saturationFactor = 1.0f;

            CLICK = true;
            flipX = false;
            flipY = false;

            // Apply the reset state
            UpdateImage();
        }

        private void Reset_Effects_Click(object sender, EventArgs e)
        {
            resetall();
        }
        private void EXIT_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Are you sure you want to EXIT", "EXIT Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
            else
            {
                return;
            }
        }
       
        private void exit_password_Click(object sender, EventArgs e)
        {
            panel_password.Visible = false;
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
        }
        private void fps_textBox_TextChanged(object sender, EventArgs e)
        {
        }
        private void Height_TextChanged(object sender, EventArgs e)
        {
        }
        private void Width_TextChanged(object sender, EventArgs e)
        {
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }
        private void label9_Click(object sender, EventArgs e)
        {
        }
        private void button1_Click(object sender, EventArgs e)
        {
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }
        private void Password_textBox_TextChanged(object sender, EventArgs e)
        {
        }
        private void label19_Click(object sender, EventArgs e)
        {
        }
        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
        }
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
        }
        private void Bright_textBox_TextChanged_1(object sender, EventArgs e)
        {
        }
        private void saturation_textBox_ChangeUICues(object sender, UICuesEventArgs e)
        {
        }
        private void pictureBox_OFFCAM_Click(object sender, EventArgs e)
        {
        }
        private void saturation_textBox_Click(object sender, EventArgs e)
        {
        }
        private void contrast_textBox_Click(object sender, EventArgs e)
        {
        }
        private void highlight_textBox_Click(object sender, EventArgs e)
        {
        }
        private void sharpness_textBox_Click(object sender, EventArgs e)
        {
        }
        private void Bright_textBox_Click(object sender, EventArgs e)
        {
        }
        private void exposure_textBox_Click(object sender, EventArgs e)
        {
        }
        private void trackBar_MouseWheel(object sender, MouseEventArgs e)
        {
        }
        private void trackBar_MouseEnter(object sender, EventArgs e)
        {
        }
        private void pictureBox_cam_Click(object sender, EventArgs e)
        {
        }
        private void label1_Click(object sender, EventArgs e)
        {
        }
        private void label3_Click(object sender, EventArgs e)
        {
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }
        private void Showpassword_CheckedChanged(object sender, EventArgs e)
        {
            Password_textBox.PasswordChar = Showpassword.Checked ? '\0' : '*';
        }
    }
}