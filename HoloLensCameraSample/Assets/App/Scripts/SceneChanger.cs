using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SceneSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoloLensCameraSample
{
    /// <summary>
    /// コンテンツシーンを変更するためのクラス
    /// </summary>
    public sealed class SceneChanger : MonoBehaviour
    {
        IMixedRealitySceneSystem sceneSystem;

        // コンテンツのタグ名
        private readonly string content01 = "Content01";
        private readonly string content02 = "Content02";

        void Start()
        {
            sceneSystem = MixedRealityToolkit.Instance.GetService<IMixedRealitySceneSystem>();
        }

        public async void LoadContent01()
        {
            await sceneSystem.LoadContentByTag(content01, LoadSceneMode.Single);
        }

        public async void LoadContent02()
        {
            await sceneSystem.LoadContentByTag(content02, LoadSceneMode.Single);
        }

        public async void LoadContentsAdditive()
        {
            await sceneSystem.LoadContentByTag(content01, LoadSceneMode.Single);
            await sceneSystem.LoadContentByTag(content02, LoadSceneMode.Additive);
        }

        public async void NextContents()
        {
            if (sceneSystem.NextContentExists)
            {
                await sceneSystem.LoadNextContent(true, LoadSceneMode.Single);
            }
        }

        public async void PrevContents()
        {
            if (sceneSystem.PrevContentExists)
            {
                await sceneSystem.LoadPrevContent(true, LoadSceneMode.Single);
            }
        }
    }
}