/*
    This game object simulates the user's position in front of the Kinect almost entirely. The only thing that is missing is simulating the head rotation.
    To essay the project in simulation mode, one only has to tick the Simulation checkbox within BodyView game object in the Editor. This deactivates the Kinect.

    The Simulator game object with this script attached to it was created in order to make contributing to the project on macOS possible.
    Even though we managed to get the Kinect running in Unity on macOS, it was still a huge struggle to work with it in comparison to its Windows aquivalent.
*/

using UnityEngine;

public class SimulatorControl : MonoBehaviour
{

    private int direction = 0;

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.right * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.left * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = 1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            direction = -1;
        }
    }

    public int getDirection(){
        return direction;
    }

    public void setDirection(int direction){
        this.direction = direction;
    }
}
