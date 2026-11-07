using UnityEngine;

public class GyroCameraLook : MonoBehaviour
{
    private bool giroscopioListo = false;
    private Quaternion conversionBase;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            conversionBase = new Quaternion(0, 0, 1, 0);
            giroscopioListo = true;
        }
        else
        {
            Debug.LogWarning("El dispositivo no cuenta con giroscopio habilitado.");
        }
    }

    void Update()
    {
        if (!giroscopioListo) return;

        transform.localRotation = GyroToUnity(Input.gyro.attitude) * conversionBase;
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}
