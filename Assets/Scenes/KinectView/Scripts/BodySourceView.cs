/*
    Copyright 2020
    Hochschule für angewandte Wissenschaften Hamburg

    Project Description
        Zoom it like I walk it

        A project realized for the class "Interaktive Systeme" held by Professorin Katrin Wolf.
        A digital exhibition showing off the disastrous effects of human activity in Ethiopia.
        The technical requirement was to create a project that relies on implicit interactions.

        IMPORTANT:  This project can also be essayed without a Kinect Sensor. Simply tick the Simulation checkbox within BodyView game object.
                    Refer to SimulatorControl script for more details.

    Contributors
        
        Matthias Pawlitzek
        #2315565

        Max Bauer
        #2323129

    The copyright of the photographs used in this project go to:
        Kieran Dodds
        http://www.kierandodds.com

    The photographs and the description text were found on 2nd of January 2020 on worldphoto.com
        https://www.worldphoto.org/sony-world-photography-awards/winners-galleries/2019/professional/3rd-place-hierotopia-kieran-dodds
*/

using UnityEngine;
using System;
using TMPro;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using KinectGesture;
using UnityEngine.Rendering.PostProcessing;

public enum Ranges
{
    FRONT,
    LEFT,
    RIGHT,
    BACK,
    NONE
}

public class BodySourceView : MonoBehaviour
{
    const float PLAYER_MAX_X = 1.2f;
    const float PLAYER_MIN_X = -1.2f;
    const float PLAYER_MAX_Z = 4.0f;
    const float PLAYER_MIN_Z = 1.85f;
    const float PLAYER_MIN_Z_FOR_BLUR = 1.3f;

    // Blur begins 20 cm before reaching the final border.
    const float BLUR_AREA_M = 0.2f;
    const float MAX_APERTURE = 20f;
    const float MIN_APERTURE = 1f;

    const float BLUR_START_MIN_X = PLAYER_MIN_X + BLUR_AREA_M; // -1
    const float BLUR_START_MAX_X = PLAYER_MAX_X - BLUR_AREA_M; // 1
    const float BLUR_START_MIN_Z = PLAYER_MIN_Z_FOR_BLUR + BLUR_AREA_M; // 1.5
    const float BLUR_START_MAX_Z = PLAYER_MAX_Z - BLUR_AREA_M; // 3.8

    const float TOGGLE_LIGHT_Z = 3.8f;
    const float MAIN_CAMERA_MAX_Z = -40.0f;
    const float MAIN_CAMERA_MIN_Z = -4.0f;
    const int MOVE_LEFT = 1;
    const int MOVE_RIGHT = -1;

    public Material BoneMaterial;
    public GameObject BodySourceManager;
    public GameObject mainCamera;
    public GameObject gallery;
    public List<GameObject> picturesArray;
    public GameObject currentPicture;
    public GameObject simulator;
    public GameObject lightSwitchSounds;
    public Vector3 baseVector;
    public bool simulation = false;
    private bool multipleInside = false;

    DepthOfField DoF = null;
    Vignette vignette = null;
    public static SwipeGestureLeft swipeLeft = new SwipeGestureLeft();
    public static SwipeGestureRight swipeRight = new SwipeGestureRight();
    public static bool swiping = false;

    // Extern Components
    private Transform mainCameraTransform;
    private static SimulatorControl simulatorControl;
    private AudioSource[] sounds;
    public TextMeshProUGUI outOfRangeText;
    public TextMeshProUGUI multiplePersonText;

    private bool movingToNextPicture = false;

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    void Start()
    {
        mainCameraTransform = mainCamera.GetComponent<Transform>();
        simulatorControl = simulator.GetComponent<SimulatorControl>();
        sounds = lightSwitchSounds.GetComponents<AudioSource>();

        baseVector.Set(0, 0, 1);

        PostProcessVolume volume = mainCamera.GetComponent<PostProcessVolume>();
        volume.profile.TryGetSettings(out DoF);
        volume.profile.TryGetSettings(out vignette);

        if (simulation) {
            Debug.Log("RUNNING A SIMULATION. KINECT NOT ACTIVATED.");
        }
        swipeLeft.SwipeLeftRecognized += onSwipeLeft;
        swipeRight.SwipeRightRecognized += onSwipeRight;
    }

    static void onSwipeLeft(object sender, EventArgs e)
    {
        if (!swiping)
        {
            simulatorControl.setDirection(1);
        }
    }
    static void onSwipeRight(object sender, EventArgs e)
    {
        if (!swiping)
        {
            simulatorControl.setDirection(-1);
        }
    }

    void Update()
    {
        if (simulation) {
            HandleSimulation();
        }
        else
        {
            HandleKinect();
        }
    }

    private void HandleSimulation()
    {
        ApplyMainCameraMovement(simulator.GetComponent<Transform>().position.x, simulator.GetComponent<Transform>().position.z);
        ManageSwipeGestures();
    }

    private void HandleKinect()
    {
        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {

                trackedIds.Add(body.TrackingId);
            }
        }

        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked) {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                if (_Bodies.Count > 1)
                {
                    blurOnMultiplePersons();
                } else
                {
                    revertFaceColors();
                }

                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }
    }

    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            lr.SetVertexCount(2);
            lr.material = BoneMaterial;
            lr.SetWidth(0.05f, 0.05f);

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }
        return body;
    }

    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        Vector3 neckVector = new Vector3();
        Vector3 headVector = new Vector3();
        Vector3 neckToHead;

        swipeLeft.Update(body);
        swipeRight.Update(body);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;

            if (jt == Kinect.JointType.Neck)
            {
                neckVector.x = sourceJoint.Position.X;
                neckVector.y = sourceJoint.Position.Y;
                neckVector.z = sourceJoint.Position.Z;
            }

            if (jt == Kinect.JointType.Head)
            {
                headVector.x = sourceJoint.Position.X;
                headVector.y = sourceJoint.Position.Y;
                headVector.z = sourceJoint.Position.Z;
            }

            neckToHead = new Vector3(
                headVector.x - neckVector.x,
                headVector.y - neckVector.y,
                headVector.z - neckVector.z
                );

            if (jt == Kinect.JointType.Head) {
                // When a head joint is detected, apply main camera movement and rotation as well as potential blur effects.
                ApplyMainCameraMovement(sourceJoint.Position.X, sourceJoint.Position.Z);

                Ranges range = inBlurArea(sourceJoint);

                if (range != Ranges.NONE)
                {
                    startBlur(range, sourceJoint);
                } else
                {
                    outOfRangeText.faceColor = new Color32(255, 255, 255, 0);
                }

                // When the user is in a certain Z range, also apply main camera rotation.
                if (sourceJoint.Position.Z < PLAYER_MIN_Z && sourceJoint.Position.Z > BLUR_START_MIN_Z) {
                    ApplyMainCameraRotation(Vector3.Angle(baseVector, neckToHead));
                }
                else
                {
                    /*
                        When rotation was applied but the user steps backwards and exits the rotation area,
                        the main camera should smoothly rotate back to its original rotation value of (0,0,0,0).

                        We took the gallery game object as reference since RotateTowards() requires the rotation of a game object as target position.
                        It could have also been any other, non-rotated game object.
                    */
                    mainCameraTransform.rotation = Quaternion.RotateTowards(
                        mainCameraTransform.rotation,
                        gallery.GetComponent<Transform>().rotation,
                        15f * Time.deltaTime);
                }
            }
            ManageSwipeGestures();

            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyObject.transform.Find(jt.ToString());
            jointObj.localPosition = GetVector3FromJoint(sourceJoint);

            LineRenderer lr = jointObj.GetComponent<LineRenderer>();
            if (targetJoint.HasValue)
            {
                lr.SetPosition(0, jointObj.localPosition);
                lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                lr.SetColors(GetColorForState(sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
            }
            else
            {
                lr.enabled = false;
            }
        }
    }

    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.cyan;

        default:
            return Color.black;
        }
    }
    
    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        Vector3 x = new Vector3(joint.Position.X *10 , joint.Position.Y * 10, joint.Position.Z * 10 );
        return x;
    }

    /*
        The below functions were all specifically written for this project.
        Basically, the Kinect's data stream is evaluated and applied to make the project idea become reality.
    */

    // This function takes data from the Kinect as input and applies main camera movement.
    private void ApplyMainCameraMovement(float playerPosX, float playerPosZ)
    {
        if(movingToNextPicture)
        {
            return;
        }

        float cameraPosZ = MapNonLinear(playerPosZ);

        // Apply Main Camera Z Position
        if (playerPosZ >= PLAYER_MIN_Z && playerPosZ <= PLAYER_MAX_Z)
        {
            mainCameraTransform.position = new Vector3(
                mainCameraTransform.position.x,
                mainCameraTransform.position.y,
                cameraPosZ);
        }

        // Turn on the spot light attached to every picture when the user is closer than 3.8 meters to the Kinect.
        if (playerPosZ <= TOGGLE_LIGHT_Z)
        {
            if (currentPicture.GetComponent<Transform>().GetChild(2).gameObject.active == false){
                sounds[0].Play();
            }
            currentPicture.GetComponent<Transform>().GetChild(2).gameObject.SetActive(true);
        }
        // Or turn it off.
        else
        {
            if (currentPicture.GetComponent<Transform>().GetChild(2).gameObject.active == true) {
                {
                    sounds[1].Play();
                }
            }
            currentPicture.GetComponent<Transform>().GetChild(2).gameObject.SetActive(false);
        }  

        /*
            Calucalte the X shifting ratio factor. The further the user is away from the Kinect, the less the X shifting should take place and vice-versa.
            The scale factor was determined experimentally and is mapped on the dimensions of the frame the projector in our classroom was projecting to.
            The frame's width was roughly 2m. A smaller frame would require a smaller scale factor.
        */
        int scaleFactor = 22;
        float ratio = (1 - (playerPosZ / PLAYER_MAX_Z)) * scaleFactor;
      
        // Apply Main Camera X Position
        if (playerPosX >= PLAYER_MIN_X && playerPosX <= PLAYER_MAX_X)
        {
            mainCameraTransform.transform.position = new Vector3(
                playerPosX * ratio,
                mainCameraTransform.transform.position.y,
                mainCameraTransform.transform.position.z);
        }  
    }

    // Map the head pitch angle range from 80 to 90 degrees to the main camera's rotation angle when the user is in a certain range from the Kinect.
    private void ApplyMainCameraRotation(float neckAngle)
    {
        float angle = MapLinear(neckAngle, 80.0f, 90.0f, -20.0f, 5.0f);

        mainCameraTransform.rotation = Quaternion.RotateTowards(
                mainCameraTransform.rotation,
                Quaternion.AngleAxis(angle, new Vector3(1, 0, 0)),
                30 * Time.deltaTime
                );
    }

    // This function is the entry point for switching to another picture.
    private void ManageSwipeGestures()
    {
        int direction = simulatorControl.getDirection();
        if (!movingToNextPicture && direction != 0)
        {
            if (!EndOfGalleryReached(direction))
            {
                currentPicture.GetComponent<Transform>().GetChild(2).gameObject.SetActive(false);
                currentPicture = picturesArray[picturesArray.IndexOf(currentPicture) - direction];
                movingToNextPicture = true;
                simulatorControl.setDirection(0);
            }
        }
        if (movingToNextPicture)
        {
            MoveToNextPicture();
        }
    }

    // If a swipe gesture was detected, check if a swipe into a certain direction would be valid.
    private bool EndOfGalleryReached(int direction)
    {
        return (currentPicture == picturesArray[0] && direction == MOVE_LEFT)
            || (currentPicture == picturesArray[5] && direction == MOVE_RIGHT);
    }

    /*  
        If switching to another picture is possible, move the Gallery game object to its new position.
        We intentionally do not move the camera to a new position because of three reasons:
        1) Because of the fact that when running a simulation, the main camera's movement is based on the position of the simulator game object.
            So whenever we wanted to change the main camera's position directly, we would also had to change the simulator's position.
            However since the main camera should depend on the simulator, this would be bad software design.
        2) Since the user in reality does not physically move to another picture and instead keeps its position,
            we wanted to make the main camera do the same.
        3) The Gallery game object contains all visible game objects important to this project. Simply moving it is very easy.
    */  
    private void MoveToNextPicture()
    {
        gallery.GetComponent<Transform>().position = Vector3.MoveTowards(gallery.GetComponent<Transform>().position, -currentPicture.GetComponent<Transform>().localPosition, 15 * Time.deltaTime);
        if(gallery.GetComponent<Transform>().position == -currentPicture.GetComponent<Transform>().localPosition)
        {
            swiping = false;
            movingToNextPicture = false;
            simulatorControl.setDirection(0);
        }
    }

    private void blurOnMultiplePersons()
    {
        if (!multipleInside)
        {
            DoF.aperture.value = 1f;
            multiplePersonText.faceColor = new Color32(255, 255, 255, 255);
            multipleInside = true;
        }

    }

    private void revertFaceColors()
    {
        if (multipleInside)
        {
            DoF.aperture.value = 20f;
            multiplePersonText.faceColor = new Color32(255, 255, 255, 0);
            multipleInside = false;
        }
    }

    private void startBlur(Ranges range, Kinect.Joint sourceJoint)
    {
        float blurDistance = 0;
        switch (range)
        {
            case Ranges.FRONT:
                blurDistance = sourceJoint.Position.Z - PLAYER_MIN_Z_FOR_BLUR;
                break;
            case Ranges.BACK:
                blurDistance = PLAYER_MAX_Z - sourceJoint.Position.Z;
                break;
            case Ranges.LEFT:
                blurDistance = sourceJoint.Position.X - PLAYER_MIN_X;
                break;
            case Ranges.RIGHT:
                blurDistance = PLAYER_MAX_X - sourceJoint.Position.X;
                break;
        }

        if (blurDistance >= 0f)
        {
            var mappedValue = MapLinear(blurDistance, 0f, BLUR_AREA_M, MIN_APERTURE, MAX_APERTURE);
            DoF.aperture.value = mappedValue;
            var textAlpha = MapLinear(blurDistance, 0f, BLUR_AREA_M, 255f, 0f);
            var vignetteValue = MapLinear(blurDistance, 0f, BLUR_AREA_M, 0.5f, 0f);
            
            vignette.intensity.value = vignetteValue;
            outOfRangeText.faceColor = new Color32(255, 255, 255, Convert.ToByte(textAlpha));
        }        
    }

    private Ranges inBlurArea(Kinect.Joint joint)
    {
        float px = joint.Position.X;
        float pz = joint.Position.Z;

        if(px < BLUR_START_MIN_X)
        {
            return Ranges.LEFT;
        } else if(px > BLUR_START_MAX_X)
        {
            return Ranges.RIGHT;
        } else if(pz < BLUR_START_MIN_Z)
        {
            return Ranges.FRONT;
        } else if(pz > BLUR_START_MAX_Z)
        {
            return Ranges.BACK;
        }
        return Ranges.NONE;
    }

    /*
        This function is used once for determining the main camera's rotation and once for determining the blur value
    */
    private float MapLinear(float value, float sourceMin, float sourceMax, float destMin, float destMax)
    {
        return (value - sourceMin) / (sourceMax - sourceMin) * (destMax - destMin) + destMin;
    }

    /*
        This function is only used once for mapping the user Z position onto the main camera Z range from -40 to -4.
        We map it in a non-linear way to make examining the pictures from close range easier since movements in this range won't have a big impact on the zoom factor.
    */
    private float MapNonLinear(float x)
    {
        return (float)(1.93521f * Math.Pow(x, 5) - 28.0282f * Math.Pow(x, 4) + 153.417f * Math.Pow(x, 3) - 405.986f * Math.Pow(x, 2) + 521.014f * x - 263.394f);
    }
}