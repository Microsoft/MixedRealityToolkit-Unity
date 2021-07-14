using System.Collections.Generic;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Test DataController that logs the event to the console
    /// </summary>
    /// 
    public class DataControllerTest : DataControllerGOBase
    {
        /// <summary>
        /// Process the specified command with the specified bound datum and optional parameters.
        /// </summary>
        /// <param name="command">Which command to issue, as a string.</param>
        /// <param name="data">A data reference specifying the data to be acted upon, usually a unique identifier.</param>
        /// <param name="optionalParameters">Optional parameters, or null if none.</param>
        public override void ProcessCommand(string command, object data, Dictionary<string, object> optionalParameters)
        {
            Debug.Log("DataController received command '" + command + "' for object " + data.ToString());
        }
    }
}

