/*************************************************************************************************************************************************
 *This script was made using Sasquath B studio Tutorial on the ultimate 2D Controller
 *Sasquath B studio. 27/06/2024. ULTIMATE 2D Platformer Controller for Unity (Part 2). Youtube. https://www.youtube.com/watch?v=j1HN7wsFHcY&t=505s
 *Sasquath B studio. 6/06/2024. ULTIMATE 2D Platformer Controller for Unity. Youtube. https://www.youtube.com/watch?v=zHSWG05byEc
 *************************************************************************************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.InputSystem;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    [Header("references")]
    public PlayerMovementStats MoveStats;
    [SerializeField] private Collider2D _feetColl;
    [SerializeField] private Collider2D _bodyColl;
    private Rigidbody2D _rb;
    private SpriteRenderer SpriteRenderer;
    //Movement var
    private float HorizontalVelocity { get; set; }
    private bool _IsFacingRight;

    // Collision check vars
    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private RaycastHit2D _wallHit;
    private RaycastHit2D _lastWallHit;

    //Flags
    private bool _isGrounded;
    private bool _bumpedHead;
    private bool _isTouchingWall;

    //jump vars
    public float _VerticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;

    // wall jump
    private bool _useWallJumpMoveStats;
    private bool _isWallJumping;
    private float _wallJumpTime;
    private bool _isWallJumpFastFalling;
    private bool _isWallJumpFalling;
    private float _wallJumpFastFallTime;
    private float _wallJumpFastFallReleaseSpeed;

    private float _wallJumpPostBufferTimer;

    private float _wallJumpApexPoint;
    private float _timePastWallJumpApexThreshold;
    private bool _isPastWallJumpApexThreshold;

    //wall sliding
    private bool _isWallSliding;
    private bool _isWallSlideFalling;

    // dash vars
    private bool _isdashing;
    private bool _isAirDashing;
    private float _dashTimer;
    private float _dashOnGroundTimer;
    private int _numberOfDashesUsed;

    private Vector2 _dashDirection;
    private bool _isDashFastFalling;
    private float _dashFastFallTime;
    private float _dashFastFallReleaseSpeed;

    //apex vars
    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;

    //jump buffer vars
    private float _jumpBufferTimer;
    private bool _jumpReleasedDuringBuffer;

    //coyote time vars
    private float _coyoteTimer;

    //animator
    private Animator animator;

    private void Awake()
    {
        _IsFacingRight = true;
        SpriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();
        WallSlideCheck();
        LandCheck();
        WallJumpCheck();
        DashCheck();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        WallSlide();
        WallJump();
        Jump();
        Fall();
        Dash();
        if (_isGrounded)
        {
            Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
        }
        else
        {
            //Wall jumping
            if (_useWallJumpMoveStats)
            {
                Move(MoveStats.WallJumpAcceleration, MoveStats.WallJumpDeceleration, InputManager.Movement);
            }
            //Airborne
            else
            {
                Move(MoveStats.AirAcceleration, MoveStats.AirDeceleration, InputManager.Movement);
            }
        }
        ApplyVelocity();
    }

    #region horizontal movement
    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (!_isdashing)
        {

            if (Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold)
            {
                TurnCheck(moveInput);
                //Checks if he needs to turn 

                float targetVelocity = 0f;
                if (InputManager.RunIsHeld)
                {
                    targetVelocity = moveInput.x * MoveStats.MaxRunSpeed;
                }
                else
                {
                    targetVelocity = moveInput.x * MoveStats.MaxWalkSpeed;
                }

                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            }

            else if (Mathf.Abs(moveInput.x) < MoveStats.MoveThreshold)
            {
                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, 0f, deceleration * Time.fixedDeltaTime * MoveStats.Friction);
            }
            animator.SetFloat("Velocity_x", Mathf.Abs(HorizontalVelocity));
        }
    }
    #endregion

    #region Is Right Or Left
    private void TurnCheck(Vector2 MoveInput)
    {
        if (_IsFacingRight && MoveInput.x < 0)
        {
            Turn(false);
        }
        else if (!_IsFacingRight && MoveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            _IsFacingRight = true;
            SpriteRenderer.flipX = false;
        }
        else
        {
            _IsFacingRight = false;
            SpriteRenderer.flipX = true;
        }
    }
    #endregion

    #region Fall LandCheck CountTimers ApplyVelocity
    private void Fall()
    {
        // Normal gravity while falling
        if (!_isGrounded && !_isJumping && !_isWallSliding && !_isWallJumping && !_isDashFastFalling && !_isdashing)
        {
            _isFalling = true;
            _VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
        }
    }

    private void LandCheck()
    {
        //LANDING
        if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || _isWallSliding || _isDashFastFalling) && _isGrounded && _VerticalVelocity <= 0f)
        {
           ResetJumpValues();
           StopWallSlide();
           ResetWallJumpValues();
           ResetDashes();
            
            _numberOfJumpsUsed = 0;

            _VerticalVelocity = Physics2D.gravity.y;

            if(_isDashFastFalling && _isGrounded)
            {
                ResetDashValues();
                return;
            }
            ResetDashValues();
        } 
    }

    private void CountTimers()
    {
        _jumpBufferTimer -= Time.deltaTime;
        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else { _coyoteTimer = MoveStats.JumpCoyoteTime; }

        if (!ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer -= Time.deltaTime;
        }

        if (_isGrounded)
        {
            _dashOnGroundTimer -= Time.deltaTime;
        }
    }

    private void ApplyVelocity()
    {
        // Clamp fall speed
        _VerticalVelocity = Mathf.Clamp(_VerticalVelocity, -MoveStats.MaxFallSpeed, 50f);

        // Apply velocity to Rigidbody
        _rb.velocity = new Vector2(HorizontalVelocity, _VerticalVelocity);

        //Apply velocity to parameters on animator
        animator.SetFloat("Velocity_y", _rb.velocity.y);
    }
    #endregion

    #region Jump
    private void ResetJumpValues()
    {
        _isJumping = false;
        _isFalling = false;
        _isFastFalling = false;
        _fastFallTime = 0f;
        _isPastApexThreshold = false;
    }
    private void Jump()
    {
        if (_isJumping)
        {
            // Check for head bump
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            // Gravity while ascending
            if (_VerticalVelocity >= 0f)
            {
                _apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, _VerticalVelocity);

                if (_apexPoint > MoveStats.ApexThreshold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshold = 0f;
                    }

                    if (_timePastApexThreshold < MoveStats.ApexHangTime)
                    {
                        _VerticalVelocity = Mathf.Lerp(_VerticalVelocity, 0f, Time.fixedDeltaTime);
                    }
                    else
                    {
                        _VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
                    }
                    _timePastApexThreshold += Time.fixedDeltaTime;
                }
                else if (!_isFastFalling)
                {
                    _VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }
            else
            {
                // Gravity while descending
                if (!_isFastFalling)
                {
                    _VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                if (_VerticalVelocity < 0f && !_isFalling)
                {
                    _isFalling = true;
                }
            }
        }
        //Jump cut
        if (_isFastFalling)
        {
            if (_fastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                _VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else
            {
                _VerticalVelocity += Mathf.Lerp(_fastFallReleaseSpeed, 0f, _fastFallTime / MoveStats.TimeForUpwardsCancel);
            }
            _fastFallTime += Time.fixedDeltaTime;
        }
    }


    private void JumpChecks()
    {
        //WHEN WE PRESS THE JUMP BUTTON
        if (InputManager.JumpWasPressed)
        {
            if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0)
            {
                return;
            }
            else if (_isWallSliding || (_isTouchingWall && !_isGrounded))
            {
                return;
            }
            _jumpBufferTimer = MoveStats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }
        //WHEN WE RELEASE THE JUMP BUTTON
        if (InputManager.JumpWasReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }

            if (_isJumping && _VerticalVelocity > 0)
            {
                if (_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = MoveStats.TimeForUpwardsCancel;
                    _VerticalVelocity = 0f;
                }
                else
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = _VerticalVelocity;
                }
            }
        }

        //INITIATE JUMP WITH JUMP BUFFERING AND COYOTE TIME
        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (_jumpReleasedDuringBuffer)
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = _VerticalVelocity;
            }
        }

        //DOUBLE JUMP
        else if (_jumpBufferTimer > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling || _isAirDashing || _isDashFastFalling) && !_isTouchingWall && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(1);
            if (_isDashFastFalling)
            {
                _isDashFastFalling = false;
            }
        }

        //AIR JUMP AFTER COYOTE TIME LAPSED
        else if (_jumpBufferTimer > 0f && _isFalling && !_isWallSlideFalling && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
        {
            _isFastFalling = false;
            InitiateJump(2);
        }
    }

    private void InitiateJump(int NumberOfJumpsUsed)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        ResetWallJumpValues();

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += NumberOfJumpsUsed;
        _VerticalVelocity = MoveStats.InitialJumpVelocity;
    }
    #endregion

    #region collision checks
    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
        Vector2 boxCastSize = new(_feetColl.bounds.size.x, MoveStats.GroundDetectionRayLength);
        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, ~MoveStats.PlayerLayer);
        if (_groundHit.collider != null)
        {
            _isGrounded = true;
        }
        else
        { _isGrounded = false; }


        #region Debug Visualization
        if (MoveStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.right * boxCastSize.x, rayColor);
        }
        #endregion
        animator.SetBool("IsGrounded", _isGrounded);
    }

    private void BumpedHead()
    {
        Vector2 boxCastOrigin = new(_feetColl.bounds.center.x, _bodyColl.bounds.max.y);
        Vector2 boxCastSize = new(_feetColl.bounds.size.x * MoveStats.HeadWidth, MoveStats.GroundDetectionRayLength);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.GroundDetectionRayLength, ~MoveStats.PlayerLayer);
        if (_headHit.collider != null)
        {
            _bumpedHead = true;
        }
        else
        { _bumpedHead = false; }

        #region Debug Visualization
        if (MoveStats.DebugShowHeadBumpBox)
        {
            Color rayColor;
            if (_bumpedHead)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.right * boxCastSize.x, rayColor);
        }
        #endregion
    }

    private void IsTouchingWall()
    {
        float originEndPoint = 0f;
        if (_IsFacingRight)
        {
            originEndPoint = _bodyColl.bounds.max.x;
        }
        else
        {
            originEndPoint = _bodyColl.bounds.min.x;
        }

        float adjustedHeight = _bodyColl.bounds.size.y * MoveStats.WallDetectionRayHeightMultiplier;

        Vector2 boxCastOrigin = new(originEndPoint, _bodyColl.bounds.center.y);
        Vector2 boxCastSize = new(MoveStats.WallDetectionRayLength, adjustedHeight);

        _wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, MoveStats.WallDetectionRayLength, ~MoveStats.PlayerLayer);

        if (_wallHit.collider != null)
        {
            _lastWallHit = _wallHit;
            _isTouchingWall = true;
        }
        else
        {
            _isTouchingWall = false;
        }
        animator.SetBool("isWallTouching", _isTouchingWall);
        #region Debug Visualization

        if (MoveStats.DebugShowWallHitBox)
        {
            Color rayColor;
            if (_isTouchingWall)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Vector2 boxBottomLeft = new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxBottomRight = new(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxTopLeft = new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);
            Vector2 boxTopRight = new(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColor);
            Debug.DrawLine(boxBottomRight, boxTopRight, rayColor);
            Debug.DrawLine(boxTopRight, boxTopLeft, rayColor);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColor);
        }
        #endregion
    }
    private void CollisionChecks()
    {
        BumpedHead();
        IsGrounded();
        IsTouchingWall();
    }

    #endregion

    #region WallSlide
    private void WallSlideCheck()
    {
            if (_isTouchingWall && !_isGrounded)
            {
                if (_VerticalVelocity < 0f && !_isWallSliding)
                {
                    ResetJumpValues();
                    ResetWallJumpValues();
                    ResetDashValues();

                    _isWallSlideFalling = false;
                    _isWallSliding = true;

                    if (MoveStats.ResetJumpsOnWallSlide)
                    {
                        ResetDashes();
                    }
                }
            }
            else if (_isWallSliding && !_isTouchingWall && !_isGrounded && !_isWallSlideFalling)
            {
                _isWallSlideFalling = true;
                StopWallSlide();
            }
            else
            {
                StopWallSlide();
            }
  
    }

    private void StopWallSlide()
    {
        if (_isWallSliding)
        {
            _numberOfJumpsUsed++;

            _isWallSliding = false;
        }
    }

    private void WallSlide()
    {
        if (_isWallSliding)
        {
            _VerticalVelocity = Mathf.Lerp(_VerticalVelocity,-MoveStats.WallSlideSpeed*(1-MoveStats.Friction), MoveStats.WallSlideDecelerationSpeed * Time.fixedDeltaTime);
        }
    }
    #endregion

    #region WallJump
    private void ResetWallJumpValues()
    {
        _isWallSlideFalling = false;
        _useWallJumpMoveStats = false;
        _isWallJumping = false;
        _isWallJumpFastFalling = false;
        _isWallJumpFalling = false;
        _isPastWallJumpApexThreshold = false;
        _wallJumpFastFallTime = 0f;
        _wallJumpTime = 0f;
    }
    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;
        }

        if (InputManager.JumpWasReleased && !_isWallSliding && !_isTouchingWall && _isWallJumping)
        {
            if(_VerticalVelocity > 0f)
            {
                if (_isPastWallJumpApexThreshold)
                {
                    _isPastWallJumpApexThreshold = false;
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel;

                    _VerticalVelocity = 0f;
                }
                else
                {
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallReleaseSpeed = _VerticalVelocity;
                }
            }
        }

        if(InputManager.JumpWasPressed && _wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
        }
    }

    private void InitiateWallJump()
    {
        if (!_isWallJumpFalling)
        {
            _isWallJumping = true;
            _useWallJumpMoveStats = true;
        }

        StopWallSlide();
        ResetJumpValues();
        _wallJumpTime = 0f;

        _VerticalVelocity = MoveStats.InitialWallJumpVelocity;
        int dirMultiplier = 0;
        Vector2 hitPoint = _lastWallHit.collider.ClosestPoint(_bodyColl.bounds.center);

        if(hitPoint.x > transform.position.x)
        {
            dirMultiplier = -1;
        }
        else { dirMultiplier = 1; }

        HorizontalVelocity = Mathf.Abs(MoveStats.WallJumpDirection.x) * dirMultiplier;
    }

    private void WallJump()
    {
        //IS WALL JUMPING
        if (_isWallJumping)
        {
            _wallJumpTime += Time.fixedDeltaTime;
            if (_wallJumpTime >= MoveStats.TimeTillJumpApex)
            {
                _useWallJumpMoveStats = false;
            }
            //HIT HEAD
            if (_bumpedHead)
            {
                _isWallJumpFastFalling = true;
                _useWallJumpMoveStats = false;
            }
            //GRAVITY IN THE ASCENDING
            if (_VerticalVelocity >= 0f)
            {
                _wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, _VerticalVelocity);
                if (_wallJumpApexPoint > MoveStats.ApexThreshold)
                {
                    if (!_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = true;
                        _timePastWallJumpApexThreshold = 0f;
                    }

                    if (_isPastWallJumpApexThreshold)
                    {
                        _timePastWallJumpApexThreshold += Time.fixedDeltaTime;
                        if (_timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
                        {
                            _VerticalVelocity = 0f;
                        }
                        else
                        {
                            _VerticalVelocity -= 0.01f;
                        }
                    }
                }
                //GRAVITY IN ASCENDING BUT NOT PAST APEX TRESHOLD
                else if (!_isWallJumpFastFalling)
                {
                    _VerticalVelocity += (MoveStats.WallJumpGravity * Time.fixedDeltaTime);
                    if (_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = false;
                    }
                }
            }

            //GRAVITY ON DESCENDING
            else if(!_isWallJumpFastFalling)
            {
                _VerticalVelocity += (MoveStats.WallJumpGravity * Time.fixedDeltaTime);
            }
            else if(_VerticalVelocity < 0f)
            {
                if (!_isWallJumpFalling)
                {
                    _isWallJumpFalling = true;
                }
            }

            //HANDLE WALL JUMP CUT TIME
            if (_isWallJumpFastFalling)
            {
                if (_wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
                {
                    _VerticalVelocity += MoveStats.WallJumpGravity * MoveStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (_wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
                {
                    _VerticalVelocity = Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, (_wallJumpFastFallTime / MoveStats.TimeForUpwardsCancel));
                }
                _wallJumpFastFallTime += Time.fixedDeltaTime;
            }
        }
    }

    #endregion

    #region 8 directional dash
    private bool ShouldApplyPostWallJumpBuffer()
    {
        if (!_isGrounded && (_isTouchingWall || _isWallSliding))
        {
            return true;
        }
        else { return false;}
    }
   private void DashCheck()
    {
       if (InputManager.DashWasPressed)
        {
            //ground dash
            if (_isGrounded && _dashOnGroundTimer < 0f && !_isdashing)
            {
                InitiateDash();
            }
            //air dash
            else if(!_isGrounded && _numberOfDashesUsed < MoveStats.NumberOfDashes)
            {
                _isAirDashing = true;
                InitiateDash();

                //you left a wall slide but dashed within the wall jump post buffer time
                if(_wallJumpPostBufferTimer > 0f)
                {
                    _numberOfJumpsUsed--;
                    if(_numberOfJumpsUsed < 0 )
                    {
                        _numberOfJumpsUsed = 0;
                    }
                }
            }
        }
    }

    private void InitiateDash()
    {
        _dashDirection = InputManager.Movement;

       Vector2 ClosestDirection = Vector2.zero;
       float minDistance = Vector2.Distance(_dashDirection, MoveStats.DashDirections[0]);

        for(int i = 0; i <  MoveStats.DashDirections.Length; i++)
        {
            if(_dashDirection == MoveStats.DashDirections[i])
            {
                ClosestDirection = _dashDirection;
                break;
            }
            float distance =  Vector2.Distance(_dashDirection,MoveStats.DashDirections[i]);

            bool isDiagonal = (Mathf.Abs(MoveStats.DashDirections[i].x) == 1 && Mathf.Abs(MoveStats.DashDirections[i].x) == 1);
            if (isDiagonal)
            {
                distance = MoveStats.DashDiagonallyBias;
            }
            else if (distance > minDistance)
            {
                minDistance = distance;
                ClosestDirection = MoveStats.DashDirections[i];
            }
        }
        //Handle directions with NO inputs
        if(ClosestDirection == Vector2.zero)
        {
            if (_IsFacingRight)
            {
                ClosestDirection = Vector2.right;
            }
            else{ClosestDirection  = Vector2.left;}
        }

        _dashDirection = ClosestDirection;
        _numberOfDashesUsed++;
        _isdashing = true;
        _dashTimer = 0f;
        _dashOnGroundTimer = MoveStats.TimeBtwDashesOnGround;

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();
    }

    private void Dash()
    {
        if (_isdashing)
        {
            //Stop the dash after the timer
            _dashTimer += Time.fixedDeltaTime;
            if (_dashTimer >= MoveStats.DashTime)
            {
                if (_isGrounded)
                {
                    ResetDashes();
                }

                _isAirDashing = false;
                _isdashing = false;

                if(!_isJumping && !_isWallJumping)
                {
                    _dashFastFallTime = 0f;
                    _dashFastFallReleaseSpeed = _VerticalVelocity;

                    if (!_isGrounded)
                    {
                        _isDashFastFalling = true;
                    }
                }

                return;
            }
            
            HorizontalVelocity = MoveStats.DashSpeed * _dashDirection.x;

            if(_dashDirection.y != 0 || _isAirDashing)
            {
                _VerticalVelocity = MoveStats.DashSpeed * _dashDirection.y;
            }
        }
        //Handle dash cut time  
        else if (_isDashFastFalling)
        {
            if(_VerticalVelocity > 0f)
            {
                if(_dashFastFallTime < MoveStats.DashTimeForUpwardsCancel)
                {
                    _VerticalVelocity = Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, (_dashFastFallTime / MoveStats.DashTimeForUpwardsCancel));
                }
                else if(_dashFastFallTime >= MoveStats.DashTimeForUpwardsCancel)
                {
                    _VerticalVelocity += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                _dashFastFallTime += Time.fixedDeltaTime;
            }
            else
            {
                _VerticalVelocity += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
        }
    }

    private void ResetDashValues()
    {
        _isDashFastFalling = false;
        _dashOnGroundTimer = -0.01f;
    }
    
    private void ResetDashes()
    {
        _numberOfDashesUsed = 0;
    }
    #endregion
}

