#region licence/info

//////project name
//Firmata Plugin

//////description
//Plugin to use with Arduino with Firmata 2.0 OS


//////licence
//GNU Lesser General Public License (LGPL)
//english: http://www.gnu.org/licenses/lgpl.html
//german: http://www.gnu.de/lgpl-ger.html

//////language/ide
//C# sharpdevelop 

//////dependencies
//VVVV.PluginInterfaces.V1;
//VVVV.Utils.VColor;
//VVVV.Utils.VMath;

//////initial author
//wirmachenbunt C.Engler

#endregion licence/info

//use what you need
using System;
using System.Drawing;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using HeliosDac;

//the vvvv node namespace
namespace VVVV.Nodes
{

    //class definition
    public class PluginTemplateNode : IPlugin, IDisposable
    {
        #region field declaration

        //the host (mandatory)
        private IPluginHost FHost;
        // Track whether Dispose has been called.
        private bool FDisposed = false;

        //input pin declaration
        private IValueIn PointXYInput;
        private IColorIn PointColorInput;
        private IValueIn DrawFrame;
        private IValueIn ScanRateInput;
        private IValueIn DrawTest;
        private IValueIn EnablePlugin;

        //output pin declaration
        private IStringOut ExceptionsOut;
        private IValueOut NDevicesOut;


        #endregion field declaration

        #region constructor/destructor

        HeliosController heliosController;
        int numberOfDevices = 0;
        string excpStr = "";
        string excpInnerStr = "";


        public PluginTemplateNode()
        {
            //the nodes constructor
            //nothing to declare for this node

        }

        // Implementing IDisposable's Dispose method.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
            if (numberOfDevices != 0 && open) heliosController.CloseDevices(); 
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!FDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.

                    // Freeing connection
                    //heliosController.CloseDevices();

                }
                // Release unmanaged resources. If disposing is false,
                // only the following code is executed.

                if (FHost != null)
                    FHost.Log(TLogType.Debug, "PluginTemplateNode is being deleted");

                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.
            }
            FDisposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~PluginTemplateNode()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
        #endregion constructor/destructor

        #region node name and infos

        //provide node infos 
        private static IPluginInfo FPluginInfo;
        public static IPluginInfo PluginInfo
        {
            get
            {
                if (FPluginInfo == null)
                {
                    //fill out nodes info
                    //see: http://www.vvvv.org/tiki-index.php?page=Conventions.NodeAndPinNaming
                    FPluginInfo = new PluginInfo();

                    //the nodes main name: use CamelCaps and no spaces
                    FPluginInfo.Name = "HeliosLaserDac";
                    //the nodes category: try to use an existing one
                    FPluginInfo.Category = "Devices";
                    //the nodes version: optional. leave blank if not
                    //needed to distinguish two nodes of the same name and category
                    FPluginInfo.Version = "1.0.0";

                    //the nodes author: your sign
                    FPluginInfo.Author = "post4k";
                    //describe the nodes function
                    FPluginInfo.Help = "Connects to Helios Laser Dac and outputs frames";
                    //specify a comma separated list of tags that describe the node
                    FPluginInfo.Tags = "Laser";

                    //give credits to thirdparty code Lused
                    FPluginInfo.Credits = "Uses HeliosLaserDac library by Gitle Mikkelsen";
                    //any known problems?
                    FPluginInfo.Bugs = "Prolly a lot lmao";
                    //any known usage of the node that may cause troubles?
                    FPluginInfo.Warnings = "Remember lasers are dangerous. Stay safe.";

                    //leave below as is
                    System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                    System.Diagnostics.StackFrame sf = st.GetFrame(0);
                    System.Reflection.MethodBase method = sf.GetMethod();
                    FPluginInfo.Namespace = method.DeclaringType.Namespace;
                    FPluginInfo.Class = method.DeclaringType.Name;
                    //leave above as is
                }
                return FPluginInfo;
            }
        }

        public bool AutoEvaluate
        {
            //return true if this node needs to calculate every frame even if nobody asks for its output
            get { return true; }
        }

        #endregion node name and infos

        #region pin creation

        //this method is called by vvvv when the node is created
        public void SetPluginHost(IPluginHost Host)
        {
            //assign host
            FHost = Host;

            //create inputs
            string[] xynames = new string[] { "X,", "Y" };
            FHost.CreateValueInput("Point", 2, xynames, TSliceMode.Dynamic, TPinVisibility.True, out PointXYInput);
            PointXYInput.SetSubType(-1, 1, 0.01, 0, false, false, false);

            FHost.CreateColorInput("Point Color", TSliceMode.Dynamic, TPinVisibility.True, out PointColorInput);
            PointColorInput.SetSubType(new RGBAColor(0, 0, 0, 0), true);

            FHost.CreateValueInput("Scan Rate", 1, null, TSliceMode.Single, TPinVisibility.True, out ScanRateInput);
            ScanRateInput.SetSubType(1000, 65000, 100, 25000, false, false, true);

            FHost.CreateValueInput("Draw Frame", 1, null, TSliceMode.Single, TPinVisibility.True, out DrawFrame);
            DrawFrame.SetSubType(0, 1, 1, 0, true, false, false);

            FHost.CreateValueInput("Draw Test", 1, null, TSliceMode.Single, TPinVisibility.True, out DrawTest);
            DrawTest.SetSubType(0, 1, 1, 0, true, false, false);

            FHost.CreateValueInput("Enable", 1, null, TSliceMode.Single, TPinVisibility.True, out EnablePlugin);
            EnablePlugin.SetSubType(0, 1, 1, 0, false, true, false);



            //create outputs	    	
            FHost.CreateStringOutput("Errors", TSliceMode.Dynamic, TPinVisibility.True, out ExceptionsOut);
            ExceptionsOut.SetSubType("No errors",false);

            FHost.CreateValueOutput("Number of Devices", 1, null, TSliceMode.Dynamic, TPinVisibility.True, out NDevicesOut);
            NDevicesOut.SetSubType(0, double.MaxValue, 1, 0, false, false, true);

        }

        #endregion pin creation

        #region mainloop

        public void Configurate(IPluginConfig Input)
        {
            //nothing to configure in this plugin
            //only used in conjunction with inputs of type cmpdConfigurate
        }

        bool open = false;
        bool error = false;
        public void Evaluate(int SpreadMax)
        {
            // draw laser frame

            double drawFrame = 0;
            double testPattern = 0;
            double enable = 0;
            double scanRate = 25000;

            DrawFrame.GetValue(0, out drawFrame);
            DrawTest.GetValue(0, out testPattern);
            EnablePlugin.GetValue(0, out enable);
            ScanRateInput.GetValue(0, out scanRate);

            try
            {
                excpStr = "";
                excpInnerStr = "";

                if (enable == 1)
                {
                    if (!open)
                    {
                        open = true;
                        heliosController = new HeliosController();
                        numberOfDevices = heliosController.OpenDevices();
                    }

                    if (numberOfDevices<1)
                    {
                        throw new Exception("No HeliosLaserDac Device Found");
                    }

                    if (numberOfDevices==1)
                    {
                        /* draw pattern */
                        if (testPattern == 1)
                        {
                            DrawTestPattern();
                            testPattern = 0;
                        }

                        if (drawFrame == 1)
                        {
                            int pointCount = PointXYInput.SliceCount;
                            int colorCount = PointColorInput.SliceCount;

                            HeliosPoint[] points = new HeliosPoint[pointCount];'
                            
                            for (int i = 0; i<pointCount; i++)
                            {
                                // position from -4096 - 4096 to -1 - 1
                                double x = 0; 
                                double y = 0;
                                PointXYInput.GetValue2D(i, out x, out y);
                                ushort laserX = (ushort)((x * 0xFFF + 0xFFF) / 2);
                                ushort laserY = (ushort)((y * 0xFFF + 0xFFF) / 2);

                                // colors from 0 - 255 to 0 - 1
                                RGBAColor rgba = new RGBAColor(0, 0, 0, 0);
                                PointColorInput.GetColor(i, out rgba);
                                byte laserRed = (byte)(rgba.R * 0xFF);
                                byte laserGreen = (byte)(rgba.G * 0xFF);
                                byte laserBlue = (byte)(rgba.B * 0xFF);
                                byte laserIntensity = (byte)(rgba.A * 0xFF);

                                // assign
                                points[i].X = laserX;
                                points[i].Y = laserY;
                                points[i].Red = laserRed;
                                points[i].Green = laserGreen;
                                points[i].Blue = laserBlue;
                                points[i].Intensity = laserIntensity;
                            }

                            //draw frame
                            bool isReady = false;
                            for (int k = 0; k < 50; k++)
                            {
                                if (heliosController.GetStatus(0))
                                {
                                    isReady = true;
                                    break;
                                }
                            }
                            try
                            {
                                if (isReady)
                                    heliosController.WriteFrame(0, (ushort)scanRate, points);
                            }
                            catch
                            {
                                isReady = false;
                            }
                        }
                    }
                }
                if (enable == 0 && open)
                {
                    heliosController.CloseDevices();
                    numberOfDevices = 0;
                    open = false;
                }
            }
            catch (Exception e)
            {
                if (e.Message!=null) excpStr = e.Message;
                if (e.InnerException!=null) excpInnerStr = e.InnerException.Message;
                
                
                if (open)
                {
                    heliosController.CloseDevices();
                    numberOfDevices = 0;
                    open = false;
                }
            }

           
            NDevicesOut.SetValue(0, numberOfDevices);
            ExceptionsOut.SliceCount = 2;
            ExceptionsOut.SetString(0, excpStr);
            ExceptionsOut.SetString(1, excpInnerStr);
        }

        public void DrawTestPattern()
        {
            // make a square with different colours and a cross in the middle
        }

        #endregion mainloop  
    }
}