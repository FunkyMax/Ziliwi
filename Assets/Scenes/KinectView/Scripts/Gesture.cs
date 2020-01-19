using System;
using Kinect = Windows.Kinect;

namespace KinectGesture
{
    public enum GesturePartResult
    {
        Failed,
        Succeeded,
    }
    public interface IGestureSegment
    {
        GesturePartResult Update(Kinect.Body body);
    }

    public class SwipeSegmentLeft : IGestureSegment
    {
        public GesturePartResult Update(Kinect.Body body)
        {

            float handDistanceToSpineMid = Math.Abs(body.Joints[Kinect.JointType.SpineMid].Position.Y -
                                          body.Joints[Kinect.JointType.HandLeft].Position.Y);
            if(handDistanceToSpineMid < 0.07)
            {
                if (body.Joints[Kinect.JointType.HandLeft].Position.X <
                body.Joints[Kinect.JointType.ElbowLeft].Position.X)
                {
                    return GesturePartResult.Succeeded;
                }
            }
            return GesturePartResult.Failed;
        }
    }

    public class SwipeSegmentRight : IGestureSegment
    {
        public GesturePartResult Update(Kinect.Body body)
        {

            float handDistanceToSpineMid = Math.Abs(body.Joints[Kinect.JointType.SpineMid].Position.Y -
                                            body.Joints[Kinect.JointType.HandRight].Position.Y);

            if(handDistanceToSpineMid < 0.07)
            {
                if (body.Joints[Kinect.JointType.HandRight].Position.X >
                body.Joints[Kinect.JointType.ElbowRight].Position.X)
                {
                    return GesturePartResult.Succeeded;
                }
            }
            return GesturePartResult.Failed;
        }
    }

  
    public class SwipeGestureRight
    {
        readonly int windowSize = 10;
        IGestureSegment[] segments;

        int currentSegment = 0;
        int frameCount = 0;
        public event EventHandler SwipeRightRecognized;

        public SwipeGestureRight()
        {
            SwipeSegmentRight swipe = new SwipeSegmentRight();

            segments = new IGestureSegment[]
            {
                swipe,
                swipe,
                swipe,
                swipe,
            };
        }

        public void Update(Kinect.Body body)
        {
            GesturePartResult result = segments[currentSegment].Update(body);
            

            if (result == GesturePartResult.Succeeded)
            {
                if (currentSegment + 1 < segments.Length)
                {
                    currentSegment++;
                    frameCount = 0;
                }
                else
                {
                    if (SwipeRightRecognized != null)
                    {
                        SwipeRightRecognized(this, new EventArgs());
                        Reset();
                    }
                }
            } else if (result == GesturePartResult.Failed || frameCount == windowSize)
            {
                Reset();
            } else
            {
                frameCount++;
            }
        }

        public void Reset()
        {
            frameCount = 0;
            currentSegment = 0;
        }

    }

    public class SwipeGestureLeft
    {
        readonly int windowSize = 10;
        IGestureSegment[] segments;

        int currentSegment = 0;
        int frameCount = 0;
        public event EventHandler SwipeLeftRecognized;

        public SwipeGestureLeft()
        {
            SwipeSegmentLeft swipe = new SwipeSegmentLeft();

            segments = new IGestureSegment[]
            {
                swipe,
                swipe,
                swipe,
                swipe,
            };
        }

        public void Update(Kinect.Body body)
        {
            GesturePartResult result = segments[currentSegment].Update(body);


            if (result == GesturePartResult.Succeeded)
            {
                if (currentSegment + 1 < segments.Length)
                {
                    currentSegment++;
                    frameCount = 0;
                }
                else
                {
                    if (SwipeLeftRecognized != null)
                    {
                        SwipeLeftRecognized(this, new EventArgs());
                        Reset();
                    }
                }
            }
            else if (result == GesturePartResult.Failed || frameCount == windowSize)
            {
                Reset();
            }
            else
            {
                frameCount++;
            }
        }

        public void Reset()
        {
            frameCount = 0;
            currentSegment = 0;
        }

    }
   
}
