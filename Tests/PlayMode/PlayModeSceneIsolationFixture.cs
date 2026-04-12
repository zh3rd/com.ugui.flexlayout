using System.Collections;
using System;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEngine.UI.Flex.Tests.PlayMode
{
    public abstract class PlayModeSceneIsolationFixture
    {
        private Scene m_PreviousActiveScene;
        private Scene m_TestScene;

        [UnitySetUp]
        public IEnumerator UnitySetUpSceneIsolation()
        {
            m_PreviousActiveScene = SceneManager.GetActiveScene();
            m_TestScene = SceneManager.CreateScene($"FlexPlayMode_{Guid.NewGuid():N}");
            SceneManager.SetActiveScene(m_TestScene);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDownSceneIsolation()
        {
            if (m_PreviousActiveScene.IsValid() && m_PreviousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(m_PreviousActiveScene);
            }

            if (m_TestScene.IsValid() && m_TestScene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(m_TestScene);
                if (unload != null)
                {
                    while (!unload.isDone)
                    {
                        yield return null;
                    }
                }
            }

            yield return null;
        }
    }
}
