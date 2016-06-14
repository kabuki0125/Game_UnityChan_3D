using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// クラス：定められた点を中心に旋回するカメラコントローラ
/// </summary>
public class LookAtPointCameraController : UIBase
{
    
    [SerializeField]
    private float spinSpeed = 0.1f;
    [SerializeField]
    private Vector3 postionFromCenter = new Vector3(0f, 1.5f, -3f);
    
    void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
    
    /// <summary>
    /// 生成.
    /// </summary>
    public static LookAtPointCameraController Create(Transform target)
    {
        var go = Instantiate(Resources.Load("LookAtPointCamera")) as GameObject;
        var com = go.GetComponent<LookAtPointCameraController>();
        com.InitInternal(target);
        return com;
    }
    private void InitInternal(Transform target)
    {
        m_target = target;
        m_camera = this.GetScript<Camera>("camera");
        m_tCenter = this.GetScript<Transform>("center");
        
        m_camera.transform.position = m_target.position + postionFromCenter;
    }
	
	void Update () 
    {
        // 右クリックを押しながらでカメラ視点方向移動.
        if(Input.GetButton("Fire2")){
            m_mousePos += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * spinSpeed;
            m_mousePos.x = Mathf.Clamp(m_mousePos.x, -3f, 3f);
            m_mousePos.y = Mathf.Clamp(m_mousePos.y, -0.5f, 0.5f);
            m_mousePos.y = Math.Abs(m_mousePos.y) >= 0.5f ? m_mousePos.y: 0f;   // 縦軸はしっかりドラッグしないと回転しないように調整しとく
            m_tCenter.eulerAngles += new Vector3(m_mousePos.y, m_mousePos.x, 0f);
            
            // 一回転すると困るので縦方向の回転範囲制御
            if(m_mousePos.y > 0 && (m_tCenter.eulerAngles.x >= 35f && m_tCenter.eulerAngles.x < 325f)){
                m_tCenter.eulerAngles = new Vector3(35f, m_tCenter.eulerAngles.y, 0f);
            }else if(m_mousePos.y < 0 && (m_tCenter.eulerAngles.x > 35f &&  m_tCenter.eulerAngles.x <= 325f)){
                m_tCenter.eulerAngles = new Vector3(325f, m_tCenter.eulerAngles.y, 0f);
            }
        }
        if(Input.GetButtonUp("Fire2")){
            m_mousePos = Vector2.zero;
        }
        
        // 向きリセット
        if(Input.GetButtonUp("Fire3")){
            iTween.RotateTo(m_tCenter.gameObject, new Vector3(0f, m_target.eulerAngles.y, 0f), 0.2f);
        }
        
        // 位置は同期
        m_tCenter.position = m_target.position;
	}
    
    private Vector2 m_mousePos = Vector2.zero;
    private Transform m_target;
    private Camera m_camera;
    private Transform m_tCenter;
    
}
