using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// An object that can provide a data source.
    /// </summary>
    /// <remarks>
    /// This is useful for situations where the lack of multiple inheritance makes
    /// it difficult to directly implement IDataSource in the object, but instead can
    /// lead to the correct data source that's managed externally.
    /// </remarks>
    
    public interface IDataSourceProvider
    {
        IDataSource GetDataSource();
    }
}
