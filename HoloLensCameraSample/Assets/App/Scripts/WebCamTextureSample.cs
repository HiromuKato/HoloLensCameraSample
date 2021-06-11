using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoloLensCameraSample
{
    /// <summary>
    /// WebCamTexture を利用したサンプル
    /// ドキュメント：https://docs.unity3d.com/ja/current/ScriptReference/WebCamTexture.html
    /// </summary>
    public class WebCamTextureSample : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawImage;

        private WebCamTexture webCamTexture;

        private void Start()
        {
            var w = 896;
            var h = 504;

            // Webカメラの取得
            WebCamDevice userCameraDevice = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(userCameraDevice.name, w, h, 15);
            rawImage.texture = webCamTexture;

            Debug.Log($"DeviceName: {webCamTexture.deviceName}");
            Debug.Log($"TextureSize: {webCamTexture.width} x {webCamTexture.height}");
        }

        private void OnDestroy()
        {
            webCamTexture.Stop();
        }

        public void StartWebCam()
        {
            webCamTexture.Play();
            Debug.Log($"DeviceName: {webCamTexture.deviceName}");
            Debug.Log($"WebCamTexuture: {webCamTexture.width} x {webCamTexture.height}");
        }

        public void StopWebCam()
        {
            webCamTexture.Stop();
        }
    }
}
