using UnityEngine;
using System.Collections;

internal enum CarType
{
    Wheel4,
    WheelForward,
    WheelBack
}
public class Motor : MonoBehaviour
{
    public bool UseLookWalk;

    [SerializeField]
    private CarType _CarType = CarType.Wheel4;
  
    public Transform centerOfMass;

    public float enginePower;
    
    public float EngineJump;
    
    [SerializeField]
    float MaxSpeed;
    public bool TypeBreksDown;
    
    public bool OnOffTurn;
    public float RelativeenginePower;
    
    public float turnPower;
    
    public float breaks;
    [Range(0, 1)]
    [SerializeField]
    private float m_SteerHelper;
    [Range(0, 1)]
    [SerializeField]
    private float m_TractionControl;
    public Wheel[] wheel;
    [SerializeField]
    private WheelEffect[] m_WheelEffects = new WheelEffect[4];
    [SerializeField]
    private WheelCollider[] m_WheelColliders = new WheelCollider[4];
    [SerializeField]
    private GameObject[] m_WheelMeshes = new GameObject[4];
    [SerializeField]
    private HitCar[] m_HitCollider = new HitCar[3];
    
    public float m_SlipLimit;
    private Quaternion[] m_WheelMeshLocalRotations;

    private float m_OldRotation;
    
    private float acceleration;
    
    private bool jumping;

    public bool RoofCar;
    public bool FrontCar;
    public bool BackCar;
    public GameObject Floor;
    Rigidbody rbody;
    private float m_CurrentTorque;
    private float speed;
    private float torque;
    private float turnSpeed;
    private bool MotorInBrige;
    private bool TargetHit;
    public bool NonPersonMove;
    private float FreezY;
    private int CountWheels;

    // Use this for initialization
    void Awake()
    {
        rbody = GetComponent<Rigidbody>();
    }
    void Start()
    {
        CountWheels = wheel.Length;
        rbody.centerOfMass = centerOfMass.localPosition;
        m_WheelMeshLocalRotations = new Quaternion[4];
        RelativeenginePower = enginePower;
        for (int i = 0; i < CountWheels; i++)
        {
            m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;
        }
        m_CurrentTorque = enginePower - (m_TractionControl * enginePower);
        FreezY = -180f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (NonPersonMove)
        {
            torque = Input.GetAxis("Vertical");
                                               
            turnSpeed = Input.GetAxis("Horizontal") * turnPower;
            if(UseLookWalk)
            {
                torque = 1f;
                Move(torque, turnSpeed);
                TractionControl();
            }
            else if (TargetHit && !UseLookWalk)
            {
                Move(torque, turnSpeed);
                TractionControl();
            }
            else if(!UseLookWalk)
            {
                torque = 0;
                Break();
                KinematicBreak();
            }
            else 
            {
                torque = 0.5f;
                Move(torque, turnSpeed);
                TractionControl();
            }
            
        }
    }

    public void Hit(bool hit)
    {
        TargetHit = hit;
    }

    private void SteerHelper()
    {
        for (int i = 0; i < CountWheels; i++)
        {
            WheelHit wheelhit;
            m_WheelColliders[i].GetGroundHit(out wheelhit);
            if (wheelhit.normal == Vector3.zero)
                return;
        }
        
        if (Mathf.Abs(m_OldRotation - transform.eulerAngles.y) < 10f)
        {
            var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
            rbody.velocity = velRotation * rbody.velocity;
        }
        m_OldRotation = transform.eulerAngles.y;
    }
    IEnumerator LowAcceleration()
    {
        for (float i = 0; i < 40; i++)
        {
            yield return new WaitForSeconds(1f);
        }

    }
    
    public void Move(float torque, float turnSpeed)
    {
        speed = rbody.velocity.magnitude * 3.6f;
        SteerHelper();

        if (Input.GetKey(KeyCode.LeftControl) && wheel[0].Ground() && wheel[1].Ground() && wheel[2].Ground() && wheel[3].Ground())
        {
            wheel[0].JumpDamper(0);
            wheel[1].JumpDamper(0);
            wheel[2].JumpDamper(0);
            wheel[3].JumpDamper(0);
            var jump = EngineJump * rbody.mass;
            rbody.AddForce(rbody.transform.up * jump, ForceMode.Impulse);
            jumping = true;
        }
        else if (jumping && wheel[0].Ground() && wheel[1].Ground() && wheel[2].Ground() && wheel[3].Ground())
        {
            for (int i = 0; i < CountWheels; i++)
            {
                wheel[i].JumpDamper(3500);
            }
            jumping = false;

        }
        Break();
       
        switch (_CarType)
        {
            case CarType.Wheel4:
                wheel[0].Move(torque * (m_CurrentTorque / 3f));
                wheel[1].Move(torque * (m_CurrentTorque / 3f));
                wheel[2].Move(torque * (m_CurrentTorque / 3f));
                wheel[0].Break(0);
                wheel[1].Break(0);
                wheel[2].Break(0);
                break;
            case CarType.WheelBack:
                wheel[1].Move(torque * (m_CurrentTorque / 2f));
                wheel[2].Move(torque * (m_CurrentTorque / 2f));
                wheel[0].Break(0);
                wheel[1].Break(0);
                wheel[2].Break(0);
                break;
            case CarType.WheelForward:
                wheel[0].Move(torque * (m_CurrentTorque / 2f));
                wheel[1].Move(torque * (m_CurrentTorque / 2f));
                wheel[0].Break(0);
                wheel[1].Break(0);
                wheel[2].Break(0);
                wheel[3].Break(0);
                break;
        }
       
        if (speed > MaxSpeed)
            rbody.velocity = (MaxSpeed / 3.6f) * rbody.velocity.normalized;

        KinematicBreak();
        if (Input.GetKey(KeyCode.Space))
        {
            switch (_CarType)
            {
                case CarType.Wheel4:
                    for (int i = 0; i < CountWheels; i++)
                    {
                        wheel[i].Break(breaks);
                        wheel[i].HandBrake(3f);
                    }
                    break;
                case CarType.WheelBack:
                    wheel[2].Break(breaks);
                    wheel[2].HandBrake(3f);
                    wheel[3].Break(breaks);
                    wheel[3].HandBrake(3f);
                    break;
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            switch (_CarType)
            {
                case CarType.Wheel4:
                    for (int i = 0; i < CountWheels; i++)
                    {
                        wheel[i].Break(0);
                        wheel[i].HandBrake(1.5f);
                    }
                    break;
                case CarType.WheelBack:
                    wheel[2].Break(0);
                    wheel[2].HandBrake(1.5f);
                    wheel[3].Break(0);
                    wheel[3].HandBrake(1.5f);
                    break;
            }
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            RelativeenginePower = enginePower * acceleration;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            RelativeenginePower = enginePower;
        }

        Quaternion quat;
        Vector3 position;
        m_WheelColliders[3].GetWorldPose(out position, out quat);
        m_WheelMeshes[4].transform.rotation = quat;

        if (!OnOffTurn)
        {
            wheel[0].Turn(turnSpeed);
        }
        else
        {
            gameObject.transform.rotation = new Quaternion(gameObject.transform.rotation.x, FreezY, gameObject.transform.rotation.z, gameObject.transform.rotation.w);
        }
        RoofCar = m_HitCollider[0].OnTrigger;
        BackCar = m_HitCollider[1].OnTrigger;
        FrontCar = m_HitCollider[2].OnTrigger;
    }
    
    private void Break()
    {
        if (speed > 5 && Vector3.Angle(transform.forward, rbody.velocity) < 50f && torque < 0)
        {
            
            for (int i = 0; i < CountWheels; i++)
            {
                if (TypeBreksDown)
                {
                    wheel[i].Break(breaks);
                    torque = 0;
                }
                else
                {
                    wheel[i].Break(breaks * 2);
                    wheel[i].HandBrake(5f);
                    torque = 0;
                }
            }

        }
        else
        {
            for (int i = 0; i < CountWheels; i++)
            {
                wheel[i].HandBrake(3f);
            }
        }
    }

    public float GetVelosity()
    {
        Rigidbody MainRigidBody = gameObject.GetComponent<Rigidbody>();
        return MainRigidBody.velocity.x;
    }

    private void KinematicBreak()
    {
        if (torque == 0)
        {
            for (int i = 0; i < CountWheels; i++)
            {
                wheel[i].Break(breaks / 40);
            }
        }
        else
        {
            for (int i = 0; i < CountWheels; i++)
            {
                wheel[i].Break(0);
            }
        }
    }

    private void TractionControl()
    {
        WheelHit wheelHit;
        for (int i = 0; i < CountWheels; i++)
        {
            m_WheelColliders[i].GetGroundHit(out wheelHit);

            JustTorque(wheelHit.forwardSlip);
        }
    }
    private void JustTorque(float forwardSlip)
    {
        if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0)
        {
            m_CurrentTorque -= 50 * m_TractionControl;
        }
        else
        {
            m_CurrentTorque += 50 * m_TractionControl;
            if (m_CurrentTorque > enginePower)
            {
                m_CurrentTorque = enginePower;
            }
        }
    }


    public void DisableBreak()
    {
        for (int i = 0; i < CountWheels; i++)
        {
            wheel[i].Break(0);
        }
    }
    

    public void OnTriggerBar()
    {
        GameObject MC = GameObject.Find("MainControll");
        TypeControll TC = MC.GetComponent<TypeControll>();
        GameObject Person = GameObject.Find("FPSController");
        Vector3 PersonLocalPosition = Person.transform.position;
    }

    public void OffTriggerBar()
    {
        MotorInBrige = false;
        GameObject MC = GameObject.Find("MainControll");
        TypeControll TC = MC.GetComponent<TypeControll>();
        TC.SetTypeControll("Person");
        GameObject Person = GameObject.Find("FPSController");
        Vector3 PersonLocalPosition = Person.transform.position;
    }

}
