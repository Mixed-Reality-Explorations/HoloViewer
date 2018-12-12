using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoreographyManager : Object
{
    public GameObject myMenu;
    private Camera _camera;
    private MyNetworkManager _manager;

    private Trajectory _currTrajectory;
    private List<Trajectory> _trajectories;
    public HashSet<int> _selectedTrajectories = new HashSet<int>();

    private float _choreographyCount = 0f;
    private float _choreographyLength = 8f; // start with a default 8 count

    private Vector3 _prevControllerPos;
    private int stepTime = 5;

    private List<Color> colorChoices = new List<Color>()
    {
        Color.green,
        Color.magenta,
        Color.yellow,
        Color.blue,
        Color.cyan,
        Color.red,
    };

    private const short DEFAULT_MODE = 4000;
    private const short ZOOM_MODE = DEFAULT_MODE + 1;
    private const short ROTATE_MODE = ZOOM_MODE + 1;
    private const short TRANSLATE_MODE = ROTATE_MODE + 1;
    private short editingMode = DEFAULT_MODE;
    private short rotateMode = 0;

    // 0xRRGGBBAA
   // public static Color ZOOM_COLOR = new Color();"0xED00D8FF";
    public static string ZOOM_COLOR = "0xED00D8FF";
    public static string ROTATE_COLOR = "0x002CFFFF";
    public static string TRANSLATE_COLOR = "0xFFC300FF";
    private Color controllerColor = Color.gray;

    private GameObject currentFocus;

    public ChoreographyManager(Camera camera, MyNetworkManager manager)
    {
        _camera = camera;
        _manager = manager;
        _trajectories = new List<Trajectory>();
        initializeTrajectory();
    }

    private Color getColor()
    {
        return colorChoices[_trajectories.Count % colorChoices.Count];
    }

    public void initializeTrajectory()
    {
        Debug.Log("Starting a new trajectory... at _choreographyCount: " + _choreographyCount);
        _currTrajectory = new Trajectory(_manager.getWaypointPrefab(), getColor(), _camera);
        _currTrajectory._entranceCount = _choreographyCount;
    }

    public void trackController(Vector3 pos)
    {
        _currTrajectory.recordPosition(pos);
        _choreographyCount++;
    }

    public void finishTracking()
    {
        Debug.Log("finishTracking a new choreography, _choreoCount = " + _choreographyCount + ", _choreoLen: " + _choreographyLength + ", _currTraj.len: " + _currTrajectory.trackedTime);
        // Finish the current tracked trajectory
        if (_currTrajectory.trackedTime > 10)
        {
            // Store the current trajectory only if it is over 10 frames long
            _trajectories.Add(_currTrajectory);
            
            //
            if (_choreographyCount > _choreographyLength)
            {
                _choreographyLength = _choreographyCount;
                Debug.Log("new choreographyLength = " + _choreographyCount);
            }

            // Add a new trajectory selector to the menu
            GameObject newTrajButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newTrajButton.transform.parent = myMenu.transform;
            newTrajButton.transform.localPosition = new Vector3(-0.8f + 0.2f * _trajectories.Count, 0.5f, -1.2f);
            newTrajButton.transform.localScale = new Vector3(0.15f, 0.3f, 0.03f);
            newTrajButton.GetComponent<MeshRenderer>().material.color = _currTrajectory.color;
            newTrajButton.tag = "trajButton";
            newTrajButton.name = "traj" + _trajectories.Count;
        }
        Debug.Log("finished a trajectory");
    }

    private bool modeCurrentlyActive(string name)
    {
        // this menu item is the currently active one
        if (name.Contains("Rotate") && editingMode == ROTATE_MODE ||
            name.Contains("Zoom") && editingMode == ZOOM_MODE ||
            name.Contains("Translate") && editingMode == TRANSLATE_MODE ||
            name.Contains("X_AXIS") && rotateMode == Trajectory.X_AXIS ||
            name.Contains("Y_AXIS") && rotateMode == Trajectory.Y_AXIS ||
            name.Contains("Z_AXIS") && rotateMode == Trajectory.Z_AXIS)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void handleMenuSelect(bool trigger)
    {

        GameObject[] menuBlocks = GameObject.FindGameObjectsWithTag("menuItem");
        foreach (GameObject menuBlock in menuBlocks)
        {
            if (!modeCurrentlyActive(menuBlock.name)) { 
                menuBlock.GetComponent<MeshRenderer>().material.color = Color.blue;
            } else
            {
                menuBlock.GetComponent<MeshRenderer>().material.color = Color.yellow;
            }
        }

        GameObject[] trajBlocks = GameObject.FindGameObjectsWithTag("trajButton");
        foreach (GameObject trajBlock in trajBlocks)
        {
            int currTrajId = int.Parse(trajBlock.name.Substring(4, 1));
            if (_selectedTrajectories.Contains(currTrajId))
            {
                trajBlock.transform.localScale = new Vector3(0.2f, 0.2f, 0.03f);
                trajBlock.GetComponent<MeshRenderer>().material.color = Color.yellow;
            }
            else
            {
                trajBlock.transform.localScale = new Vector3(0.15f, 0.2f, 0.03f);
                trajBlock.GetComponent<MeshRenderer>().material.color = Color.yellow;
            }
        }

        // collect what you're gazing at when you hit the trigger.
        RaycastHit hitInfo;
        if (Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out hitInfo,
                20.0f,
                Physics.DefaultRaycastLayers))
        {
            //Debug.Log("hit an object? " + hitInfo.collider.gameObject.name);
            string gazingAt = hitInfo.collider.gameObject.name;
            // lots of error messages and really slow responses if you don't pop out
            // after gazing at the object menu.
            // can maybe also fix this by making it not gazeable somehow?
            if (gazingAt.Contains("ObjectMenu"))
            {
                return;
            }
            GameObject collidedObj = hitInfo.collider.gameObject;
            collidedObj.GetComponent<MeshRenderer>().material.color = Color.green;

            if (trigger)
            {
                collidedObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
                if (gazingAt.Contains("Rotate"))
                {
                    Debug.Log("rotate was clicked");
                    editingMode = ROTATE_MODE;
                }
                else if (gazingAt.Contains("Zoom"))
                {
                    Debug.Log("zoom was clicked");
                    editingMode = ZOOM_MODE;
                    rotateMode = 0;
                }
                else if (gazingAt.Contains("Translate"))
                {
                    Debug.Log("translate was clicked");
                    editingMode = TRANSLATE_MODE;
                    rotateMode = 0;
                }
                else if (gazingAt.Contains("traj"))
                {
                    int trajId = int.Parse(gazingAt.Substring(4, 1)) - 1;
                    _selectedTrajectories.Add(trajId);
                    Debug.Log("Trajectory " + trajId + " is selected, total selected: " + _selectedTrajectories.Count + ", selected: " + _selectedTrajectories.ToString());
                }
                else if (gazingAt.Contains("X_AXIS"))
                {
                    rotateMode = Trajectory.X_AXIS;
                }
                else if (gazingAt.Contains("Y_AXIS"))
                {
                    rotateMode = Trajectory.Y_AXIS;
                }
                else if (gazingAt.Contains("Z_AXIS"))
                {
                    rotateMode = Trajectory.Z_AXIS;
                }

            }
        }
    }

    public Color handleEditing(Vector2 touchpad, Vector3 pos, bool padTouched)
    {
        Color color = Color.gray;
        switch (editingMode)
        {
            case ZOOM_MODE:
                foreach (int trajInd in _selectedTrajectories)
                {
                    Debug.Log("zoom mode, trajInd: " + trajInd);
                    Trajectory traj = _trajectories[trajInd];
                    traj.zoom(touchpad);
                }
                ColorUtility.TryParseHtmlString(ZOOM_COLOR, out color);
                break;
            case ROTATE_MODE:
                foreach (int trajInd in _selectedTrajectories)
                {
                    Trajectory traj = _trajectories[trajInd];
                    traj.rotate(rotateMode, touchpad);
                }
                ColorUtility.TryParseHtmlString(ROTATE_COLOR, out color);
                break;
            case TRANSLATE_MODE:
                if (padTouched && (_prevControllerPos != null))
                {
                    Debug.Log("pos: " + pos);
                    Debug.Log("prev pos: " + _prevControllerPos);
                    foreach (int trajInd in _selectedTrajectories)
                    {
                        Trajectory traj = _trajectories[trajInd];
                        traj.translate(pos, _prevControllerPos);
                    }
                }
                //if (_choreographyCount % stepTime == 0)
                //{
                _prevControllerPos = pos;
                //}

                ColorUtility.TryParseHtmlString(TRANSLATE_COLOR, out color);
                break;
        }
        return color;
    }

    public void resetReplay()
    {
        // not one hundo sure how this should change
        Debug.Log("resetReplay..., _choreographyCount gets set to 0");
        _choreographyCount = 0;
    }

    public void replayTrajectories()
    {
        foreach (Trajectory trajectory in _trajectories)
        {
            trajectory.replay(_choreographyCount); 
        }
        _choreographyCount++;

        if (_choreographyCount > _choreographyLength)
        {
            _choreographyCount = 0;
        }
       
    }

    
}
