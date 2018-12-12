using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.XR.WSA.Input;
using System.Collections;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;
//using MathNet.Numerics;

public class MyNetworkManager : MonoBehaviour
{

    // SETTINGS TO SWITCH FROM DESKTOP TO HOLOLENS DEV
    private bool DESKTOP_DEV = false;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public static Vector3 CAMERA_POSITION;

    public string serverIP;
    public bool calibrated = true;
    public GameObject _anchor;
    public GameObject _menu;

    public Camera _camera;
    public ChoreographyManager _choreographyManager;
    public Transform waypointPrefab;

    NetworkClient myClient;

    const short NotConnected = 1000;
    const short Tracking = NotConnected + 1;
    const short Calibrating = Tracking + 1;
    const short Editing = Calibrating + 1;
    const short Connecting = Editing + 1;
    const short Gesture = Connecting + 1;
    const short Replay = Gesture + 1;
    const short Menu = Replay + 1;
    short currState = NotConnected;

    private Matrix4x4 _transformMatrix;

    private bool _currTriggerRight;
    private bool _currGripRight;
    private Vector3 _currPositionRight;
    private Vector2 _currTouchpadRight;
    private bool _currPadClickedRight;
    private bool _currPadTouchedRight;

    private bool _currTriggerLeft;
    private bool _currGripLeft;
    private Vector3 _currPositionLeft;
    private Vector2 _currTouchpadLeft;
    private bool _currPadClickedLeft;
    private bool _currPadTouchedLeft;

    private List<Vector3> _matX;
    private List<Vector3> _matY;

    public GameObject _controllerTrackerRight;
    public GameObject _controllerTrackerLeft;
    private Vector3 _defaultPos;
    private float _defaultRot;

    private GameObject controllerTarget = null;
    public GameObject _controllerTargetPrefab;

    private Vector3 _p0;
    private Vector3 _a1;
    private Vector3 _a2;
    private Vector3 _a3;
    private short _currStage = 0;

    private GameObject _textSample;
    private GameObject _textSample2;

    public float camera_speed = 1.0f;

    private void Awake()
    {
        Debug.Log("Awake should only get called once right?");
        //InteractionManager.InteractionSourceDetected += HandManager_InteractionSourceDetected;
        //InteractionManager.InteractionSourceUpdated += HandManager_InteractionSourceUpdated;
        if (DESKTOP_DEV)
        {
            SetupForDesktopDev();
        } else
        {
            SetupForHololensDev();
        }
        
    }

    public Transform getWaypointPrefab()
    {
        return waypointPrefab;
    }

    void SetupForDesktopDev()
    {
        Debug.Log("desktop dev mode");
        Trajectory.SPHERE_SIZE = 0.1f;
        Trajectory.REPLAY_CUBE_SIZE = 0.15f;
        _camera.transform.position = new Vector3(0, 1, -10);
        _controllerTrackerLeft.transform.localScale = new Vector3(Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE);
        _controllerTrackerRight.transform.localScale = new Vector3(Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE);
        serverIP = "127.0.0.1"; // localhost

    }

    void SetupForHololensDev()
    {
        Debug.Log("hololens dev mode ");
        Trajectory.SPHERE_SIZE = 0.05f;
        Trajectory.REPLAY_CUBE_SIZE = 0.07f;
        _camera.transform.position = new Vector3(0, 0, 0);
        _controllerTrackerLeft.transform.localScale = new Vector3(Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE);
        _controllerTrackerRight.transform.localScale = new Vector3(Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE, Trajectory.SPHERE_SIZE);
        Debug.Log("DID YOU REMEMBER TO SET THE IP ADDRESS?");
    }

    void Start()
    {
        if (_textSample == null) _textSample = GameObject.Find("TextSample");
        if (_textSample2 == null) _textSample2 = GameObject.Find("TextSample2");
        if (_menu == null) _menu = GameObject.Find("ObjectMenu");
        _menu.SetActive(false);

        foreach (Transform child in _menu.transform)
        {
            Color color = Color.gray;
            string text = "nothingburger";
            Debug.Log("menu obj name: " + child.name);
            if (child.name.Contains("RotateCube"))
            {
                text = "ROTATE";
                var success = ColorUtility.TryParseHtmlString(ChoreographyManager.ROTATE_COLOR, out color);
            } else if (child.name.Contains("ZoomCube"))
            {
                text = "ZOOM";
                ColorUtility.TryParseHtmlString(ChoreographyManager.ZOOM_COLOR, out color);
            } else if (child.name.Contains("TranslateCube"))
            {
                text = "TRANSLATE";
                ColorUtility.TryParseHtmlString(ChoreographyManager.TRANSLATE_COLOR, out color);
            }

            child.GetComponent<MeshRenderer>().material.color = color;

        }


        //if (_choreoInfo == null) _choreoInfo = GameObject.Find("ChoreoInfo");
        //GameObject temp = GameObject.Find("Text");
        //_choreoInfo = temp.GetComponent<Text>();
        //_choreoInfo.text = "different text";

        //scriptCube scriptcube = new scriptCube();
        //GameObject rotateCube = scriptCube.createCube("Rotate", 30);
    }

    
    void Update()
    {
      
        if (currState == NotConnected)
        {
            SetupClient();
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            Camera.main.transform.Translate(new Vector3(camera_speed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            Camera.main.transform.Translate(new Vector3(-camera_speed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            Camera.main.transform.Translate(new Vector3(0, -camera_speed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Camera.main.transform.Translate(new Vector3(0, camera_speed * Time.deltaTime, 0));
        }
        if (Input.GetMouseButton(0))
        {
            yaw += 2.0f * Input.GetAxis("Mouse X");
            pitch -= 2.0f * Input.GetAxis("Mouse Y");

            Camera.main.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

    }

    // Create a client and connect to the server port
    public void SetupClient()
    {
        currState = Connecting;
        Debug.Log("Connecting to the server...");
        myClient = new NetworkClient();
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        myClient.RegisterHandler(Tracking, OnTracking);
        myClient.RegisterHandler(Gesture, OnGesture);
        myClient.Connect(serverIP, 4444);
    }

    public void OnGesture(NetworkMessage netMsg)
    {
        Debug.Log("received a gesture message");
    }

    // Start the calibration when connected.
    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
        _choreographyManager = new ChoreographyManager(_camera, this);
        _choreographyManager.myMenu = _menu;
        _anchor.transform.position = new Vector3(0f, 0f, 0f);
        currState = Calibrating;
        //currState = Tracking;        
    }

    void handleCalibration()
    {
        if (_currGripRight)
        {

            if (_currStage == 0)
            {
                Debug.Log("stage 0 of calibration");
                _p0 = _currPositionRight;
                _anchor.transform.position = new Vector3(0.1f, 0f, 0f);
                _currStage += 1;
                return;
            }

            if (_currStage == 1)
            {
                Debug.Log("stage 1 of calibration");
                _a1 = _currPositionRight - _p0;
                _anchor.transform.position = new Vector3(0f, 0.1f, 0f);
                _currStage += 1;
                return;
            }
            
            if (_currStage == 2)
            {
                Debug.Log("stage 2 of calibration");
                _a2 = _currPositionRight - _p0;
                _anchor.transform.position = new Vector3(0f, 0f, 0.1f);
                _currStage += 1;
                return;
            }

            if (_currStage == 3)
            {
                Debug.Log("stage 3 of calibration");
                _a3 = _currPositionRight - _p0;
                GameObject dirIndicator = GameObject.Find("HeadsUpDirectionIndicator");
                Destroy(dirIndicator);
                Destroy(_anchor);
                currState = Editing;
                Debug.Log("now in editing mode...");
                return;
            }
        }
    }

    // When receives a new tracking message
    void OnTracking(NetworkMessage netMsg)
    {
        TrackingMessage trackMsg = netMsg.ReadMessage<TrackingMessage>();

        if (trackMsg.id == 1)
        {
            _currTriggerRight = trackMsg.trigger;
            //_currTriggerRight = trackMsg.triggerRight;
            _currPositionRight = trackMsg.position;
            _currGripRight = trackMsg.grip;
            _currTouchpadRight = trackMsg.touchPad;
            _currPadClickedRight = trackMsg.padClicked;
            _currPadTouchedRight = trackMsg.padTouched;
            //Debug.Log("Right, pos: " + _currPositionRight + ", trig: " + _currTriggerRight + ", pad: " + _currPadTouchedRight);
        }

        if (trackMsg.id == 2)
        {
            _currTriggerLeft = trackMsg.trigger;
            //_currHandleLeft = trackMsg.handleLeft;
            _currPositionLeft = trackMsg.position;
            _currGripLeft = trackMsg.grip;
            _currTouchpadLeft = trackMsg.touchPad;
            _currPadClickedLeft = trackMsg.padClicked;
            _currPadTouchedLeft = trackMsg.padTouched;
            //Debug.Log("Left, pos: " + _currPositionLeft + ", trig: " + _currTriggerLeft + ", pad: " + _currPadTouchedLeft);
        }


        if (currState == Calibrating)
        {
            //Debug.Log("Calibrating...");
            _controllerTrackerRight.transform.position = _currPositionRight;
            _controllerTrackerLeft.transform.position = _currPositionLeft;
            handleCalibration();
            // slightly sketchy since grip wasn't working quite right during calibration step - prob a threading problem and/or a network latency issue.
            _currGripLeft = false;
            _currGripRight = false;
        }

        if (currState == Tracking)
        {
            //Debug.Log("Tracking");
            // show current controller location.
            _controllerTrackerRight.transform.position = calibratedPos(_currPositionRight);
            _controllerTrackerLeft.transform.position = calibratedPos(_currPositionLeft);

            _choreographyManager.trackController(calibratedPos(_currPositionRight));
            if (_currGripRight) // stop tracking this trajectory.
            {
                _choreographyManager.finishTracking();
                currState = Editing;
                Debug.Log("stop tracking, enter editing mode...");
                return;
            }
        }
        if (currState == Editing)
        {
            // show current controller location.
            _controllerTrackerRight.transform.position = calibratedPos(_currPositionRight);
            _controllerTrackerLeft.transform.position = calibratedPos(_currPositionLeft);
            Color newControllerColor = _choreographyManager.handleEditing(_currTouchpadLeft, calibratedPos(_currPositionRight), _currPadTouchedRight);
            _controllerTrackerLeft.GetComponent<MeshRenderer>().material.color = newControllerColor;

            //switch back to tracking: 
            if (_currTriggerRight)
            {
                Debug.Log("go back to tracking from editing...");
                currState = Tracking;
                _choreographyManager.initializeTrajectory();
            }
            if (_currTriggerLeft)
            {
                currState = Replay;
                _choreographyManager.resetReplay();
                Debug.Log("Replay mode...");
            }
            if (_currGripLeft)
            {
                Debug.Log("Menu...");
                _choreographyManager._selectedTrajectories = new HashSet<int>();
                currState = Menu;
                _menu.SetActive(true);
                // set all menu block items to default color.
                GameObject[] menuBlocks = GameObject.FindGameObjectsWithTag("menuItem");
                foreach (GameObject menuBlock in menuBlocks)
                {
                    menuBlock.GetComponent<MeshRenderer>().material.color = Color.yellow;
                }
                // launch menu and use raycasting to see what you're looking at.
                _currGripLeft = false;
            }
        }

        if (currState == Menu)
        {
            //_menu.transform.LookAt(_camera.transform);
            _choreographyManager.handleMenuSelect(_currTriggerLeft);
            _currTriggerRight = false;

            //_choreographyManager.handleEditing(_currTriggerRight);
            if (_currGripLeft)
            {
                Debug.Log("close menu, back to editing mode");
                currState = Editing;
                _menu.SetActive(false);
                _currGripLeft = false;
            }

        }

        if (currState == Replay)
        {
            _choreographyManager.replayTrajectories();
            if (_currGripRight)
            {
                currState = Editing;
            }
        }
    }

    Vector3 calibratedPos(Vector3 pos)
    {
        float a1 = Vector3.Dot(pos - _p0, _a1) / (_a1.magnitude * _a1.magnitude);
        float a2 = Vector3.Dot(pos - _p0, _a2) / (_a2.magnitude * _a2.magnitude);
        float a3 = Vector3.Dot(pos - _p0, _a3) / (_a3.magnitude * _a3.magnitude);

        return new Vector3(a1*0.1f, a2*0.1f, a3*0.1f);
    }

    private void HandManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs args)
    {
        Debug.Log(args);
        uint id = args.state.source.id;

        Vector3 pos;
        if (args.state.sourcePose.TryGetPosition(out pos))
        {
            _anchor.transform.position = pos;
            _anchor.transform.rotation = Quaternion.identity;
        }
    }

    private void HandManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs args)
    {
        uint id = args.state.source.id;
        Vector3 pos;
        Quaternion rot;

        if (args.state.sourcePose.TryGetPosition(out pos))
        {
            _anchor.transform.position = pos;
            _matX.Add(pos);
            _matY.Add(_currPositionRight);
        }

        if (args.state.sourcePose.TryGetRotation(out rot))
        {
            _anchor.transform.rotation = rot;
        }
    }
}

public class TrackingMessage : MessageBase
{
    public int id;
    public bool trigger;
    public Vector3 position;
    public Vector2 touchPad;
    public bool grip;
    public bool padClicked;
    public bool padTouched;
}

//http://geek1337.blogspot.com/2017/04/unity3d-creating-cube-with-text-on-each.html
public class scriptCube
{
    static GameObject addSide(int size, string text)
    {
        //First we create a canvas - object to hold the UI:
        GameObject mainObj = new GameObject();
        Canvas canvasObj = mainObj.AddComponent<Canvas>();
        canvasObj.renderMode = RenderMode.WorldSpace;

        // Then we create a rawimage-object and we connect it to the parent object:
        GameObject childObj2 = new GameObject();
        RawImage rawimageObj = childObj2.AddComponent<RawImage>();
        rawimageObj.rectTransform.SetSizeWithCurrentAnchors
             (RectTransform.Axis.Horizontal, size);
        rawimageObj.rectTransform.SetSizeWithCurrentAnchors
             (RectTransform.Axis.Vertical, size);
        rawimageObj.color = Color.yellow;
        childObj2.transform.SetParent(mainObj.transform, false);

        //We also have to create the text - object and connect it to the parent object:
        GameObject childObj1 = new GameObject();
        Text textObj = childObj1.AddComponent<Text>();
        textObj.font = (Font)Resources.GetBuiltinResource
            (typeof(Font), "Arial.ttf"); ;
        textObj.text = text;
        textObj.alignment = TextAnchor.MiddleCenter;
        textObj.enabled = true;
        textObj.fontSize = 14;
        textObj.color = Color.black;
        textObj.rectTransform.SetSizeWithCurrentAnchors
            (RectTransform.Axis.Horizontal, size);
        textObj.rectTransform.SetSizeWithCurrentAnchors
            (RectTransform.Axis.Vertical, size);
        childObj1.transform.SetParent(mainObj.transform, false);

        return mainObj;
    }

    public static GameObject createCube(string name, int size)
    {

        GameObject mainObj = new GameObject();
        mainObj.name = name;

        GameObject side1 = addSide(size, name);
        side1.transform.SetParent(mainObj.transform);
        side1.transform.position = new Vector3(0, 0, -size / 2);
        side1.transform.rotation = Quaternion.Euler(0, 0, 0);

        GameObject side2 = addSide(size, name);
        side2.transform.SetParent(mainObj.transform);
        side2.transform.position = new Vector3(0, 0, size / 2);
        side2.transform.rotation = Quaternion.Euler(0, 180, 0);

        GameObject side3 = addSide(size, name);
        side3.transform.SetParent(mainObj.transform);
        side3.transform.position = new Vector3(0, -size / 2, 0);
        side3.transform.rotation = Quaternion.Euler(90, 0, 0);

        GameObject side4 = addSide(size, name);
        side4.transform.SetParent(mainObj.transform);
        side4.transform.position = new Vector3(0, size / 2, 0);
        side4.transform.rotation = Quaternion.Euler(90, 0, 0);

        GameObject side5 = addSide(size, name);
        side5.transform.SetParent(mainObj.transform);
        side5.transform.position = new Vector3(-size / 2, 0, 0);
        side5.transform.rotation = Quaternion.Euler(0, 90, 0);

        GameObject side6 = addSide(size, name);
        side6.transform.SetParent(mainObj.transform);
        side6.transform.position = new Vector3(size / 2, 0, 0);
        side6.transform.rotation = Quaternion.Euler(0, 270, 0);

        mainObj.transform.rotation = Quaternion.Euler(45, 45, 45);

        return mainObj;
    }
}