using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// クラス：プライヤー操作キャラクターのコントローラ.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float animSpeed = 1.5f;
    
#region キャラクターコントローラ用パラメタ
    // 前進速度
    [SerializeField]
    private float forwardSpeed = 7.0f;
    // 後退速度
    [SerializeField]
    private float backwardSpeed = 2.0f;
    // 旋回速度
    [SerializeField]
    private float rotateSpeed = 2.0f;
    // ジャンプ威力
    [SerializeField]
    private float jumpPower = 3.0f; 
#endregion
    
    // Mecanimでカーブ調整を使うか設定する
    [SerializeField]
    private bool useCurves= true;
    [SerializeField]
    private float useCurvesHeight = 0.5f;        // カーブ補正の有効高さ（地面をすり抜けやすい時には大きくする）
    
    void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
        m_col = GetComponent<CapsuleCollider>();
        // CapsuleColliderコンポーネントのHeight、Centerの初期値を保存する
        m_orgColHight = m_col.height;
        m_orgVectColCenter = m_col.center;
        
        LookAtPointCameraController.Create(this.transform);
    }
    
    // メイン処理.リジッドボディと絡めるので、FixedUpdate内で処理を行う.
    void FixedUpdate ()
    {
        float h = Input.GetAxis("Horizontal");                  // 入力デバイスの水平軸をhで定義
        float v = Input.GetAxis("Vertical");                    // 入力デバイスの垂直軸をvで定義
        m_animator.SetFloat("Speed", v);                        // Animator側で設定している"Speed"パラメタにvを渡す
        m_animator.SetFloat("Direction", h);                    // Animator側で設定している"Direction"パラメタにhを渡す
        m_animator.speed = animSpeed;                           // Animatorのモーション再生速度に animSpeedを設定する
        m_currentBaseState = m_animator.GetCurrentAnimatorStateInfo(0); // 参照用のステート変数にBase Layer (0)の現在のステートを設定する
        m_rb.useGravity = true;//ジャンプ中に重力を切るので、それ以外は重力の影響を受けるようにする
        
        // 以下、キャラクターの移動処理
        m_velocity = new Vector3(0, 0, v);        // 上下のキー入力からZ軸方向の移動量を取得
        // キャラクターのローカル空間での方向に変換
        m_velocity = transform.TransformDirection(m_velocity);
        //以下のvの閾値は、Mecanim側のトランジションと一緒に調整する
        if (v > 0.1) {
            this.ResetAttack();
            m_velocity *= forwardSpeed;       // 移動速度を掛ける
        } else if (v < -0.1) {
            this.ResetAttack();
            m_velocity *= backwardSpeed;  // 移動速度を掛ける
        }

        // ジャンプ
        if (Input.GetButtonDown("Jump")) {
            // アニメーションのステートがLocomotionの最中かつステート遷移中でなかったらジャンプできる
            if(m_currentBaseState.fullPathHash == locoState && !m_animator.IsInTransition(0)){
                m_rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
                m_animator.SetBool("Jump", true);     // Animatorにジャンプに切り替えるフラグを送る
            }
        }
        // 攻撃1
        if (m_currentBaseState.fullPathHash != atk3State && Input.GetButtonDown("Fire1")) {
            if(!m_animator.IsInTransition(0)){
                if(m_currentBaseState.fullPathHash == atk1State){
                    m_animator.SetBool("Attack2", true);
                }else if(m_currentBaseState.fullPathHash == atk2State){
                    m_animator.SetBool("Attack3", true);
                }else{
                    m_animator.SetBool("Attack1", true);
                }
            }
        }

        // 上下のキー入力でキャラクターを移動させる
        transform.localPosition += m_velocity * Time.fixedDeltaTime;

        // 左右のキー入力でキャラクタをY軸で旋回させる
        transform.Rotate(0, h * rotateSpeed, 0);    

        // 以下、Animatorの各ステート中での処理
        if(m_currentBaseState.fullPathHash == locoState){
            this.StateProcLocomotion();
        }else if(m_currentBaseState.fullPathHash == jumpState){
            this.StateProcJump();
        }else if(m_currentBaseState.fullPathHash == idleState){
            this.StateProcIdle();
        }else if(m_currentBaseState.fullPathHash == restState){
            this.StateProcRest();
        }else if(m_currentBaseState.fullPathHash == atk1State){
            this.StateProcAttack();
        }else if(m_currentBaseState.fullPathHash == atk2State){
            this.StateProcAttack();
        }else if(m_currentBaseState.fullPathHash == atk3State){
            this.ResetAttack();
        }
    }
    
#region 状態遷移処理

    // 移動状態
    void StateProcLocomotion()
    {
        // カーブでコライダ調整をしている時は、念のためにリセットする
        if(useCurves){
            ResetCollider();
        }
    }
    
    // ジャンプ状態
    void StateProcJump()
    {
        // ステートがトランジション中でない場合
        if(m_animator.IsInTransition(0)){
            return;
        }
        // 以下、カーブ調整をする場合の処理
        if(useCurves){
            // 以下JUMP00アニメーションについているカーブJumpHeightとGravityControl
            // JumpHeight:JUMP00でのジャンプの高さ（0〜1）
            // GravityControl:1⇒ジャンプ中（重力無効）、0⇒重力有効
            float jumpHeight = m_animator.GetFloat("JumpHeight");
            float gravityControl = m_animator.GetFloat("GravityControl"); 
            if(gravityControl > 0){
                m_rb.useGravity = false;  //ジャンプ中の重力の影響を切る
            }

            // レイキャストをキャラクターのセンターから落とす
            Ray ray = new Ray(transform.position + Vector3.up, -Vector3.up);
            RaycastHit hitInfo = new RaycastHit();
            // 高さが useCurvesHeight 以上ある時のみ、コライダーの高さと中心をJUMP00アニメーションについているカーブで調整する
            if (Physics.Raycast(ray, out hitInfo)){
                if (hitInfo.distance > useCurvesHeight){
                    m_col.height = m_orgColHight - jumpHeight;          // 調整されたコライダーの高さ
                    float adjCenterY = m_orgVectColCenter.y + jumpHeight;
                    m_col.center = new Vector3(0, adjCenterY, 0); // 調整されたコライダーのセンター
                }
                else{
                    // 閾値よりも低い時には初期値に戻す（念のため）                   
                    ResetCollider();
                }
            }
        }
        // Jump bool値をリセットする（ループしないようにする）               
        m_animator.SetBool("Jump", false);
    }
    
    // 待機状態
    void StateProcIdle()
    {
        //カーブでコライダ調整をしている時は、念のためにリセットする
        if(useCurves){
            ResetCollider();
        }
        // スペースキーを入力したらRest状態になる
        if (Input.GetButtonDown("Jump")) {
            m_animator.SetBool("Rest", true);
        }
    }
    
    // 休憩状態
    void StateProcRest()
    {
        // ステートが遷移中でない場合、Rest bool値をリセットする（ループしないようにする）
        if(!m_animator.IsInTransition(0)){
            m_animator.SetBool("Rest", false);
        }
    }
    
    // 攻撃状態
    void StateProcAttack()
    {
        this.StartCoroutine("DelayEndAttack");
        
    }
    
#endregion
    
    private IEnumerator DelayEndAttack()
    {
        yield return new WaitForSeconds(2f);
        if(!m_animator.IsInTransition(0)){
            this.ResetAttack();
        }  
    }
    
    // キャラクターのコライダーサイズのリセット.コンポーネントのHeight、Centerの初期値を戻す.
    private void ResetCollider()
    {
        m_col.height = m_orgColHight;
        m_col.center = m_orgVectColCenter;
    }
    
    private void ResetAttack()
    {
        m_animator.SetBool("Attack1", false);
        m_animator.SetBool("Attack2", false);
        m_animator.SetBool("Attack3", false);
    }
    
    private Animator m_animator;
    private AnimatorStateInfo m_currentBaseState;         // base layerで使われる、アニメーターの現在の状態の参照
    private Vector3 m_velocity;   // キャラクターコントローラ（カプセルコライダ）の移動量
    private Rigidbody m_rb;
    private CapsuleCollider m_col;
    private float m_orgColHight;    // CapsuleColliderで設定されているコライダのHeiht、Centerの初期値を収める変数
    private Vector3 m_orgVectColCenter;
    
    // アニメーター各ステートへの参照
    private static readonly int idleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int locoState = Animator.StringToHash("Base Layer.Locomotion");
    private static readonly int jumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int restState = Animator.StringToHash("Base Layer.Rest");
    private static readonly int atk1State = Animator.StringToHash("Base Layer.M_Kick");
    private static readonly int atk2State = Animator.StringToHash("Base Layer.RoleKick");
    private static readonly int atk3State = Animator.StringToHash("Base Layer.DropKick");
}
