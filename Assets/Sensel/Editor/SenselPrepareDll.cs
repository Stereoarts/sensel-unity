using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Sensel
{

[InitializeOnLoad]
public class PrepareDll
{
	const string SenselLibDirectory_Windows = @"C:\Program Files\Sensel\SenselLib";
	const string PluginsDirectory = @"Assets/Sensel/Plugins";

	const string LockFileName = "Temp/_SenselPrepareDll_Locked";

	static BuildTarget[] BuildTargets = new BuildTarget[] {
		BuildTarget.StandaloneWindows,
		BuildTarget.StandaloneWindows64,
	};

	static RuntimePlatform[] EditorPlatforms = new RuntimePlatform[] {
		RuntimePlatform.WindowsEditor,
		RuntimePlatform.WindowsEditor,
	};

	static string[] SenselLibPlatforms = new string[] {
		@"x86",
		@"x64",
	};

	static string[] PluginsPlatforms = new string[] {
		@"x86",
		@"x86_64",
	};

	static string[] DllNames = new string[] {
		"LibSensel.dll",
		"LibSenselDecompress.dll"
	};

	static PrepareDll()
	{
		if( Application.isPlaying ) {
			return;
		}

		if( File.Exists( LockFileName ) ) {
			return;
		}

		try {
			File.WriteAllBytes( LockFileName, new byte[] {} );
		} catch( System.Exception ) {
		}

		Process();
	}

	static string _GetSenselLibDirectory()
	{
		return SenselLibDirectory_Windows;
	}

	public static void Process()
	{
		if( _IsPreparedDlls() ) {
			if( _IsExistsSenselLib() ) {
				if( !_IsExpiredDlls() ) {
					return; // Success.
				}

				Debug.LogWarning("Dlls are expired. Update all Dlls.");
			} else {
				return; // Sensel Lib is not found. (Not required, counts as success.)
			}
		}

		if( !_IsExistsSenselLib() ) {
			_SenselLib_NotFound();
			return;
		}

		Debug.Log( "[Sensel] PrepareDlls" );
		_PrepareDlls();
	}

	static void _PrepareDlls()
	{
		if( !_CreateDirectory( PluginsDirectory ) ) {
			return;
		}

		for( int i = 0, length = PluginsPlatforms.Length; i < length; ++i ) {
			if( EditorPlatforms[i] != Application.platform ) {
				continue; // Unsupported platform.(OSXEditor / WindowsEditor)
			}

			if( !_CreateDirectory( _GetPluginDirectory( PluginsPlatforms[i] ) ) ) {
				continue;
			}

			foreach( string dllName in DllNames ) {
				var sourceFileName = _GetSenselLibPath( SenselLibPlatforms[i], dllName );
				var pluginName = _GetPluginName( PluginsPlatforms[i], dllName );
				if( _CopyDllInternal( sourceFileName, pluginName ) ) {
					AssetDatabase.ImportAsset( pluginName, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
					_SetPluginAttributes( i, pluginName );
				}
			}
		}
	}

	static bool _CreateDirectory( string directoryName )
	{
		if( !Directory.Exists( directoryName ) ) {
			try {
				Directory.CreateDirectory( directoryName );
				return true;
			} catch( System.Exception e ) {
				Debug.LogError( e.ToString() );
				return true;
			}
		}

		return true;
	}

	static string _GetSenselLibPath( string platform, string dllName )
	{
		return Path.Combine( Path.Combine( _GetSenselLibDirectory(), platform ), dllName );
	}

	static string _GetPluginDirectory( string platform )
	{
		return PluginsDirectory + "/" + platform;
	}

	static string _GetPluginName( string platform, string dllName )
	{
		return PluginsDirectory + "/" + platform + "/" + dllName;
	}

	static bool _CopyDllInternal( string sourceFileName, string destFileName )
	{
		try {
			File.Copy( sourceFileName, destFileName, true );
			return true;
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return false;
		}
	}

	static void _SetPluginAttributes( int platformIndex, string pluginName )
	{
		var pluginImporterSetter = new _PluginImporterSetter( BuildTargets[platformIndex], pluginName );
		if( !pluginImporterSetter.isExists ) {
			Debug.LogError("GetAtPath() failed. [" + pluginName + "]");
			return;
		}

		pluginImporterSetter.SetPlugin();
	}

	class _PluginImporterSetter
	{
		BuildTarget _buildTarget;
		string _pluginName;
		PluginImporter _pluginImporter;
		bool _isWritten;

		public _PluginImporterSetter( BuildTarget buildTarget, string pluginName )
		{
			_buildTarget = buildTarget;
			_pluginName = pluginName;
			_pluginImporter = PluginImporter.GetAtPath( pluginName ) as PluginImporter;
		}

		public bool isExists {
			get {
				return _pluginImporter != null;
			}
		}

		public void SetPlugin()
		{
			switch( _buildTarget ) {
			case BuildTarget.StandaloneWindows:
				_SetPlugin_x86();
				break;
			case BuildTarget.StandaloneWindows64:
				_SetPlugin_x86_64();
				break;
			}

			_WriteImportSettingsIfDirty();
		}

		void _SetPlugin_x86()
		{
			_SetCompatibleWithAnyPlatform( false );
			_SetCompatibleWithPlatform_Editor( true );
			_SetPlatformData( "Editor", "OS", "Windows" );
			_SetPlatformData( "Editor", "CPU", "x86" );
			_SetCompatibleWithPlatform( BuildTarget.StandaloneWindows, true );
			_SetPlatformData( BuildTarget.StandaloneWindows, "CPU", "x86" );
		}

		void _SetPlugin_x86_64()
		{
			_SetCompatibleWithAnyPlatform( false );
			_SetCompatibleWithPlatform_Editor( true );
			_SetPlatformData( "Editor", "OS", "Windows" );
			_SetPlatformData( "Editor", "CPU", "x86_64" );
			_SetCompatibleWithPlatform( BuildTarget.StandaloneWindows64, true );
			_SetPlatformData( BuildTarget.StandaloneWindows64, "CPU", "x86_64" );
		}

		void _SetPlatformData( string platformName, string key, string value )
		{
			if( _pluginImporter != null ) {
				if( _pluginImporter.GetPlatformData( platformName, key ) != value ) {
					_pluginImporter.SetPlatformData( platformName, key, value );
					_isWritten = true;
				}
			}
		}

		void _SetPlatformData( BuildTarget platform, string key, string value )
		{
			if( _pluginImporter != null && (int)platform >= 0 ) {
				if( _pluginImporter.GetPlatformData( platform, key ) != value ) {
					_pluginImporter.SetPlatformData( platform, key, value );
					_isWritten = true;
				}
			}
		}

		void _SetCompatibleWithPlatform_Editor( bool enable )
		{
			_SetCompatibleWithPlatform( "Editor", enable );
		}

		void _SetCompatibleWithAnyPlatform( bool enable )
		{
			if( _pluginImporter != null ) {
				if( _pluginImporter.GetCompatibleWithAnyPlatform() != enable ) {
					_pluginImporter.SetCompatibleWithAnyPlatform( enable );
					_isWritten = true;
				}
			}
		}

		void _SetCompatibleWithPlatform( string platformName, bool enable )
		{
			if( _pluginImporter != null ) {
				if( _pluginImporter.GetCompatibleWithPlatform( platformName ) != enable ) {
					_pluginImporter.SetCompatibleWithPlatform( platformName, enable );
					_isWritten = true;
				}
			}
		}

		void _SetCompatibleWithPlatform( BuildTarget platform, bool enable )
		{
			if( _pluginImporter != null && (int)platform >= 0 ) {
				if( _pluginImporter.GetCompatibleWithPlatform( platform ) != enable ) {
					_pluginImporter.SetCompatibleWithPlatform( platform, enable );
					_isWritten = true;
				}
			}
		}

		void _WriteImportSettingsIfDirty()
		{
			if( _isWritten ) {
				_isWritten = false;
				AssetDatabase.WriteImportSettingsIfDirty( _pluginName );
				AssetDatabase.Refresh();
			}
		}
	}

	static bool _IsPreparedDlls()
	{
		bool missingAnything = false;

		for( int i = 0, length = PluginsPlatforms.Length; i < length; ++i ) {
			if( EditorPlatforms[i] != Application.platform ) {
				continue; // Unsupported platform.(OSXEditor / WindowsEditor)
			}

			foreach( string dllName in DllNames ) {
				var pluginName = _GetPluginName( PluginsPlatforms[i], dllName );
				if( !_IsExistsFile( pluginName ) ) {
					missingAnything = true;
				}
			}
		}

		return !missingAnything;
	}

	static bool _IsExpiredDlls()
	{
		bool expiredAnything = false;

		for( int i = 0, length = PluginsPlatforms.Length; i < length; ++i ) {
			if( EditorPlatforms[i] != Application.platform ) {
				continue; // Unsupported platform.(OSXEditor / WindowsEditor)
			}

			foreach( string dllName in DllNames ) {
				var sourceFileName = _GetSenselLibPath( SenselLibPlatforms[i], dllName );
				var pluginName = _GetPluginName( PluginsPlatforms[i], dllName );
				if( !_IsExistsFile( sourceFileName ) || !_IsExistsFile( pluginName ) ) {
					continue;
				}

				byte[] sourceBytes = _ReadAllBytes( sourceFileName );
				byte[] pluginBytes = _ReadAllBytes( pluginName );
				if( sourceBytes == null || pluginBytes == null ) {
					continue;
				}

				if( !_IsEqualBytes( sourceBytes, pluginBytes ) ) {
					Debug.LogWarning( "Expired. [" + pluginName + "]" );
					expiredAnything = true;
					continue;
				}
			}
		}

		return expiredAnything;
	}

	static bool _IsExistsFile( string fileName )
	{
		if( !File.Exists( fileName ) ) {
			Debug.LogWarning( "Not found [" + fileName + "]" );
			return false;
		}

		return true;
	}

	static byte[] _ReadAllBytes( string fileName )
	{
		try {
			return System.IO.File.ReadAllBytes( fileName );
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return null;
		}
	}

	static bool _IsEqualBytes( byte[] sourceBytes, byte[] destBytes )
	{
		if( sourceBytes == null || destBytes == null ) {
			return false; // Skip.
		}

		if( sourceBytes.Length != destBytes.Length ) {
			return false;
		}

		int length = sourceBytes.Length;
		for( int i = 0; i < length; ++i ) {
			if( sourceBytes[i] != destBytes[i] ) {
				return false;
			}
		}

		return true;
	}

	static bool _IsExistsSenselLib()
	{
		return Directory.Exists( _GetSenselLibDirectory() );
	}

	static void _SenselLib_NotFound()
	{
		Debug.LogWarning( "SenselLib is not found. [" + _GetSenselLibDirectory() + "]" );
	}
}

}
