using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


public class OpenFaceNativePluginWrapper
{
    [DllImport("OpenFaceNativePlugin")]
    private static extern IntPtr InitializeFaceTracker(string modelLocation, string faceDetectorLocation, string faceAnalyzerRootLocation, bool quietMode);

    [DllImport("OpenFaceNativePlugin")]
    private static extern int UpdateFaceTracker(IntPtr pFaceTracker);

    [DllImport("OpenFaceNativePlugin")]
    private static extern int TerminateFaceTracker(IntPtr pFaceTracker);

    [DllImport("OpenFaceNativePlugin")]
    private static extern void GetFaceTrackingValues(IntPtr pFaceTracker, out FaceTrackingValues values, int sizeofStruct);

    public const int translationLength = 3;
    public const int rotationEulerLength = 3;
    public const int gazeDirectionLeftLength = 3;
    public const int gazeDirectionRightLength = 3;
    public const int AUsIntensityLength = 17;
    public const int AUsClassLength = 18;

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceTrackingValues
    {
        // -1 is perfect detection and 1 is worst detection
        public double detectionCertainty;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = translationLength)]
        public double[] translation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = rotationEulerLength)]
        public double[] rotationEuler;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = gazeDirectionLeftLength)]
        public float[] gazeDirectionLeft;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = gazeDirectionRightLength)]
        public float[] gazeDirectionRight;

        // intensity of AUs (0 is not present, 1 is present at minimum intensity, 5 is maximum intensity)
        // 1, 2, 4, 5, 6, 7, 9, 10, 12, 14, 15, 17, 20, 23, 25, 26, 45.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = AUsIntensityLength)]
        public double[] AUsIntensity;
        // Exist or not(0 or 1)
        // 1, 2, 4, 5, 6, 7, 9, 10, 12, 14, 15, 17, 20, 23, 25, 26, 28, 45.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = AUsClassLength)]
        public double[] AUsClass;
    }

    private IntPtr faceTracker;
    private FaceTrackingValues faceTrackingValues;
    private static object locker = new object();

    public OpenFaceNativePluginWrapper()
    {
        faceTrackingValues.translation = new double[translationLength];
        faceTrackingValues.rotationEuler = new double[rotationEulerLength];
        faceTrackingValues.gazeDirectionLeft = new float[gazeDirectionLeftLength];
        faceTrackingValues.gazeDirectionRight = new float[gazeDirectionRightLength];
        faceTrackingValues.AUsIntensity = new double[AUsIntensityLength];
        faceTrackingValues.AUsClass = new double[AUsClassLength];
    }

    static OpenFaceNativePluginWrapper()
    {
    }

    public void Initialize(string modelLocation = "", string faceDetectorLocation = "", string faceAnalyzerRootLocation = "", bool quietMode = false)
    {
        faceTracker = InitializeFaceTracker(modelLocation, faceDetectorLocation, faceAnalyzerRootLocation, quietMode);
        if(faceTracker == IntPtr.Zero)
        {
            throw new Exception("InitializeFaceTracker is Failed");
        }
    }

    public void Update()
    {
        int ret = UpdateFaceTracker(faceTracker);
        if(ret != 0)
        {
            Console.WriteLine("Update facial tracking is failed: {0}", ret);
            return;
        }

        lock(locker)
        {
            GetFaceTrackingValues(faceTracker, out faceTrackingValues, Marshal.SizeOf(typeof(FaceTrackingValues)));
        }
    }

    public void Terminate()
    {
        TerminateFaceTracker(faceTracker);
    }

    public FaceTrackingValues GetFaceTrackingValues()
    {
        // フィールドのデータはNativeで書き換えられるため、コピーした値を返す
        FaceTrackingValues ret;
        lock (locker)
        {
            ret = faceTrackingValues;
        }
        return ret;
    }
}
