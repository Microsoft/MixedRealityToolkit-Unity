using System.Collections.Generic;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Base GameObject monobehavior for dispatching commands related to the bound data.
    /// </summary>
    /// <remarks>
    /// When an interactable object is instantiated, particularly those in a data bound collection, there is generally a 
    /// need to invoke commands, such as those initiated by a user action, that must be tied to the specific data that is bound to 
    /// that instantiated object. In particular, some form of unique identifier must be provided that differentiates each item in a list of items.
    /// 
    /// This interface provides such as mechanism.
    /// </remarks>
    public abstract class DataControllerGOBase : MonoBehaviour, IDataController
    {
        /// <summary>
        /// Send the specified command with the specified bound datum and optional parameters.
        /// </summary>
        /// <param name="command">Which command to issue, as a string.</param>
        /// <param name="data">A data reference specifying the data to be acted upon, usually a unique identifier.</param>
        /// <param name="optionalParameters">Optional parameters, or null if none.</param>
        public abstract void ProcessCommand(string command, object data, Dictionary<string, object> optionalParameters);
    }
}
