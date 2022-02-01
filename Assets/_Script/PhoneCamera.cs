using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;

public class PhoneCamera : MonoBehaviour
{
    private bool camAvailable;
    private WebCamTexture backCam;
    private Texture defaultBackGround;

    public RawImage background;

    public AspectRatioFitter fit;

    public string dirPath;
    
    public byte[] saveImageBytes;

    GameObject dialog = null;

    // Start is called before the first frame update
    void Start()
    {
        defaultBackGround = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            print("No Cams");
            camAvailable = false;
        }

        for (int i = 0; i < devices.Length; ++i)
        {
            if (!devices[i].isFrontFacing)
            {
                backCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
            }
        }

        if (backCam == null)
        {
            print("No back Cam");
            return;
        }

        backCam.Play();
        background.texture = backCam;
        camAvailable = true;
        
        var jc = new AndroidJavaClass("android.os.Environment");
        var path = jc.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", 
                jc.GetStatic<string>("DIRECTORY_DCIM"))
            .Call<string>("getAbsolutePath");

        dirPath = path + "/Camera";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        
        #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
        #endif
        
    }
    
    static byte[] ScreenshotWebcam(WebCamTexture wct)
    {
        Texture2D colorTex = new Texture2D(wct.width, wct.height, TextureFormat.RGBA32, false);

        byte[] colorByteData = Color32ArrayToByteArray(wct.GetPixels32());

        colorTex.LoadRawTextureData(colorByteData);
        colorTex.Apply();

        return colorTex.EncodeToPNG();
    }
    
    static byte[] Color32ArrayToByteArray(Color32[] colors)
    {
        // https://stackoverflow.com/a/21575147/2496170

        if (colors == null || colors.Length == 0) return null;

        int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));

        int length = lengthOfColor32 * colors.Length;

        byte[] bytes = new byte[length];

        GCHandle handle = default(GCHandle);

        try
        {
            handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            Marshal.Copy(ptr, bytes, 0, length);
        }
        finally
        {
            if (handle != default(GCHandle)) handle.Free();
        }

        return bytes;
    }

    // Update is called once per frame
    void Update()
    {
        if (!camAvailable)
            return;
        float ratio = (float) backCam.width / (float) backCam.height;
        fit.aspectRatio = ratio;

        float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -backCam.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
    }

    public void takePicture()
    {
        byte[] data = ScreenshotWebcam(backCam);

        File.WriteAllBytes(dirPath + "/R_" + Random.Range(0, 100000) + ".png", data);
        // File.WriteAllBytes(dirPath + "/R_" + Random.Range(0, 100000) + ".png", saveImageBytes);
        Debug.Log(data.Length / 1024 + "Kb was saved as: " + dirPath);
    }
}
