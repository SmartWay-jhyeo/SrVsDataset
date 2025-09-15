using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MVSDK;
using CameraHandle = System.Int32;
using MvApi = MVSDK.MvApi;
using System.Runtime.InteropServices;


namespace Basic
{
    public partial class Settings : Form
    {
        public CameraHandle m_hCamera; // camera handle
        private int m_iResolutionIndex = 0; // Select the default resolution index number
        private tSdkImageResolution m_tRoiResolution; // user-defined resolution
        private bool m_bInited = false;

        public Settings()
        {
            InitializeComponent();
        }

        private void trackBar_RedGain_Scroll(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            int r = trackBar_RedGain.Value;
            int g = trackBar_GreenGain.Value;
            int b = trackBar_BlueGain.Value;

            // After the scroll to update the value of the left input box
            textBox_RedGain.Text = r.ToString();
            textBox_GreenGain.Text = g.ToString();
            textBox_BlueGain.Text = b.ToString();

            MvApi.CameraSetGain(m_hCamera, r, g, b);
        }

        private void textBox_RedGain_TextChanged(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_RedGain.Text == ""
                || textBox_GreenGain.Text == ""
                || textBox_BlueGain.Text == ""
                )
            {
                return;
            }

            string s1 = textBox_RedGain.Text;
            int r = Convert.ToInt32(s1);
            
            string s2 = textBox_GreenGain.Text;
            int g = Convert.ToInt32(s2);

            string s3 = textBox_BlueGain.Text;
            int b = Convert.ToInt32(s3);

            // Change the value in the input box to update the scroll bar.
            trackBar_RedGain.Value = r;
            trackBar_GreenGain.Value = g;
            trackBar_BlueGain.Value = b;

            MvApi.CameraSetGain(m_hCamera, r, g, b);
        }

        // button1_Click One button white balance for color camera.
        private void button1_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetOnceWB(m_hCamera);

            // with the value of the new RGB control
            UpdateRgbGainControls();

            this.Invalidate();

        }


        // Saturation scroll bar event
        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }
            int saturation = trackBar4.Value;
            textBox_Saturation.Text = saturation.ToString();
            MvApi.CameraSetSaturation(m_hCamera, saturation);
        }

        // Saturation input box event
        private void textBox_Saturation_TextChanged(object sender, EventArgs e)
        {
            
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_Saturation.Text == "")
            {
                return;
            }

            int saturation = Convert.ToInt32(textBox_Saturation.Text);
            trackBar4.Value = saturation;
            MvApi.CameraSetSaturation(m_hCamera, saturation);
        }

        private void checkBox_MonoMode_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_MonoMode.Checked)
            {
                MvApi.CameraSetMonochrome(m_hCamera, 1);
            }
            else{
                MvApi.CameraSetMonochrome(m_hCamera, 0);
            }

        }

        private void checkBox_InverseImage_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_InverseImage.Checked)
            {
                MvApi.CameraSetInverse(m_hCamera, 1);
            }
            else
            {
                MvApi.CameraSetInverse(m_hCamera, 0);
            }
        }

        // Adjust by scroll bar contrast
        private void trackBar_Contrast_Scroll(object sender, EventArgs e)
        {
             if (this.ActiveControl != sender)
             {
                 return;
             }

             int contrast = trackBar_Contrast.Value;
           
             // After the scroll to update the value of the left input box
             textBox_Contrast.Text = contrast.ToString ();
            
             MvApi.CameraSetContrast (m_hCamera, contrast);
        }

        // Text input box contrast adjustment
        private void textBox_Contrast_TextChanged(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_Contrast.Text == "")
            {
                return;
            }

            int contrast = Convert.ToInt32(textBox_Contrast.Text);
            trackBar_Contrast.Value = contrast;
            MvApi.CameraSetContrast(m_hCamera, contrast);
        }

        // Adjust the gamma by the scroll bar
        private void trackBar_Gamma_Scroll(object sender, EventArgs e)
        {
             if (this.ActiveControl != sender)
             {
                 return;
             }

             int gamma = trackBar_Gamma.Value;
             double fGamma = ((double) gamma) / 100.0; // The value of gamma in SDK ranges from 0 to 1000, corresponding to 0 to 10.0 on the interface. The gamma of 1.0 is the original value
             // After the scroll to update the value of the left input box
             textBox_Gamma.Text = fGamma.ToString ();

             MvApi.CameraSetGamma (m_hCamera, gamma);
        }

        // Adjust the gamma through the text box
        private void textBox_Gamma_TextChanged(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_Gamma.Text == "")
            {
                return;
            }

            double fGamma = Convert.ToDouble(textBox_Gamma.Text);
            int gamma = (int)(fGamma * 100.0);// The gamma of the SDK ranges from 0 to 1000, corresponding to 0 to 10.0 on the interface. The gamma of 1.0 is the original value
            trackBar_Contrast.Value = gamma;
            MvApi.CameraSetGamma(m_hCamera, gamma);
        }

        // Adjust the sharpness with the scroll bar
        private void trackBar_Sharpness_Scroll(object sender, EventArgs e)
        {
             if (this.ActiveControl != sender)
             {
                 return;
             }
             int sharpness = trackBar_Sharpness.Value;

             // After the scroll to update the value of the left input box
             textBox_Sharpness.Text = sharpness.ToString ();

             MvApi.CameraSetSharpness (m_hCamera, sharpness);
        }
        // Adjust the sharpness with the text box
        private void textBox_Sharpness_TextChanged(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_Sharpness.Text == "")
            {
                return;
            }

            int sharpness = Convert.ToInt32(textBox_Sharpness.Text);
            trackBar_Sharpness.Value = sharpness;
            MvApi.CameraSetSharpness(m_hCamera, sharpness);
        }

        // 2D noise reduction
        private void checkBox_2DDenoise_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_2DDenoise.Checked == true)
            {
                MvApi.CameraSetNoiseFilter(m_hCamera, 1);
            }
            else
            {
                MvApi.CameraSetNoiseFilter(m_hCamera, 0);
            }
        }

        // 3D noise reduction
        private void comboBox_3DDenoise_SelectedIndexChanged(object sender, EventArgs e)
        {
            int counts = comboBox_3DDenoise.SelectedIndex;

            if (counts == 0)//Disabled
            {
                MvApi.CameraSetDenoise3DParams(m_hCamera, 0, 0,null);

            }
            else{
                MvApi.CameraSetDenoise3DParams(m_hCamera, 1, counts + 1, null);
            }
            
        }

        // flip the image horizontally once
        private void checkBox_HFlip_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_HFlip.Checked == true)
            {
                MvApi.CameraSetMirror(m_hCamera, 0, 1);
            }
            else{
                MvApi.CameraSetMirror(m_hCamera, 0, 0);
            }
        }

        // The image is flipped vertically
        private void checkBox_VFlip_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_VFlip.Checked == true)
            {
                MvApi.CameraSetMirror(m_hCamera, 1, 1);
            }
            else
            {
                MvApi.CameraSetMirror(m_hCamera, 1, 0);
            }
        }

        // The image is flipped 90 degrees
        private void radioButton_Rotate90_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetRotate(m_hCamera, 1);
        }
        // flip the image 180 degrees
        private void radioButton_Rotate180_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetRotate(m_hCamera, 2);
        }
        // Image flip 270 degrees
        private void radioButton__Rotate270_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetRotate(m_hCamera, 3);
        }
        // image flip prohibited
        private void radioButton_Forbbiden_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetRotate(m_hCamera, 0);
        }

        // Set as the default resolution
        private void radioButton_ResolutionPreset_CheckedChanged(object sender, EventArgs e)
        {

            
            tSdkImageResolution t;
            MvApi.CameraGetImageResolution(m_hCamera, out t);
            t.iIndex = m_iResolutionIndex;// switch the default resolution, just set the index value on the line. The remaining value can be ignored, or fill 0
            MvApi.CameraSetImageResolution(m_hCamera,ref t);
            UpdateResolution();
        }

        // Set to custom resolution
        private void radioButton_ResolutionROI_CheckedChanged(object sender, EventArgs e)
        {

            MvApi.CameraSetImageResolution(m_hCamera, ref m_tRoiResolution);
            UpdateResolution();
        }

        // Select a preset resolution to set
        private void comboBox_RresPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
             m_iResolutionIndex = comboBox_RresPreset.SelectedIndex;

             tSdkImageResolution t;
             MvApi.CameraGetImageResolution (m_hCamera, out t);
             t.iIndex = m_iResolutionIndex; // switch the default resolution, only need to set the index value on the line.
             MvApi.CameraSetImageResolution (m_hCamera, ref t);

        }

        // Visualize custom resolution.
        private void button_ROI_Click(object sender, EventArgs e)
        {
            tSdkImageResolution t;
            CameraSdkStatus status;
            MvApi.CameraGetImageResolution(m_hCamera, out t);
            status = MvApi.CameraCustomizeResolution(m_hCamera, ref t);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                m_tRoiResolution = t;
                MvApi.CameraSetImageResolution(m_hCamera, ref m_tRoiResolution);
            }
        }

        // Set the camera acquisition mode, divided into continuous, soft trigger and hard trigger 3 modes. Either way, the captured images are all the same interface.
        private void comboBox_TriggerMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            int iMode = comboBox_TriggerMode.SelectedIndex;
            MvApi.CameraSetTriggerMode(m_hCamera, iMode); // 0 means continuous mode, 1 is soft trigger, 2 is hard trigger.
            button_SwTrigger.Enabled = (iMode == 1 ? true : false);
        }

        // Set the trigger mode of the external trigger signal, which is divided into two types: upper edge and lower edge.
        private void comboBox_ExtSignalMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            int iMode = comboBox_ExtSignalMode.SelectedIndex;
        }

        // Set the debounce time of the external trigger signal.
        private void button_SetJetterTime_Click(object sender, EventArgs e)
        {
            uint iJitterTime = System.Convert.ToUInt32(textBox_JetterTime.Text);
            MvApi.CameraSetExtTrigJitterTime(m_hCamera, iJitterTime);
        }

        // Set the mode of the flash to automatic. When the camera is working in the trigger mode, the exposure will automatically output the flash sync control signal.
        private void radioButton_StrobeModeAuto_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetStrobeMode(m_hCamera, (int)emStrobeControl.STROBE_SYNC_WITH_TRIG_AUTO);
        }

        // Set the flash mode to manual mode
        private void radioButton_StrobeModeManul_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetStrobeMode(m_hCamera, (int)emStrobeControl.STROBE_SYNC_WITH_TRIG_MANUAL);
        }

        // Set the effective polarity of the flash signal in semi-auto mode.
        private void comboBox_StrobePriority_SelectedIndexChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetStrobePolarity(m_hCamera, comboBox_StrobePriority.SelectedIndex);
        }

        // Set the delay time of the flash in semi-auto mode. The unit is microseconds
        private void button_SetStrobeDelay_Click(object sender, EventArgs e)
        {
            int iDelay = System.Convert.ToInt32(textBox_StrobeDelayTime.Text);
            MvApi.CameraSetStrobeDelayTime(m_hCamera, (uint)iDelay);
        }

        // Set the flash pulse width for the camera in semi-auto mode. Note that the unit is microseconds.
        private void button_SetStrobePulseWidth_Click(object sender, EventArgs e)
        {
            int iWidth;
            iWidth = System.Convert.ToInt32(textBox_StrobePulseWidth.Text);
            MvApi.CameraSetStrobePulseWidth(m_hCamera, (uint)iWidth);
        }

        // Modify the camera's analog gain value with the scroll bar
        private void textBox_AnalogGain_TextChanged(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            if (textBox_AnalogGain.Text == "")
            {
                return;
            }

            int iGain = System.Convert.ToInt32(textBox_AnalogGain.Text);
            trackBar_AnalogGain.Value = iGain;
           
            MvApi.CameraSetAnalogGain(m_hCamera, iGain);
        }

        // Modify the camera's analog gain value by entering the control
        private void trackBar_AnalogGain_Scroll(object sender, EventArgs e)
        {
            if (this.ActiveControl != sender)
            {
                return;
            }

            int iGain = trackBar_AnalogGain.Value;
            textBox_AnalogGain.Text = iGain.ToString ();

            MvApi.CameraSetAnalogGain (m_hCamera, iGain);
        }

        // Modify camera exposure parameters
        private void textBox_ExposureTime_TextChanged(object sender, EventArgs e)
        {
            if (textBox_ExposureTime.Text == "")
            {
                return;
            }

            double dExpTime = System.Convert.ToDouble(textBox_ExposureTime.Text);
            MvApi.CameraSetExposureTime(m_hCamera, dExpTime);
        }



        private void UpdateResolution()
        {
            tSdkImageResolution tRes;
            MvApi.CameraGetImageResolution(m_hCamera, out tRes);


            if (tRes.iIndex == 0xff) // 0xff means custom resolution
            {
                
                comboBox_RresPreset.Enabled = false;
                button_ROI.Enabled = true;
            }
            else
            {
               
                comboBox_RresPreset.Enabled = true;
                button_ROI.Enabled = false;
                m_iResolutionIndex = tRes.iIndex;
            }


        }

        private void UpdateRgbGainControls()
        {
            int r, g, b;
            r = g = b = 0;
            MvApi.CameraGetGain(m_hCamera, ref r, ref g, ref b);

            trackBar_RedGain.Value = r;
            trackBar_GreenGain.Value = g;
            trackBar_BlueGain.Value = b;

            textBox_RedGain.Text = r.ToString();
            textBox_GreenGain.Text = g.ToString();
            textBox_BlueGain.Text = b.ToString();

        }

        // According to the camera's current parameters, refresh the interface controls, to be synchronized.
        public void UpdateControls()
        {
            // According to the camera description information, set the parameters of the control adjustment range.
            tSdkCameraCapbility tCameraDeviceInfo;
            MvApi.CameraGetCapability(m_hCamera, out tCameraDeviceInfo);

            // get the three digital RGB gain update interface
            trackBar_RedGain.SetRange(0, 399);
            trackBar_GreenGain.SetRange(0, 399);
            trackBar_BlueGain.SetRange(0, 399);
            UpdateRgbGainControls();

            //saturation
            int saturation = 0;
            MvApi.CameraGetSaturation(m_hCamera, ref saturation);
            trackBar4.SetRange(0, 100);
            trackBar4.Value = saturation;
            textBox_Saturation.Text = saturation.ToString();

            // color to black and white mode
            uint bEnable = 0;
            MvApi.CameraGetMonochrome(m_hCamera, ref bEnable);
            checkBox_MonoMode.Checked = (bEnable == 1 ? true : false);

            // anti-color mode
            MvApi.CameraGetInverse(m_hCamera, ref bEnable);
            checkBox_InverseImage.Checked = (bEnable == 1 ? true : false);

            // contrast
            int contrast = 0;
            MvApi.CameraGetContrast(m_hCamera, ref contrast);
            trackBar_Contrast.SetRange(0, 200);
            trackBar_Contrast.Value = contrast;
            textBox_Contrast.Text = contrast.ToString();

            // resolution
            comboBox_RresPreset.Items.Clear();

            // populate the resolution list
            tSdkImageResolution[] infos = new tSdkImageResolution[tCameraDeviceInfo.iImageSizeDesc]; 
            IntPtr ptr = tCameraDeviceInfo.pImageSizeDesc;
            for (int i = 0; i < infos.Length; i++)  
            {  
                infos[i] = (tSdkImageResolution)Marshal.PtrToStructure((IntPtr)((Int64)ptr + i * Marshal.SizeOf(new tSdkImageResolution())), typeof(tSdkImageResolution));
                string sDescription = System.Text.Encoding.Default.GetString ( infos[i].acDescription );
                comboBox_RresPreset.Items.Insert(comboBox_RresPreset.Items.Count, sDescription);
            }  
            //Marshal.FreeHGlobal (ptr);
            UpdateResolution();
            comboBox_RresPreset.SelectedIndex = m_iResolutionIndex;
            radioButton_ResolutionROI.Checked = !comboBox_RresPreset.Enabled;
            radioButton_ResolutionPreset.Checked = comboBox_RresPreset.Enabled;
            MvApi.CameraGetImageResolution(m_hCamera, out m_tRoiResolution);
            m_tRoiResolution.iIndex = 0xff;

            // gamma
            int gamma = 0;
            MvApi.CameraGetGamma(m_hCamera, ref gamma);
            trackBar_Gamma.SetRange(0, 1000);
            trackBar_Gamma.Value = gamma;
            double fGamma = (((double)gamma) / 100.0); // For better understanding, the interface 1.0 corresponds to the gamma value of SDK 100, meaning 1 times. When gamma is 1, it is the default Gamma correction is not turned on.
            textBox_Gamma.Text = fGamma.ToString();

            // Sharpness
            int sharpness = 0;
            MvApi.CameraGetSharpness(m_hCamera, ref sharpness);
            trackBar_Sharpness.SetRange(0, 200);
            trackBar_Sharpness.Value = sharpness;
            textBox_Sharpness.Text = sharpness.ToString();

            // 2D noise reduction
            MvApi.CameraGetNoiseFilterState(m_hCamera, ref bEnable);
            if (bEnable == 1)
            {
                checkBox_2DDenoise.Checked = true;
            }
            else
            {
                checkBox_2DDenoise.Checked = false;
            }

            // 3D noise reduction
            int bUseWeight;
            int counts;
            int iEnable;
            MvApi.CameraGetDenoise3DParams(m_hCamera, out iEnable, out counts, out bUseWeight, null);
            // populate the list
            comboBox_3DDenoise.Items.Clear();
            comboBox_3DDenoise.Items.Insert(comboBox_3DDenoise.Items.Count, "disabled");
            int j ;
            for (j = 2;j <= 8;j++)
            {
                comboBox_3DDenoise.Items.Insert(comboBox_3DDenoise.Items.Count,j.ToString());
            }

            if (iEnable == 0)
            {
                comboBox_3DDenoise.SelectedIndex = 0;
            }
            else{
                comboBox_3DDenoise.SelectedIndex = counts - 1;
            }


            // horizontal mirror
            MvApi.CameraGetMirror(m_hCamera, 0, ref bEnable);
            if (bEnable == 1)
            {
                checkBox_HFlip.Checked = true;
            }
            else
            {
                checkBox_HFlip.Checked = false;
            }
            // Vertical image
            MvApi.CameraGetMirror(m_hCamera, 1, ref bEnable);
            if (bEnable == 1)
            {
                checkBox_VFlip.Checked = true;
            }
            else
            {
                checkBox_VFlip.Checked = false;
            }

            // Rotate the image
            int iRotate;
            MvApi.CameraGetRotate(m_hCamera, out iRotate);
            radioButton_Rotate90.Checked = (iRotate == 1 ? true : false);
            radioButton_Rotate180.Checked = (iRotate == 2 ? true : false);
            radioButton__Rotate270.Checked = (iRotate == 3 ? true : false);
            radioButton_Forbbiden.Checked = (iRotate == 0 ? true : false);

            // image capture mode
            int iGrabMode = 0;
            MvApi.CameraGetTriggerMode(m_hCamera, ref iGrabMode);
            comboBox_TriggerMode.Items.Clear();
            comboBox_TriggerMode.Items.Insert(comboBox_TriggerMode.Items.Count, "Continuous Acquisition Mode");
            comboBox_TriggerMode.Items.Insert(comboBox_TriggerMode.Items.Count, "soft trigger mode");
            comboBox_TriggerMode.Items.Insert(comboBox_TriggerMode.Items.Count, "hard trigger mode");
            comboBox_TriggerMode.SelectedIndex = iGrabMode;
            button_SwTrigger.Enabled = (iGrabMode == 1 ? true : false);

            // external trigger signal mode
            int iSignalMode = 0;
            MvApi.CameraGetExtTrigSignalType(m_hCamera, ref iSignalMode);
            comboBox_ExtSignalMode.Items.Clear();
            comboBox_ExtSignalMode.Items.Insert(comboBox_ExtSignalMode.Items.Count, "rising edge trigger");
            comboBox_ExtSignalMode.Items.Insert(comboBox_ExtSignalMode.Items.Count, "falling edge trigger");
            comboBox_ExtSignalMode.SelectedIndex = iSignalMode;

            // external trigger signal debounce time
            uint uJitterTime = 0;
            MvApi.CameraGetExtTrigJitterTime(m_hCamera, ref uJitterTime);
            textBox_JetterTime.Text = uJitterTime.ToString();

            // Flash signal mode
            int iStrobMode = 0;
            MvApi.CameraGetStrobeMode(m_hCamera, ref iStrobMode);
            radioButton_StrobeModeAuto.Checked = (iStrobMode == 0 ? true : false);
            radioButton_StrobeModeManul.Checked = (iStrobMode == 1 ? true : false);

            // Effective polarity of the flash in semi-auto mode
            int uPriority = 0;
            MvApi.CameraGetStrobePolarity(m_hCamera, ref uPriority);
            comboBox_StrobePriority.Items.Clear();
            comboBox_StrobePriority.Items.Insert(comboBox_StrobePriority.Items.Count, "active high");
            comboBox_StrobePriority.Items.Insert(comboBox_StrobePriority.Items.Count, "active low");
            comboBox_StrobePriority.SelectedIndex = uPriority;

            // Flash time delay in semi-automatic mode
            uint uDelayTime = 0;
            MvApi.CameraGetStrobeDelayTime(m_hCamera, ref uDelayTime);
            textBox_StrobeDelayTime.Text = uDelayTime.ToString();

            // The pulse width in flash semi-auto mode
            uint uPluseWidth = 0;
            MvApi.CameraGetStrobePulseWidth(m_hCamera, ref uPluseWidth);
            textBox_StrobePulseWidth.Text = uPluseWidth.ToString();


            // camera's analog gain value

            trackBar_AnalogGain.SetRange((int)tCameraDeviceInfo.sExposeDesc.uiAnalogGainMin, (int)tCameraDeviceInfo.sExposeDesc.uiAnalogGainMax);

            // Camera exposure time
            double dCameraExpTimeMin = 0; // The minimum time in units
            double dCameraExpTimeMax = 0; // The maximum time
            MvApi.CameraGetExposureLineTime(m_hCamera, ref dCameraExpTimeMin); // The camera's exposure time, the minimum value of one line time pixel.
            dCameraExpTimeMax = (dCameraExpTimeMin * (double)tCameraDeviceInfo.sExposeDesc.uiExposeTimeMax);
            label_ExpMin.Text = "Minimum:" + dCameraExpTimeMin.ToString("f3") + " ms";
            label_ExpMax.Text = "Maximum:" + dCameraExpTimeMax.ToString("f3") + " ms";

            uint uState = 0;
            MvApi.CameraGetAeState(m_hCamera, ref uState);

            radioButton_AutoExp.Checked = (uState == 1?true:false);
            radioButton_ManulExp.Checked = (uState == 0 ? true : false);

            UpdateExpsoureControls();
        }

        private void UpdateExpsoureControls()
        {
            uint uState = 0;
            MvApi.CameraGetAeState(m_hCamera, ref uState);

            if (uState == 1)
            {
                textBox_AnalogGain.Enabled = false;
                textBox_ExposureTime.Enabled = false;
                trackBar_AnalogGain.Enabled = false;

               
            }
            else
            {
                textBox_AnalogGain.Enabled = true;
                textBox_ExposureTime.Enabled = true;
                trackBar_AnalogGain.Enabled = true;

                int iAnalogGain = 0;
                MvApi.CameraGetAnalogGain(m_hCamera, ref iAnalogGain);

                trackBar_AnalogGain.Value = iAnalogGain;
                textBox_AnalogGain.Text = iAnalogGain.ToString();

                double dCameraExpTime = 0;
                MvApi.CameraGetExposureTime(m_hCamera, ref dCameraExpTime);
                textBox_ExposureTime.Text = dCameraExpTime.ToString();

             
            }
        }

        private void Settings_Shown(object sender, EventArgs e)
        {
            UpdateControls();
            m_bInited = true;
        }

        // Restore camera default parameters
        private void button_DefaultParam_Click (object sender, EventArgs e)
        {
            MvApi.CameraLoadParameter (m_hCamera, (int) emSdkParameterTeam.PARAMETER_TEAM_DEFAULT);
            UpdateControls ();
            this.Refresh ();
            MessageBox.Show ("camera has been restored to default parameters");
        }

        // Save the camera parameters to the specified file
        private void button_SaveParamToFile_Click (object sender, EventArgs e)
        {
            string FileName = "c:\\camera.config"; // save the parameters of the path and file can be modified, but the suffix must be config end.
            MvApi.CameraSaveParameterToFile (m_hCamera, FileName);
            MessageBox.Show ("Parameter saved successfully:" + FileName);

        }

        // Load camera parameters from the specified file
        private void button_LoadParamFromeFIle_Click (object sender, EventArgs e)
        {
            string FileName = "c:\\camera.config";
            MvApi.CameraReadParameterFromFile (m_hCamera, FileName);
            MessageBox.Show ("Parameter loaded successfully:" + FileName);
            UpdateControls ();
            this.Refresh ();
        }

        private void radioButton_AutoExp_CheckedChanged (object sender, EventArgs e)
        {
            MvApi.CameraSetAeState (m_hCamera, 1); // set to auto exposure mode
            UpdateExpsoureControls();
        }

        private void radioButton_ManulExp_CheckedChanged(object sender, EventArgs e)
        {
            MvApi.CameraSetAeState(m_hCamera, 0);// Set to manual exposure mode
            UpdateExpsoureControls();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_hCamera <= 0 || m_bInited == false)
            {
                return;
            }

            uint AeMode = 0;
            MvApi.CameraGetAeState(m_hCamera, ref AeMode);

            if (AeMode == 1)
            {
                int iGain = 0;
                double dExpTime = 0;
                MvApi.CameraGetAnalogGain(m_hCamera, ref iGain);
                MvApi.CameraGetExposureTime(m_hCamera, ref dExpTime);
                textBox_AnalogGain.Text = iGain.ToString();
                trackBar_AnalogGain.Value = iGain;
                textBox_ExposureTime.Text = dExpTime.ToString();
            }
        }

        private void button_SwTrigger_Click(object sender, EventArgs e)
        {
            MvApi.CameraSoftTriggerEx(m_hCamera, 1);// When the soft trigger is executed, the camera's internal buffer will be emptied, and the exposure will be resumed for an image.
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        



    }
}
