using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    private ulong _trackingId = 0;

    public GameManager GestureManager;

    public Body[] GetData()
    {
        return _Data;
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.BodyFrameSource.OpenReader();

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void Update()
    {
        if (_Reader != null)
        {
            //Debug.Log("reader");
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                //Debug.Log("frame");
                if (_Data == null)
                {
                    //Debug.Log("data");
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }
                //line added
                _trackingId = 0;
                frame.GetAndRefreshBodyData(_Data);

                frame.Dispose();
                frame = null;
                //added
                foreach (var body in _Data)
                {
                    //Debug.Log("for loop");
                    if (body != null && body.IsTracked)
                    {
                        //Debug.Log("body");
                        _trackingId = body.TrackingId;
                        if (GestureManager != null)
                        {
                            //Debug.Log("bodysource");
                            GestureManager.SetTrackingId(body.TrackingId);
                        }
                        break;
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
