using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARFoundation;

public class ButtonHandlers : MonoBehaviour
{
	Camera mainCamera;
	RenderTexture renderTex;
	Texture2D screenshot;
	Texture2D LoadScreenshot;
	int width = Screen.width;   // for Taking Picture
	int height = Screen.height; // for Taking Picture

	public void LaunchScene(string sceneName)
    {
		Debug.Log("LoadScene: " + sceneName);
		var xrManagerSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;
		xrManagerSettings.DeinitializeLoader();
		SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
		xrManagerSettings.InitializeLoaderSync();
	}
	public void TakeScreenshot()
    {
        Debug.Log("TakeScreenshot");
		if (Application.platform == RuntimePlatform.Android)
		{
			if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
			{
				Permission.RequestUserPermission(Permission.ExternalStorageWrite);
				if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)) return;
				else StartCoroutine(CaptureScreen());
			}
			else
			{
				StartCoroutine(CaptureScreen());
			}
		}
		else
		{
			StartCoroutine(CaptureScreen());
		}
    }

	public IEnumerator CaptureScreen()
	{
		yield return null; // Wait till the last possible moment before screen rendering to hide the UI

		GameObject.Find("AppInfoCanvas").GetComponent<Canvas>().enabled = false;
		yield return new WaitForEndOfFrame(); // Wait for screen rendering to complete
		if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
		{
			renderTex = new RenderTexture(width, height, 24);
			screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
			screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		}
		else if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight)
		{
			renderTex = new RenderTexture(height, width, 24);
			screenshot = new Texture2D(height, width, TextureFormat.RGB24, false);
			screenshot.ReadPixels(new Rect(0, 0, height, width), 0, 0);
		}
		mainCamera = Camera.main.GetComponent<Camera>(); // for Taking Picture
		mainCamera.targetTexture = renderTex;
		RenderTexture.active = renderTex;
		mainCamera.Render();
		screenshot.Apply(); //false
		RenderTexture.active = null;
		mainCamera.targetTexture = null;

		string screenShotName = "SamoborNT-AR-" + System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + ".png";
		string folderPath, path;

		if (Application.platform == RuntimePlatform.Android)
		{
			var javaClass = new AndroidJavaClass("android.os.Environment");
			folderPath = javaClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", 
						 javaClass.GetStatic<string>("DIRECTORY_DCIM")).Call<string>("getAbsolutePath");
			folderPath += "/" + "Screenshots";
		}
		else
		{
			// on Win7 - C:/Users/Username/AppData/LocalLow/CompanyName/GameName
			folderPath = Application.persistentDataPath + "/" + "screenshots";
		}

		if (!System.IO.Directory.Exists(folderPath))
		{
			System.IO.Directory.CreateDirectory(folderPath);
		}

		path = folderPath + "/" + screenShotName;
		File.WriteAllBytes(path, screenshot.EncodeToPNG());
		
		// on Win7 - it's in project files (Asset folder)
		//File.WriteAllBytes (Application.dataPath + "/" + screenShotName, screenshot.EncodeToPNG ());  
		//File.WriteAllBytes ("picture1.png", screenshot.EncodeToPNG ());
		//File.WriteAllBytes (Application.dataPath + "/../../picture3.png", screenshot.EncodeToPNG ());
		//Application.CaptureScreenshot ("picture2.png");
		GameObject.Find("AppInfoCanvas").GetComponent<Canvas>().enabled = true; // Show UI after we're done
		Debug.Log("Screenshot dest: " + path);

		string[] paths = new string[1];
		paths[0] = path;
		ScanFile(paths);

		void ScanFile(string[] path)
		{
			using (AndroidJavaClass PlayerActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				AndroidJavaObject playerActivity = PlayerActivity.GetStatic<AndroidJavaObject>("currentActivity");
				using (AndroidJavaObject Conn = new AndroidJavaObject("android.media.MediaScannerConnection", playerActivity, null))
				{
					Conn.CallStatic("scanFile", playerActivity, path, null, null);
				}
			}
		}
	}
}
