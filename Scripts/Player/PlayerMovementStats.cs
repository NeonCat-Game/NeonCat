/*************************************************************************************************************************************************
 *This script was made using Sasquath B studio Tutorial on the ultimate 2D Controller
 *Sasquath B studio. 27/06/2024. ULTIMATE 2D Platformer Controller for Unity (Part 2). Youtube. https://www.youtube.com/watch?v=j1HN7wsFHcY&t=505s
 *Sasquath B studio. 6/06/2024. ULTIMATE 2D Platformer Controller for Unity. Youtube. https://www.youtube.com/watch?v=zHSWG05byEc
 ************************************************************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement")]
public class PlayerMovementStats : ScriptableObject
{
    [Header("Walk")]
    [Range(1f, 100f)] public float MaxWalkSpeed = 12.5f; //Max walk speed we are triggering to our character
    [Range(0.25f, 50f)] public float GroundAcceleration = 5f; //acceleration on ground if the current speed isn't 0
    [Range(0.25f, 50f)] public float GroundDeceleration = 20f; //decelaration on ground after the character stopped moving
    [Range(0.25f, 50f)] public float AirAcceleration = 5f; //accleration when we are not hitting the ground but are jumping or falling
    [Range(0.25f, 50f)] public float AirDeceleration = 5f; //deceleration when we are not hitting the ground but are jumping or falling
    [Range(0.25f, 50f)] public float WallJumpAcceleration = 5f; //acceleration while we are neither jumping or falling or hitting the ground but are walljumping
    [Range(0.25f, 50f)] public float WallJumpDeceleration = 5f; //deceleration while we are neither jumping or falling or hitting the ground but are walljumping

    [Header("Run")]
    [Range(1f, 100f)] public float MaxRunSpeed = 20f; //Max run speed

    [Header("IsGrounded/Collision Checks")]
    public LayerMask PlayerLayer; //Layer we are going to ignore
    public float GroundDetectionRayLength = 0.02f; //Ground raycast length to check if there's ground beneath us
    public float HeadDetectionRayLength = 0.02f; //Head raycast length to check if we didn't hit the floor upside us
    public float WallDetectionRayLength = 0.02f; //Wall raycast length to check if we are currently hitting a wall
    [Range(0f, 1f)] public float HeadWidth = 0.75f; //Size of the box raycast we are using in the head raycast
    [Range(0f, 2f)] public float WallDetectionRayHeightMultiplier= 0.9f; //Size of the box raycast we are using in the wall raycast

    [Header("Jump")]
    public float JumpHeight = 6.5f; //Max Jump Height we allow our character to jump each time
    [Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f; //Compensation factor to adjust the height of our jump
    public float TimeTillJumpApex = 0.35f; //Time till the jump hits the max height
    [Range(0.01f, 5f)] public float GravityOnReleaseMultiplier = 2f; //multiplier when we are fast falling
    public float MaxFallSpeed = 26f; //Max speed while falling (don't place this number too high or either the character can bypass collision boxes at a certain speed)
    [Range(1, 5)] public int NumberOfJumpsAllowed = 2; //Max number of jumps allow 2 mean you can do 1 air jump and 1 normal jump


    [Header("Reset Jump Options")]
    public bool ResetJumpsOnWallSlide = true; //Reset walljumps if hit wall

    [Header("Jump cut")]
    [Range(0.02f, 0.3f)] public float TimeForUpwardsCancel = 0.027f; //time to cancel the normal jump speed and apply the fast falling speed
    
    [Header("Jump Apex")]
    [Range(0.02f, 1f)] public float ApexThreshold = 0.97f; //Apex threshold to start descending
    [Range(0.01f, 1f)] public float ApexHangTime = 0.075f; //Hang time you will be kept on apex point of the jump

    [Header("Jump  Buffer")]
    [Range(0f, 1f)] public float JumpBufferTime = 0.125f; //max time allowed when we release the jump botton

    [Header("Jump Coyote Time")]
    [Range(0f, 1f)] public float JumpCoyoteTime = 0.1f; //max time allowed for the user to jump again if we are not hitting the ground

    [Header("Wall Slide")]
    [Min(0.01f)] public float WallSlideSpeed = 5f; //max wall slide speed while falling
    [Range(0.25f, 50f)] public float WallSlideDecelerationSpeed = 50f; //max wall slide deceleration

    [Header("Wall Jump")]
    public Vector2 WallJumpDirection = new(-20f, 6.5f); //current wall jump direction (right left)
    [Range(0f, 1f)] public float WallJumpPostBufferTime = 0.125f; //walljump after the buffer time
    [Range(0.01f, 5f)] public float WallJumpGravityOnReleaseMultiplier = 1f; //multiplier on the gravity when you wall jump (increasing this number makes it so you fall faster on wall jump)

    [Header("Dash")]
    [Range(0f, 1f)] public float DashTime = 0.11f; //the amount of time you will be dashing
    [Range(1f, 200f)] public float DashSpeed = 40f;//the max speed of the dash
    [Range(0f, 1f)] public float TimeBtwDashesOnGround = 0.225f; //the time of the ground dashes
    public bool ResetDashOnWalls = true; //resets dashes when hitting a wall
    [Range(1, 10)] public int NumberOfDashes = 2; //max amount of air dashes
    [Range(0f, 0.5f)] public float DashDiagonallyBias = 0.4f; 

    [Header("Dash Cancel Time")]
    [Range(0.01f, 5f)] public float DashGravityOnReleaseMultiplier = 1f; //multiplier when falling using the dash (increasing this number makes it so you fall faster after the dash)
    [Range(0.02f, 0.3f)] public float DashTimeForUpwardsCancel = 0.027f; //time to cancel the dash

    [Header("Friction and Bounciness")]
    [Range(0.1f, 0.9f)] public float Friction = 0.2f; //current friction of the character
    [Range(0.1f, 0.9f)] public float Bounciness = 0.2f; //current bounciness of the character

    [Header("Move Limits")]
    [Range(0.1f, 1f)] public float MoveThreshold; //movetreshold so we move horizontal

    [Header("Debug")]
    public bool DebugShowIsGroundedBox; //shows the raycast of isgrounded
    public bool DebugShowHeadBumpBox; //shows the raycast of isbumpedhead
    public bool DebugShowWallHitBox; //shows the raycast of wallhit

    [Header("JumpVisualization Tool")]
    public bool ShowWalkJumpArc = false;
    public bool ShowRunJumpArc = false;
    public bool StopOnCollision = true;
    public bool DrawRight = true;
    [Range(5, 100)] public int ArcResolution = 20;
    [Range(0, 500)] public int VisualizationSteps = 90;

    public readonly Vector2[] DashDirections = new Vector2[]
    {
        new(0,0), //Nothing
        new(1,0), //Right
        new Vector2(1,1).normalized, //Top Right
        new(0,1), //Up
        new Vector2(-1,1).normalized, //Top Left
        new(-1,0), //Left
        new Vector2(-1,-1).normalized, //Bottom Left
        new(0,-1), //Down
        new Vector2(1,-1).normalized, //Bottom Right
        new()
    };

    //Jump
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set; }

    //Wall Jump
    public float WallJumpGravity { get; private set; }
    public float InitialWallJumpVelocity { get; private set; }
    public float AdjustedWallJumpHeight { get; private set; }

    private void OnValidate()
    {
        CalculateValues();
    }

    private void OnEnable()
    { 
        CalculateValues();
    }
    private void CalculateValues()
    {
        //Jump
        AdjustedJumpHeight = JumpHeight * JumpHeightCompensationFactor;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        InitialJumpVelocity = Mathf.Abs(Gravity)*TimeTillJumpApex;
        //Wall Jump
        AdjustedWallJumpHeight = WallJumpDirection.y * JumpHeightCompensationFactor;
        WallJumpGravity = -(2f * AdjustedWallJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        InitialWallJumpVelocity = Mathf.Abs(WallJumpGravity) * TimeTillJumpApex;
    }

}
