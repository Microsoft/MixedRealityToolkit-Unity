// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{




    /// <summary>
    /// Simple test data source that programmatically changes variables in a data source. 
    /// </summary>
    /// 
    /// <remarks>
    /// Using a simple <key,value> store, it's possible to separate data from view
    /// to simplify the integration of generic view prefabs that are populated from
    /// external information.
    /// </remarks>

    public class DataSourceReflectionTest : MonoBehaviour, IDataSourceProvider
    {
        /// <summary>
        /// A class to contain data that is to be used as a data source. This is akin to a view model
        /// that will be used to populate a view.
        /// </summary>
        private class TestInfo
        {
            public string firstname;
            public string lastname;
            public string stylesheet;
            public string status;
            public int score;
        }

        private float _deltaSeconds;
        private int _nextOneSecondTarget;
        private int _nextFiveSecondTarget;
        private bool _styleYellow;
        private int _status = 0;
        private int _score = 100;

        private DataSourceReflection _dataSource;
        private TestInfo _dataSourceObject = new TestInfo();

        /// <summary>
        /// IDataSourceProvider method used to provide the correct
        /// datasource, which in this case is the DataSourceReflection instance with
        /// the specified _dataSourceObject attached to it to provide the actual
        /// data using reflection.
        /// </summary>


        public IDataSource GetDataSource()
        {
            
            if ( _dataSource == null )
            {
                _dataSource = new DataSourceReflection(_dataSourceObject);
            }

            return _dataSource;
        }

        private void Awake()
        {
            GetDataSource();
            InitializeData();
            _deltaSeconds = 0;
            _nextOneSecondTarget = 0;
        }

        // Update is called once per frame
        private void Update()
        {
            string[] statusText = { "open", "pending", "cancelled", "inprogress", "completed" };
            _deltaSeconds += Time.deltaTime;
            int tenthsOfSeconds = (int)(_deltaSeconds * 10.0);

            _dataSource.DataChangeSetBegin();

            if (tenthsOfSeconds > _nextFiveSecondTarget)
            {
                if (_styleYellow)
                {
                    _dataSourceObject.stylesheet = "standard";
                }
                else
                {
                    _dataSourceObject.stylesheet = "yellow";
                }
                _styleYellow = !_styleYellow;
                _nextFiveSecondTarget += 50;
            }

            if (tenthsOfSeconds > _nextOneSecondTarget)
            {
                _nextOneSecondTarget += 10;
                _dataSourceObject.score = ++_score;

                if ((++_status % statusText.Length) == 0)
                {
                    _status = 0;
                }

                // Two different ways to set values in the provided object
                _dataSource.SetValue("status", statusText[_status]);
                // _dataSourceObject.status = statusText[_status];
            }

            _dataSource.NotifyAllChanged();
            _dataSource.DataChangeSetEnd();

        }

        private void InitializeData()
        {
            _dataSource.DataChangeSetBegin();

            _dataSourceObject.firstname = "Michael";
            _dataSourceObject.lastname = "Hoffman";
            _dataSourceObject.score = 123;
            _dataSourceObject.status = "open";
            _dataSourceObject.stylesheet = "standard";

            _dataSource.NotifyAllChanged();
            _dataSource.DataChangeSetEnd();
        }
    }
}
