using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class Trajectory : Object
{
    public static float SPHERE_SIZE = 0.05F;
    public static float REPLAY_CUBE_SIZE = 0.05F;

    public Color color;
    public int trackedTime;
    private Transform _waypointPrefab;
    //public int _currCount;

    private int stepTime;
    public GameObject _replayCube;
    public List<GameObject> spheres;
    public float _entranceCount;

    private Camera _camera;

    private float _rotateSpeed = 50.0f;
    private float _translateSpeed = 1f;
    public float _zoomSpeed = .01f;

    public const int X_AXIS = 3000;
    public const int Y_AXIS = X_AXIS + 1;
    public const int Z_AXIS = Y_AXIS + 1;

    public Trajectory(Transform waypointPrefab, Color color, Camera camera, int stepTime = 5)
    {
        this.color = color;
        _waypointPrefab = waypointPrefab;
        trackedTime = 0;
        this.stepTime = stepTime;
        spheres = new List<GameObject>();
        _camera = camera;
        _replayCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _replayCube.SetActive(false); // hide it until we're in replay mode.
    }

    public void recordPosition(Vector3 currPosition)
    {
        //if (trackedTime % stepTime == 0)
        //{
         
        // use prefabs later
        //Transform t = Instantiate(_waypointPrefab, currPosition, Quaternion.identity);
        //GameObject sphere = t.gameObject;
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = currPosition;
        sphere.transform.localScale = new Vector3(SPHERE_SIZE, SPHERE_SIZE, SPHERE_SIZE);
        sphere.GetComponent<MeshRenderer>().material.color = color;
        spheres.Add(sphere);
        //}
        trackedTime++;
    }

    public void replay(float currCount)
    {
        // This trajectory hasn't come in yet
        if (currCount < _entranceCount )
        {
            _replayCube.SetActive(false);
            
        }
        // or has already finished
        else if ((currCount - _entranceCount) >= spheres.Count) {
            _replayCube.SetActive(false);
        }
        // the moment this trajectory comes in
        else if (currCount == _entranceCount)
        {
            //Debug.Log("_currCount: " + currCount + ", _entranceCount: " + _entranceCount);
            _replayCube.SetActive(true);
            foreach (GameObject sphere in spheres)
            {
                var color = sphere.GetComponent<Renderer>().material.color;
                //sphere.GetComponent<MeshRenderer>().material.color.a = 0.5f;
                color.a = 1f;
                sphere.GetComponent<Renderer>().material.color = color;
            }
        }

        // for now, don't change the color when you replay - can add this back in later.
        /*
        foreach (GameObject sphere in spheres)
        {
            var color = sphere.GetComponent<Renderer>().material.color;
            //sphere.GetComponent<Renderer>().material.color.a = 0.5f;
            var newColor = new Color(0, 0, color.b, 0.5f);
            //color.a = 0.5f;
            sphere.GetComponent<Renderer>().material.color = newColor;
        }
        */ 


        // currently showing
        if (currCount >= _entranceCount && (currCount - _entranceCount) < spheres.Count)
        {
            
            int index = (int)currCount - (int)_entranceCount;
            //Debug.Log("should be replaying now... index: " + index + ", currCount: " + currCount + ", _entranceCount: " + _entranceCount);
            _replayCube.transform.position = spheres[index].transform.position;
            _replayCube.transform.localScale = new Vector3(REPLAY_CUBE_SIZE, REPLAY_CUBE_SIZE, REPLAY_CUBE_SIZE);
            _replayCube.GetComponent<MeshRenderer>().material.color = color;
        }
    }

    public void translate(Vector3 pos, Vector3 prevPos)
    {
        float xDelta = pos.x - prevPos.x;
        float yDelta = pos.y - prevPos.y;
        float zDelta = pos.z - prevPos.z;
        foreach (GameObject sphere in spheres)
        {
            sphere.transform.Translate(xDelta *_translateSpeed, yDelta *_translateSpeed, zDelta *_translateSpeed );
        }
        /*
        Debug.Log("translate....touchPad.x: " + touchPad.x + ", .y: " + touchPad.y);
        
        foreach (GameObject sphere in spheres) {
            sphere.transform.Translate(touchPad.x * _translateSpeed, touchPad.y * _translateSpeed, 0);
        }
        */
    }

    public void rotate(int axis, Vector2 touchPad)
    {
        //transform.up = y
        //transform.right = ?
        // transform.forward = ?
        Vector3 transform = new Vector3(0,0,0);
        switch(axis)
        {
            case Y_AXIS:
                transform = _camera.transform.up;
                break;
            case X_AXIS:
                transform = _camera.transform.right;
                break;
            case Z_AXIS:
                transform = _camera.transform.forward;
                break;
        }

        int direction = 0;
        //right side of touchpad
        if (touchPad.x > 0.7f)
        {
            direction = 1;
        }
        // left side of touchpad
        if (touchPad.x < -0.7f)
        {
            direction = -1;
        }
        foreach (GameObject sphere in spheres)
        {
            sphere.transform.RotateAround(calculateCenter(), transform, direction * _rotateSpeed * Time.deltaTime);
        }
    }

    public void zoom(Vector2 touchPad)
    {
        int direction = 0;
        // top of touchpad
        if (touchPad.y > 0.7f)
        {
            // expand
            direction = 1;
        }
        // bottom of touchpad
        if (touchPad.y < -0.7f)
        {
            // contract
            direction = -1;
        }

        float sphere_scale;
        foreach (GameObject sphere in spheres)
        {
            sphere.transform.position += direction * (calculateCenter() - sphere.transform.position) * _zoomSpeed;
        }
    }

    public Vector3 calculateCenter()
    {
        Vector3 center = new Vector3(0.0f, 0.0f, 0.0f);
        float count = 0.0f;
        foreach (var sphere in spheres)
        {
            center += sphere.transform.position;
            count++;
        }
        return center / count;
    }

    

}