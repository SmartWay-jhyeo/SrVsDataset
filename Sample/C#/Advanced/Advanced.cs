using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using MVSDK;
using Snapshot; 
using CameraHandle = System.Int32;
using MvApi = MVSDK.MvApi;
using System.IO;



namespace Basic
{
    

    public partial class Advanced : Form
    {
        

        #region variable
        protected CameraHandle m_hCamera = 0;   // handle
        protected IntPtr m_ImageBuffer;         // Preview channel RGB image cache
        protected IntPtr m_ImageBufferSnapshot; // capture channel RGB image cache
        protected tSdkCameraCapbility tCameraCapability; // Camera characterization
        protected int m_iDisplayedFrames = 0; // The total number of frames already displayed
        protected CAMERA_SNAP_PROC m_CaptureCallback;
        protected IntPtr m_iCaptureCallbackCtx; // contextual parameter of image callback function
        protected Thread m_tCaptureThread; // Image capture thread
        protected bool m_bExitCaptureThread = false; // Use threads to collect, let the thread exit the mark
        protected IntPtr m_iSettingPageMsgCallbackCtx; // camera configuration interface message callback function context parameters
        protected tSdkFrameHead m_tFrameHead;
        protected SnapshotDlg m_DlgSnapshot = new SnapshotDlg (); // Display window for capturing images
        protected Settings      m_DlgSettings = new Settings();
        protected bool          m_bEraseBk = false;
        protected bool          m_bSaveImage = false;
        #endregion

        public Advanced()
        {
            InitializeComponent();

            if (InitCamera() == true)
            {
                MvApi.CameraPlay(m_hCamera);
                BtnPlay.Text = "Pause";
            }

        }
       

#if USE_CALL_BACK
        public void ImageCaptureCallback(CameraHandle hCamera, IntPtr pFrameBuffer, ref tSdkFrameHead pFrameHead, IntPtr pContext)
        {
            //Image processing, the original output is converted to RGB format bitmap data, while overlay white balance, saturation, LUT ISP processing.
            MvApi.CameraImageProcess(hCamera, pFrameBuffer, m_ImageBuffer, ref pFrameHead);
            //Overlay reticle, auto exposure window, white balance window information (only superimposed set to visible). 
            MvApi.CameraImageOverlay(hCamera, m_ImageBuffer, ref pFrameHead);
            //Call the SDK package interface, preview image
            MvApi.CameraDisplayRGB24(hCamera, m_ImageBuffer, ref pFrameHead);
            m_tFrameHead = pFrameHead;
            m_iDisplayedFrames++;

            if (pFrameHead.iWidth != m_tFrameHead.iWidth || pFrameHead.iHeight != m_tFrameHead.iHeight)
            {
                timer2.Enabled = true;
                timer2.Start();
                m_tFrameHead = pFrameHead;
            }
        }
#else
        public void CaptureThreadProc()
        {
            CameraSdkStatus eStatus;
            tSdkFrameHead FrameHead;
            IntPtr uRawBuffer;//The rawbuffer is applied internally by the SDK. Application layer do not call delete like release function
  
            while(m_bExitCaptureThread == false)
            {
                //500 milliseconds timeout, the image is not captured before the thread will be suspended, release the CPU, so the thread without calling sleep
                eStatus = MvApi.CameraGetImageBuffer(m_hCamera, out FrameHead, out uRawBuffer, 500);

                if (eStatus == CameraSdkStatus.CAMERA_STATUS_SUCCESS)//If the trigger mode, it may timeout
                {
                    //Image processing, the original output is converted to RGB format bitmap data, while overlay white balance, saturation, LUT ISP processing.
                    MvApi.CameraImageProcess(m_hCamera, uRawBuffer, m_ImageBuffer, ref FrameHead);
                    //Overlay reticle, auto exposure window, white balance window information (only superimposed set to visible).
                    MvApi.CameraImageOverlay(m_hCamera, m_ImageBuffer, ref FrameHead);
                    //Call the SDK package interface, preview image
                    MvApi.CameraDisplayRGB24(m_hCamera, m_ImageBuffer, ref FrameHead);
                    //After successful call CameraGetImageBuffer must be released, the next time you can continue to call CameraGetImageBuffer to capture the image.
                    MvApi.CameraReleaseImageBuffer(m_hCamera,uRawBuffer);

                    if (FrameHead.iWidth != m_tFrameHead.iWidth || FrameHead.iHeight != m_tFrameHead.iHeight)
                    {
                        m_bEraseBk = true;
                        m_tFrameHead = FrameHead;  
                    }

                    m_iDisplayedFrames++;

                    if (m_bSaveImage)
                    {
                        MvApi.CameraSaveImage(m_hCamera, "c:\\test.bmp", m_ImageBuffer, ref FrameHead, emSdkFileType.FILE_BMP, 100);
                        m_bSaveImage = false;
                    }
                }
           
            }
           
        }
#endif

        /* Camera callback function for window configuration
        hCamera: current camera handle
        MSG: Message Type,
            SHEET_MSG_LOAD_PARAM_DEFAULT = 0, // Load the default parameters of the button is clicked, load the default parameters to complete the trigger the message,
            SHEET_MSG_LOAD_PARAM_GROUP = 1, // trigger the message after the switch parameter group is completed,
            SHEET_MSG_LOAD_PARAM_FROMFILE = 2, // The Load Parameters button is clicked, which has been triggered after the camera parameters have been loaded from the file
            SHEET_MSG_SAVE_PARAM_GROUP = 3 // Save parameters button is clicked, the message is triggered after the parameters are saved
            For details, see the emSdkPropSheetMsg type in CameraDefine.h
        uParam: parameters attached to the message, different messages, parameters have different meanings.
            When MSG is SHEET_MSG_LOAD_PARAM_DEFAULT, uParam represents the index number loaded into the default parameter group, starting from 0, corresponding to the four groups of A, B, C and D
            When MSG is SHEET_MSG_LOAD_PARAM_GROUP, uParam represents the index number of the parameter group after the switch, starting from 0, corresponding to the four groups of A, B, C and D
            When MSG is SHEET_MSG_LOAD_PARAM_FROMFILE, uParam represents the index number of the parameter group covered by the parameter in the file, starting from 0, corresponding to the four groups of A, B, C and D, respectively
            When MSG is SHEET_MSG_SAVE_PARAM_GROUP, uParam represents the index number of the currently saved parameter group, starting from 0 and corresponding to the four groups of A, B, C and D, respectively
         */
        public void SettingPageMsgCalBack(CameraHandle hCamera, uint MSG, uint uParam, IntPtr pContext)
        {

        }

        private bool InitCamera()
        {
            CameraSdkStatus status;
            tSdkCameraDevInfo[] tCameraDevInfoList;
            IntPtr ptr;
            int i;
#if USE_CALL_BACK
            CAMERA_SNAP_PROC pCaptureCallOld = null;
#endif
            if (m_hCamera > 0)
            {
                //Has been initialized, returned directly

                return true;
            }

            status = MvApi.CameraEnumerateDevice(out tCameraDevInfoList);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                if (tCameraDevInfoList != null)//At this point iCameraCounts returned the actual number of connected cameras. If greater than 1, initialize the first camera
                {
                    status = MvApi.CameraInit(ref tCameraDevInfoList[0], -1,-1, ref m_hCamera);
                    if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    {
                        //Get the camera characterization
                        MvApi.CameraGetCapability(m_hCamera, out tCameraCapability);

                        m_ImageBuffer = Marshal.AllocHGlobal(tCameraCapability.sResolutionRange.iWidthMax * tCameraCapability.sResolutionRange.iHeightMax*3 + 1024);
                        m_ImageBufferSnapshot = Marshal.AllocHGlobal(tCameraCapability.sResolutionRange.iWidthMax * tCameraCapability.sResolutionRange.iHeightMax * 3 + 1024);

                        if (tCameraCapability.sIspCapacity.bMonoSensor != 0)
                        {
                            MvApi.CameraSetIspOutFormat(m_hCamera, (uint)MVSDK.emImageFormat.CAMERA_MEDIA_TYPE_MONO8);
                        }

                        //Initialize the display module, using the SDK's internal display interface
                        MvApi.CameraDisplayInit(m_hCamera, PreviewBox.Handle);
                        MvApi.CameraSetDisplaySize(m_hCamera, PreviewBox.Width, PreviewBox.Height);

                        //Set the capture channel resolution.
                        tSdkImageResolution tResolution;
                        tResolution.uSkipMode = 0;
                        tResolution.uBinAverageMode = 0;
                        tResolution.uBinSumMode = 0;
                        tResolution.uResampleMask  = 0;
                        tResolution.iVOffsetFOV = 0;
                        tResolution.iHOffsetFOV = 0;
                        tResolution.iWidthFOV  = tCameraCapability.sResolutionRange.iWidthMax;
                        tResolution.iHeightFOV = tCameraCapability.sResolutionRange.iHeightMax;
                        tResolution.iWidth = tResolution.iWidthFOV;
                        tResolution.iHeight = tResolution.iHeightFOV;
                        //tResolution.iIndex = 0xff; represents the custom resolution if tResolution.iWidth and tResolution.iHeight
                        //Defined as 0, then follow the preview channel to capture the resolution. Snapshot channel resolution can be dynamically changed.
                        //In this example, the capture resolution is fixed to the maximum resolution.
                        tResolution.iIndex = 0xff;
                        tResolution.acDescription = new byte[32];//Descriptive information may not be set
                        tResolution.iWidthZoomHd = 0;
                        tResolution.iHeightZoomHd = 0;
                        tResolution.iWidthZoomSw = 0;
                        tResolution.iHeightZoomSw = 0;
                     
                        MvApi.CameraSetResolutionForSnap(m_hCamera, ref tResolution);

                        //Have the SDK dynamically create the camera's configuration window based on the camera's model.
                        MvApi.CameraCreateSettingPage(m_hCamera,this.Handle,tCameraDevInfoList[0].acFriendlyName,/*SettingPageMsgCalBack*/null,/*m_iSettingPageMsgCallbackCtx*/(IntPtr)null,0);

                        // Two ways to get preview image, set callback function or use timer or independent thread mode,
                        // take the initiative to call CameraGetImageBuffer interface to capture.
                        // This example demonstrates only two ways, note that the two ways can also be used at the same time, but in the callback function,
                        // Do not use CameraGetImageBuffer, otherwise it will cause deadlock.
#if USE_CALL_BACK
                        m_CaptureCallback = new CAMERA_SNAP_PROC(ImageCaptureCallback);
                        MvApi.CameraSetCallbackFunction(m_hCamera, m_CaptureCallback, m_iCaptureCallbackCtx, ref pCaptureCallOld);
#else // If you need to use multi-threaded, use the following way
                        m_bExitCaptureThread = false;
                        m_tCaptureThread = new Thread(new ThreadStart(CaptureThreadProc));
                        m_tCaptureThread.Start();

#endif
                        //MvApi.CameraReadSN and MvApi.CameraWriteSN used to read and write from the camera user-defined serial number or other data, 32 bytes
                        //MvApi.CameraSaveUserData and MvApi.CameraLoadUserData are used to read custom data from the camera, 512 bytes


                        m_DlgSettings.m_hCamera = m_hCamera;
                        return true;

                    }
                    else
                    {
                        m_hCamera = 0;
                        StateLabel.Text = "Camera init error";
                        String errstr = string.Format("Camera initialization error, error code {0}, error reason", status);
                        String errstring = MvApi.CameraGetErrorString(status);
                        // string str1
                        MessageBox.Show(errstr + errstring, "ERROR");
                        Environment.Exit(0);
                        return false;
                    }


                }
            }
            else
            {
                MessageBox.Show("did not find the camera, if you have connected to the camera may not be sufficient authority, try to run the program with administrator privileges.");
                Environment.Exit(0);
            }

            return false;
        
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (m_hCamera < 1)// The camera has not been initialized yet
            {
                if (InitCamera() == true)
                {
                    MvApi.CameraPlay(m_hCamera);
                    BtnPlay.Text = "Pause";
                }
            }
            else//Has been initialized
            {
                if (BtnPlay.Text == "Play")
                {
                    MvApi.CameraPlay(m_hCamera);
                    BtnPlay.Text = "Pause";
                }
                else
                {
                    MvApi.CameraPause(m_hCamera);
                    BtnPlay.Text = "Play";
                }
            }
        }

        private void BasicForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_hCamera > 0)
            {
#if !USE_CALL_BACK //The use of callback function does not need to stop the thread
                m_bExitCaptureThread = true;
                while (m_tCaptureThread.IsAlive)
                {
                    Thread.Sleep(10);
                }
#endif
                MvApi.CameraUnInit(m_hCamera);
                Marshal.FreeHGlobal(m_ImageBuffer);
                Marshal.FreeHGlobal(m_ImageBufferSnapshot);
                m_hCamera = 0;
            }

        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            if (m_hCamera > 0)
            {
                m_DlgSettings.Show();
            }
        }

        // 1 second update video information
        private void timer1_Tick(object sender, EventArgs e)
        {
            tSdkFrameStatistic tFrameStatistic;
            if (m_hCamera > 0)
            {
                //Get the SDK image frame statistics, capture frames, error frames and so on.
                MvApi.CameraGetFrameStatistic(m_hCamera, out tFrameStatistic);
                //Show the frame rate of the application has its own record.
                string sFrameInfomation = String.Format("| Resolution:{0}*{1} | Display frames{2} | Capture frames{3} |", m_tFrameHead.iWidth, m_tFrameHead.iHeight, m_iDisplayedFrames, tFrameStatistic.iCapture);
                StateLabel.Text = sFrameInfomation;
                
            }
            else
            {
                StateLabel.Text = "";
            }
        }

        //Used for refreshing the background drawing when switching resolution
        private void timer2_Tick(object sender, EventArgs e)
        {
            //After switching the resolution, erase the background once
            if (m_bEraseBk == true)
            {
                m_bEraseBk = false;
                PreviewBox.Refresh();
            }
        }

        private void BtnSnapshot_Click(object sender, EventArgs e)
        {
            tSdkFrameHead tFrameHead;
            IntPtr uRawBuffer;//The SDK allocates memory for RAW data and releases it
           
                          
            if (m_hCamera <= 0)
            {
                return;//The camera has not been initialized yet, the handle is invalid
            }

            // CameraSnapToBuffer will switch the resolution to take pictures, slower. Do real-time processing, it is recommended to use CameraGetImageBuffer function to take a picture or callback function.
            if (MvApi.CameraSnapToBuffer(m_hCamera, out tFrameHead, out uRawBuffer,500) == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {

                // Convert the raw data from the camera to RGB format into memory m_ImageBufferSnapshot
                MvApi.CameraImageProcess(m_hCamera, uRawBuffer, m_ImageBufferSnapshot, ref tFrameHead);
                // CameraSnapToBuffer must be successfully released CameraReleaseImageBuffer release SDK allocated RAW data buffer
                // Otherwise, a deadlock will occur, the preview and snap channels will be blocked until CameraReleaseImageBuffer is called and unlocked.
                MvApi.CameraReleaseImageBuffer(m_hCamera, uRawBuffer);
                // update snapshot display window.
                m_DlgSnapshot.UpdateImage(ref tFrameHead, m_ImageBufferSnapshot);
                m_DlgSnapshot.Show(); 
            }
        }

        private void BasicForm_Load(object sender, EventArgs e)
        {

        }

        private void SaveImage_Click(object sender, EventArgs e)
        {
            m_bSaveImage = true;// notice preview thread, save a picture. You can also refer to Snapshot in BtnSnapshot_Click, re-grab a picture, and then call MvApi.CameraSaveImage for picture saving.
        }

    }
}