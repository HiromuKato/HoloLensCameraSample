using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if WINDOWS_UWP
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;
#endif

namespace HoloLensCameraSample
{
    /// <summary>
    /// MediaCapture でビデオキャプチャを行うサンプル
    /// Capapilities で VideosLibrary, Webcam, Microphone を有効にすること
    ///
    /// 参考：
    /// https://docs.microsoft.com/ja-jp/windows/uwp/audio-video-camera/basic-photo-video-and-audio-capture-with-mediacapture
    /// https://docs.microsoft.com/ja-jp/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers
    /// </summary>
    public class MediaCaptureVideoSample : MonoBehaviour
    {
        /// <summary>
        /// 動画撮影完了時に呼ばれるイベントリスナー
        /// </summary>
        public Action<string> VideoCapturedListener = null;

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

        public void StartCapture()
        {
#if WINDOWS_UWP
            StartCaptureUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }

        public void StopCapture()
        {
#if WINDOWS_UWP
            StopCaptureUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }

        public void PauseCapture()
        {
#if WINDOWS_UWP
            PauseCaptureUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }

        public void ResumeCapture()
        {
#if WINDOWS_UWP
            ResumeCaptureUWP();
#else
            Debug.LogWarning("MediaCapture works only WINDOWS_UWP.");
#endif
        }


#if WINDOWS_UWP
        private bool isRecording = false;
        private MediaCapture mediaCapture;
        private LowLagMediaRecording mediaRecording;
        private string fileName;

        /// <summary>
        /// ビデオに効果を追加するためのクラス
        /// </summary>
        private class VideoEffectDefinition : IVideoEffectDefinition
        {
            public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";

            public IPropertySet Properties { get; private set; }

            // コンストラクタ
            public VideoEffectDefinition()
            {
                Properties = new PropertySet();
                // ビデオキャプチャのホログラムを有効または無効にするフラグ
                Properties.Add("HologramCompositionEnabled", true);
                // ホログラムのキャプチャ中に画面の記録インジケーターを有効または無効にするフラグ
                Properties.Add("RecordingIndicatorEnabled", true);
                // ホログラムのグローバル不透明度係数を設定する
                Properties.Add("GlobalOpacityCoefficient", 0.0f);
                // キャプチャする holographic カメラビューの構成を示すために使用される列挙
                Properties.Add("PreferredHologramPerspective", 1);
            }
        }

        /// <summary>
        /// オーディオをビデオに含めるためのクラス
        /// </summary>
        private class AudioEffectDefinition : IAudioEffectDefinition
        {
            public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureAudioEffect";

            public IPropertySet Properties { get; private set; }

            // コンストラクタ
            public AudioEffectDefinition()
            {
                Properties = new PropertySet();
                // 使用するオーディオソースを示すために使用する列挙
                // 0 (Mic オーディオのみ)、1 (システムオーディオのみ)、2 (Mic およびシステムオーディオ)
                Properties.Add("MixerMode", 2);
                // システムオーディオボリューム(範囲は 0.0 - 5.0)
                Properties.Add("LoopbackGain", 5.0);
                // Mic ボリューム( 範囲は 0.0 - 5.0)
                Properties.Add("MicrophoneGain", 5.0);
            }
        }

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
                foreach (var d in p.SupportedRecordMediaDescription)
                {
                    Debug.Log($"{d.Width}x{d.Height} {d.FrameRate}");
                }
            }

            // 好みの値を指定する
            // 参考：https://docs.microsoft.com/ja-jp/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2
            var width = 960;
            var height = 540;
            var framerate = 30;
            var match = (from profile in profiles
                         from desc in profile.SupportedRecordMediaDescription
                         where desc.Width == width && desc.Height == height && Math.Round(desc.FrameRate) == framerate
                         select new { profile, desc }).FirstOrDefault();

            if (match != null)
            {
                mediaInitSettings.VideoProfile = match.profile;
                mediaInitSettings.RecordMediaDescription = match.desc;
                Debug.Log($"Selected media : {match.desc.Width}x{match.desc.Height}");
            }
            else
            {
                // Could not locate a WVGA 30FPS profile, use default video recording profile
                Debug.LogWarning("Can't find profile.");
                mediaInitSettings.VideoProfile = profiles[0];
            }

            mediaCapture = null;
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(mediaInitSettings);
            await mediaCapture.AddVideoEffectAsync(new MediaCaptureVideoSample.VideoEffectDefinition(), MediaStreamType.VideoRecord);
            await mediaCapture.AddAudioEffectAsync(new MediaCaptureVideoSample.AudioEffectDefinition());

            mediaCapture.CameraStreamStateChanged += MediaCapture_CameraStreamStateChanged;
            mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            mediaCapture.Failed += MediaCapture_Failed;
            mediaCapture.FocusChanged += MediaCapture_FocusChanged;
            mediaCapture.PhotoConfirmationCaptured += MediaCapture_PhotoConfirmationCaptured;
            mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;
            mediaCapture.ThermalStatusChanged += MediaCapture_ThermalStatusChanged;
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

            if (isRecording)
            {
                await mediaRecording.StopAsync();
                await mediaRecording.FinishAsync();
            }

            mediaCapture.CameraStreamStateChanged -= MediaCapture_CameraStreamStateChanged;
            mediaCapture.CaptureDeviceExclusiveControlStatusChanged -= MediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            mediaCapture.Failed -= MediaCapture_Failed;
            mediaCapture.FocusChanged -= MediaCapture_FocusChanged;
            mediaCapture.PhotoConfirmationCaptured -= MediaCapture_PhotoConfirmationCaptured;
            mediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
            mediaCapture.ThermalStatusChanged -= MediaCapture_ThermalStatusChanged;

            mediaCapture = null;
            Debug.Log("Cleaned up.");
        }

        /// <summary>
        /// ビデオキャプチャを開始する
        /// </summary>
        private async Task StartCaptureUWP()
        {
            if (isRecording)
            {
                Debug.LogWarning("Already starting capture.");
                return;
            }

            Debug.Log("Start capture.");

            isRecording = true;

            // ビデオの保存先を指定する（ビデオライブラリに保存する場合）
            /*
            StorageLibrary myVideos = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            StorageFile file = await myVideos.SaveFolder.CreateFileAsync("video.mp4", CreationCollisionOption.GenerateUniqueName);
            */

            // ビデオの保存先を指定する（ドキュメント配下に保存する場合）
            /*
            StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
            var videoFolder = await documentsFolder.GetFolderAsync("<FolderName>");
            */

            // ビデオの保存先を指定する（アプリ内に保存する場合）
            StorageFolder videoFolder = ApplicationData.Current.LocalFolder;
            // ファイル名生成
            var now = DateTime.Now;
            fileName = now.ToString($"{now:yyyyMMddHHmmss}") + "_video.mp4";
            StorageFile file = await videoFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            // ストレージファイルとビデオのエンコードを指定する
            mediaRecording = await mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);

            try
            {
                await mediaRecording.StartAsync();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                // キャプチャ失敗 nullを渡す
                if (VideoCapturedListener != null)
                {
                    VideoCapturedListener(null);
                }
            }
        }

        /// <summary>
        /// ビデオキャプチャを停止する
        /// </summary>
        private async Task StopCaptureUWP()
        {
            if (!isRecording)
            {
                return;
            }
            Debug.Log("Stop capture.");

            await mediaRecording.StopAsync();

            // ビデオキャプチャ終了を知らせるコールバック
            if (VideoCapturedListener != null)
            {
                VideoCapturedListener(fileName);
            }

            isRecording = false;

            await mediaRecording.FinishAsync();
        }

        /// <summary>
        /// ビデオキャプチャの一時停止を行う
        /// </summary>
        private async Task PauseCaptureUWP()
        {
            await mediaRecording.PauseAsync(Windows.Media.Devices.MediaCapturePauseBehavior.ReleaseHardwareResources);
        }

        /// <summary>
        /// ビデオキャプチャの一時停止を再開する
        /// </summary>
        private async Task ResumeCaptureUWP()
        {
            await mediaRecording.ResumeAsync();
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
            await mediaRecording.StopAsync();
            Debug.LogWarning("Record limitation exceeded. Capture stopped.");
        }

        /// <summary>
        /// キャプチャデバイスの熱ステータスが変化したときに発生する
        /// </summary>
        private async void MediaCapture_ThermalStatusChanged(MediaCapture sender, object args)
        {
            if (mediaCapture.ThermalStatus == MediaCaptureThermalStatus.Overheated)
            {
                await mediaRecording.StopAsync();
                Debug.LogWarning("ThermalStatus is overheated. Capture stopped.");
            }
        }
        #endregion
#endif

    }
}
