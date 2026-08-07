namespace DDMAutoGUI.Constants
{
    /// <summary>
    /// Contains standardized error codes and messages for the DDM Auto GUI application.
    /// </summary>
    public static class ErrorCodes
    {

        public static readonly ErrorInfo conCont =      new ErrorInfo("-5000", "Failed to connect to controller");
        public static readonly ErrorInfo conSettings =  new ErrorInfo("-5001", "Failed to verify settings on controller");
        public static readonly ErrorInfo conIOMaster =  new ErrorInfo("-5002", "Failed to connect to I/O-Link master");
        public static readonly ErrorInfo conIOPort =    new ErrorInfo("-5003", "Failed to connect to expected I/O-Link port(s)");
        public static readonly ErrorInfo conLaser =     new ErrorInfo("-5007", "Failed to connect to laser sensor");
        public static readonly ErrorInfo conCamTop =    new ErrorInfo("-5008", "Failed to connect to top camera");
        public static readonly ErrorInfo conCamSide =   new ErrorInfo("-5009", "Failed to connect to side camera");
        public static readonly ErrorInfo conHB =        new ErrorInfo("-5010", "Failed to parse heartbeat");
        public static readonly ErrorInfo conServer =    new ErrorInfo("-5011", "Failed to connect to results server");
        public static readonly ErrorInfo conDaq =       new ErrorInfo("-6100", "Could not connect to DAQ");
        public static readonly ErrorInfo conDaqChan =   new ErrorInfo("-6101", "Failed to add input channel");
        public static readonly ErrorInfo daqAcqFail =   new ErrorInfo("-6103", "Data acquisition failed");
        public static readonly ErrorInfo daqEmpty =     new ErrorInfo("-6104", "Data matrix empty");
        public static readonly ErrorInfo daqNoSignal =  new ErrorInfo("-6107", "No Hall signal detected");

        public class ErrorInfo
        {

            public string Code { get; }
            public string Message { get; }

            public ErrorInfo(string code, string message)
            {
                Code = code;
                Message = message;
            }
        }
    }
}
