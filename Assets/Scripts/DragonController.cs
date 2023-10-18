using FIMSpace.FSpine;
using FIMSpace.GroundFitter;
using UnityEngine;
using System.Collections;
using FIMSpace;
using FIMSpace.Basics;

public class DragonController : FGroundFitter_MovementLook
{
    private FSpineAnimator spine;
    private float doubleClickTimer = 0f;
    private bool flying = false;
    private Quaternion targetFlyRotation;
    private bool MoveForward;
    private float verticleFlySpeed = 0f;
    private FBasic_TPPCameraBehaviour tpp;
    [SerializeField] private GameObject _windPrefab;

    public ParticleSystem particleSystem;

    protected override void Start()
    {
        base.Start();

        clips.AddClip("JumpUp");
        clips.AddClip("FallToLanding");
        clips.AddClip("ToFly");

        clips.AddClip("FlyStationary");
        clips.AddClip("Fly");
        clips.AddClip("Glide");

        tpp = Camera.main.GetComponent<FBasic_TPPCameraBehaviour>();
    }


    protected override void Update()
    {
        base.Update();
        doubleClickTimer -= Time.deltaTime;


        if (MoveVector == Vector3.forward)
        {
            Debug.Log("MoveForward = true;");
            MoveForward = true;
        }

        else
        {
            MoveForward = false;
            Debug.Log("MoveForward = false");
        }

        if (flying)
        {
            tpp.FollowingOffset = new Vector3(0f, Mathf.Lerp(tpp.FollowingOffset.y, 1f, Time.deltaTime * 2f));
            tpp.FollowingOffsetDirection = new Vector3(0f, 0f,
                Mathf.Lerp(tpp.FollowingOffsetDirection.z, -15f, Time.deltaTime * 2f));
        }
        else
        {
            tpp.FollowingOffset = new Vector3(0f, Mathf.Lerp(tpp.FollowingOffset.y, 3f, Time.deltaTime * 2f));
            tpp.FollowingOffsetDirection = new Vector3(0f, 0f,
                Mathf.Lerp(tpp.FollowingOffsetDirection.z, -7f, Time.deltaTime * 2f));
        }
    }

    protected override void HandleAnimations()
    {
        if (!flying)
        {
            if (inAir)
            {
                if (YVelocity < -3f) CrossfadeTo("FallToLanding", 0.7f);
            }
            else
            {
                if (ActiveSpeed > 0.15f)
                {
                    if (Sprint)
                    {
                        CrossfadeTo("Run", 0.25f);
                    }
                    else
                    {
                        CrossfadeTo("Walk", 0.25f);
                    }
                }

                else
                {
                    CrossfadeTo("Idle", 0.25f);
                }
            }
        }
        else
        {
            if (MoveForward)
            {
                CrossfadeTo("Fly");
            }
            else
            {
                if (ActiveSpeed > SprintingSpeed)
                {
                    CrossfadeTo("Glide");
                }
                else
                {
                    if (ActiveSpeed > BaseSpeed)
                    {
                        CrossfadeTo("Fly");
                    }
                    else
                    {
                        CrossfadeTo("FlyStationary");
                    }
                }
            }
        }

        // If object is in air we just slowing animation speed to zero
        if (animatorHaveAnimationSpeedProp)
            FAnimatorMethods.LerpFloatValue(animator, "AnimationSpeed",
                MultiplySprintAnimation ? (ActiveSpeed / BaseSpeed) : Mathf.Min(1f, (ActiveSpeed / BaseSpeed)));
    }

    protected override void HandleGravity()
    {
        if (!flying)
        {
            base.HandleGravity();
        }
        else
        {
            RaycastHit groundHit = fitter.CastRay();
            if (groundHit.transform)
            {
                if (Mathf.Abs(groundHit.point.y - transform.position.y) < 0.25f)
                {
                    flying = false;
                    RefreshHitGroundVars(groundHit);
                    YVelocity = -ActiveSpeed * transform.rotation.eulerAngles.normalized.y;
                    fitter.UpAxisRotation = transform.rotation.eulerAngles.y;
                }
            }
        }
    }

    protected override void HandleTransforming()
    {
        if (!flying)
        {
            base.HandleTransforming();
        }
        else
        {
            fitter.enabled = false;
            inAir = true;
            float? yAdjustPos = null;
            targetFlyRotation = Camera.main.transform.rotation;
            targetFlyRotation = Quaternion.Euler(targetFlyRotation.eulerAngles.x - 15f,
                targetFlyRotation.eulerAngles.y + RotationOffset, targetFlyRotation.eulerAngles.z);

            if (MoveForward)
            {
                if (!Sprint)
                {
                    ActiveSpeed = Mathf.Lerp(ActiveSpeed, BaseSpeed * 4f, delta * AccelerationSpeed * 0.5f);
                    Debug.Log("Normal fly");
                   
                }
                else
                {
                    ActiveSpeed = Mathf.Lerp(ActiveSpeed, SprintingSpeed * 5f, delta * AccelerationSpeed * 0.35f);
                    Debug.Log("Sprint fly");
                    particleSystem.Play();
                    // Debug.Log(ActiveSpeed);
                }
            }
            else
            {
                Debug.Log("Stand in air");
                ActiveSpeed = Mathf.Lerp(ActiveSpeed, -0.05f, Time.deltaTime * 0.75f);
                if (ActiveSpeed < 0f) ActiveSpeed = 0f;
                particleSystem.Stop();
            }

            transform.position += transform.forward * ActiveSpeed * delta;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetFlyRotation, Time.deltaTime * 3f);

            if (Input.GetKey(KeyCode.LeftControl)) ActiveSpeed = Mathf.Lerp(ActiveSpeed, 0f, Time.deltaTime * 4f);
            if (Input.GetKey(KeyCode.Space)) verticleFlySpeed = Mathf.Lerp(verticleFlySpeed, 1f, Time.deltaTime);
            else if (Input.GetKey(KeyCode.Z)) verticleFlySpeed = Mathf.Lerp(verticleFlySpeed, -1f, Time.deltaTime);
            else
            {
                verticleFlySpeed = Mathf.Lerp(verticleFlySpeed, 0f, Time.deltaTime);
            }

            transform.position += Vector3.up * verticleFlySpeed * 20f * delta;
        }
    }


    float? yAdjustPos = null;

    protected override void ApplyTransforming()
    {
        if (!flying)
        {
            base.ApplyTransforming();
        }
        else
        {
            if (UsePhysics && rigb)
            {
                float yVelo = YVelocity;

                if (!inAir)
                    if (yAdjustPos != null)
                    {
                        yVelo = (yAdjustPos.Value - rigb.position.y) / Time.fixedDeltaTime;
                        //yAdjustPos = null;
                    }

                rigb.velocity = new Vector3(lastVelocity.x, yVelo, lastVelocity.z);
            }
        }
    }


    public override void Jump()
    {
        base.Jump();
        CrossfadeTo("JumpUp", 0.5f);
        if (doubleClickTimer > 0f)
        {
            StartFlying();
        }

        doubleClickTimer = 0.5f;
    }


    private void StartFlying()
    {
        flying = true;
        CrossfadeTo("Fly");
    }

    // private void WindEffect()
    // {
    //     if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W)) {
    //         // Shift key is being pressed - enable particle system
    //         particleSystem.Play();
    //     } else {
    //         // Shift key is not being pressed - disable particle system
    //         
    //         particleSystem.Stop();
    //     }
    // }
}