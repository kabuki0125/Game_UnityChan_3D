using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[ExecuteInEditMode]
public class SplashScreen : MonoBehaviour
{
	void NextLevel ()
	{
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}
}
