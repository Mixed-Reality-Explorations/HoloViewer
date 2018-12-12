using UnityEngine;
using UnityEngine.Networking;

public class MyNetworkManager : MonoBehaviour
{
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public static Vector3 CAMERA_POSITION;

    public string serverIP;
    public bool calibrated = true;
    public GameObject _anchor;

    public Camera _camera;

    NetworkClient myClient;

    const short NotConnected = 1000;
    const short Initializing = NotConnected + 1;
    const short Calibrating = Initializing + 1;
    const short Tracking = Calibrating + 1;
    const short Connecting = Tracking + 1;
    short currState = NotConnected;

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

    public GameObject _controllerTrackerRight;
    public GameObject _controllerTrackerLeft;
    private Vector3 _defaultPos;
    private float _defaultRot;

    private Vector3 _p0;
    private Vector3 _a1;
    private Vector3 _a2;
    private Vector3 _a3;
    private short _currStage = 0;

    public float camera_speed = 1.0f;
    
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
        myClient.Connect(serverIP, 4444);
    }

    // Start the calibration when connected.
    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
        _anchor.transform.position = new Vector3(0f, 0f, 0f);
        currState = Calibrating;       
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
                currState = Tracking;
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
            _currPositionRight = trackMsg.position;
            _currGripRight = trackMsg.grip;
            _currTouchpadRight = trackMsg.touchPad;
            _currPadClickedRight = trackMsg.padClicked;
            _currPadTouchedRight = trackMsg.padTouched;
            Debug.Log("Right, pos: " + _currPositionRight + ", trig: " + _currTriggerRight + ", pad: " + _currPadTouchedRight);
        }

        if (trackMsg.id == 2)
        {
            _currTriggerLeft = trackMsg.trigger;
            _currPositionLeft = trackMsg.position;
            _currGripLeft = trackMsg.grip;
            _currTouchpadLeft = trackMsg.touchPad;
            _currPadClickedLeft = trackMsg.padClicked;
            _currPadTouchedLeft = trackMsg.padTouched;
            Debug.Log("Left, pos: " + _currPositionLeft + ", trig: " + _currTriggerLeft + ", pad: " + _currPadTouchedLeft);
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
            Debug.Log("Tracking");
            // show current controller location.
            _controllerTrackerRight.transform.position = calibratedPos(_currPositionRight);
            _controllerTrackerLeft.transform.position = calibratedPos(_currPositionLeft);
        }
    }

    Vector3 calibratedPos(Vector3 pos)
    {
        float a1 = Vector3.Dot(pos - _p0, _a1) / (_a1.magnitude * _a1.magnitude);
        float a2 = Vector3.Dot(pos - _p0, _a2) / (_a2.magnitude * _a2.magnitude);
        float a3 = Vector3.Dot(pos - _p0, _a3) / (_a3.magnitude * _a3.magnitude);

        return new Vector3(a1*0.1f, a2*0.1f, a3*0.1f);
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