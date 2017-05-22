/******************************************************************************************
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
using System.Collections.Generic;
using UnityEngine;

namespace Sensel
{
	namespace Samples
	{
		class SenselVisualizer : MonoBehaviour
		{
#pragma warning disable 0649
			public Material material;

			IntPtr _handle = IntPtr.Zero;
			SenselSensorInfo _sensor_info;
			SenselFrame _frame = new SenselFrame();
			float[] _forces;

			List<Mesh> _tempMeshes = new List<Mesh>();
			List<Vector3> _tempVertices = new List<Vector3>();
			List<Vector2> _tempUVs = new List<Vector2>();

			const float PosScale = 0.1f;
			const float ForceScale = 0.2f;
			const float ForceUVScale = 0.2f;

			void Awake()
			{
				SenselDeviceList list = new SenselDeviceList();
				list.num_devices = 0;
				Sensel.senselGetDeviceList(ref list);
				Debug.Log("Num Devices: " + list.num_devices);
				if (list.num_devices != 0)
				{
					Sensel.senselOpenDeviceByID(ref _handle, list.devices[0].idx);
				}
				if (_handle != IntPtr.Zero)
				{
					Sensel.senselGetSensorInfo(_handle, ref _sensor_info);
					Debug.Log("Sensel Device: " + System.Text.Encoding.Default.GetString(list.devices[0].serial_num));
					Debug.Log("Width: " + _sensor_info.width+"mm");
					Debug.Log("Height: " + _sensor_info.height + "mm");
					Debug.Log("Cols: " + _sensor_info.num_cols); // 185
					Debug.Log("Rows: " + _sensor_info.num_rows); // 105
					Sensel.senselSetFrameContent(_handle, 1);
					Sensel.senselAllocateFrameData(_handle, _frame);
					Sensel.senselStartScanning(_handle);

					_forces = new float[(int)_sensor_info.num_cols * (int)_sensor_info.num_rows];

					for( int y = 0; y < _sensor_info.num_rows; ++y ) {
						var vZ = ((float)y - (float)_sensor_info.num_rows * 0.5f) * -PosScale;
						for( int x = 0; x < _sensor_info.num_cols; ++x ) {
							var vX = ((float)x - (float)_sensor_info.num_cols * 0.5f) * PosScale;
							_tempVertices.Add( new Vector3( vX, 0.0f, vZ ) );
							_tempUVs.Add( new Vector2( 0.0f, 1.0f ) );
						}
					}

					Mesh tempMesh = _AddMesh( _tempVertices, _tempUVs );

					List<int> tempIndices = new List<int>();
					int vertexIndex = 0;
					for( int y = 0; y < _sensor_info.num_rows - 1; ++y ) {
						for( int x = 0; x < _sensor_info.num_cols - 1; ++x, ++vertexIndex ) {
							tempIndices.Add( vertexIndex + 0 );
							tempIndices.Add( vertexIndex + 1 );
							tempIndices.Add( vertexIndex + _sensor_info.num_cols + 1 );
							tempIndices.Add( vertexIndex + _sensor_info.num_cols + 0 );
						}
					}

					if( tempMesh != null && tempIndices.Count > 0 ) {
						tempMesh.SetIndices( tempIndices.ToArray(), MeshTopology.Quads, 0 );
					}

					foreach( var mesh in _tempMeshes ) {
						mesh.RecalculateNormals();
						mesh.RecalculateBounds();
					}
				}
			}

			Mesh _AddMesh( List<Vector3> vertices, List<Vector2> uvs )
			{
				Mesh mesh = new Mesh();
				mesh.SetVertices( vertices );
				mesh.SetUVs( 0, uvs );
				_tempMeshes.Add( mesh );
				return mesh;
			}

			void Update()
			{
				if( _handle != IntPtr.Zero ) {
					Int32 num_frames = 0;
					Sensel.senselReadSensor(_handle);
					Sensel.senselGetNumAvailableFrames(_handle, ref num_frames);
					for( int f = 0; f < num_frames; ++f ) {
						Sensel.senselGetFrame(_handle, _frame);
						if( f + 1 == num_frames ) {
							System.Array.Copy( _frame.force_array, _forces, _forces.Length );
						}
					}

					int vertexIndex = 0;
					for( int y = 0; y < _sensor_info.num_rows; ++y ) {
						for( int x = 0; x < _sensor_info.num_cols; ++x, ++vertexIndex ) {
							Vector3 v = _tempVertices[vertexIndex];
							v.y = _forces[vertexIndex] * ForceScale;
							_tempVertices[vertexIndex] = v;
							Vector2 uv = _tempUVs[vertexIndex];
							uv.y = Mathf.Clamp01(_forces[vertexIndex] * ForceUVScale);
							_tempUVs[vertexIndex] = uv;
						}
					}

					foreach( var mesh in _tempMeshes ) {
						mesh.SetVertices( _tempVertices );
						mesh.SetUVs( 0, _tempUVs );
						mesh.UploadMeshData( false );
					}

					foreach( var mesh in _tempMeshes ) {
						Graphics.DrawMesh( mesh, this.transform.localToWorldMatrix, this.material, 0 );
					}
				}
			}

			void OnDestroy()
			{
				foreach( var mesh in _tempMeshes ) {
					Mesh.Destroy( mesh );
				}
				_tempMeshes.Clear();

				if( _handle != IntPtr.Zero ) {
					Sensel.senselStopScanning( _handle );
					Sensel.senselClose( _handle );
				}
			}
		}
	}
}
