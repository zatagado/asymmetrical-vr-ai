using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// A pathfinding calculation to a specific target position.
/// </summary>
public struct PathCalculation
{
    public List<Vector3> vectorPath;
    public List<GraphNode> nodePath;
    public TargetObjective target;

    /// <summary>
    /// Constructor for PathCalculation.
    /// A pathfinding calculation to a specific target position.
    /// </summary>
    /// <param name="vectorPath">A list of position vectors from pathfinding.</param>
    /// <param name="nodePath">A list of nodes from pathfinding results.</param>
    /// <param name="target">The target objective.</param>
    public PathCalculation(List<Vector3> vectorPath, List<GraphNode> nodePath, TargetObjective target)
    {
        this.vectorPath = vectorPath;
        this.nodePath = nodePath;
        this.target = target;
    }
}

/// <summary>
/// Allows AI to input controls as if it were a human PC player and constructs the AI behavior tree.
/// </summary>
public class AIController : MonoBehaviour, PCInputInterface
{
    // AI input control properties
    public bool JumpInput { get; set; } = false;
    public bool CrouchInput { get; set; } = false;
    public float VerticalInput { get; set; } = 0;
    public float HorizontalInput { get; set; } = 0;
    public float LookX { get; set; } = 0;
    public float LookY { get; set; } = 0;
    public bool ShowNameTagsInput => false;
    public bool InteractInput { get; set; } = false;

    private BTRootNode behaviorTreeRoot = null; // Root node used to build the AI behavior tree.
    private DoNothingAction doNothing = null; // AI behavior for no behavior. Could explore singleton pattern in future.
    [SerializeField] private PCMovementController moveCon = null; // Default movement controller for human/AI PC players

    public Action OnTargetChanged = null; // Event occurs when the AI player changes its target

    private PathCalculation pathCalc = new PathCalculation(null, null, null); // pathfinding calculation to a target
    private PathCalculation avoidPathCalc = new PathCalculation(null, null, null); // pathfinding calculation to a target position that allows the AI to avoid getting close to the VR player

    // Used when a pathfinding calculation for the target is pending because pathfinding is asynchronous.
    private TargetObjective pendingTarget = null;
    private TargetObjective pendingAvoidTarget = null;

    [SerializeField] private GameObject agentPrefab = null; // Gameobject contains scripts responsible for pathfinding.
    private Seeker aiSeeker = null;
    private AIPath aiPathfinder = null;
    private Transform playerTransform = null;
    private Vector3 movementDir = Vector3.zero;
    private TargetObjective target = null;
    private TargetObjective avoidTarget = null;
    private Transform vrPlayer = null;
    private Transform vrPlayerHead = null;

    private PlayerData playerData = null;

    // Variables for debugging. Remove in final builds.
    [Header("What is the AI currently doing?")]
    [TextArea(3, 10)] public string currentAction = null;
    [TextArea(3, 30)] public string treeNodes = null;

    // Getters and setters
    /// <summary>
    /// Get the AI player ID.
    /// </summary>
    public int ID { get => playerData.ID; }
    /// <summary>
    /// Get the AI PCMovementController.
    /// </summary>
    public PCMovementController MoveCon => moveCon;
    /// <summary>
    /// Get the regular path calculation.
    /// </summary>
    public PathCalculation PathCalc => pathCalc;
    /// <summary>
    /// Get the path calculation used to avoid the VR player getting too close.
    /// </summary>
    public PathCalculation AvoidPathCalc { get => avoidPathCalc; }
    /// <summary>
    /// Required for node graph information in behavior tree objects.
    /// </summary>
    public Seeker AISeeker => aiSeeker;
    /// <summary>
    /// Contains some information about the AI for pathfinding.
    /// </summary>
    public AIPath AIPathfinder => aiPathfinder;
    /// <summary>
    /// Gets the AI player transform object.
    /// </summary>
    public Transform PlayerTransform => playerTransform;
    /// <summary>
    /// Gets or sets the movement direction of the AI.
    /// </summary>
    public Vector3 MovementDir { get => movementDir; set => movementDir = value; }
    /// <summary>
    /// Gets the target for the AI. Sets the target and triggers a OnTargetChanged event if the target is different than 
    /// the previous target.
    /// </summary>
    public TargetObjective Target
    {
        get => target;
        set
        {
            if (value != target)
            {
                target = value;
                OnTargetChanged?.Invoke();
            }
            else
            {
                target = value;
            }
        }
    }
    /// <summary>
    /// Gets or sets the avoid target for the AI.
    /// </summary>
    public TargetObjective AvoidTarget { get => avoidTarget; set => avoidTarget = value; }
    /// <summary>
    /// Gets the forward direction for the VR player. Important because the AI player avoids the VR player in an elliptical 
    /// radius with the VR forward direction determining the rotation of the ellipse.
    /// </summary>
    public Vector3 VRPlayerForward
    {
        get
        {
            Vector3 forward = vrPlayerHead.forward;
            forward.y = 0;
            return forward.normalized;
        }
    }
    /// <summary>
    /// Gets the VR player position. Important for knowing how to avoid the VR player.
    /// </summary>
    public Vector3 VRPlayerPosition
    {
        get
        {
            Vector3 position = vrPlayerHead.position;
            position.y = vrPlayer.position.y;
            return position;
        }
    }
    /// <summary>
    /// Gets the quaternion rotation of the VR player.
    /// </summary>
    public Quaternion VRPlayerRotation => Quaternion.LookRotation(VRPlayerForward, Vector3.up);
    
    // Debug Variables
    [SerializeField] private bool startMoving = true;
    public string message = null;

    /// <summary>
    /// Initializes an AI player.
    /// </summary>
    public void InitializeAIPlayer()
    {
        playerTransform = transform;
        vrPlayer = GameController.VRPlayerData.transform;
        vrPlayerHead = GameController.VRSyncMoveConHMD != null ? GameController.VRSyncMoveConHMD.LocalHead : vrPlayer;

        playerData = GetComponent<PlayerData>();
        aiSeeker = Instantiate(agentPrefab, playerTransform.position, playerTransform.rotation).GetComponent<Seeker>(); // Instantiates AI gameobject for pathfinding
        aiSeeker.pathCallback = OnPathComplete;
        aiPathfinder = aiSeeker.GetComponent<AIPath>();
        aiPathfinder.maxSpeed = moveCon.WalkSpeed;

        movementDir = playerTransform.forward;
        startMoving = true;

        InitializeBehaviorTree();
    }

    /// <summary>
    /// Creates the behavior tree structure for the AI with the correct types of nodes.
    /// </summary>
    private void InitializeBehaviorTree()
    {
        BTSequence[] arenaSequences = new BTSequence[GameController.Arenas.Length];
        doNothing = new DoNothingAction(this);

        // Arena type can be in a randomized order, so create AI behavior tree for specific arena type in that order
        for (int i = 0; i < GameController.Arenas.Length; i++)
        {
            Arena arena = GameController.Arenas[i];
            switch (arena)
            {
                case BarrierArena barrierArena:
                    arenaSequences[i] = InitializeBarrierSequence(barrierArena);
                    break;
                case LockArena lockArena:
                    arenaSequences[i] = InitializeLockSequence(lockArena);
                    break;
                case PatternArena patternArena:
                    arenaSequences[i] = InitializePatternSequence(patternArena);
                    break;
            }
        }

        BTNode actionBranch = new BTSelector // select between arenas
        (
            new List<BTNode>
            {
                arenaSequences[0], // will need to change this for different arena sizes.
                //arenaSequences[1],
                //arenaSequences[2],
                doNothing
            }
        );

        behaviorTreeRoot = new BTRootNode(actionBranch);
    }

    /// <summary>
    /// Constructs behavior tree for barrier type arena.
    /// </summary>
    /// <param name="barrierArena">Barrier arena object for adding listeners to events.</param>
    /// <returns></returns>
    private BTSequence InitializeBarrierSequence(BarrierArena barrierArena)
    {
        BTSequence barrierArenaSequence = null;

        // AI does this while in the air
        BTSequence midAir = new BTSequence(new List<BTNode> {
            new IsMidAirConditional(this),
            new RotateMidAir(this)
        });

        // AI avoids VR player within a certain elliptical radius of them
        AvoidVROnPathAction avoidVROnPath = new AvoidVROnPathAction(this);
        RotateForwardAction rotateForward = new RotateForwardAction(this);
        MoveToTargetAction mainMoveToTarget = new MoveToTargetAction(this, 0.2f);

        BTSequence avoidMove = new BTSequence(new List<BTNode>
        {
            new AvoidCloseVRAction(this, new Vector2(3f, 4.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, 0.5f), 
                1.5f, 2.0f, 40, new Tuple<float, float>[]{ Tuple.Create(7f, 0.6f), Tuple.Create(15f, 0f) }, 5f),
            new RotateTowardsVRAction(this),
            new MoveToAvoidTargetDecorator(this, 0.001f)
        });

        // AI goes to a jail
        TargetJailAction barrierJails = new TargetJailAction(this);

        BTSequence barrierJailTargeting = new BTSequence(new List<BTNode> {
            barrierJails,
            avoidVROnPath,
            new BTSelector(new List<BTNode>
            {
                avoidMove,
                new BTSequence(new List<BTNode>
                {
                    rotateForward,
                    mainMoveToTarget
                })
            })
        });

        // AI goes to a relic/artifact
        TargetRelicAction barrierRelic = new TargetRelicAction(this, 0.05f);

        BTSequence relicTargetingSequence = new BTSequence(new List<BTNode> {
            barrierRelic,
            avoidVROnPath,
            new BTSelector(new List<BTNode>
            {
                avoidMove,
                new BTSequence(new List<BTNode>
                {
                    rotateForward,
                    mainMoveToTarget
                })
            })
        });

        if ((GameColor)playerData.Color == GameColor.White) // If the AI color is white, then their job is to tell the other PC players what color console buttons they need to press
        {
            // Move to positions around the console while waiting for other PC players to press the console button 
            TargetNearbyPositionAction nearbyPosition = new TargetNearbyPositionAction(this);
            MoveToTargetAction nearbyMoveToTarget = new MoveToTargetAction(this, 0.3f);

            BTSequence nearbyConsoleTargeting = new BTSequence(new List<BTNode>
            {
                nearbyPosition,
                avoidVROnPath,
                new BTSelector(new List<BTNode>
                {
                    avoidMove,
                    new BTSequence(new List<BTNode>
                    {
                        rotateForward,
                        nearbyMoveToTarget
                    })
                })
            });

            // Find a console to move towards so it can alert other PC players of the console color
            TargetBarrierConsoleObserveAction barrierConsoles = new TargetBarrierConsoleObserveAction(this);
            MoveObserveConsoleDecorator moveObserveConsole = new MoveObserveConsoleDecorator(this, 1.5f);

            BTSequence barrierConsoleTargeting = new BTSequence(new List<BTNode> {
                barrierConsoles,
                avoidVROnPath,
                new BTSelector(new List<BTNode>
                {
                    avoidMove,
                    new BTSequence(new List<BTNode>
                    {
                        rotateForward,
                        moveObserveConsole
                    })
                })
            });

            // AI selects a random position to go to
            TargetRandomPositionAction randomPosition = new TargetRandomPositionAction(this);
            MoveToTargetAction randomMoveToTarget = new MoveToTargetAction(this, 0.2f);

            BTSequence randomTargetingSequence = new BTSequence(new List<BTNode> {
                randomPosition,
                avoidVROnPath,
                new BTSelector(new List<BTNode> {
                    avoidMove,
                    new BTSequence(new List<BTNode> {
                        rotateForward,
                        randomMoveToTarget
                    })
                })
            });

            // Putting the AI behavior tree together
            barrierArenaSequence = new BTSequence(new List<BTNode> {
                new IsBarrierArenaConditional(this),  // could change this to a delegate for more efficiency?
                new BTSelector(new List<BTNode> {
                    new BTSequence(new List<BTNode> {
                        new IsPlayerInJailConditional(this, GetComponent<PCAbilityController>()),
                        doNothing
                    }),
                    new BTSequence(new List<BTNode> {
                        new BTSelector(new List<BTNode> {
                            midAir,
                            barrierJailTargeting,
                            relicTargetingSequence,
                            nearbyConsoleTargeting,
                            barrierConsoleTargeting,
                            randomTargetingSequence,
                            doNothing
                        }),
                        new SnapAIToModelAction(this, 0.25f)
                    })
                }),
            });

            // Assigning listeners to action events once all objects are created
            barrierArena.OnPlayerJailed += barrierJails.AddJailTargets;
            barrierArena.OnJailReleased += barrierJails.ResetJailTargets;
            barrierArena.OnJailButtonStateChanged += barrierJails.ChangeJailButtonState;
            barrierArena.OnArenaEnd += barrierJails.ResetJailTargets;

            barrierArena.OnRelicUnlocked += barrierRelic.AddRelicTarget;
            barrierArena.OnRelicLocked += barrierRelic.ResetRelicTarget;
            barrierArena.OnArenaEnd += barrierRelic.ResetRelicTarget;

            moveObserveConsole.OnReachedTarget += nearbyPosition.StartFindingTarget;
            moveObserveConsole.OnReachedTarget += nearbyPosition.FindNewTarget;
            nearbyMoveToTarget.OnReachedTarget += nearbyPosition.FindNewTarget;
            barrierArena.OnConsoleColorsChanged += nearbyPosition.ResetConsoleTargets;

            barrierArena.OnRedConsolesSafe += barrierConsoles.AddConsoleTargets;
            barrierArena.OnGreenConsolesSafe += barrierConsoles.AddConsoleTargets;
            barrierArena.OnBlueConsolesSafe += barrierConsoles.AddConsoleTargets;

            barrierArena.OnRedBarrierStateChanged += barrierConsoles.RedBarrierStateChanged;
            barrierArena.OnGreenBarrierStateChanged += barrierConsoles.GreenBarrierStateChanged;
            barrierArena.OnBlueBarrierStateChanged += barrierConsoles.BlueBarrierStateChanged;

            barrierArena.OnConsoleColorsChanged += barrierConsoles.ResetConsoleTargets;

            barrierArena.OnArenaEnd += barrierConsoles.ResetConsoleTargets;

            moveObserveConsole.OnReachedTarget += moveObserveConsole.ChatLocation;
            moveObserveConsole.OnReachedTarget += moveObserveConsole.SetKnowsLocation;
        }
        else // otherwise the player color is Red, Green, or Blue and must press console buttons of their color.
        {
            // AI goes to a button
            TargetBarrierConsolePressAction barrierConsolePress = new TargetBarrierConsolePressAction(this, 0.05f);
            MoveInteractConsoleDecorator moveInteractConsole = new MoveInteractConsoleDecorator(this, 0.7f);

            BTSequence barrierConsoleTargeting = new BTSequence(new List<BTNode> {
                barrierConsolePress,
                avoidVROnPath,
                new BTSelector(new List<BTNode>
                {
                    avoidMove,
                    new BTSequence(new List<BTNode>
                    {
                        rotateForward,
                        moveInteractConsole
                    })
                })
            });

            // AI selects a random position to go to
            TargetRandomPositionAction randomPosition = new TargetRandomPositionAction(this);
            MoveToTargetAction randomMoveToTarget = new MoveToTargetAction(this, 0.2f);

            BTSequence randomTargetingSequence = new BTSequence(new List<BTNode> {
                randomPosition,
                avoidVROnPath,
                new BTSelector(new List<BTNode>
                {
                    avoidMove,
                    new BTSequence(new List<BTNode>
                    {
                        rotateForward,
                        randomMoveToTarget
                    })
                })
            });

            // Putting the AI behavior tree together
            barrierArenaSequence = new BTSequence(new List<BTNode> {
                new IsBarrierArenaConditional(this),  // could change this to a delegate for more efficiency?
                new BTSelector(new List<BTNode> {
                    new BTSequence(new List<BTNode> {
                        new IsPlayerInJailConditional(this, GetComponent<PCAbilityController>()),
                        doNothing
                    }),
                    new BTSequence(new List<BTNode> {
                        new BTSelector(new List<BTNode> {
                            midAir,
                            barrierJailTargeting,
                            relicTargetingSequence,
                            barrierConsoleTargeting,
                            randomTargetingSequence,
                            doNothing
                        }),
                        new SnapAIToModelAction(this, 0.25f)
                    })
                })
            });

            // Assigning listeners to action events once all objects are created
            barrierArena.OnPlayerJailed += barrierJails.AddJailTargets;
            barrierArena.OnJailReleased += barrierJails.ResetJailTargets;
            barrierArena.OnJailButtonStateChanged += barrierJails.ChangeJailButtonState;
            barrierArena.OnArenaEnd += barrierJails.ResetJailTargets;

            barrierArena.OnRelicUnlocked += barrierRelic.AddRelicTarget;
            barrierArena.OnRelicLocked += barrierRelic.ResetRelicTarget;
            barrierArena.OnArenaEnd += barrierRelic.ResetRelicTarget;

            switch ((GameColor)playerData.Color)
            {
                case GameColor.Red:
                    barrierArena.OnRedConsolesSafe += barrierConsolePress.AddConsoleTargets;
                    barrierArena.OnRedBarrierStateChanged += barrierConsolePress.BarrierStateChanged;
                    break;
                case GameColor.Green:
                    barrierArena.OnGreenConsolesSafe += barrierConsolePress.AddConsoleTargets;
                    barrierArena.OnGreenBarrierStateChanged += barrierConsolePress.BarrierStateChanged;
                    break;
                case GameColor.Blue:
                    barrierArena.OnBlueConsolesSafe += barrierConsolePress.AddConsoleTargets;
                    barrierArena.OnBlueBarrierStateChanged += barrierConsolePress.BarrierStateChanged;
                    break;
            }

            barrierArena.OnConsoleColorsChanged += barrierConsolePress.ResetConsoleTargets;
            barrierArena.OnArenaEnd += barrierConsolePress.ResetConsoleTargets;

            randomMoveToTarget.OnReachedTarget += randomPosition.ResetRandomTarget;
        }
        return barrierArenaSequence;
    }

    // Future types of arenas that require some new AI behaviors.

    /// <summary>
    /// Constructs behavior tree for lock type arena. 
    /// </summary>
    /// <param name="lockArena">Lock arena object for adding listeners to events.</param>
    /// <returns></returns>
    private BTSequence InitializeLockSequence(LockArena lockArena)
    {
        // TODO
        return null;
    }

    /// <summary>
    /// Constructs behavior tree for pattern type arena.
    /// </summary>
    /// <param name="patternArena">Pattern arena object for adding listeners to events.</param>
    /// <returns></returns>
    private BTSequence InitializePatternSequence(PatternArena patternArena)
    {
        // TODO
        return null;
    }

    /// <summary>
    /// Runs each frame. Responsible for running the Tick() function making AI execute behaviors.
    /// </summary>
    private void Update()
    {
        if (PhotonNetwork.IsMasterClient) // The multiplayer master client handles AI processes
        {
            if (Input.GetKeyDown(KeyCode.E)) // Allows master client to stop and start AI for debugging
            {
                startMoving = !startMoving;
                Debug.Log(startMoving ? "AI started moving" : "AI stopped moving");
            }

            if (startMoving) // execute behaviors
            {
                InteractInput = false;

                treeNodes = "";

                behaviorTreeRoot.Tick(); // Runs all behaviors in behavior tree
            }
            else // otherwise tells the AI to do nothing
            {
                JumpInput = false;
                CrouchInput = false;
                VerticalInput = 0;
                HorizontalInput = 0;
                LookX = 0;
                LookY = 0;
            }
        }
    }

    /// <summary>
    /// Sends the AI to the set position. "Teleporting" the AI.
    /// </summary>
    /// <param name="position">The position you would like to send the AI to.</param>
    public void SetPosition(Vector3 position)
    {
        // The AI transform and aiPathfinder are separate so they must both have their position set.
        transform.position = position;
        aiPathfinder.Teleport(position);
    }

    /// <summary>
    /// Executed when the aiSeeker trigger the pathCallback event.
    /// </summary>
    /// <param name="path">The completed path.</param>
    public void OnPathComplete(Path path)
    {
        if (path.error)
        {
            Debug.LogError("The AI path calculation encountered an error.");
            return;
        }

        MultiTargetPath multiPath = path as MultiTargetPath;
        if (multiPath == null) // single target path
        {
            pathCalc = new PathCalculation(path.vectorPath, path.path, pendingTarget);
        }
        else // multi target path
        {
            pathCalc = new PathCalculation(multiPath.vectorPaths[0], multiPath.nodePaths[0], pendingTarget);
            avoidPathCalc = new PathCalculation(multiPath.vectorPaths[1], multiPath.nodePaths[1], pendingAvoidTarget);
        }
    }

    /// <summary>
    /// Tells the AI to perform a pathfinding calculation after a target has been decided by the behavior tree.
    /// </summary>
    public void CalculatePath()
    {
        if (target != null)
        {
            aiSeeker.StartPath(aiPathfinder.GetFeetPosition(), target.Position);
            pendingTarget = target;
        }
    }

    /// <summary>
    /// Tells the AI to perform multiple pathfinding calculations at once. This occurs when the AI is too close to the 
    /// VR player and must choose a target position in order to avoid while another target has been decided by the 
    /// behavior tree.
    /// </summary>
    public void CalculateMultiplePaths()
    {
        if (target != null)
        {
            if (avoidTarget != null)
            {
                aiSeeker.StartMultiTargetPath(aiPathfinder.GetFeetPosition(), new Vector3[] { target.Position, avoidTarget.Position }, true);
                pendingTarget = target;
                pendingAvoidTarget = avoidTarget;
            }
            else
            {
                aiSeeker.StartPath(aiPathfinder.GetFeetPosition(), target.Position);
                pendingTarget = target;
            }
        }
    }

    /// <summary>
    /// Runs when the AI is disabled. Used for some cleanup.
    /// </summary>
    private void OnDisable()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            aiSeeker.pathCallback = null;
        }
    }
}
