using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hydrogen.Entities
{
    public class SampleSceneLoader : MonoBehaviour
    {
        [SerializeField, Range(0.0001f, 10.0f)]
        private float m_waitDelay = 2.0f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(m_waitDelay);

            yield return StartCoroutine(LoadScene("DontReplaceConverters"));
            
            yield return new WaitForSeconds(5.0f);

            yield return StartCoroutine(LoadScene("SimpleSubSceneLoader", LoadSceneMode.Single));
        }
        

        private static IEnumerator LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
            
            op.allowSceneActivation = true;

            yield return op;
        }
    }
}
