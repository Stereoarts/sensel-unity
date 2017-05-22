﻿/******************************************************************************************
* MIT License
*
* Copyright (c) 2013-2017 Sensel, Inc.
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
******************************************************************************************/

using System;
using UnityEngine;

namespace Sensel
{
	namespace Samples
	{
		class HelloSensel : MonoBehaviour
		{
			void Start()
			{
				IntPtr handle = new IntPtr(0);
				//SenselFrame frame = new SenselFrame();
				SenselDeviceList list = new SenselDeviceList();
				SenselSensorInfo sensor_info = new SenselSensorInfo();
				list.num_devices = 0;
				Sensel.senselGetDeviceList(ref list);
				Debug.Log("Num Devices: " + list.num_devices);
				if (list.num_devices != 0)
				{
					Sensel.senselOpenDeviceByID(ref handle, list.devices[0].idx);
				}
				if (handle.ToInt64() != 0)
				{
					Sensel.senselGetSensorInfo(handle, ref sensor_info);
					Debug.Log("Sensel Device: " + System.Text.Encoding.Default.GetString(list.devices[0].serial_num));
					Debug.Log("Width: " + sensor_info.width+"mm");
					Debug.Log("Height: " + sensor_info.height + "mm");
					Debug.Log("Cols: " + sensor_info.num_cols);
					Debug.Log("Rows: " + sensor_info.num_rows);

					Sensel.senselClose(handle);
				}
			}
		}
	}
}
