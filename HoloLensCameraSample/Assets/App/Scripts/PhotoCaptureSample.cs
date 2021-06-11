using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

namespace HoloLensCameraSample
{
    /// <summary>
    /// PhotoCaputreを利用したカメラ画像のキャプチャサンプル
    /// ドキュメント：https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/locatable-camera-in-unity
    /// </summary>
    public class PhotoCaptureSample : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawImage;

        private PhotoCapture photoCaptureObject = null;

        private bool isCapturing = false;

        int Width;
        int Height;

        // Start is called before the first frame update
        void Start()
        {

        }

        /// <summary>
        /// 写真撮影を開始する
        /// </summary>
        public void StartPhotoCapture()
        {
            if (isCapturing)
            {
                Debug.Log("Now Capturing...");
                return;
            }
            isCapturing = true;
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        }

        void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            // 対応サイズ表示
            var supportedResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);
            foreach (var s in supportedResolutions)
            {
                // ★HoloLens 2 実機だと 3904 x 2196 しか表示されない
                Debug.Log($"Supported Resolution: {s.width} x {s.height} (Refresh Rate: {s.refreshRate})");
            }

            // 本来は 3904 x 2196 以外も選択できるはず
            // 参考：https://docs.microsoft.com/ja-jp/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2
            var w = 1280;
            var h = 720;
            Resolution cameraResolution;
            var cameraResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);
            var match = (from resolution in cameraResolutions
                         where resolution.width == w && resolution.height == h
                         select new { resolution }).FirstOrDefault();

            if (match != null)
            {
                cameraResolution = match.resolution;
                Debug.Log($"Match: {match.resolution.width} x  {match.resolution.height}");
            }
            else
            {
                cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                Debug.Log("Not match camera resolution.(Default value selected)");
            }

            CameraParameters c = new CameraParameters();
            c.hologramOpacity = 0.0f;
            c.cameraResolutionWidth = cameraResolution.width;
            c.cameraResolutionHeight = cameraResolution.height;
            c.pixelFormat = CapturePixelFormat.BGRA32;

            Width = cameraResolution.width;
            Height = cameraResolution.height;
            Debug.Log($"Image Size: {Width} x {Height}");

            captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }

        // ファイルにキャプチャする
        /*
        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

                photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            }
            else
            {
                Debug.LogError("Unable to start photo mode!");
            }
        }

        void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                Debug.Log("Saved Photo to disk!");
                photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            }
            else
            {
                Debug.Log("Failed to save Photo to disk");
            }
        }
        */

        // メモリにデータをキャプチャする
        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
            else
            {
                Debug.LogError("Unable to start photo mode!");
            }
        }

        //  Texture2Dを取得する場合
        /*
        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            if (result.success)
            {
                // Create our Texture2D for use and set the correct resolution
                Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
                // Copy the raw image data into our target texture
                photoCaptureFrame.UploadImageDataToTexture(targetTexture);

                // Do as we wish with the texture such as apply it to a material, etc.
                if (photoCaptureFrame.hasLocationData)
                {
                    photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

                    Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                    Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                    photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);

                    Debug.Log(position);
                    Debug.Log(rotation);
                }

                rawImage.texture = targetTexture;

            }
            // Clean up
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

            isCapturing = false;
        }
        */

        // 生のバイト列を取得する場合
        // photoCaptureFrameのドキュメント：https://docs.unity3d.com/ja/current/ScriptReference/Windows.WebCam.PhotoCaptureFrame.html
        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            if (result.success)
            {
                List<byte> imageBufferList = new List<byte>();
                // Copy the raw IMFMediaBuffer data into our empty byte list.
                photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

                // バイト配列ををのままテクスチャにして表示する場合（上下反転している）
                /*
                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(imageBufferList.ToArray());
                tex.Apply();
                rawImage.texture = tex;
                return;
                */


                // 何かしらの処理を行う場合

                // In this example, we captured the image using the BGRA32 format.
                // So our stride will be 4 since we have a byte for each rgba channel.
                // The raw image data will also be flipped so we access our pixel data
                // in the reverse order.
                //
                // この例では、BGRA32フォーマットで画像をキャプチャしています。
                // したがって、各rgbaチャンネルに1バイトあるので、ストライドは4になります。
                // 生の画像データは反転しているので、ピクセルデータへは逆の順序でアクセスすることになります。
                int stride = 4;
                float denominator = 1.0f / 255.0f;
                List<Color> colorArray = new List<Color>();
                for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
                {
                    float a = (int)(imageBufferList[i - 0]) * denominator;
                    float r = (int)(imageBufferList[i - 1]) * denominator;
                    float g = (int)(imageBufferList[i - 2]) * denominator;
                    float b = (int)(imageBufferList[i - 3]) * denominator;

                    colorArray.Add(new Color(r, g, b, a));
                }
                // Now we could do something with the array such as texture.SetPixels() or run image processing on the list
                // これで、texture.SetPixels() のような配列を使った処理や、リストに対する画像処理を行うことができます。

                // 画像処理サンプル（必要なければコメントするだけでOK）
                ImageEffectSample(colorArray);

                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        // 左右反転していたので元に戻してテクスチャに設定
                        tex.SetPixel(x, y, colorArray[y * Width + (Width - 1) - x]);
                    }
                }
                tex.Apply();

                rawImage.texture = tex;
            }

            // Clean up
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

            isCapturing = false;
        }

        // グレイスケール処理
        private void ImageEffectSample(List<Color> data)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color c = data[y * Width + x];
                    float gray = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
                    c.r = gray;
                    c.g = gray;
                    c.b = gray;
                    c.a = 1.0f;
                    data[y * Width + x] = c;
                }
            }
        }
    }
}
