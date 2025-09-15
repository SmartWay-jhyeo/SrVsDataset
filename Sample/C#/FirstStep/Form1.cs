using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MVSDK;
using CameraHandle = System.Int32;
using MvApi = MVSDK.MvApi;
using System.IO;
using System.Drawing.Imaging;

namespace FirstStep
{
    public partial class Form1 : Form
    {
        #region variable
        protected IntPtr m_Grabber = IntPtr.Zero;
        protected CameraHandle m_hCamera = 0;
        protected tSdkCameraDevInfo m_DevInfo;
        protected ColorPalette m_GrayPal;
        protected pfnCameraGrabberFrameCallback m_FrameCallback;
        #endregion

        public Form1()
        {
            InitializeComponent();

            m_FrameCallback = new pfnCameraGrabberFrameCallback(CameraGrabberFrameCallback);
            InitCamera();

            comboBox1.SelectedIndex = 0;
        }

        private void InitCamera()
        {
            CameraSdkStatus status = 0;

            tSdkCameraDevInfo[] DevList;
            MvApi.CameraEnumerateDevice(out DevList);
            int NumDev = (DevList != null ? DevList.Length : 0);
            if (NumDev < 1)
            {
                MessageBox.Show("Camera not scanned");
                return;
            }
            else if (NumDev == 1)
            {
                status = MvApi.CameraGrabber_Create(out m_Grabber, ref DevList[0]);
            }
            else
            {
                status = MvApi.CameraGrabber_CreateFromDevicePage(out m_Grabber);
            }

            if (status == 0)
            {
                MvApi.CameraGrabber_GetCameraDevInfo(m_Grabber, out m_DevInfo);
                MvApi.CameraGrabber_GetCameraHandle(m_Grabber, out m_hCamera);
                MvApi.CameraCreateSettingPage(m_hCamera, this.Handle, m_DevInfo.acFriendlyName, null, (IntPtr)0, 0);

                MvApi.CameraGrabber_SetRGBCallback(m_Grabber, m_FrameCallback, IntPtr.Zero);

                // black and white camera settings ISP output grayscale image
                // Color Camera ISP will output BGR24 image by default
                tSdkCameraCapbility cap;
                MvApi.CameraGetCapability (m_hCamera, out cap);
                if (cap.sIspCapacity.bMonoSensor != 0)
                {
                    MvApi.CameraSetIspOutFormat (m_hCamera, (uint) MVSDK.emImageFormat.CAMERA_MEDIA_TYPE_MONO8);

                    // Create grayscale palette
                    Bitmap Image = new Bitmap (1, 1, PixelFormat.Format8bppIndexed);
                    m_GrayPal = Image.Palette;
                    for (int Y = 0; Y <m_GrayPal.Entries.Length; Y ++)
                        m_GrayPal.Entries [Y] = Color.FromArgb (255, Y, Y, Y);
                }

                // set VFlip, because the data output by the SDK is from bottom to top by default, VFlip can be directly converted to Bitmap
                MvApi.CameraSetMirror (m_hCamera, 1, 1);

                // To illustrate how to use the camera data to create a Bitmap in a callback and display it in a PictureBox, we do not use the SDK's built-in drawing operations
                //MvApi.CameraGrabber_SetHWnd(m_Grabber, this.DispWnd.Handle);

                MvApi.CameraGrabber_StartLive(m_Grabber);
            }
            else
            {
                MessageBox.Show(String.Format("Failed to open the camera, reason:{0}", status));
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            MvApi.CameraGrabber_Destroy(m_Grabber);
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            if (m_Grabber != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera, 1);
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (m_Grabber != IntPtr.Zero)
                MvApi.CameraGrabber_StartLive(m_Grabber);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (m_Grabber != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber);
        }

        private void buttonSnap_Click(object sender, EventArgs e)
        {
            if (m_Grabber != IntPtr.Zero)
            {
                IntPtr Image;
                if (MvApi.CameraGrabber_SaveImage(m_Grabber, out Image, 2000) == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    string filename = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory.ToString(), 
                        string.Format("{0}.bmp", System.Environment.TickCount));

                    MvApi.CameraImage_SaveAsBmp(Image, filename);

                    MvApi.CameraImage_Destroy(Image);

                    MessageBox.Show(filename);
                }
                else
                {
                    MessageBox.Show("Snap failed");
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_Grabber != IntPtr.Zero)
            {
                tSdkGrabberStat stat;
                MvApi.CameraGrabber_GetStat(m_Grabber, out stat);
                string info = String.Format("| Resolution:{0}*{1} | DispFPS:{2} | CapFPS:{3} |", 
                    stat.Width, stat.Height, stat.DispFps, stat.CapFps);
                StateLabel.Text = info;
            }
        }

        private void CameraGrabberFrameCallback(
            IntPtr Grabber,
            IntPtr pFrameBuffer,
            ref tSdkFrameHead pFrameHead,
            IntPtr Context)
        {
            // data processing callback

            // As the monochrome camera sets the ISP output grayscale image after the camera is turned on
            // So here pFrameBuffer = 8 bit grayscale data
            // otherwise BGR24 data will be output like a color camera

            // Color Camera ISP will output BGR24 image by default
            // pFrameBuffer = BGR24 data

            // Execute GC once to free memory
            GC.Collect();

            int w = pFrameHead.iWidth;
            int h = pFrameHead.iHeight;
            Boolean gray = (pFrameHead.uiMediaType == (uint)MVSDK.emImageFormat.CAMERA_MEDIA_TYPE_MONO8); 
            Bitmap Image = new Bitmap(w, h, 
                gray ? w : w * 3, 
                gray ? PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb, 
                pFrameBuffer);

            // If the grayscale to set the color palette
            if (gray)
            {
                Image.Palette = m_GrayPal;
            }

            this.Invoke((EventHandler)delegate
            {
                DispWnd.Image = Image;
                DispWnd.Refresh();
            });
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetIOState(m_hCamera, 0, checkBox1.Checked ? (uint)1 : (uint)0);
        }

        private void SetPWMState()
        {
            uint brightness = (uint)numericUpDown1.Value;
            uint freqSel = (uint)comboBox1.SelectedIndex;
            if (checkBox2.Checked)
                freqSel |= 0x80000000;
            MvApi.CameraSetOutPutPWM(m_hCamera, 1, freqSel, brightness);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SetPWMState();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetPWMState();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            SetPWMState();
        }
    }
}
