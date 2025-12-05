using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    public bool Open;
    public float Rotation_Y;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!Open)return;
        if(Rotation_Y < 90f)
        {
            Rotation_Y += Time.deltaTime * 45f;
            transform.rotation = Quaternion.Euler(this.gameObject.transform.rotation.x, Rotation_Y, this.gameObject.transform.rotation.z);
        } 
        Debug.Log(Rotation_Y);
    }
}
