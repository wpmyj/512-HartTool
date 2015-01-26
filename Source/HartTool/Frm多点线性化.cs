﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading ;
using System.Windows.Forms;
using HartSDK;

namespace HartTool
{
    public partial class Frm多点线性化 : Form, IHartCommunication
    {
        public Frm多点线性化()
        {
            InitializeComponent();
        }

        #region 私有变量
        private Thread _TrdReadAD = null;
        #endregion

        #region 私有方法
        private void ReadAD_Task()
        {
            try
            {
                while (true)
                {
                    if (HartDevice != null && HartDevice.IsConnected)
                    {
                        float ad = HartDevice.ReadPVAD(false);
                        this.Invoke((Action)(() => { txtAD.Text = ad.ToString("F0"); }));
                    }
                    Thread.Sleep(200);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        #region 实现接口 IHartCommunication
        public HartSDK.HartDevice HartDevice { get; set; }

        public HartSDK.UniqueIdentifier CurrentDevice { get; set; }

        public void ReadData()
        {
            btnWrite.Enabled = HartDevice != null && HartDevice.IsConnected;
            if (HartDevice != null && HartDevice.IsConnected)
            {
                for (int i = 1; i <= 10; i++)
                {
                    LinearizationItem li = HartDevice.ReadLinearizationItem((byte)i);
                    (this.Controls["txtP" + i.ToString()] as TextBox).Text = li != null ? li.SensorValue.ToString() : null;
                    (this.Controls["txtAD" + i.ToString()] as TextBox).Text = li != null ? li.SensorValue.ToString() : null;
                }
            }
        }
        #endregion

        #region 事件处理程序
        private void Frm多点线性化_Load(object sender, EventArgs e)
        {
            _TrdReadAD = new Thread(new ThreadStart(ReadAD_Task));
            _TrdReadAD.IsBackground = true;
            _TrdReadAD.Start();
        }

        private void Frm多点线性化_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_TrdReadAD != null)
            {
                _TrdReadAD.Abort();
                _TrdReadAD = null;
            }
        }

        private void txtP1_Enter(object sender, EventArgs e)
        {
            string temp = (sender as Control).Tag.ToString();
            if (this.Controls["button" + temp] != null)
            {
                for (int i = 1; i <= 10; i++)
                {
                    this.Controls["button" + i.ToString()].Enabled = i.ToString() == temp;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (HartDevice == null || !HartDevice.IsConnected) return;
            string temp = (sender as Control).Tag.ToString();
            int ind = 0;
            if (int.TryParse(temp, out ind) && ind >= 1 && ind <= 10)
            {
                (this.Controls["txtAD" + ind.ToString()] as TextBox).Text = HartDevice.ReadPVAD(false).ToString("F0");
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            if (HartDevice == null || !HartDevice.IsConnected) return;
            List<LinearizationItem> lis = new List<LinearizationItem>() { new LinearizationItem() { SensorValue = 0, SensorAD = 0 } };
            for (int i = 1; i <= 10; i++)
            {
                string strP = (this.Controls["txtP" + i.ToString()] as TextBox).Text;
                string strAD = (this.Controls["txtAD" + i.ToString()] as TextBox).Text;
                float fp = 0;
                float fAD = 0;
                if (!string.IsNullOrEmpty(strP) && !string.IsNullOrEmpty(strAD))
                {
                    if (float.TryParse(strP, out fp) && fp >= 0 && float.TryParse(strAD, out fAD) && fAD >= 0)
                    {
                        LinearizationItem li = new LinearizationItem() { SensorValue = fp, SensorAD = fAD };
                        lis.Add(li);
                    }
                }
            }
            lis.Add(new LinearizationItem() { SensorValue = 0, SensorAD = 70000 });
            if (lis.Count % 2 == 1) lis.Add(new LinearizationItem() { SensorValue = 0, SensorAD = 70000 });
            bool ret = HartDevice.WriteLinearizationItems(lis.ToArray());
            MessageBox.Show(ret ? "下载成功" : HartDevice.GetLastError(), "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
