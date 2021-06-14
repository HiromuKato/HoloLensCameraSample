using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if WINDOWS_UWP
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
#endif

namespace HoloLensCameraSample
{
    /// <summary>
    /// MediaCapture でフォトキャプチャを行うサンプル
    /// Capapilities で VideosLibrary, Webcam, Microphone を有効にすること
    ///
    /// 参考：
    /// https://docs.microsoft.com/ja-jp/windows/uwp/audio-video-camera/basic-photo-video-and-audio-capture-with-mediacapture
    /// https://docs.microsoft.com/ja-jp/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers
    /// </summary>
    public class MediaCapturePhotoSample : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawImage;

        private async void Start()
        {
            await Initialize();
        }

        private async void OnDestroy()
        {
            await CleanupResources();
        }

        public async Task Initialize()
        {
#if WINDOWS_UWP
            await InitializeUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }

        public async Task CleanupResources()
        {
#if WINDOWS_UWP
            await CleanupResourcesUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }

        public void TakePhoto()
        {
#if WINDOWS_UWP
            TakePhotoUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }


#if WINDOWS_UWP
        private MediaCapture mediaCapture;
        private LowLagPhotoCapture lowLagCapture;
        byte[] bytes = null;
        private bool isPhotoCapturing = false;
        private Texture2D tex;

        /// <summary>
        /// MediaCapture オブジェクトの初期化を行う
        /// </summary>
        private async Task InitializeUWP()
        {
            var videoDeviceId = await GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel.Back);

            var mediaInitSettings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = videoDeviceId,
                //SharingMode = MediaCaptureSharingMode.SharedReadOnly
            };

            IReadOnlyList<MediaCaptureVideoProfile> profiles = MediaCapture.FindAllVideoProfiles(videoDeviceId);

            // サポートされいてるサイズ・フレームレートを表示
            Debug.Log("Supported size & frame rate");
            foreach (var p in profiles)
            {
                foreach (var d in p.SupportedPhotoMediaDescription)
                {
                    Debug.Log($"{d.Width}x{d.Height} {d.FrameRate}");
                }
            }

            // 好みの値を指定する
            // 参考：https://docs.microsoft.com/ja-jp/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2
            var width = 1280;
            var height = 720;
            var framerate = 15;
            var match = (from profile in profiles
                         from desc in profile.SupportedRecordMediaDescription
                         where desc.Width == width && desc.Height == height && Math.Round(desc.FrameRate) == framerate
                         select new { profile, desc }).FirstOrDefault();

            if (match != null)
            {
                mediaInitSettings.VideoProfile = match.profile;
                mediaInitSettings.PhotoMediaDescription = match.desc; // PhotoMediaDescriptionである点に注意する
                Debug.Log($"Selected media : {match.desc.Width}x{match.desc.Height}");
            }
            else
            {
                Debug.LogWarning("Can't find profile.");
                mediaInitSettings.VideoProfile = profiles[0];
            }

            mediaCapture = null;
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(mediaInitSettings);

            mediaCapture.CameraStreamStateChanged += MediaCapture_CameraStreamStateChanged;
            mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            mediaCapture.Failed += MediaCapture_Failed;
            mediaCapture.FocusChanged += MediaCapture_FocusChanged;
            mediaCapture.PhotoConfirmationCaptured += MediaCapture_PhotoConfirmationCaptured;
            mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;
            mediaCapture.ThermalStatusChanged += MediaCapture_ThermalStatusChanged;

            // Prepare and capture photo
            lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
        }

        /// <summary>
        /// サポートされているビデオプロファイルの DeviceID を取得する
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        public async Task<string> GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel panel)
        {
            string deviceId = string.Empty;

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                // Check if the device on the requested panel supports Video Profile
                //if (MediaCapture.IsVideoProfileSupported(device.Id) && device.EnclosureLocation.Panel == panel)
                if (MediaCapture.IsVideoProfileSupported(device.Id))
                {
                    // HoloLens 2 の実機が返すのは背面カメラ1つのみ
                    Debug.Log("Panel: " + device.EnclosureLocation.Panel.ToString());
                    Debug.Log("DevideId: " + device.Id);

                    // We've located a device that supports Video Profiles on expected panel
                    deviceId = device.Id;
                    break;
                }
            }

            return deviceId;
        }

        /// <summary>
        /// キャプチャセッションを破棄し、関連付けられているリソースをクリーンアップする
        /// </summary>
        private async Task CleanupResourcesUWP()
        {
            if (mediaCapture == null)
            {
                return;
            }

            // LowLagPhotoCapture セッションをシャットダウンし、関連するリソースを解放
            await lowLagCapture.FinishAsync();

            mediaCapture.CameraStreamStateChanged -= MediaCapture_CameraStreamStateChanged;
            mediaCapture.CaptureDeviceExclusiveControlStatusChanged -= MediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            mediaCapture.Failed -= MediaCapture_Failed;
            mediaCapture.FocusChanged -= MediaCapture_FocusChanged;
            mediaCapture.PhotoConfirmationCaptured -= MediaCapture_PhotoConfirmationCaptured;
            mediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
            mediaCapture.ThermalStatusChanged -= MediaCapture_ThermalStatusChanged;

            mediaCapture = null;
        }

        /// <summary>
        /// カメラ画像の取得
        /// </summary>
        /// <returns></returns>
        public async Task TakePhotoUWP()
        {
            if (isPhotoCapturing)
            {
                Debug.LogWarning("キャプチャ中です");
                return;
            }

            // CaptureAsync を繰り返し呼び出して、複数の写真をキャプチャすることも可能
            isPhotoCapturing = true;
            var capturedPhoto = await lowLagCapture.CaptureAsync();
            isPhotoCapturing = false;

            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;
            int w = softwareBitmap.PixelWidth;
            int h = softwareBitmap.PixelHeight;
            Debug.Log($"{w} x {h}");

            if (bytes == null)
            {
                bytes = new byte[w * h * 4];
            }
            // 上下反転したデータが格納される
            softwareBitmap.CopyToBuffer(bytes.AsBuffer());

            // 上下反転したデータをnewBytesに格納する、またBGRAをRGBAにする
            int stride = 4;
            var newBytes = new byte[w * h * stride];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w * stride; x += stride)
                {
                    newBytes[(h - 1 - y) * (w * stride) + x + 0] = bytes[y * (w * stride) + x + 2]; // R
                    newBytes[(h - 1 - y) * (w * stride) + x + 1] = bytes[y * (w * stride) + x + 1]; // G
                    newBytes[(h - 1 - y) * (w * stride) + x + 2] = bytes[y * (w * stride) + x + 0]; // B
                    newBytes[(h - 1 - y) * (w * stride) + x + 3] = bytes[y * (w * stride) + x + 3]; // A
                }
            }

            // キャプチャした画像の表示 
            if (tex == null)
            {
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            }
            tex.LoadRawTextureData(newBytes);
            tex.Apply();
            rawImage.texture = tex;
        }

        #region MediaCapture Event
        /// <summary>
        /// カメラストリームの状態が変化したときに発生する
        /// </summary>
        private void MediaCapture_CameraStreamStateChanged(MediaCapture sender, object args)
        {
            Debug.Log("MediaCapture CameraStreamStateChanged.");
        }

        /// <summary>
        /// キャプチャデバイスの排他制御ステータスが変更されたときに発生する
        /// </summary>
        private void MediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            Debug.Log("MediaCapture CaptureDeviceExclusiveControlStatusChanged.");
        }

        /// <summary>
        /// メディアのキャプチャ中にエラーが発生したときに発生する
        /// </summary>
        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.LogError("MediaCapture Failed.");
        }

        /// <summary>
        /// キャプチャデバイスがフォーカスを変更したときに発生する
        /// </summary>
        private void MediaCapture_FocusChanged(MediaCapture sender, MediaCaptureFocusChangedEventArgs args)
        {
            Debug.Log("MediaCapture FocusChanged.");
        }

        /// <summary>
        /// 写真確認フレームがキャプチャされたときに発生する
        /// </summary>
        private void MediaCapture_PhotoConfirmationCaptured(MediaCapture sender, PhotoConfirmationCapturedEventArgs args)
        {
            Debug.Log("MediaCapture PhotoConfirmationCaptured.");
        }

        /// <summary>
        /// 1つの録画の上限 (現在は 3 時間) を超える場合に発生する
        /// </summary>
        private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            Debug.LogWarning("Record limitation exceeded. Capture stopped.");
        }

        /// <summary>
        /// キャプチャデバイスの熱ステータスが変化したときに発生する
        /// </summary>
        private async void MediaCapture_ThermalStatusChanged(MediaCapture sender, object args)
        {
            if (mediaCapture.ThermalStatus == MediaCaptureThermalStatus.Overheated)
            {
                Debug.LogWarning("ThermalStatus is overheated. Capture stopped.");
            }
        }
        #endregion
#endif

    }
}
